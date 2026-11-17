namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Schablone einer Entity: der Name der „Tabelle" und die Menge der
/// Attribute (Spaltennamen), die für diese Entity in der Datenbank bereits
/// vorkommen. Dient dem Neuanlegen von Datei-Testfällen als Vorlage, damit
/// der Anwender erkennt, welche Entities und Attribute es schon gibt.
/// </summary>
public class EntityTemplate
{
    /// <summary>Name der Entity (z. B. "Gebaeude").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Bereits bekannte Attribute (Spaltennamen) dieser Entity.</summary>
    public IReadOnlyList<string> Attributes { get; init; } = [];
}
