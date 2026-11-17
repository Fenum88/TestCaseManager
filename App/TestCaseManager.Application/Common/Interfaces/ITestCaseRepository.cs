using TestCaseManager.Application.Common.Models;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Repository für Testfälle mit fachlichen Such- und Statistikabfragen.
/// </summary>
public interface ITestCaseRepository : IRepository<TestCase>
{
    /// <summary>
    /// Lädt einen Testfall vollständig, d. h. inklusive Parametern,
    /// erwarteten Ergebnissen, Anhängen, Tags, Kategorie und Historie.
    /// </summary>
    /// <param name="id">Primärschlüssel des Testfalls.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<TestCase?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sucht Testfälle anhand der übergebenen Filterkriterien und liefert
    /// schlanke Anzeigemodelle für die Listendarstellung.
    /// </summary>
    /// <param name="filter">Filterkriterien (Freitext, Status, Kategorie, Tag).</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<List<TestCaseListItem>> SearchAsync(TestCaseFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prüft, ob bereits ein anderer Testfall mit diesem Namen existiert
    /// (Verhinderung doppelter Namen).
    /// </summary>
    /// <param name="name">Zu prüfender Name.</param>
    /// <param name="excludeId">Eigener Schlüssel, der von der Prüfung ausgenommen wird.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Liefert die Gesamtanzahl aller Testfälle.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Liefert die Gesamtanzahl aller Dateianhänge (für die Dashboard-Statistik).</summary>
    Task<int> CountAttachmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Liefert die Anzahl Testfälle je Status.</summary>
    Task<Dictionary<TestCaseStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Liefert die Anzahl Testfälle je Kategorie.</summary>
    Task<List<CategoryCount>> GetCategoryCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Liefert die zuletzt geänderten Testfälle.</summary>
    /// <param name="count">Maximale Anzahl der Ergebnisse.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<List<TestCaseListItem>> GetRecentlyChangedAsync(int count, CancellationToken cancellationToken = default);
}
