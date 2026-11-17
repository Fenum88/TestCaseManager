using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Filterkriterien für die Suche nach Testfällen.
/// Alle Kriterien sind optional und werden UND-verknüpft.
/// </summary>
public class TestCaseFilter
{
    /// <summary>
    /// Freitextsuche über Name, Beschreibung und Version
    /// (Groß-/Kleinschreibung wird ignoriert).
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>Nur Testfälle mit diesem Status.</summary>
    public TestCaseStatus? Status { get; set; }

    /// <summary>Nur Testfälle dieser Kategorie.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Nur Testfälle mit diesem Tag.</summary>
    public Guid? TagId { get; set; }
}
