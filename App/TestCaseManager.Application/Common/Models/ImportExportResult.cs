namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Ergebnis eines Einlese-Vorgangs (Dateien → Datenbank).
/// </summary>
public class ImportResult
{
    /// <summary>Anzahl der eingelesenen Testfälle.</summary>
    public int TestCaseCount { get; set; }

    /// <summary>Anzahl der eingelesenen Dateien.</summary>
    public int FileCount { get; set; }

    /// <summary>Detailmeldungen des Vorgangs (für die Anzeige im Log-Panel).</summary>
    public List<string> Messages { get; } = [];

    /// <summary>Fehler, die einzelne Dateien betrafen (der Rest wurde dennoch verarbeitet).</summary>
    public List<string> Errors { get; } = [];

    /// <summary>Menschlich lesbare Zusammenfassung des Vorgangs.</summary>
    public string Summary =>
        $"{TestCaseCount} Testfall/-fälle mit {FileCount} Datei(en) eingelesen"
        + (Errors.Count > 0 ? $", {Errors.Count} Fehler" : ".");
}

/// <summary>
/// Ergebnis eines Auslese-Vorgangs (Datenbank → Dateien).
/// </summary>
public class ExportResult
{
    /// <summary>Anzahl der ausgelesenen Testfälle.</summary>
    public int TestCaseCount { get; set; }

    /// <summary>Anzahl der geschriebenen Dateien.</summary>
    public int FileCount { get; set; }

    /// <summary>Detailmeldungen des Vorgangs (für die Anzeige im Log-Panel).</summary>
    public List<string> Messages { get; } = [];

    /// <summary>Fehler, die einzelne Dateien betrafen.</summary>
    public List<string> Errors { get; } = [];

    /// <summary>Menschlich lesbare Zusammenfassung des Vorgangs.</summary>
    public string Summary =>
        $"{TestCaseCount} Testfall/-fälle mit {FileCount} Datei(en) ausgelesen"
        + (Errors.Count > 0 ? $", {Errors.Count} Fehler" : ".");
}
