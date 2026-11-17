using Microsoft.EntityFrameworkCore;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Persistence.Repositories;

/// <summary>
/// EF-Core-Implementierung des Repositories für Datei-Testfälle.
/// </summary>
public class ImportedTestCaseRepository : RepositoryBase<ImportedTestCase>, IImportedTestCaseRepository
{
    /// <summary>Erzeugt das Repository für den übergebenen Kontext.</summary>
    /// <param name="context">Gemeinsamer Kontext der Arbeitseinheit.</param>
    public ImportedTestCaseRepository(TestCaseManagerDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<ImportedTestCase?> GetByNumberWithDetailsAsync(string number,
        CancellationToken cancellationToken = default)
    {
        var normalized = number.Trim();
        return DetailedQuery().FirstOrDefaultAsync(t => t.Number == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ImportedTestCase?> GetByIdWithDetailsAsync(Guid id,
        CancellationToken cancellationToken = default)
        => DetailedQuery().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<List<ImportedTestCase>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        => DetailedQuery().OrderBy(t => t.Number).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<ImportedTestCaseListItem>> GetSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        return await Set
            .OrderBy(t => t.Number)
            .Select(t => new ImportedTestCaseListItem
            {
                Id = t.Id,
                Number = t.Number,
                InputFileCount = t.Files.Count(f => f.Section == TestCaseFileSection.Input),
                OutputFileCount = t.Files.Count(f => f.Section == TestCaseFileSection.Output),
                EntityCount = t.Files.SelectMany(f => f.Entities).Count(),
                RowCount = t.Files.SelectMany(f => f.Entities).SelectMany(e => e.Rows).Count(),
                ValueCount = t.Files.SelectMany(f => f.Entities)
                    .SelectMany(e => e.Rows).SelectMany(r => r.Values).Count(),
                ImportedBy = t.ModifiedBy ?? t.CreatedBy,
                ImportedAt = t.ModifiedAt ?? t.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntityTemplate>> GetEntityTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        // Bekannte (Entity, Spalte)-Paare aus den vorhandenen Werten ziehen.
        var pairs = await Context.Set<DataValue>()
            .Select(v => new { Entity = v.DataRow!.DataEntity!.Name, Column = v.ColumnName })
            .Distinct()
            .ToListAsync(cancellationToken);

        // Auch Entities ohne Werte berücksichtigen (vollständige Schablone).
        var entityNames = await Context.Set<DataEntity>()
            .Select(e => e.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var attributesByEntity = pairs
            .GroupBy(p => p.Entity, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => p.Column).Distinct(StringComparer.OrdinalIgnoreCase)
                      .OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        return entityNames
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new EntityTemplate
            {
                Name = name,
                Attributes = attributesByEntity.TryGetValue(name, out var attrs) ? attrs : []
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DatabaseAttributeRow>> GetAllAttributesAsync(
        CancellationToken cancellationToken = default)
    {
        // Flache Projektion über den Navigationspfad Wert → Zeile → Entity →
        // Datei → Testfall. Vollständig parametrisiert (kein dynamisches SQL).
        return await Context.Set<DataValue>()
            .Select(v => new DatabaseAttributeRow
            {
                TestCaseId = v.DataRow!.DataEntity!.TestCaseFile!.ImportedTestCase!.Id,
                Number = v.DataRow.DataEntity.TestCaseFile.ImportedTestCase.Number,
                Entity = v.DataRow.DataEntity.Name,
                RowIndex = v.DataRow.RowIndex,
                Column = v.ColumnName,
                Value = v.Value,
                DataType = v.DataType
            })
            .OrderBy(r => r.Number).ThenBy(r => r.Entity).ThenBy(r => r.RowIndex)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Basisabfrage inklusive des gesamten Objektgraphen.</summary>
    private IQueryable<ImportedTestCase> DetailedQuery()
        => Set
            .Include(t => t.Files.OrderBy(f => f.Section).ThenBy(f => f.SortOrder))
                .ThenInclude(f => f.Entities.OrderBy(e => e.SortOrder))
                    .ThenInclude(e => e.Rows.OrderBy(r => r.RowIndex))
                        .ThenInclude(r => r.Values.OrderBy(v => v.SortOrder))
            .AsSplitQuery();
}
