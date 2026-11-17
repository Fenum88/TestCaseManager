namespace TestCaseManager.Domain.Enums;

/// <summary>
/// Unterstützte Datentypen für Eingabeparameter eines Testfalls.
/// Der eigentliche Wert wird typneutral als Zeichenkette gespeichert;
/// dieser Datentyp steuert Validierung und Interpretation.
/// So können neue Typen ohne Schemaänderung ergänzt werden.
/// </summary>
public enum ParameterDataType
{
    /// <summary>Freitext ohne Formatprüfung.</summary>
    Text = 0,

    /// <summary>Ganzzahl (long-Wertebereich).</summary>
    Integer = 1,

    /// <summary>Dezimalzahl (invariante Schreibweise mit Punkt oder Komma).</summary>
    Decimal = 2,

    /// <summary>Wahrheitswert (true/false bzw. ja/nein).</summary>
    Boolean = 3,

    /// <summary>Datum (ohne Uhrzeit).</summary>
    Date = 4,

    /// <summary>Datum mit Uhrzeit.</summary>
    DateTime = 5,

    /// <summary>Strukturierte Daten im JSON-Format.</summary>
    Json = 6
}
