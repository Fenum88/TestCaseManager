using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Aggregierte Kennzahlen für das Dashboard.
/// </summary>
public class DashboardStatistics
{
    /// <summary>Gesamtanzahl aller Testfälle.</summary>
    public int TotalTestCases { get; init; }

    /// <summary>Anzahl der Kategorien.</summary>
    public int TotalCategories { get; init; }

    /// <summary>Anzahl der Tags.</summary>
    public int TotalTags { get; init; }

    /// <summary>Anzahl der Dateianhänge.</summary>
    public int TotalAttachments { get; init; }

    /// <summary>Anzahl Testfälle je Status.</summary>
    public IReadOnlyDictionary<TestCaseStatus, int> StatusCounts { get; init; }
        = new Dictionary<TestCaseStatus, int>();

    /// <summary>Anzahl Testfälle je Kategorie (Kategoriename → Anzahl).</summary>
    public IReadOnlyList<CategoryCount> CategoryCounts { get; init; } = [];

    /// <summary>Die zuletzt geänderten Testfälle.</summary>
    public IReadOnlyList<TestCaseListItem> RecentTestCases { get; init; } = [];

    /// <summary>Die jüngsten Einträge aus dem Audit-Log.</summary>
    public IReadOnlyList<AuditLogEntry> RecentChanges { get; init; } = [];
}

/// <summary>
/// Anzahl Testfälle einer Kategorie (für die Dashboard-Statistik).
/// </summary>
/// <param name="Name">Kategoriename.</param>
/// <param name="Count">Anzahl zugeordneter Testfälle.</param>
public record CategoryCount(string Name, int Count);
