using TestCaseManager.Domain.Common;

namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Generisches Repository (Repository-Pattern) für Basisoperationen
/// auf einem Entitätstyp. Spezifische Repositories erweitern dieses
/// Interface um fachliche Abfragen.
/// </summary>
/// <typeparam name="TEntity">Der verwaltete Entitätstyp.</typeparam>
public interface IRepository<TEntity> where TEntity : EntityBase
{
    /// <summary>Lädt eine Entität anhand ihres Primärschlüssels.</summary>
    /// <param name="id">Primärschlüssel.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Die Entität oder <c>null</c>, falls nicht vorhanden.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lädt alle Entitäten des Typs.</summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Merkt eine neue Entität zum Einfügen vor.</summary>
    /// <param name="entity">Die einzufügende Entität.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Merkt eine Entität zum Löschen vor.</summary>
    /// <param name="entity">Die zu löschende Entität.</param>
    void Remove(TEntity entity);
}
