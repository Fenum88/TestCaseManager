using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Repository für Kategorien.
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Sucht eine Kategorie anhand ihres Namens
    /// (Groß-/Kleinschreibung wird ignoriert).
    /// </summary>
    /// <param name="name">Kategoriename.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Liefert die Gesamtanzahl aller Kategorien.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
