using Microsoft.EntityFrameworkCore;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Persistence.Repositories;

/// <summary>
/// EF-Core-Implementierung des Testfall-Repositories inklusive
/// Such-, Eindeutigkeits- und Statistikabfragen.
/// </summary>
public class TestCaseRepository : RepositoryBase<TestCase>, ITestCaseRepository
{
    /// <summary>Erzeugt das Repository für den übergebenen Kontext.</summary>
    /// <param name="context">Gemeinsamer Kontext der Arbeitseinheit.</param>
    public TestCaseRepository(TestCaseManagerDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<TestCase?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Set
            .Include(t => t.Category)
            .Include(t => t.Parameters.OrderBy(p => p.SortOrder))
            .Include(t => t.ExpectedResults.OrderBy(r => r.SortOrder))
            .Include(t => t.Attachments)
            .Include(t => t.Tags)
            .Include(t => t.History.OrderByDescending(h => h.ChangedAt))
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<TestCaseListItem>> SearchAsync(TestCaseFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.Trim().ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(search)
                || (t.Description != null && t.Description.ToLower().Contains(search))
                || t.Version.ToLower().Contains(search));
        }

        if (filter.Status is not null)
        {
            query = query.Where(t => t.Status == filter.Status);
        }

        if (filter.CategoryId is not null)
        {
            query = query.Where(t => t.CategoryId == filter.CategoryId);
        }

        if (filter.TagId is not null)
        {
            query = query.Where(t => t.Tags.Any(tag => tag.Id == filter.TagId));
        }

        return await ProjectToListItems(query
                .OrderByDescending(t => t.ModifiedAt ?? t.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return Set.AnyAsync(t => t.Name.ToLower() == normalized
                                 && (excludeId == null || t.Id != excludeId),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => Set.CountAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAttachmentsAsync(CancellationToken cancellationToken = default)
        => Context.Attachments.CountAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Dictionary<TestCaseStatus, int>> GetStatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = await Set
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return groups.ToDictionary(g => g.Status, g => g.Count);
    }

    /// <inheritdoc />
    public async Task<List<CategoryCount>> GetCategoryCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = await Context.Categories
            .Select(c => new { c.Name, Count = c.TestCases.Count })
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .ToListAsync(cancellationToken);

        var result = counts.Select(c => new CategoryCount(c.Name, c.Count)).ToList();

        var withoutCategory = await Set.CountAsync(t => t.CategoryId == null, cancellationToken);
        if (withoutCategory > 0)
        {
            result.Add(new CategoryCount("(ohne Kategorie)", withoutCategory));
        }

        return result;
    }

    /// <inheritdoc />
    public Task<List<TestCaseListItem>> GetRecentlyChangedAsync(int count,
        CancellationToken cancellationToken = default)
    {
        return ProjectToListItems(Set.AsNoTracking()
                .OrderByDescending(t => t.ModifiedAt ?? t.CreatedAt)
                .Take(count))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Projiziert eine Testfall-Abfrage auf das schlanke Listen-DTO.
    /// Die Tag-Namen werden serverseitig als Liste geladen und erst im
    /// Speicher zu einer Zeichenkette verbunden (providerunabhängig).
    /// </summary>
    private static IQueryable<TestCaseListItem> ProjectToListItems(IQueryable<TestCase> query)
    {
        return query.Select(t => new TestCaseListItem
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CategoryName = t.Category != null ? t.Category.Name : null,
            Version = t.Version,
            Status = t.Status,
            CreatedBy = t.CreatedBy,
            LastChangedAt = t.ModifiedAt ?? t.CreatedAt,
            ParameterCount = t.Parameters.Count,
            ExpectedResultCount = t.ExpectedResults.Count,
            Tags = string.Join(", ", t.Tags.OrderBy(tag => tag.Name).Select(tag => tag.Name))
        });
    }
}
