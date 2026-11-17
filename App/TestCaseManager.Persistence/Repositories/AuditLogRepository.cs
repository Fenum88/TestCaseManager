using Microsoft.EntityFrameworkCore;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Repositories;

/// <summary>
/// EF-Core-Implementierung des (nur lesenden) Audit-Log-Repositories.
/// Einträge entstehen ausschließlich automatisch im
/// <see cref="TestCaseManagerDbContext"/> beim Speichern.
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly TestCaseManagerDbContext _context;

    /// <summary>Erzeugt das Repository für den übergebenen Kontext.</summary>
    /// <param name="context">Gemeinsamer Kontext der Arbeitseinheit.</param>
    public AuditLogRepository(TestCaseManagerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<List<AuditLogEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return _context.AuditLogEntries
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
