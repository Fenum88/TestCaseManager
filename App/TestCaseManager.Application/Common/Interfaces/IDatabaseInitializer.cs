namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Initialisiert die Datenbank beim Anwendungsstart:
/// legt die Datenbank bei Bedarf an, wendet ausstehende Migrationen an
/// (Tabellen erzeugen, Spalten ergänzen, Indizes und Beziehungen anlegen)
/// und befüllt Stammdaten. Es sind keine manuellen SQL-Skripte nötig.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Prüft das Datenbankschema und bringt es auf den aktuellen Stand.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
