using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Repository für die Abgleichprofile. Die Profile sind eine gemeinsam
/// genutzte Stammdatenliste: Alle Nutzer sehen denselben Bestand.
/// </summary>
public interface ICompareProfileRepository : IRepository<CompareProfile>
{
    /// <summary>
    /// Lädt alle Profile samt ihren Schlüsselregeln in Anzeigereihenfolge.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<List<CompareProfile>> GetAllWithRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>Merkt mehrere Profile zum Löschen vor (inklusive ihrer Regeln).</summary>
    /// <param name="profiles">Die zu löschenden Profile.</param>
    void RemoveRange(IEnumerable<CompareProfile> profiles);

    /// <summary>
    /// Ersetzt die Schlüsselregeln eines bereits gespeicherten Profils.
    ///
    /// Die Zustände werden dabei ausdrücklich gesetzt (alte Regeln löschen,
    /// neue einfügen). Würde man die Regeln nur über die Navigationsliste
    /// austauschen, stufte Entity Framework die neuen Objekte als Änderung
    /// bestehender Zeilen ein – ihr Schlüssel ist bereits gefüllt, weil er
    /// clientseitig vergeben wird.
    /// </summary>
    /// <param name="profile">Das gespeicherte Profil.</param>
    /// <param name="rules">Die neuen Regeln in gewünschter Reihenfolge.</param>
    void ReplaceRules(CompareProfile profile, IEnumerable<CompareProfileRule> rules);
}
