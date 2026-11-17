using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Common.Validation;
using TestCaseManager.Application.Validation;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Factories;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Implementierung der Testfall-Anwendungsfälle auf Basis von
/// Unit of Work und Repositories. Enthält die gesamte Geschäftslogik:
/// Validierung, Eindeutigkeit des Namens, Auflösung von Kategorie und
/// Tags sowie das Kopieren von Testfällen.
/// </summary>
public class TestCaseService : ITestCaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TestCaseValidator _validator;
    private readonly ILogger<TestCaseService> _logger;

    /// <summary>
    /// Erzeugt den Service mit seinen Abhängigkeiten (Dependency Injection).
    /// </summary>
    /// <param name="unitOfWork">Arbeitseinheit mit allen Repositories.</param>
    /// <param name="validator">Validierungsregeln für Testfälle.</param>
    /// <param name="logger">Logger für Diagnose und Fehlerprotokollierung.</param>
    public TestCaseService(IUnitOfWork unitOfWork, TestCaseValidator validator, ILogger<TestCaseService> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TestCaseListItem>> SearchAsync(TestCaseFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return await _unitOfWork.TestCases.SearchAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TestCase?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => _unitOfWork.TestCases.GetWithDetailsAsync(id, cancellationToken);

    /// <inheritdoc />
    public TestCase CreateNew() => TestCaseFactory.CreateNew();

    /// <inheritdoc />
    public async Task SaveAsync(TestCase testCase, string? categoryName, IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(testCase);
        ArgumentNullException.ThrowIfNull(tagNames);

        NormalizeInput(testCase);

        // 1. Feldvalidierung (Pflichtfelder, Längen, Datentypen).
        var errors = _validator.Validate(testCase).ToList();

        // 2. Doppelte Namen datenbankweit verhindern.
        if (!string.IsNullOrWhiteSpace(testCase.Name)
            && await _unitOfWork.TestCases.NameExistsAsync(testCase.Name, testCase.Id, cancellationToken))
        {
            errors.Add(new ValidationError("Name",
                $"Es existiert bereits ein Testfall mit dem Namen \"{testCase.Name}\"."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        // 3. Kategorie und Tags anhand der Namen auflösen (bei Bedarf anlegen).
        await ResolveCategoryAsync(testCase, categoryName, cancellationToken);
        await ResolveTagsAsync(testCase, tagNames, cancellationToken);

        // 4. Neu anlegen oder bestehende (nachverfolgte) Entität aktualisieren.
        var isNew = await _unitOfWork.TestCases.GetByIdAsync(testCase.Id, cancellationToken) is null;
        if (isNew)
        {
            await _unitOfWork.TestCases.AddAsync(testCase, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Testfall {TestCaseName} ({TestCaseId}) wurde {Action}.",
            testCase.Name, testCase.Id, isNew ? "angelegt" : "aktualisiert");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var testCase = await _unitOfWork.TestCases.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Der Testfall mit der Id {id} wurde nicht gefunden.");

        _unitOfWork.TestCases.Remove(testCase);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Testfall {TestCaseName} ({TestCaseId}) wurde gelöscht.", testCase.Name, id);
    }

    /// <inheritdoc />
    public async Task<TestCase> CopyAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        var source = await _unitOfWork.TestCases.GetWithDetailsAsync(sourceId, cancellationToken)
            ?? throw new InvalidOperationException($"Der Testfall mit der Id {sourceId} wurde nicht gefunden.");

        var newName = await BuildUniqueCopyNameAsync(source.Name, cancellationToken);
        var copy = TestCaseFactory.CreateCopy(source, newName);

        await _unitOfWork.TestCases.AddAsync(copy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Testfall {SourceName} wurde als {CopyName} kopiert.", source.Name, copy.Name);
        return copy;
    }

    /// <summary>
    /// Entfernt führende/nachgestellte Leerzeichen aus allen Texteingaben,
    /// damit Validierung und Eindeutigkeitsprüfung auf bereinigten Werten arbeiten.
    /// </summary>
    private static void NormalizeInput(TestCase testCase)
    {
        testCase.Name = testCase.Name.Trim();
        testCase.Version = testCase.Version.Trim();
        testCase.Description = string.IsNullOrWhiteSpace(testCase.Description) ? null : testCase.Description.Trim();

        foreach (var parameter in testCase.Parameters)
        {
            parameter.Name = parameter.Name.Trim();
        }

        foreach (var result in testCase.ExpectedResults)
        {
            result.Name = result.Name.Trim();
        }
    }

    /// <summary>
    /// Ordnet dem Testfall die Kategorie mit dem angegebenen Namen zu.
    /// Existiert sie noch nicht, wird sie automatisch angelegt.
    /// </summary>
    private async Task ResolveCategoryAsync(TestCase testCase, string? categoryName,
        CancellationToken cancellationToken)
    {
        var name = categoryName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            testCase.Category = null;
            testCase.CategoryId = null;
            return;
        }

        var category = await _unitOfWork.Categories.GetByNameAsync(name, cancellationToken);
        if (category is null)
        {
            category = new Category { Name = name };
            await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        }

        testCase.Category = category;
        testCase.CategoryId = category.Id;
    }

    /// <summary>
    /// Ersetzt die Tag-Zuordnung des Testfalls durch die angegebenen Namen.
    /// Unbekannte Tags werden automatisch angelegt, Duplikate ignoriert.
    /// </summary>
    private async Task ResolveTagsAsync(TestCase testCase, IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken)
    {
        var resolved = new List<Tag>();

        foreach (var rawName in tagNames)
        {
            var name = rawName.Trim();
            if (name.Length == 0 || resolved.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var tag = await _unitOfWork.Tags.GetByNameAsync(name, cancellationToken)
                      ?? new Tag { Name = name };
            resolved.Add(tag);
        }

        testCase.Tags.Clear();
        testCase.Tags.AddRange(resolved);
    }

    /// <summary>
    /// Bildet einen eindeutigen Namen für eine Kopie nach dem Muster
    /// "Name (Kopie)", "Name (Kopie 2)", "Name (Kopie 3)" usw.
    /// </summary>
    private async Task<string> BuildUniqueCopyNameAsync(string sourceName, CancellationToken cancellationToken)
    {
        var candidate = $"{sourceName} (Kopie)";
        var counter = 2;

        while (await _unitOfWork.TestCases.NameExistsAsync(candidate, null, cancellationToken))
        {
            candidate = $"{sourceName} (Kopie {counter})";
            counter++;
        }

        return candidate;
    }
}
