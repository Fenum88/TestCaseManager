using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Persistenter Speicher der Abgleichprofile. Jedes Profil bündelt die
/// Schlüsselregeln, über die beim Vergleich die Zeilen einer Entität
/// einander zugeordnet werden.
/// </summary>
public interface ICompareProfileStore
{
    /// <summary>Liefert alle gespeicherten Profile.</summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<CompareProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Speichert die übergebenen Profile vollständig (ersetzt den Bestand).</summary>
    /// <param name="profiles">Die zu speichernden Profile.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task SaveAsync(IEnumerable<CompareProfile> profiles, CancellationToken cancellationToken = default);
}
