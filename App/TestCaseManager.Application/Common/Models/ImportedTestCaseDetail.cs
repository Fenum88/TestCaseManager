namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Vollständige Detailansicht eines Datei-Testfalls: Kopfdaten und alle
/// gespeicherten Attribute in flacher, anzeigefertiger Form.
///
/// Die Attribute stammen aus dem generischen Meta-Modell
/// (Datei → Entity → Zeile → Wert) und werden hier bewusst flach
/// ausgegeben, damit die Oberfläche sie sortieren, filtern und gruppieren
/// kann, ohne den Objektgraphen selbst durchlaufen zu müssen.
/// </summary>
public class ImportedTestCaseDetail
{
    /// <summary>Primärschlüssel des Datei-Testfalls.</summary>
    public Guid Id { get; init; }

    /// <summary>Testfallnummer (fachlicher Schlüssel, z. B. "0001").</summary>
    public string Number { get; init; } = string.Empty;

    /// <summary>Benutzer, der zuletzt eingelesen hat.</summary>
    public string ImportedBy { get; init; } = string.Empty;

    /// <summary>Zeitpunkt des letzten Einlesens.</summary>
    public DateTime ImportedAt { get; init; }

    /// <summary>Alle Attribute des Testfalls über alle Dateien hinweg.</summary>
    public IReadOnlyList<DataAttributeItem> Attributes { get; init; } = [];
}

/// <summary>
/// Ein einzelnes Attribut (eine „Zelle") eines Datei-Testfalls, angereichert
/// um seine Herkunft (Abschnitt, Datei, Entity, Zeile), damit es außerhalb
/// des Objektgraphen eindeutig eingeordnet werden kann.
/// </summary>
public class DataAttributeItem
{
    /// <summary>
    /// Primärschlüssel des zugrunde liegenden Wertes. Über ihn wird eine
    /// Änderung eindeutig zugeordnet – der Pfad (Entity · Zeile · Spalte) wäre
    /// dafür ungeeignet, da Spaltennamen je Zeile abweichen dürfen.
    /// </summary>
    public Guid ValueId { get; init; }

    /// <summary>Abschnitt der Herkunftsdatei ("Eingabe" oder "Ausgabe").</summary>
    public string Section { get; init; } = string.Empty;

    /// <summary>Name der Herkunftsdatei ohne Endung (z. B. "0001_Vertragsbeginn").</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>Name der Entity/Tabelle (z. B. "Gebaeude").</summary>
    public string EntityName { get; init; } = string.Empty;

    /// <summary>Nullbasierter Zeilenindex innerhalb der Entity.</summary>
    public int RowIndex { get; init; }

    /// <summary>Spaltenname (z. B. "tuer", "baujahr").</summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>Logischer Wert ohne umschließende Anführungszeichen.</summary>
    public string? Value { get; init; }

    /// <summary>Deutscher Anzeigename des abgeleiteten Datentyps.</summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>Gibt an, ob der Wert in der Datei in Anführungszeichen stand.</summary>
    public bool IsQuoted { get; init; }

    /// <summary>
    /// Gruppenüberschrift für die Anzeige: fasst Abschnitt, Datei und Entity
    /// zu einer Zeile zusammen (z. B. "Eingabe · 0001_Vertragsbeginn · Gebaeude").
    /// </summary>
    public string GroupHeader => $"{Section} · {FileName} · {EntityName}";

    /// <summary>Anzeigetext für die Anführungszeichen-Kennzeichnung.</summary>
    public string QuotedDisplay => IsQuoted ? "\"…\"" : string.Empty;
}
