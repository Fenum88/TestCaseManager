namespace TestCaseManager.Domain.Common;

/// <summary>
/// Basisklasse für Entitäten, deren Erstellung und letzte Änderung
/// nachvollziehbar sein müssen. Die Felder werden zentral beim Speichern
/// (im DbContext) automatisch gefüllt und dürfen von der Anwendungslogik
/// nicht manuell gesetzt werden.
/// </summary>
public abstract class AuditableEntity : EntityBase
{
    /// <summary>Zeitpunkt der Erstellung (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Benutzer, der den Datensatz erstellt hat.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der letzten Änderung (UTC).</summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>Benutzer, der den Datensatz zuletzt geändert hat.</summary>
    public string? ModifiedBy { get; set; }
}
