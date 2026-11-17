namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Eine Warn- oder Fehlermeldung eines Berechnungslaufs, einem Testfall
/// zugeordnet (für das Zusammenfassungs-Tabellenblatt).
/// </summary>
public class CompareRunNote
{
    /// <summary>Betroffene Testfallnummer (oder "—", wenn nicht zuordenbar).</summary>
    public string TestCaseNumber { get; init; } = "—";

    /// <summary>Schweregrad ("Warnung" oder "Fehler").</summary>
    public string Severity { get; init; } = string.Empty;

    /// <summary>Meldungstext.</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>Ergebnisinformationen des Berechnungslaufs eines Systems.</summary>
public class CompareRunInfo
{
    /// <summary>Beschriftung des Systems (z. B. "Substanzwertverfahren v1.0").</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Rückgabecode des Prozesses.</summary>
    public int ExitCode { get; init; }

    /// <summary>Warn- und Fehlermeldungen des Laufs.</summary>
    public IReadOnlyList<CompareRunNote> Notes { get; init; } = [];
}

/// <summary>
/// Auftrag für den Vergleich zweier bereits berechneter Ausgabeordner.
/// Verglichen werden alle einander entsprechenden Ausgabedateien attributweise.
/// </summary>
public class CompareOutputsRequest
{
    /// <summary>Ausgabeordner des ersten Systems (Wurzel mit {Nummer}_TF-Unterordnern).</summary>
    public string DirectoryA { get; init; } = string.Empty;

    /// <summary>Ausgabeordner des zweiten Systems.</summary>
    public string DirectoryB { get; init; } = string.Empty;

    /// <summary>Beschriftung des ersten Systems (z. B. "Substanzwertverfahren v1.0").</summary>
    public string LabelA { get; init; } = "A";

    /// <summary>Beschriftung des zweiten Systems.</summary>
    public string LabelB { get; init; } = "B";

    /// <summary>Zielordner für die je Datei erzeugten Vergleichsdateien (gleiche Struktur).</summary>
    public string CompareDirectory { get; init; } = string.Empty;

    /// <summary>Pfad der Gesamt-CSV mit allen Abgleichen (deutsches Format).</summary>
    public string CsvPath { get; init; } = string.Empty;

    /// <summary>Pfad der Excel-Mappe mit Zusammenfassung und Abgleich.</summary>
    public string ExcelPath { get; init; } = string.Empty;

    /// <summary>Anzahl der gerechneten Testfälle (aus dem Eingabeordner).</summary>
    public int TestCaseCount { get; init; }

    /// <summary>Laufergebnis des ersten Systems (für die Zusammenfassung).</summary>
    public CompareRunInfo? InfoA { get; init; }

    /// <summary>Laufergebnis des zweiten Systems (für die Zusammenfassung).</summary>
    public CompareRunInfo? InfoB { get; init; }

    /// <summary>
    /// Schlüsselregeln je Entität. Für Entitäten mit Regel werden die Zeilen
    /// über die Schlüsselwerte einander zugeordnet, sonst über den Zeilenindex.
    /// </summary>
    public IReadOnlyList<CompareKeyRule> KeyRules { get; init; } = [];
}

/// <summary>Ergebnis eines Ausgabevergleichs.</summary>
public class CompareResult
{
    /// <summary>Anzahl der verglichenen Dateipaare.</summary>
    public int FilesCompared { get; init; }

    /// <summary>Gesamtzahl der verglichenen Attribute (Zellen).</summary>
    public int TotalAttributes { get; init; }

    /// <summary>Anzahl der Attribute mit Unterschied (inkl. nur einseitig vorhandener).</summary>
    public int Differences { get; init; }

    /// <summary>Pfad der erzeugten Gesamt-CSV.</summary>
    public string CsvPath { get; init; } = string.Empty;

    /// <summary>Pfad der erzeugten Excel-Mappe.</summary>
    public string ExcelPath { get; init; } = string.Empty;

    /// <summary>Menschlich lesbare Meldungen (je Datei eine Zeile).</summary>
    public List<string> Messages { get; init; } = [];

    /// <summary>Fehler-/Warnmeldungen (z. B. nur einseitig vorhandene Dateien).</summary>
    public List<string> Errors { get; init; } = [];

    /// <summary>Kurzfassung des Ergebnisses.</summary>
    public string Summary =>
        $"{FilesCompared} Datei(en) verglichen, {Differences} Unterschied(e) bei {TotalAttributes} Attributen.";
}
