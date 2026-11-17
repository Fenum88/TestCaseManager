using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Serviceschicht für Tags (Schlagwörter).
/// </summary>
public interface ITagService
{
    /// <summary>Liefert alle Tags alphabetisch sortiert.</summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
}
