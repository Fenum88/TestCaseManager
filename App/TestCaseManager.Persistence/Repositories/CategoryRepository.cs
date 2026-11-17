using Microsoft.EntityFrameworkCore;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Repositories;

/// <summary>
/// EF-Core-Implementierung des Kategorien-Repositories.
/// </summary>
public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    /// <summary>Erzeugt das Repository für den übergebenen Kontext.</summary>
    /// <param name="context">Gemeinsamer Kontext der Arbeitseinheit.</param>
    public CategoryRepository(TestCaseManagerDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return Set.FirstOrDefaultAsync(c => c.Name.ToLower() == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => Set.CountAsync(cancellationToken);
}
