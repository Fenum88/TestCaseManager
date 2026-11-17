using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Persistenter Speicher der Testszenarien. Erlaubt der Oberfläche, Szenarien
/// anzulegen, zu ändern und zu entfernen.
/// </summary>
public interface ITestScenarioStore
{
    /// <summary>Liefert alle gespeicherten Szenarien.</summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<TestScenario>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Speichert die übergebene Liste vollständig (ersetzt den Bestand).</summary>
    /// <param name="scenarios">Die zu speichernden Szenarien.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task SaveAsync(IEnumerable<TestScenario> scenarios, CancellationToken cancellationToken = default);
}
