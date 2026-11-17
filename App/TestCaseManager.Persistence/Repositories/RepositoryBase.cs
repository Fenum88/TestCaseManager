using Microsoft.EntityFrameworkCore;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Domain.Common;

namespace TestCaseManager.Persistence.Repositories;

/// <summary>
/// Generische Basisimplementierung des Repository-Patterns auf Basis
/// von Entity Framework Core. Alle Abfragen sind parametrisiert –
/// SQL-Injection ist damit ausgeschlossen.
/// </summary>
/// <typeparam name="TEntity">Der verwaltete Entitätstyp.</typeparam>
public abstract class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : EntityBase
{
    /// <summary>Der EF-Core-Kontext der Arbeitseinheit.</summary>
    protected TestCaseManagerDbContext Context { get; }

    /// <summary>Das DbSet des verwalteten Entitätstyps.</summary>
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    /// <summary>Erzeugt das Repository für den übergebenen Kontext.</summary>
    /// <param name="context">Gemeinsamer Kontext der Arbeitseinheit.</param>
    protected RepositoryBase(TestCaseManagerDbContext context)
    {
        Context = context;
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Set.FindAsync([id], cancellationToken);

    /// <inheritdoc />
    public virtual Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => Set.ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    /// <inheritdoc />
    public virtual void Remove(TEntity entity)
        => Set.Remove(entity);
}
