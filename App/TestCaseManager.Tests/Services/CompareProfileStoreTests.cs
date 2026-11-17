using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Infrastructure.Services;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für den JSON-Speicher der Abgleichprofile
/// (<see cref="JsonCompareProfileStore"/>): Rundlauf mit mehreren Profilen,
/// leere Regelliste und Verhalten bei fehlender Datei.
/// </summary>
public class CompareProfileStoreTests : IDisposable
{
    private readonly string _folder;
    private readonly string _file;

    public CompareProfileStoreTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "tcm-profile-" + Guid.NewGuid().ToString("N"));
        _file = Path.Combine(_folder, "abgleich-profile.json");
    }

    private JsonCompareProfileStore BuildStore()
        => new(new CompareProfileStoreOptions { FilePath = _file },
            NullLogger<JsonCompareProfileStore>.Instance);

    [Fact]
    public async Task GetAll_OhneDatei_LiefertLeereListe()
    {
        Assert.Empty(await BuildStore().GetAllAsync());
    }

    [Fact]
    public async Task SaveDannGetAll_LiefertMehrereProfileInhaltsgleich()
    {
        await BuildStore().SaveAsync(
        [
            new CompareProfile
            {
                Name = "Provision nach Datum",
                Description = "Ordnet über das Datum zu.",
                KeyRules =
                [
                    new CompareKeyRule { Entity = "Provision", KeyColumns = ["datum"] },
                    new CompareKeyRule { Entity = "Raeume", KeyColumns = ["gebaeudeId", "raumnummer"] }
                ]
            },
            new CompareProfile { Name = "Nur Position", Description = "Ohne Regeln." }
        ]);

        var geladen = await BuildStore().GetAllAsync();

        Assert.Equal(2, geladen.Count);

        var nachDatum = geladen[0];
        Assert.Equal("Provision nach Datum", nachDatum.Name);
        Assert.Equal("Ordnet über das Datum zu.", nachDatum.Description);
        Assert.Equal(2, nachDatum.KeyRules.Count);
        Assert.Equal(["datum"], nachDatum.KeyRules[0].KeyColumns);
        Assert.Equal(["gebaeudeId", "raumnummer"], nachDatum.KeyRules[1].KeyColumns);

        // Profil ohne Regeln bleibt gültig (entspricht reinem Positionsvergleich).
        Assert.Equal("Nur Position", geladen[1].Name);
        Assert.Empty(geladen[1].KeyRules);
    }

    [Fact]
    public async Task Save_UeberschreibtVorherigenBestand()
    {
        var store = BuildStore();
        await store.SaveAsync([new CompareProfile { Name = "A" }]);
        await store.SaveAsync([new CompareProfile { Name = "B" }]);

        var geladen = await BuildStore().GetAllAsync();

        var profil = Assert.Single(geladen);
        Assert.Equal("B", profil.Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
