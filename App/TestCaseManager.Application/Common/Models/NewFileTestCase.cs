namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Beschreibt einen neu über die Oberfläche zusammengestellten Datei-Testfall,
/// bevor er in die Datenbank geschrieben wird. Der Aufbau folgt dem
/// generischen Meta-Modell: Testfall → Entities → Zeilen → Werte.
/// </summary>
public class NewFileTestCase
{
    /// <summary>Fachliche Testfallnummer (eindeutiger Schlüssel, z. B. "0003").</summary>
    public string Number { get; init; } = string.Empty;

    /// <summary>Bezeichnung der erzeugten Eingabedatei (z. B. "Eingabe").</summary>
    public string Designation { get; init; } = "Eingabe";

    /// <summary>Die enthaltenen Entities.</summary>
    public IReadOnlyList<NewFileEntity> Entities { get; init; } = [];
}

/// <summary>Eine Entity des neuen Datei-Testfalls mit ihren Spalten und Zeilen.</summary>
public class NewFileEntity
{
    /// <summary>Name der Entity (z. B. "Gebaeude").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Die gewählten Attribute (Spaltennamen) in Anzeigereihenfolge.</summary>
    public IReadOnlyList<string> Columns { get; init; } = [];

    /// <summary>Die Datenzeilen (jeweils Werte in der Reihenfolge von <see cref="Columns"/>).</summary>
    public IReadOnlyList<NewFileRow> Rows { get; init; } = [];
}

/// <summary>Eine Datenzeile: die Werte in derselben Reihenfolge wie die Spalten der Entity.</summary>
public class NewFileRow
{
    /// <summary>Die Werte der Zeile (Index-gleich zu den Spalten der Entity).</summary>
    public IReadOnlyList<string?> Values { get; init; } = [];
}
