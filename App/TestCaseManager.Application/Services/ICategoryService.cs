using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Serviceschicht für Kategorien (Stammdaten).
/// </summary>
public interface ICategoryService
{
    /// <summary>Liefert alle Kategorien alphabetisch sortiert.</summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
}
