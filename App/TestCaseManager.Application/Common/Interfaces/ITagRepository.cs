using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Repository für Tags (Schlagwörter).
/// </summary>
public interface ITagRepository : IRepository<Tag>
{
    /// <summary>
    /// Sucht ein Tag anhand seines Namens
    /// (Groß-/Kleinschreibung wird ignoriert).
    /// </summary>
    /// <param name="name">Tag-Name.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Liefert die Gesamtanzahl aller Tags.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
