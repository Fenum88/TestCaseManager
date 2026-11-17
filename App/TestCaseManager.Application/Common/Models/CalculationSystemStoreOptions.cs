namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Einstellungen für den JSON-Speicher der Berechnungssysteme. Wird beim Start
/// aus der Konfiguration befüllt: der Dateipfad, unter dem die Systeme abgelegt
/// werden, sowie die Erstbelegung (Seed) aus der <c>appsettings.json</c>, die
/// nur greift, wenn noch keine Speicherdatei existiert.
/// </summary>
public class CalculationSystemStoreOptions
{
    /// <summary>Vollständiger Pfad zur JSON-Datei mit den Berechnungssystemen.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Erstbelegung, die beim ersten Start (noch keine Datei vorhanden) in den
    /// Speicher übernommen wird.
    /// </summary>
    public IReadOnlyList<CalculationSystem> Seed { get; init; } = [];
}
