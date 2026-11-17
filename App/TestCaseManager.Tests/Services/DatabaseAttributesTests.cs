using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.FileFormat;
using TestCaseManager.Infrastructure.Services;
using TestCaseManager.Tests.TestSupport;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für die flache Attributabfrage der Datenbankansicht
/// (<see cref="TestCaseFileService.GetDatabaseAttributesAsync"/>).
/// </summary>
public class DatabaseAttributesTests
{
    private static TestCaseFileService BuildService(InMemoryTestContext env)
        => new(env.UnitOfWork, new IndentedKeyValueFileParser(), new IndentedKeyValueFileWriter(),
            new TestCaseFileOptions(), NullLogger<TestCaseFileService>.Instance);

    [Fact]
    public async Task GetDatabaseAttributes_LiefertAlleAttributeFlach()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        await service.CreateFileTestCaseAsync(new NewFileTestCase
        {
            Number = "0001",
            Entities =
            [
                new NewFileEntity
                {
                    Name = "Gebaeude",
                    Columns = ["tuer", "baujahr"],
                    Rows = [new NewFileRow { Values = ["2", "01.01.1965"] }]
                },
                new NewFileEntity
                {
                    Name = "Personen",
                    Columns = ["nachname"],
                    Rows = [new NewFileRow { Values = ["Bongo"] }]
                }
            ]
        });

        var rows = await service.GetDatabaseAttributesAsync();

        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.Equal("0001", r.Number));

        var tuer = rows.Single(r => r.Column == "tuer");
        Assert.Equal("Gebaeude", tuer.Entity);
        Assert.Equal("2", tuer.Value);

        Assert.Contains(rows, r => r.Entity == "Personen" && r.Column == "nachname" && r.Value == "Bongo");
    }
}
