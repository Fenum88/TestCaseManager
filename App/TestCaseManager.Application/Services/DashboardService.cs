using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Ermittelt aggregierte Kennzahlen für das Dashboard über die
/// Statistikabfragen der Repositories.
/// </summary>
public class DashboardService : IDashboardService
{
    /// <summary>Anzahl der angezeigten "zuletzt geänderten" Testfälle.</summary>
    private const int RecentTestCaseCount = 5;

    /// <summary>Anzahl der angezeigten Audit-Log-Einträge.</summary>
    private const int RecentChangeCount = 15;

    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Erzeugt den Service mit der Arbeitseinheit.</summary>
    /// <param name="unitOfWork">Arbeitseinheit mit allen Repositories.</param>
    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<DashboardStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return new DashboardStatistics
        {
            TotalTestCases = await _unitOfWork.TestCases.CountAsync(cancellationToken),
            TotalCategories = await _unitOfWork.Categories.CountAsync(cancellationToken),
            TotalTags = await _unitOfWork.Tags.CountAsync(cancellationToken),
            TotalAttachments = await _unitOfWork.TestCases.CountAttachmentsAsync(cancellationToken),
            StatusCounts = await _unitOfWork.TestCases.GetStatusCountsAsync(cancellationToken),
            CategoryCounts = await _unitOfWork.TestCases.GetCategoryCountsAsync(cancellationToken),
            RecentTestCases = await _unitOfWork.TestCases.GetRecentlyChangedAsync(RecentTestCaseCount, cancellationToken),
            RecentChanges = await _unitOfWork.AuditLog.GetRecentAsync(RecentChangeCount, cancellationToken)
        };
    }
}
