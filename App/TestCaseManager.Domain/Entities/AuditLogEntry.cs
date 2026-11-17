using TestCaseManager.Domain.Common;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Übergreifender Audit-Log-Eintrag für alle Entitäten der Anwendung.
/// Während <see cref="TestCaseHistory"/> feldgenaue Änderungen eines
/// Testfalls dokumentiert, protokolliert das Audit-Log jede Aktion
/// (Anlegen, Ändern, Löschen) auf Datensatzebene – auch für Kategorien,
/// Tags und Anhänge. Die Einträge werden automatisch beim Speichern erzeugt.
/// </summary>
public class AuditLogEntry : EntityBase
{
    /// <summary>Zeitpunkt der Aktion (UTC).</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Benutzer, der die Aktion ausgeführt hat.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Art der Aktion (angelegt, geändert, gelöscht).</summary>
    public ChangeType Action { get; set; }

    /// <summary>Name des betroffenen Entitätstyps (z. B. "TestCase").</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Primärschlüssel des betroffenen Datensatzes.</summary>
    public Guid EntityId { get; set; }

    /// <summary>Lesbare Zusammenfassung der Aktion (z. B. Name des Datensatzes).</summary>
    public string? Details { get; set; }
}
