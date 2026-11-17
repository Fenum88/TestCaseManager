using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.FileFormat;
using TestCaseManager.Infrastructure.Services;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für die schlüsselbasierte Zeilenzuordnung im Abgleich: Ohne Regel
/// zählt die Position, mit Regel der Schlüsselwert. Der Praxisfall: dieselben
/// Provisionseinträge in umgekehrter Reihenfolge dürfen mit Schlüssel „datum"
/// keine Unterschiede ergeben.
/// </summary>
public class CompareKeyMatchingTests : IDisposable
{
    // System A: Jahre aufsteigend (2025, 2026, 2027).
    private const string OutputAufsteigend =
        "b_GeVo\n" +
        "    (0)_GeVo\n" +
        "        gevo=Provision\n" +
        "b_Provision\n" +
        "    (0)_Provision\n" +
        "        datum=\"01.01.2025\"\n" +
        "        provisionsbetrag=202500.00\n" +
        "    (1)_Provision\n" +
        "        datum=\"01.01.2026\"\n" +
        "        provisionsbetrag=202600.00\n" +
        "    (2)_Provision\n" +
        "        datum=\"01.01.2027\"\n" +
        "        provisionsbetrag=202700.00\n";

    // System B: identische Einträge, aber absteigend sortiert.
    private const string OutputAbsteigend =
        "b_GeVo\n" +
        "    (0)_GeVo\n" +
        "        gevo=Provision\n" +
        "b_Provision\n" +
        "    (0)_Provision\n" +
        "        datum=\"01.01.2027\"\n" +
        "        provisionsbetrag=202700.00\n" +
        "    (1)_Provision\n" +
        "        datum=\"01.01.2026\"\n" +
        "        provisionsbetrag=202600.00\n" +
        "    (2)_Provision\n" +
        "        datum=\"01.01.2025\"\n" +
        "        provisionsbetrag=202500.00\n";

    private readonly string _root;

    public CompareKeyMatchingTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "tcm-keys-" + Guid.NewGuid().ToString("N"));
        Write("A", OutputAufsteigend);
        Write("B", OutputAbsteigend);
    }

    private void Write(string system, string content)
    {
        var dir = Path.Combine(_root, system, "0001_TF");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Provision_0001_Vertragsbeginn.Ausgabe"), content);
    }

    private static CompareService BuildService()
        => new(new IndentedKeyValueFileParser(), NullLogger<CompareService>.Instance);

    private CompareOutputsRequest BuildRequest(params CompareKeyRule[] rules) => new()
    {
        DirectoryA = Path.Combine(_root, "A"),
        DirectoryB = Path.Combine(_root, "B"),
        LabelA = "System A",
        LabelB = "System B",
        CompareDirectory = Path.Combine(_root, "V"),
        CsvPath = Path.Combine(_root, "V", "_Gesamtvergleich.csv"),
        KeyRules = rules
    };

    [Fact]
    public async Task OhneRegel_ZaehltDiePosition_UndMeldetUnterschiede()
    {
        var result = await BuildService().CompareOutputsAsync(BuildRequest());

        // Zeile 0 (2025 vs 2027) und Zeile 2 (2027 vs 2025) weichen ab,
        // Zeile 1 (2026) trifft zufällig zusammen → 4 der 6 Attribute.
        Assert.Equal(6, result.TotalAttributes);
        Assert.Equal(4, result.Differences);
    }

    [Fact]
    public async Task MitSchluesselDatum_WerdenGleicheEintraegeZugeordnet()
    {
        var result = await BuildService().CompareOutputsAsync(BuildRequest(
            new CompareKeyRule { Entity = "Provision", KeyColumns = ["datum"] }));

        // Inhaltlich identisch – die Reihenfolge spielt keine Rolle mehr.
        Assert.Equal(6, result.TotalAttributes);
        Assert.Equal(0, result.Differences);
    }

    [Fact]
    public async Task Schluesselregel_GreiftUnabhaengigVonGrossKleinschreibung()
    {
        var result = await BuildService().CompareOutputsAsync(BuildRequest(
            new CompareKeyRule { Entity = "PROVISION", KeyColumns = ["Datum"] }));

        Assert.Equal(0, result.Differences);
    }

    [Fact]
    public async Task MitSchluessel_WirdDerSchluesselwertAusgewiesen()
    {
        var result = await BuildService().CompareOutputsAsync(BuildRequest(
            new CompareKeyRule { Entity = "Provision", KeyColumns = ["datum"] }));

        var lines = File.ReadAllLines(result.CsvPath);
        Assert.Equal(
            "Testfall;GeVo;Datei;Entity;Schlüssel;Zeile;Attribut;Wert A;Wert B;Differenz (A−B);Status",
            lines[0]);

        // Der Schlüsselwert steht in der Schlüsselspalte.
        Assert.Contains(lines, l => l.Contains(";Provision;01.01.2025;"));

        // Die lesbare Vergleichsdatei nennt den Schlüssel statt der Zeilennummer.
        var report = File.ReadAllText(Path.Combine(_root, "V", "0001_TF",
            "Provision_0001_Vertragsbeginn.vergleich"));
        Assert.Contains("[Provision (datum = 01.01.2025)]", report);
    }

    [Fact]
    public async Task EintragNurInEinemSystem_WirdAlsNurAGemeldet()
    {
        // System A bekommt einen zusätzlichen Eintrag 2028.
        var dir = Path.Combine(_root, "A", "0001_TF");
        File.WriteAllText(Path.Combine(dir, "Provision_0001_Vertragsbeginn.Ausgabe"),
            OutputAufsteigend +
            "    (3)_Provision\n" +
            "        datum=\"01.01.2028\"\n" +
            "        provisionsbetrag=202800.00\n");

        var result = await BuildService().CompareOutputsAsync(BuildRequest(
            new CompareKeyRule { Entity = "Provision", KeyColumns = ["datum"] }));

        // Die beiden Attribute des Zusatzeintrags gelten als „nur A".
        Assert.Equal(8, result.TotalAttributes);
        Assert.Equal(2, result.Differences);

        var lines = File.ReadAllLines(result.CsvPath);
        Assert.Contains(lines, l => l.Contains("01.01.2028") && l.EndsWith("nur A"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
