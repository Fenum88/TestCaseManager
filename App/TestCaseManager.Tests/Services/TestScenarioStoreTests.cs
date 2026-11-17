using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Infrastructure.Services;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für den JSON-Speicher der Testszenarien
/// (<see cref="JsonTestScenarioStore"/>): Speichern/Laden im Rundlauf und
/// Verhalten bei fehlender Datei.
/// </summary>
public class TestScenarioStoreTests : IDisposable
{
    private readonly string _folder;
    private readonly string _file;

    public TestScenarioStoreTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "tcm-szenarien-" + Guid.NewGuid().ToString("N"));
        _file = Path.Combine(_folder, "szenarien.json");
    }

    private JsonTestScenarioStore BuildStore()
        => new(new TestScenarioStoreOptions { FilePath = _file },
            NullLogger<JsonTestScenarioStore>.Instance);

    [Fact]
    public async Task GetAll_OhneDatei_LiefertLeereListe()
    {
        Assert.Empty(await BuildStore().GetAllAsync());
    }

    [Fact]
    public async Task SaveDannGetAll_LiefertSzenarienInhaltsgleich()
    {
        var store = BuildStore();
        await store.SaveAsync(
        [
            new TestScenario
            {
                Name = "Altbauten",
                Description = "Alte Gebäude",
                TestCaseNumbers = ["0007", "0009", "0011"]
            }
        ]);

        var geladen = await BuildStore().GetAllAsync();

        var s = Assert.Single(geladen);
        Assert.Equal("Altbauten", s.Name);
        Assert.Equal("Alte Gebäude", s.Description);
        Assert.Equal(new[] { "0007", "0009", "0011" }, s.TestCaseNumbers);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
