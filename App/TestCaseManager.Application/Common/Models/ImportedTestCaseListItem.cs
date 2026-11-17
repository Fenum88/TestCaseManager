namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Übersichtsdaten eines Datei-Testfalls für die Anzeige in der
/// Import-/Export-Ansicht.
/// </summary>
public class ImportedTestCaseListItem
{
    /// <summary>Primärschlüssel des Datei-Testfalls.</summary>
    public Guid Id { get; init; }

    /// <summary>Testfallnummer (z. B. "0001").</summary>
    public string Number { get; init; } = string.Empty;

    /// <summary>Anzahl der Eingabedateien.</summary>
    public int InputFileCount { get; init; }

    /// <summary>Anzahl der Ausgabedateien.</summary>
    public int OutputFileCount { get; init; }

    /// <summary>Anzahl der Entities (Tabellen) über alle Dateien.</summary>
    public int EntityCount { get; init; }

    /// <summary>Anzahl der Zeilen über alle Entities.</summary>
    public int RowCount { get; init; }

    /// <summary>Anzahl der Werte (Zellen) über alle Zeilen.</summary>
    public int ValueCount { get; init; }

    /// <summary>Benutzer, der zuletzt eingelesen hat.</summary>
    public string ImportedBy { get; init; } = string.Empty;

    /// <summary>Zeitpunkt des letzten Einlesens (bzw. der letzten Änderung).</summary>
    public DateTime ImportedAt { get; init; }
}
