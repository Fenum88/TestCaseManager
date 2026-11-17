using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.FileFormat;
using TestCaseManager.Domain.Enums;
using TestCaseManager.Infrastructure.Services;
using TestCaseManager.Tests.TestSupport;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Integrationstests für das dateibasierte Ein-/Auslesen
/// (<see cref="TestCaseFileService"/>) gegen eine EF-Core-InMemory-Datenbank
/// und ein temporäres Dateisystem. Prüft insbesondere den Round-Trip:
/// Dateien einlesen → löschen → aus der Datenbank wiederherstellen.
/// </summary>
public class TestCaseFileServiceTests : IDisposable
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
        "        nachname=Bongo\n" +
        "    (1)_Personen\n" +
        "        name=Sarah\n" +
        "        nachname=Bongo\n" +
        "        geschlecht=4\n";

    private const string OutputContent =
        "b_Ergebnis\n" +
        "    (0)_Ergebnis\n" +
        "        praemie=1234.56\n";

    private readonly string _root;

    /// <summary>Legt einen isolierten temporären Ordner mit je einer Eingabe- und Ausgabedatei an.</summary>
    public TestCaseFileServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "tcm-filetests-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path.Combine(_root, "Input", "0001_TF"));
        File.WriteAllText(
            Path.Combine(_root, "Input", "0001_TF", "0001_Vertragsbeginn.Eingabe"),
            InputContent);

        Directory.CreateDirectory(Path.Combine(_root, "Output", "0001_TF"));
        File.WriteAllText(
            Path.Combine(_root, "Output", "0001_TF", "0001_Vertragsbeginn.Ausgabe"),
            OutputContent);
    }

    /// <summary>
    /// Baut den Dienst. Standardmäßig werden – wie in der Produktion – die
    /// Ausgabewerte nicht gespeichert; über <paramref name="importOutput"/>
    /// lässt sich das für einzelne Tests umschalten.
    /// </summary>
    private static TestCaseFileService BuildService(InMemoryTestContext env, bool importOutput = false)
        => new(env.UnitOfWork, new IndentedKeyValueFileParser(), new IndentedKeyValueFileWriter(),
            new TestCaseFileOptions { ImportOutputSection = importOutput },
            NullLogger<TestCaseFileService>.Instance);

    [Fact]
    public async Task ImportFolder_SpeichertTestfallInDatenbank()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        var result = await service.ImportFolderAsync(_root);

        Assert.Equal(1, result.TestCaseCount);
        Assert.Equal(1, result.FileCount);
        Assert.Empty(result.Errors);

        await using var verify = env.CreateFreshContext();
        var stored = await verify.ImportedTestCases
            .Include(t => t.Files).ThenInclude(f => f.Entities).ThenInclude(e => e.Rows).ThenInclude(r => r.Values)
            .SingleAsync();

        Assert.Equal("0001", stored.Number);
        Assert.Single(stored.Files);
        Assert.Equal(2, stored.Files[0].Entities.Count);
        // Gebaeude: 1 Zeile mit 3 Werten; Personen: 2 Zeilen (2 + 3 Werte)
        var totalValues = stored.Files[0].Entities.SelectMany(e => e.Rows).SelectMany(r => r.Values).Count();
        Assert.Equal(8, totalValues);
    }

    [Fact]
    public async Task ImportFolder_Standard_SpeichertKeineAusgabewerte()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env); // Standard: ImportOutputSection = false

        var result = await service.ImportFolderAsync(_root);

        // Trotz vorhandener Ausgabedatei wird nur die Eingabedatei gespeichert.
        Assert.Equal(1, result.FileCount);

        await using var verify = env.CreateFreshContext();
        var stored = await verify.ImportedTestCases
            .Include(t => t.Files).SingleAsync();
        Assert.Single(stored.Files);
        Assert.Equal(TestCaseFileSection.Input, stored.Files[0].Section);
    }

    [Fact]
    public async Task ImportFolder_MitAktivierterAusgabe_SpeichertEingabeUndAusgabe()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env, importOutput: true);

        var result = await service.ImportFolderAsync(_root);

        Assert.Equal(2, result.FileCount);

        await using var verify = env.CreateFreshContext();
        var stored = await verify.ImportedTestCases
            .Include(t => t.Files).SingleAsync();
        Assert.Equal(2, stored.Files.Count);
        Assert.Contains(stored.Files, f => f.Section == TestCaseFileSection.Output);
    }

    [Fact]
    public async Task RoundTrip_GeloeschteEingabeWirdInhaltsgleichWiederhergestellt()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var inputFile = Path.Combine(_root, "Input", "0001_TF", "0001_Vertragsbeginn.Eingabe");

        // 1. Einlesen in die Datenbank.
        await service.ImportFolderAsync(_root);

        // 2. Eingabedatei löschen.
        File.Delete(inputFile);
        Assert.False(File.Exists(inputFile));

        // 3. Auslesen (Wiederherstellen aus der Datenbank).
        var export = await service.ExportFolderAsync(_root);
        Assert.Equal(1, export.FileCount);
        Assert.True(File.Exists(inputFile));

        // 4. Inhaltlicher Vergleich (zeilenweise, unabhängig von Zeilenenden).
        var restored = Normalize(File.ReadAllText(inputFile));
        var expected = Normalize(InputContent);
        Assert.Equal(expected, restored);
    }

    [Fact]
    public async Task ImportFolder_ZweiterDurchlaufErsetztStattDupliziert()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        await service.ImportFolderAsync(_root);
        await service.ImportFolderAsync(_root); // erneut – darf nicht duplizieren

        await using var verify = env.CreateFreshContext();
        Assert.Equal(1, await verify.ImportedTestCases.CountAsync());
    }

    [Fact]
    public async Task GetImportedDetail_LiefertAlleAttributeMitHerkunft()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        await service.ImportFolderAsync(_root);

        var summaries = await service.GetImportedAsync();
        var detail = await service.GetImportedDetailAsync(summaries.Single().Id);

        Assert.NotNull(detail);
        Assert.Equal("0001", detail.Number);
        // Alle 8 Werte der Eingabedatei müssen flach abgebildet sein.
        Assert.Equal(8, detail.Attributes.Count);

        // Stichprobe: baujahr stammt aus Gebaeude, Zeile 0, stand in
        // Anführungszeichen und wurde als Text erkannt.
        var baujahr = detail.Attributes.Single(a => a.ColumnName == "baujahr");
        Assert.Equal("Eingabe", baujahr.Section);
        Assert.Equal("0001_Vertragsbeginn", baujahr.FileName);
        Assert.Equal("Gebaeude", baujahr.EntityName);
        Assert.Equal(0, baujahr.RowIndex);
        Assert.Equal("01.01.1965", baujahr.Value);
        Assert.True(baujahr.IsQuoted);
        Assert.Equal("Text", baujahr.DataType);

        // Stichprobe: tuer ist unquotiert und wurde als Ganzzahl erkannt.
        var tuer = detail.Attributes.Single(a => a.ColumnName == "tuer");
        Assert.Equal("2", tuer.Value);
        Assert.False(tuer.IsQuoted);
        Assert.Equal("Ganzzahl", tuer.DataType);

        // Die Gruppierung der Oberfläche muss Abschnitt, Datei und Entity zeigen.
        Assert.Equal("Eingabe · 0001_Vertragsbeginn · Gebaeude", tuer.GroupHeader);

        // Personen besitzt zwei Zeilen mit unterschiedlichen Spalten.
        var personen = detail.Attributes.Where(a => a.EntityName == "Personen").ToList();
        Assert.Equal(5, personen.Count);
        Assert.Contains(personen, a => a.RowIndex == 1 && a.ColumnName == "geschlecht");
    }

    [Fact]
    public async Task GetImportedDetail_UnbekannteId_LiefertNull()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        Assert.Null(await service.GetImportedDetailAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteImported_EntferntTestfallUndAlleAbhaengigenDaten()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        await service.ImportFolderAsync(_root);

        var summaries = await service.GetImportedAsync();
        var deleted = await service.DeleteImportedAsync(summaries.Single().Id);

        Assert.True(deleted);

        // Es dürfen keine verwaisten Dateien, Entities, Zeilen oder Werte zurückbleiben.
        await using var verify = env.CreateFreshContext();
        Assert.Equal(0, await verify.ImportedTestCases.CountAsync());
        Assert.Equal(0, await verify.TestCaseFiles.CountAsync());
        Assert.Equal(0, await verify.DataEntities.CountAsync());
        Assert.Equal(0, await verify.DataRows.CountAsync());
        Assert.Equal(0, await verify.DataValues.CountAsync());
    }

    [Fact]
    public async Task DeleteImported_UnbekannteId_LiefertFalse()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        Assert.False(await service.DeleteImportedAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteImported_LaesstDateienImDateisystemUnberuehrt()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var inputFile = Path.Combine(_root, "Input", "0001_TF", "0001_Vertragsbeginn.Eingabe");
        await service.ImportFolderAsync(_root);

        var summaries = await service.GetImportedAsync();
        await service.DeleteImportedAsync(summaries.Single().Id);

        // Die Quelldatei muss erhalten bleiben, damit erneutes Einlesen möglich ist.
        Assert.True(File.Exists(inputFile));

        var reimport = await service.ImportFolderAsync(_root);
        Assert.Equal(1, reimport.TestCaseCount);
    }

    [Fact]
    public async Task DeleteAllImported_LeertDieDatenbank()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        // Zweiten Testfall anlegen, damit "alle" mehr als einen umfasst.
        Directory.CreateDirectory(Path.Combine(_root, "Input", "0002_TF"));
        File.WriteAllText(
            Path.Combine(_root, "Input", "0002_TF", "0001_Vertragsbeginn.Eingabe"),
            InputContent);
        await service.ImportFolderAsync(_root);

        var count = await service.DeleteAllImportedAsync();

        Assert.Equal(2, count);
        await using var verify = env.CreateFreshContext();
        Assert.Equal(0, await verify.ImportedTestCases.CountAsync());
        Assert.Equal(0, await verify.DataValues.CountAsync());
    }

    [Fact]
    public async Task DeleteAllImported_OhneDaten_LiefertNull()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        Assert.Equal(0, await service.DeleteAllImportedAsync());
    }

    /// <summary>Vergleicht Inhalte unabhängig von Zeilenenden und Leerzeilen am Rand.</summary>
    private static string Normalize(string content)
        => content.Replace("\r\n", "\n").TrimEnd('\n');

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
