namespace TestCaseManager.Domain.Common;

/// <summary>
/// Basisklasse für alle Entitäten des Domänenmodells.
/// Stellt einen global eindeutigen Primärschlüssel bereit, der bereits
/// clientseitig erzeugt wird. Dadurch sind Objektgraphen (z. B. beim
/// Kopieren von Testfällen) ohne Datenbank-Roundtrip konsistent verknüpfbar.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Eindeutiger Primärschlüssel der Entität.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
}
