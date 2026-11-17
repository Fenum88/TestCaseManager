using TestCaseManager.Domain.Common;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Ein erwartetes Ergebnis eines Testfalls, z. B. der Sollwert einer
/// Berechnung. Pro Testfall können beliebig viele erwartete Ergebnisse
/// hinterlegt werden, optional mit Toleranz für numerische Vergleiche.
/// </summary>
public class ExpectedResult : EntityBase
{
    /// <summary>Fremdschlüssel zum zugehörigen Testfall.</summary>
    public Guid TestCaseId { get; set; }

    /// <summary>Zugehöriger Testfall (Navigationseigenschaft).</summary>
    public TestCase? TestCase { get; set; }

    /// <summary>Name bzw. Bezeichner des Ergebnisses (Pflichtfeld).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Datentyp des erwarteten Wertes.</summary>
    public ParameterDataType DataType { get; set; } = ParameterDataType.Text;

    /// <summary>Erwarteter Sollwert (als Zeichenkette persistiert).</summary>
    public string ExpectedValue { get; set; } = string.Empty;

    /// <summary>Zulässige Toleranz bei numerischen Vergleichen (optional).</summary>
    public string? Tolerance { get; set; }

    /// <summary>Beschreibung des erwarteten Ergebnisses.</summary>
    public string? Description { get; set; }

    /// <summary>Anzeigereihenfolge innerhalb des Testfalls.</summary>
    public int SortOrder { get; set; }
}
