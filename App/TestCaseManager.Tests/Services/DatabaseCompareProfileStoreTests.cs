using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Infrastructure.Services;
using TestCaseManager.Tests.TestSupport;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für den datenbankgestützten Speicher der Abgleichprofile
/// (<see cref="DatabaseCompareProfileStore"/>) und die einmalige Übernahme
/// der früher lokal gespeicherten Profile
/// (<see cref="CompareProfileTakeover"/>).
/// </summary>
public class DatabaseCompareProfileStoreTests : IDisposable
{
    private readonly InMemoryTestContext _env = new();
    private readonly ServiceProvider _provider;
    private readonly string _folder;

    /// <summary>
    /// Verdrahtet den Speicher mit der InMemory-Datenbank. Die Arbeitseinheit
    /// wird als Singleton registriert, damit alle Dienstbereiche denselben
    /// Kontext nutzen – im Test entspricht das einer gemeinsamen Datenbank.
    /// </summary>
    public DatabaseCompareProfileStoreTests()
    {
        _provider = new ServiceCollection()
            .AddSingleton(_env.UnitOfWork)
            .BuildServiceProvider();

        _folder = Path.Combine(Path.GetTempPath(), "tcm-dbprofile-" + Guid.NewGuid().ToString("N"));
    }

    private DatabaseCompareProfileStore BuildStore()
        => new(_provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DatabaseCompareProfileStore>.Instance);

    private static CompareProfile[] Beispiel() =>
    [
        new()
        {
            Name = "Provision nach Datum",
            Description = "Ordnet über das Datum zu.",
            KeyRules =
            [
                new CompareKeyRule { Entity = "Provision", KeyColumns = ["datum"] },
                new CompareKeyRule { Entity = "Raeume", KeyColumns = ["gebaeudeId", "raumnummer"] }
            ]
        },
        new() { Name = "Nur Position", Description = "Ohne Regeln." }
    ];

    [Fact]
    public async Task GetAll_OhneBestand_LiefertLeereListe()
    {
        Assert.Empty(await BuildStore().GetAllAsync());
    }

    [Fact]
    public async Task SaveDannGetAll_LiefertProfileInhaltsgleich()
    {
        await BuildStore().SaveAsync(Beispiel());

        var geladen = await BuildStore().GetAllAsync();

        Assert.Equal(2, geladen.Count);

        var nachDatum = geladen[0];
        Assert.Equal("Provision nach Datum", nachDatum.Name);
        Assert.Equal("Ordnet über das Datum zu.", nachDatum.Description);
        Assert.Equal(2, nachDatum.KeyRules.Count);
        Assert.Equal("Provision", nachDatum.KeyRules[0].Entity);
        Assert.Equal(["datum"], nachDatum.KeyRules[0].KeyColumns);
        Assert.Equal(["gebaeudeId", "raumnummer"], nachDatum.KeyRules[1].KeyColumns);

        Assert.Equal("Nur Position", geladen[1].Name);
        Assert.Empty(geladen[1].KeyRules);
    }

    [Fact]
    public async Task Save_BehaeltDieReihenfolgeDerProfileUndRegeln()
    {
        await BuildStore().SaveAsync(
        [
            new CompareProfile { Name = "Zebra" },
            new CompareProfile { Name = "Anton" }
        ]);

        var geladen = await BuildStore().GetAllAsync();

        // Nicht alphabetisch, sondern in der gespeicherten Reihenfolge.
        Assert.Equal("Zebra", geladen[0].Name);
        Assert.Equal("Anton", geladen[1].Name);
    }

    [Fact]
    public async Task Save_EntferntNichtMehrEnthalteneProfile()
    {
        var store = BuildStore();
        await store.SaveAsync(Beispiel());
        await store.SaveAsync([new CompareProfile { Name = "Nur Position" }]);

        var geladen = await BuildStore().GetAllAsync();

        var profil = Assert.Single(geladen);
        Assert.Equal("Nur Position", profil.Name);

        // Die Regeln des entfallenen Profils sind mitgelöscht.
        await using var verify = _env.CreateFreshContext();
        Assert.Empty(await verify.CompareProfileRules.ToListAsync());
    }

    /// <summary>
    /// Beim Speichern wird abgeglichen statt neu angelegt: Ein gleichnamiges
    /// Profil behält seinen Datensatz, damit die Auditfelder erhalten bleiben.
    /// </summary>
    [Fact]
    public async Task Save_AktualisiertGleichnamigesProfilOhneNeuanlage()
    {
        var store = BuildStore();
        await store.SaveAsync([new CompareProfile { Name = "Profil", Description = "alt" }]);

        Guid idVorher;
        await using (var vorher = _env.CreateFreshContext())
        {
            idVorher = (await vorher.CompareProfiles.SingleAsync()).Id;
        }

        await store.SaveAsync([new CompareProfile { Name = "Profil", Description = "neu" }]);

        await using var nachher = _env.CreateFreshContext();
        var gespeichert = await nachher.CompareProfiles.SingleAsync();

        Assert.Equal(idVorher, gespeichert.Id);
        Assert.Equal("neu", gespeichert.Description);
    }

