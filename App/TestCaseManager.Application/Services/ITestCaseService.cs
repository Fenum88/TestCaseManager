using TestCaseManager.Application.Common.Models;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Serviceschicht für alle Anwendungsfälle rund um Testfälle
/// (Suchen, Laden, Anlegen, Ändern, Kopieren, Löschen).
/// Der Service ist "scoped": Die Oberfläche erzeugt pro Dialog bzw.
/// Vorgang einen eigenen Dienstbereich, sodass geladene Entitäten bis
/// zum Speichern von derselben Arbeitseinheit nachverfolgt werden.
/// </summary>
public interface ITestCaseService
{
    /// <summary>Sucht Testfälle anhand von Filterkriterien für die Listendarstellung.</summary>
    /// <param name="filter">Filter (Freitext, Status, Kategorie, Tag).</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<TestCaseListItem>> SearchAsync(TestCaseFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lädt einen Testfall vollständig (inklusive Parametern, Ergebnissen,
    /// Anhängen, Tags und Historie) zur Bearbeitung in der Detailansicht.
    /// </summary>
    /// <param name="id">Primärschlüssel des Testfalls.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<TestCase?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Erzeugt einen neuen, noch nicht gespeicherten Testfall mit Standardwerten.</summary>
    TestCase CreateNew();

    /// <summary>
    /// Validiert und speichert einen Testfall (neu oder geändert).
    /// Kategorie und Tags werden anhand ihrer Namen aufgelöst und bei
    /// Bedarf automatisch angelegt.
    /// </summary>
    /// <param name="testCase">
    /// Der Testfall. Muss entweder neu sein (<see cref="CreateNew"/>) oder in
    /// derselben Servicelebensdauer über <see cref="GetWithDetailsAsync"/> geladen worden sein.
    /// </param>
    /// <param name="categoryName">Kategoriename oder <c>null</c>/leer für "keine Kategorie".</param>
    /// <param name="tagNames">Namen der zugeordneten Tags.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <exception cref="Common.Validation.ValidationException">Bei ungültigen Eingaben.</exception>
    Task SaveAsync(TestCase testCase, string? categoryName, IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default);

    /// <summary>Löscht einen Testfall samt Parametern, Ergebnissen und Anhängen.</summary>
    /// <param name="id">Primärschlüssel des Testfalls.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Erzeugt eine Kopie eines Testfalls mit automatisch vergebenem,
    /// eindeutigem Namen und speichert sie sofort.
    /// </summary>
    /// <param name="sourceId">Primärschlüssel des zu kopierenden Testfalls.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Die gespeicherte Kopie.</returns>
    Task<TestCase> CopyAsync(Guid sourceId, CancellationToken cancellationToken = default);
}
