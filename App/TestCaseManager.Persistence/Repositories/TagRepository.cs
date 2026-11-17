using Microsoft.EntityFrameworkCore;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Repositories;

/// <summary>
/// EF-Core-Implementierung des Tag-Repositories.
/// </summary>
public class TagRepository : RepositoryBase<Tag>, ITagRepository
{
    /// <summary>Erzeugt das Repository für den übergebenen Kontext.</summary>
    /// <param name="context">Gemeinsamer Kontext der Arbeitseinheit.</param>
    public TagRepository(TestCaseManagerDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return Set.FirstOrDefaultAsync(t => t.Name.ToLower() == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => Set.CountAsync(cancellationToken);
}