    [Fact]
    public async Task Save_VerwirftWirkungsloseRegeln()
    {
        await BuildStore().SaveAsync(
        [
            new CompareProfile
            {
                Name = "Profil",
                KeyRules =
                [
                    new CompareKeyRule { Entity = "Provision", KeyColumns = ["datum"] },
                    new CompareKeyRule { Entity = "", KeyColumns = ["datum"] },        // ohne Entität
                    new CompareKeyRule { Entity = "Gebaeude", KeyColumns = [] },       // ohne Schlüssel
                    new CompareKeyRule { Entity = "Personen", KeyColumns = ["  "] }    // nur Leerzeichen
                ]
            }
        ]);

        var profil = Assert.Single(await BuildStore().GetAllAsync());
        var regel = Assert.Single(profil.KeyRules);
        Assert.Equal("Provision", regel.Entity);
    }

    [Fact]
    public async Task Save_EntferntLeerzeichenUmNamenUndAttribute()
    {
        await BuildStore().SaveAsync(
        [
            new CompareProfile
            {
                Name = "  Profil  ",
                Description = "  Text  ",
                KeyRules = [new CompareKeyRule { Entity = " Provision ", KeyColumns = [" datum ", " art "] }]
            }
        ]);

        var profil = Assert.Single(await BuildStore().GetAllAsync());
        Assert.Equal("Profil", profil.Name);
        Assert.Equal("Text", profil.Description);
        Assert.Equal("Provision", profil.KeyRules[0].Entity);
        Assert.Equal(["datum", "art"], profil.KeyRules[0].KeyColumns);
    }

    /// <summary>
    /// Beim Austausch der Regeln müssen die neuen Objekte als Neuanlage und
    /// die alten als Löschung geführt werden. Würden die neuen als Änderung
    /// gelten, liefe das Speichern gegen eine echte Datenbank auf einen
    /// Nebenläufigkeitsfehler (die zu ändernde Zeile gibt es nicht). Die
    /// InMemory-Datenbank verzeiht das – der Zustand wird deshalb direkt geprüft.
    /// </summary>
    [Fact]
    public async Task ReplaceRules_FuehrtNeueRegelnAlsNeuanlage()
    {
        await BuildStore().SaveAsync(
        [
            new CompareProfile
            {
                Name = "Profil",
                KeyRules = [new CompareKeyRule { Entity = "Provision", KeyColumns = ["datum"] }]
            }
        ]);

        var gespeichert = await _env.UnitOfWork.CompareProfiles.GetAllWithRulesAsync();
        var profil = Assert.Single(gespeichert);
        var alteRegelId = profil.Rules[0].Id;

        _env.UnitOfWork.CompareProfiles.ReplaceRules(profil,
        [
            new TestCaseManager.Domain.Entities.CompareProfileRule
            {
                Entity = "Raeume", KeyColumns = "gebaeudeId", SortOrder = 0
            }
        ]);

        var zustaende = _env.DbContext.ChangeTracker
            .Entries<TestCaseManager.Domain.Entities.CompareProfileRule>()
            .ToDictionary(e => e.Entity.Id, e => e.State);

        Assert.Equal(EntityState.Deleted, zustaende[alteRegelId]);
        Assert.Contains(zustaende, z => z.Key != alteRegelId && z.Value == EntityState.Added);
    }

    [Fact]
    public async Task Uebernahme_LiestAltdateiEinUndBenenntSieUm()
    {
        var datei = SchreibeAltdatei();
        var options = new CompareProfileStoreOptions { FilePath = datei };

        var uebernommen = await new CompareProfileTakeover(BuildStore(), options,
            NullLogger<CompareProfileTakeover>.Instance).RunAsync();

        Assert.Equal(2, uebernommen);

        var geladen = await BuildStore().GetAllAsync();
        Assert.Equal(2, geladen.Count);
        Assert.Equal("Provision nach Datum", geladen[0].Name);
        Assert.Equal(["datum"], geladen[0].KeyRules[0].KeyColumns);

        // Die Datei bleibt als Sicherung liegen, wird aber nicht mehr gefunden.
        Assert.False(File.Exists(datei));
        Assert.True(File.Exists(datei + CompareProfileTakeover.TakenOverSuffix));
    }

    [Fact]
    public async Task Uebernahme_MitBestandInDerDatenbank_TutNichts()
    {
        await BuildStore().SaveAsync([new CompareProfile { Name = "Gepflegt" }]);

        var datei = SchreibeAltdatei();
        var options = new CompareProfileStoreOptions { FilePath = datei };

        var uebernommen = await new CompareProfileTakeover(BuildStore(), options,
            NullLogger<CompareProfileTakeover>.Instance).RunAsync();

        Assert.Equal(0, uebernommen);

        // Der gepflegte Bestand bleibt unangetastet, die Datei liegen.
        var profil = Assert.Single(await BuildStore().GetAllAsync());
        Assert.Equal("Gepflegt", profil.Name);
        Assert.True(File.Exists(datei));
    }

    [Fact]
    public async Task Uebernahme_OhneAltdatei_TutNichts()
    {
        var options = new CompareProfileStoreOptions
        {
            FilePath = Path.Combine(_folder, "gibtesnicht.json")
        };

        Assert.Equal(0, await new CompareProfileTakeover(BuildStore(), options,
            NullLogger<CompareProfileTakeover>.Instance).RunAsync());
    }

    /// <summary>Legt eine Altdatei im Format des früheren JSON-Speichers an.</summary>
    private string SchreibeAltdatei()
    {
        Directory.CreateDirectory(_folder);
        var pfad = Path.Combine(_folder, "abgleich-profile.json");
        File.WriteAllText(pfad, JsonSerializer.Serialize(Beispiel()));
        return pfad;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _provider.Dispose();
        _env.Dispose();

        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
