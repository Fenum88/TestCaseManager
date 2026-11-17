using TestCaseManager.Domain.Common;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Frei vergebbares Schlagwort zur Kennzeichnung von Testfällen.
/// Tags stehen in einer n:m-Beziehung zu Testfällen; die Zuordnungstabelle
/// wird von Entity Framework Core automatisch verwaltet.
/// </summary>
public class Tag : EntityBase
{
    /// <summary>Eindeutiger Name des Tags (Pflichtfeld).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Alle Testfälle, die dieses Tag tragen (Navigationseigenschaft).</summary>
    public List<TestCase> TestCases { get; set; } = [];
}
