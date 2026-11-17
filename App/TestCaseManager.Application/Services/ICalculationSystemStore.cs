using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Persistenter Speicher der konfigurierten Berechnungssysteme. Erlaubt der
/// Oberfläche, Systeme einzubinden, zu ändern und wieder zu entfernen, ohne
/// Code oder die <c>appsettings.json</c> anzufassen.
/// </summary>
public interface ICalculationSystemStore
{
    /// <summary>Liefert alle gespeicherten Berechnungssysteme.</summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<CalculationSystem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Speichert die übergebene Liste vollständig (ersetzt den Bestand).</summary>
    /// <param name="systems">Die zu speichernden Systeme.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task SaveAsync(IEnumerable<CalculationSystem> systems, CancellationToken cancellationToken = default);
}
