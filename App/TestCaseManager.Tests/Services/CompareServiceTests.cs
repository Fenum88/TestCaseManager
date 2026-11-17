using ClosedXML.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.FileFormat;
using TestCaseManager.Infrastructure.Services;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für den Vergleichsdienst (<see cref="CompareService"/>): Abgleich
/// zweier Ausgabeordner, Differenz, deutsches CSV-Format, Vergleichsdateien in
/// gleicher Ordnerstruktur und die Excel-Mappe mit Zusammenfassung und Abgleich.
/// </summary>
public class CompareServiceTests : IDisposable
{
    private const string OutputA =
        "b_GeVo\n" +
        "    (0)_GeVo\n" +
        "        gevo=Vertragsabschluss\n" +
        "b_Gebaeude\n" +
        "    (0)_Gebaeude\n" +
        "        tuer=2\n" +
        "        baujahr=\"01.01.1965\"\n" +
        "        stockwerke=4\n" +
        "        verkaufswert=310000.00\n";

    private const string OutputB =
        "b_GeVo\n" +
        "    (0)_GeVo\n" +
        "        gevo=Vertragsabschluss\n" +
        "b_Gebaeude\n" +
        "    (0)_Gebaeude\n" +
        "        tuer=2\n" +
        "        baujahr=\"01.01.1965\"\n" +
        "        stockwerke=4\n" +
        "        verkaufswert=375000.00\n";

    private readonly string _root;

    public CompareServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "tcm-compare-" + Guid.NewGuid().ToString("N"));
        WriteOutput("A", OutputA);
        WriteOutput("B", OutputB);
    }

    private void WriteOutput(string system, string content)
    {
        var dir = Path.Combine(_root, system, "0001_TF");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Vertragsabschluss_0001_Vertragsbeginn.Ausgabe"), content);
    }

    private CompareService BuildService()
        => new(new IndentedKeyValueFileParser(), NullLogger<CompareService>.Instance);

    private CompareOutputsRequest BuildRequest() => new()
    {
        DirectoryA = Path.Combine(_root, "A"),
        DirectoryB = Path.Combine(_root, "B"),
        LabelA = "System A v1.0",
        LabelB = "System B v1.0",
        CompareDirectory = Path.Combine(_root, "Vergleich"),
        CsvPath = Path.Combine(_root, "Vergleich", "_Gesamtvergleich.csv"),
        ExcelPath = Path.Combine(_root, "Vergleich", "_Gesamtvergleich.xlsx"),
        TestCaseCount = 1,
        InfoA = new CompareRunInfo { Label = "System A v1.0", ExitCode = 0 },
        InfoB = new CompareRunInfo
        {
            Label = "System B v1.0",
            ExitCode = 0,
            Notes = [new CompareRunNote { TestCaseNumber = "0001", Severity = "Warnung", Message = "WARNUNG: Test" }]
        }
    };

    [Fact]
    public async Task Compare_FindetDenEinenUnterschiedUndZaehltAttribute()
    {
        var result = await BuildService().CompareOutputsAsync(BuildRequest());

        Assert.Equal(1, result.FilesCompared);
        // GeVo-Marker zählt nicht als Attribut: tuer, baujahr, stockwerke, verkaufswert.
        Assert.Equal(4, result.TotalAttributes);
        Assert.Equal(1, result.Differences);        // nur verkaufswert
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Compare_CsvIstDeutschMitGeVoUndDifferenz()
    {
        var result = await BuildService().CompareOutputsAsync(BuildRequest());
        var lines = File.ReadAllLines(result.CsvPath);

        Assert.Equal(
            "Testfall;GeVo;Datei;Entity;Schlüssel;Zeile;Attribut;Wert A;Wert B;Differenz (A−B);Status",
            lines[0]);

        var verkaufswert = lines.Single(l => l.Contains("verkaufswert"));
        Assert.StartsWith("0001;Vertragsabschluss;", verkaufswert);
        // Deutsche Zahlen (Komma) und die Differenz 310000 − 375000 = −65000.
        Assert.Contains("310000,00", verkaufswert);
        Assert.Contains("375000,00", verkaufswert);
        Assert.Contains("-65000,00", verkaufswert);
        Assert.EndsWith("unterschied", verkaufswert);

        // Der GeVo-Marker selbst ist keine Abgleichzeile.
        Assert.DoesNotContain(lines, l => l.Contains(";gevo;"));

        var tuer = lines.Single(l => l.Contains(";tuer;"));
        Assert.EndsWith("gleich", tuer);
    }

    [Fact]
    public async Task Compare_ErzeugtVergleichsdateiInGleicherOrdnerstruktur()
    {
        await BuildService().CompareOutputsAsync(BuildRequest());

        var comparePath = Path.Combine(_root, "Vergleich", "0001_TF",
            "Vertragsabschluss_0001_Vertragsbeginn.vergleich");
        Assert.True(File.Exists(comparePath));

        var content = File.ReadAllText(comparePath);
        Assert.Contains("Geschäftsvorfall: Vertragsabschluss", content);
        Assert.Contains("verkaufswert", content);
        Assert.Contains("UNTERSCHIED", content);
        Assert.Contains("-65000", content);   // Differenz in der lesbaren Datei
    }

    [Fact]
    public async Task Compare_ErzeugtExcelMitZweiBlaettern()
    {
        var result = await BuildService().CompareOutputsAsync(BuildRequest());

        Assert.True(File.Exists(result.ExcelPath));
        using var workbook = new XLWorkbook(result.ExcelPath);

        Assert.True(workbook.Worksheets.Contains("Zusammenfassung"));
        Assert.True(workbook.Worksheets.Contains("Abgleich"));

        // Zusammenfassung: gerechnete Testfälle und die Warnung von System B.
        var summary = workbook.Worksheet("Zusammenfassung");
        Assert.Equal("Gerechnete Testfälle", summary.Cell(4, 1).GetString());
        Assert.Equal(1, summary.Cell(4, 2).GetValue<int>());
        Assert.Contains("Warnung", summary.CellsUsed().Select(c => c.GetString()));

        // Abgleich: GeVo-Spalte und die numerische Differenz -65000 der verkaufswert-Zeile.
        var sheet = workbook.Worksheet("Abgleich");
        Assert.Equal("GeVo", sheet.Cell(1, 2).GetString());
        Assert.Equal("Schlüssel", sheet.Cell(1, 5).GetString());
        var vwRow = sheet.RowsUsed().First(r => r.Cell(7).GetString() == "verkaufswert");
        Assert.Equal("Vertragsabschluss", vwRow.Cell(2).GetString());
        Assert.Equal(310000m, vwRow.Cell(8).GetValue<decimal>());
        Assert.Equal(375000m, vwRow.Cell(9).GetValue<decimal>());
        Assert.Equal(-65000m, vwRow.Cell(10).GetValue<decimal>());
        Assert.Equal("unterschied", vwRow.Cell(11).GetString());
    }

    [Fact]
    public async Task Compare_MeldetNurEinseitigVorhandeneDatei()
    {
        var extraDir = Path.Combine(_root, "A", "0002_TF");
        Directory.CreateDirectory(extraDir);
        File.WriteAllText(Path.Combine(extraDir, "Vertragsabschluss_0001_Vertragsbeginn.Ausgabe"), OutputA);

        var result = await BuildService().CompareOutputsAsync(BuildRequest());

        Assert.Contains(result.Errors, e => e.Contains("Nur in A") && e.Contains("0002_TF"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
