using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Infrastructure.Services;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für den JSON-Speicher der Berechnungssysteme
/// (<see cref="JsonCalculationSystemStore"/>): Erstbelegung (Seed),
/// Speichern/Laden im Rundlauf und das Verhalten bei fehlender Datei.
/// </summary>
public class CalculationSystemStoreTests : IDisposable
{
    private readonly string _folder;
    private readonly string _file;

    public CalculationSystemStoreTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "tcm-store-" + Guid.NewGuid().ToString("N"));
        _file = Path.Combine(_folder, "berechnungssysteme.json");
    }

    private JsonCalculationSystemStore BuildStore(params CalculationSystem[] seed)
        => new(new CalculationSystemStoreOptions { FilePath = _file, Seed = seed },
            NullLogger<JsonCalculationSystemStore>.Instance);

    [Fact]
    public async Task GetAll_OhneDatei_UebernimmtSeedUndLegtDateiAn()
    {
        var seed = new CalculationSystem { Name = "Substanz", ScriptFile = "s.py", Version = "1.0" };
        var store = BuildStore(seed);

        var systems = await store.GetAllAsync();

        Assert.Single(systems);
        Assert.Equal("Substanz", systems[0].Name);
        Assert.True(File.Exists(_file));
    }

    [Fact]
    public async Task SaveDannGetAll_LiefertGespeicherteSystemeInhaltsgleich()
    {
        var store = BuildStore();
        var eingabe = new List<CalculationSystem>
        {
            new()
            {
                Name = "Ertragswert", Executable = "python3", ScriptFile = "e.py",
                Version = "2.1", ArgumentsTemplate = "{script} --in {input} --out {output}"
            }
        };

        await store.SaveAsync(eingabe);

        // Frischer Speicher auf dieselbe Datei (kein gemeinsamer Zustand).
        var geladen = await BuildStore().GetAllAsync();

        var s = Assert.Single(geladen);
        Assert.Equal("Ertragswert", s.Name);
        Assert.Equal("python3", s.Executable);
        Assert.Equal("e.py", s.ScriptFile);
        Assert.Equal("2.1", s.Version);
        Assert.Equal("{script} --in {input} --out {output}", s.ArgumentsTemplate);
    }

    [Fact]
    public async Task Save_UeberschreibtVorherigenBestandVollstaendig()
    {
        var store = BuildStore();
        await store.SaveAsync([new CalculationSystem { Name = "A", ScriptFile = "a.py" }]);
        await store.SaveAsync([new CalculationSystem { Name = "B", ScriptFile = "b.py" }]);

        var geladen = await BuildStore().GetAllAsync();

        Assert.Single(geladen);
        Assert.Equal("B", geladen[0].Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
