using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Domain.Factories;

/// <summary>
/// Fabrik für Testfälle (Factory-Pattern).
/// Kapselt die Erzeugung neuer Testfälle mit sinnvollen Standardwerten
/// sowie das tiefe Kopieren bestehender Testfälle. Neue Testfalltypen
/// können später über zusätzliche Fabrikmethoden ergänzt werden, ohne
/// dass aufrufender Code angepasst werden muss.
/// </summary>
public static class TestCaseFactory
{
    /// <summary>
    /// Erzeugt einen neuen, leeren Testfall mit Standardwerten.
    /// </summary>
    /// <returns>Ein initialisierter Testfall im Status <see cref="TestCaseStatus.Draft"/>.</returns>
    public static TestCase CreateNew()
    {
        return new TestCase
        {
            Name = string.Empty,
            Version = "1.0",
            Status = TestCaseStatus.Draft
        };
    }

    /// <summary>
    /// Erzeugt eine tiefe Kopie eines bestehenden Testfalls inklusive
    /// Parametern, erwarteten Ergebnissen, Anhängen und Tags.
    /// Historie und Auditfelder werden bewusst nicht übernommen –
    /// die Kopie beginnt einen eigenen Lebenszyklus im Status "Entwurf".
    /// </summary>
    /// <param name="source">Der zu kopierende Testfall.</param>
    /// <param name="newName">Name der Kopie (muss eindeutig sein).</param>
    /// <returns>Eine unabhängige Kopie mit neuen Primärschlüsseln.</returns>
    public static TestCase CreateCopy(TestCase source, string newName)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        var copy = new TestCase
        {
            Name = newName,
            Description = source.Description,
            Version = source.Version,
            Status = TestCaseStatus.Draft,
            CategoryId = source.CategoryId
        };

        copy.Parameters = source.Parameters
            .Select(p => new TestParameter
            {
                TestCaseId = copy.Id,
                Name = p.Name,
                DataType = p.DataType,
                Value = p.Value,
                DefaultValue = p.DefaultValue,
                Description = p.Description,
                IsRequired = p.IsRequired,
                SortOrder = p.SortOrder
            })
            .ToList();

        copy.ExpectedResults = source.ExpectedResults
            .Select(r => new ExpectedResult
            {
                TestCaseId = copy.Id,
                Name = r.Name,
                DataType = r.DataType,
                ExpectedValue = r.ExpectedValue,
                Tolerance = r.Tolerance,
                Description = r.Description,
                SortOrder = r.SortOrder
            })
            .ToList();

        copy.Attachments = source.Attachments
            .Select(a => new Attachment
            {
                TestCaseId = copy.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                Content = a.Content.ToArray(),
                Description = a.Description
            })
            .ToList();

        // Tags werden referenziert, nicht kopiert (n:m-Beziehung auf dieselben Stammdaten).
        copy.Tags = source.Tags.ToList();

        return copy;
    }
}
