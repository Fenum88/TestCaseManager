using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Serviceschicht für die Dashboard-Kennzahlen.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Ermittelt alle Kennzahlen für das Dashboard
    /// (Anzahlen, Statusverteilung, letzte Änderungen).
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<DashboardStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
