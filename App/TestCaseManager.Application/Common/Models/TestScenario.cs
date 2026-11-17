namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Ein Testszenario fasst mehrere Testfälle (über ihre Testfallnummer) zu einer
/// benannten Gruppe zusammen. Ein Szenario kann im Berechnungsreiter als Ganzes
/// durchgerechnet werden – dann werden nur die enthaltenen Testfälle verarbeitet.
/// </summary>
public class TestScenario
{
    /// <summary>Anzeigename des Szenarios (eindeutig).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Optionale Beschreibung.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Die enthaltenen Testfallnummern (z. B. "0003", "0007").</summary>
    public IReadOnlyList<string> TestCaseNumbers { get; init; } = [];
}

/// <summary>Einstellungen für den JSON-Speicher der Testszenarien.</summary>
public class TestScenarioStoreOptions
{
    /// <summary>Vollständiger Pfad zur JSON-Datei mit den Szenarien.</summary>
    public string FilePath { get; init; } = string.Empty;
}
