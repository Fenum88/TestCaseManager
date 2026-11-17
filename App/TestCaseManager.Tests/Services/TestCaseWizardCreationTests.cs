using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.FileFormat;
using TestCaseManager.Domain.Enums;
using TestCaseManager.Infrastructure.Services;
using TestCaseManager.Tests.TestSupport;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für das Anlegen von Datei-Testfällen über den Assistenten:
/// Schablonen-Abfrage (Entities + Attribute aus der Datenbank) und das
/// Speichern eines zusammengestellten Testfalls.
/// </summary>
public class TestCaseWizardCreationTests : IDisposable
{
    private const string InputContent =
        "b_Gebaeude\n" +
        "    (0)_Gebaeude\n" +
        "        tuer=2\n" +
        "        baujahr=\"01.01.1965\"\n" +
        "        stockwerke=4\n" +
        "\n" +
        "b_Personen\n" +
        "    (0)_Personen\n" +
        "        name=Rainer\n" +
        "        nachname=Bongo\n";

    private readonly string _root;

    public TestCaseWizardCreationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "tcm-wizard-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Input", "0001_TF"));
        File.WriteAllText(
            Path.Combine(_root, "Input", "0001_TF", "0001_Vertragsbeginn.Eingabe"), InputContent);
    }

    private static TestCaseFileService BuildService(InMemoryTestContext env)
        => new(env.UnitOfWork, new IndentedKeyValueFileParser(), new IndentedKeyValueFileWriter(),
            new TestCaseFileOptions(), NullLogger<TestCaseFileService>.Instance);

    [Fact]
    public async Task GetEntityTemplates_LiefertEntitiesUndAttributeAusDatenbank()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        await service.ImportFolderAsync(_root);

        var templates = await service.GetEntityTemplatesAsync();

        var gebaeude = Assert.Single(templates, t => t.Name == "Gebaeude");
        Assert.Equal(new[] { "baujahr", "stockwerke", "tuer" }, gebaeude.Attributes.OrderBy(a => a));

        var personen = Assert.Single(templates, t => t.Name == "Personen");
        Assert.Contains("name", personen.Attributes);
        Assert.Contains("nachname", personen.Attributes);
    }

    [Fact]
    public async Task CreateFileTestCase_SpeichertGewaehlteEntitiesAttributeUndWerte()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        var draft = new NewFileTestCase
        {
            Number = "0009",
            Designation = "Eingabe",
            Entities =
            [
                new NewFileEntity
                {
                    Name = "Gebaeude",
                    Columns = ["tuer", "baujahr"],
                    Rows =
                    [
                        new NewFileRow { Values = ["3", "01.01.1990"] },
                        new NewFileRow { Values = ["5", ""] }   // leere Zelle wird ausgelassen
                    ]
                }
            ]
        };

        await service.CreateFileTestCaseAsync(draft);

        await using var verify = env.CreateFreshContext();
        var stored = await verify.ImportedTestCases
            .Include(t => t.Files).ThenInclude(f => f.Entities).ThenInclude(e => e.Rows).ThenInclude(r => r.Values)
            .SingleAsync();

        Assert.Equal("0009", stored.Number);
        var file = Assert.Single(stored.Files);
        Assert.Equal(TestCaseFileSection.Input, file.Section);
        var entity = Assert.Single(file.Entities);
        Assert.Equal("Gebaeude", entity.Name);
        Assert.Equal(2, entity.Rows.Count);

        // Zeile 0: beide Werte; tuer als Ganzzahl (unquoted), baujahr als Text (quoted).
        var row0 = entity.Rows.Single(r => r.RowIndex == 0);
        var tuer = row0.Values.Single(v => v.ColumnName == "tuer");
        Assert.Equal("3", tuer.Value);
        Assert.False(tuer.IsQuoted);
        Assert.Equal(ParameterDataType.Integer, tuer.DataType);

        var baujahr = row0.Values.Single(v => v.ColumnName == "baujahr");
        Assert.Equal("01.01.1990", baujahr.Value);
        Assert.True(baujahr.IsQuoted);
        Assert.Equal(ParameterDataType.Text, baujahr.DataType);

        // Zeile 1: nur tuer, baujahr war leer und wurde nicht gespeichert.
        var row1 = entity.Rows.Single(r => r.RowIndex == 1);
        Assert.Single(row1.Values);
        Assert.Equal("tuer", row1.Values[0].ColumnName);
    }

    [Fact]
    public async Task CreateFileTestCase_DoppelteNummer_WirftAusnahme()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        await service.ImportFolderAsync(_root); // legt "0001" an

        var draft = new NewFileTestCase
        {
            Number = "0001",
            Entities = [new NewFileEntity { Name = "X", Columns = ["a"], Rows = [new NewFileRow { Values = ["1"] }] }]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateFileTestCaseAsync(draft));
    }

    [Fact]
    public async Task CreateFileTestCase_KannWiederAusgelesenWerden()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        var draft = new NewFileTestCase
        {
            Number = "0042",
            Designation = "Eingabe",
            Entities =
            [
                new NewFileEntity
                {
                    Name = "Gebaeude",
                    Columns = ["tuer", "baujahr"],
                    Rows = [new NewFileRow { Values = ["2", "01.01.1965"] }]
                }
            ]
        };
        await service.CreateFileTestCaseAsync(draft);

        var export = await service.ExportFolderAsync(_root);
        Assert.Equal(1, export.TestCaseCount);

        var written = File.ReadAllText(
            Path.Combine(_root, "Input", "0042_TF", "0001_Eingabe.Eingabe"));
        Assert.Contains("tuer=2", written);
        Assert.Contains("baujahr=\"01.01.1965\"", written);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
