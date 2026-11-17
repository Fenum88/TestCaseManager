using TestCaseManager.Domain.Common;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Fachliche Kategorie zur Gruppierung von Testfällen
/// (z. B. "Zinsberechnung", "Tarifierung", "Rundungsregeln").
/// </summary>
public class Category : AuditableEntity
{
    /// <summary>Eindeutiger Name der Kategorie (Pflichtfeld).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Beschreibung der Kategorie.</summary>
    public string? Description { get; set; }

    /// <summary>Alle Testfälle dieser Kategorie (Navigationseigenschaft).</summary>
    public List<TestCase> TestCases { get; set; } = [];
}
