using TestCaseManager.Domain.Common;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Zentrale Entität der Anwendung: ein fiktiver Testfall für das
/// Berechnungssystem. Ein Testfall besitzt beliebig viele Eingabeparameter,
/// erwartete Ergebnisse, Anhänge und Tags sowie eine vollständige
/// Änderungshistorie.
/// </summary>
public class TestCase : AuditableEntity
{
    /// <summary>Eindeutiger, sprechender Name des Testfalls (Pflichtfeld).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Fachliche Beschreibung des Testfalls.</summary>
    public string? Description { get; set; }

    /// <summary>Fachliche Version des Testfalls (z. B. "1.0" oder "2024-R2").</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Aktueller Lebenszyklus-Status.</summary>
    public TestCaseStatus Status { get; set; } = TestCaseStatus.Draft;

    /// <summary>Fremdschlüssel zur optionalen Kategorie.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Kategorie, der der Testfall zugeordnet ist.</summary>
    public Category? Category { get; set; }

    /// <summary>Eingabeparameter des Testfalls (unbegrenzte Anzahl).</summary>
    public List<TestParameter> Parameters { get; set; } = [];

    /// <summary>Erwartete Ergebnisse des Testfalls (unbegrenzte Anzahl).</summary>
    public List<ExpectedResult> ExpectedResults { get; set; } = [];

    /// <summary>Dateianhänge (Dokumente, Bilder, PDFs).</summary>
    public List<Attachment> Attachments { get; set; } = [];

    /// <summary>Frei vergebbare Schlagwörter (n:m-Beziehung).</summary>
    public List<Tag> Tags { get; set; } = [];

    /// <summary>Änderungshistorie mit alten und neuen Werten.</summary>
    public List<TestCaseHistory> History { get; set; } = [];
}
