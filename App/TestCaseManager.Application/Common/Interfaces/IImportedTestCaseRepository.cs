using TestCaseManager.Application.Common.Models;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Repository für Datei-Testfälle (das dateibasierte Import-/Export-Subsystem).
/// </summary>
public interface IImportedTestCaseRepository : IRepository<ImportedTestCase>
{
    /// <summary>
    /// Lädt einen Datei-Testfall vollständig (Dateien, Entities, Zeilen, Werte)
    /// anhand seiner Testfallnummer.
    /// </summary>
    /// <param name="number">Testfallnummer (z. B. "0001").</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<ImportedTestCase?> GetByNumberWithDetailsAsync(string number, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lädt einen Datei-Testfall vollständig (Dateien, Entities, Zeilen, Werte)
    /// anhand seines Primärschlüssels.
    /// </summary>
    /// <param name="id">Primärschlüssel des Datei-Testfalls.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<ImportedTestCase?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lädt alle Datei-Testfälle vollständig (für den Export).
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<List<ImportedTestCase>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert Übersichtsdaten aller Datei-Testfälle (für die Anzeige in der Oberfläche).
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<List<ImportedTestCaseListItem>> GetSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ermittelt aus den vorhandenen Daten die bekannten Entities und je Entity
    /// deren bekannte Attribute (Spaltennamen). Dient als Schablone für das
    /// Neuanlegen von Datei-Testfällen.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<EntityTemplate>> GetEntityTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert alle gespeicherten Testfall-Attribute in flacher Form
    /// (Testfallnummer, Entity, Zeile, Spalte, Wert) für die Datenbankansicht.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<DatabaseAttributeRow>> GetAllAttributesAsync(CancellationToken cancellationToken = default);
}
