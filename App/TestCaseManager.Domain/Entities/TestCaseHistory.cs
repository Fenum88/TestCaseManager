using TestCaseManager.Domain.Common;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Ein Eintrag der Änderungshistorie eines Testfalls.
/// Pro geänderter Eigenschaft wird ein eigener Eintrag mit altem und
/// neuem Wert erzeugt, sodass jede Änderung feldgenau nachvollziehbar ist.
/// Die Einträge werden automatisch beim Speichern im DbContext erzeugt.
/// </summary>
public class TestCaseHistory : EntityBase
{
    /// <summary>Fremdschlüssel zum betroffenen Testfall.</summary>
    public Guid TestCaseId { get; set; }

    /// <summary>Betroffener Testfall (Navigationseigenschaft).</summary>
    public TestCase? TestCase { get; set; }

    /// <summary>Art der Änderung (angelegt, geändert, gelöscht).</summary>
    public ChangeType ChangeType { get; set; }

    /// <summary>Benutzer, der die Änderung durchgeführt hat.</summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der Änderung (UTC).</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Betroffene Eigenschaft bzw. betroffenes Element,
    /// z. B. "Name" oder "Parameter 'Zinssatz'".
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>Wert vor der Änderung.</summary>
    public string? OldValue { get; set; }

    /// <summary>Wert nach der Änderung.</summary>
    public string? NewValue { get; set; }
}
