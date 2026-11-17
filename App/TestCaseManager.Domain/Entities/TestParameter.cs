using TestCaseManager.Domain.Common;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Ein einzelner Eingabeparameter eines Testfalls.
/// Werte werden typneutral als Zeichenkette gespeichert und über
/// <see cref="DataType"/> interpretiert und validiert. Dadurch sind
/// beliebig viele Parameter unterschiedlichster Typen möglich, ohne
/// dass das Datenbankschema angepasst werden muss.
/// </summary>
public class TestParameter : EntityBase
{
    /// <summary>Fremdschlüssel zum zugehörigen Testfall.</summary>
    public Guid TestCaseId { get; set; }

    /// <summary>Zugehöriger Testfall (Navigationseigenschaft).</summary>
    public TestCase? TestCase { get; set; }

    /// <summary>Name des Parameters (Pflichtfeld, pro Testfall eindeutig).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Datentyp, gegen den der Wert validiert wird.</summary>
    public ParameterDataType DataType { get; set; } = ParameterDataType.Text;

    /// <summary>Konkreter Wert des Parameters (als Zeichenkette persistiert).</summary>
    public string? Value { get; set; }

    /// <summary>Standardwert, falls kein konkreter Wert gesetzt ist.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Beschreibung bzw. fachliche Bedeutung des Parameters.</summary>
    public string? Description { get; set; }

    /// <summary>Gibt an, ob der Parameter zwingend einen Wert benötigt.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Anzeigereihenfolge innerhalb des Testfalls.</summary>
    public int SortOrder { get; set; }
}
