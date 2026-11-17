using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Repository für das übergreifende Audit-Log.
/// Einträge werden ausschließlich automatisch beim Speichern erzeugt;
/// das Repository dient nur dem Lesen.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>Liefert die jüngsten Audit-Log-Einträge (neueste zuerst).</summary>
    /// <param name="count">Maximale Anzahl der Ergebnisse.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<List<AuditLogEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
