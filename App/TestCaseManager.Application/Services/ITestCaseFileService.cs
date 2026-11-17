using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Orchestriert das dateibasierte Ein- und Auslesen von Testfällen zwischen
/// dem Dateisystem (Ordner <c>Testfälle/Input</c> und <c>Testfälle/Output</c>)
/// und der Datenbank. Die eigentliche Format-Logik ist über
/// <see cref="FileFormat.ITestCaseFileParser"/> und
/// <see cref="FileFormat.ITestCaseFileWriter"/> austauschbar.
/// </summary>
public interface ITestCaseFileService
{
    /// <summary>
    /// Liest alle Testfälle aus dem angegebenen Wurzelordner ein und
    /// speichert sie in der Datenbank. Bereits vorhandene Testfälle mit
    /// gleicher Nummer werden ersetzt (Upsert über die Testfallnummer).
    /// </summary>
    /// <param name="rootPath">Wurzelordner (enthält <c>Input</c> und <c>Output</c>).</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<ImportResult> ImportFolderAsync(string rootPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schreibt alle in der Datenbank gespeicherten Datei-Testfälle wieder als
    /// Dateien in den angegebenen Wurzelordner. Damit lassen sich zuvor
    /// gelöschte Eingabe-/Ausgabedateien inhaltsgleich wiederherstellen.
    /// </summary>
    /// <param name="rootPath">Wurzelordner, in den geschrieben wird.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<ExportResult> ExportFolderAsync(string rootPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert Übersichtsdaten aller in der Datenbank gespeicherten Datei-Testfälle.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<ImportedTestCaseListItem>> GetImportedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert einen Datei-Testfall mit allen gespeicherten Attributen für die
    /// Detailanzeige (nur lesend).
    /// </summary>
    /// <param name="id">Primärschlüssel des Datei-Testfalls.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Die Detaildaten oder <c>null</c>, wenn kein Testfall mit dieser Id existiert.</returns>
    Task<ImportedTestCaseDetail?> GetImportedDetailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ändert einzelne Attributwerte eines Datei-Testfalls in der Datenbank.
    /// Datentyp und Anführungszeichen-Kennung werden aus dem neuen Wert nach
    /// denselben Regeln abgeleitet wie beim Einlesen, damit ein anschließendes
    /// Auslesen wieder ein formatgleiches Dateiabbild ergibt.
    ///
    /// Werte, die nicht zum angegebenen Testfall gehören, werden abgewiesen –
    /// eine Änderung kann damit nie auf einen fremden Testfall wirken.
    /// </summary>
    /// <param name="id">Primärschlüssel des Datei-Testfalls.</param>
    /// <param name="updates">Die zu übernehmenden Wertänderungen.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Anzahl der tatsächlich geänderten Werte.</returns>
    /// <exception cref="InvalidOperationException">
    /// Wenn der Testfall nicht existiert oder eine Änderung einen Wert
    /// adressiert, der nicht zu diesem Testfall gehört.
    /// </exception>
    Task<int> UpdateAttributeValuesAsync(Guid id, IReadOnlyList<AttributeValueUpdate> updates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Löscht einen Datei-Testfall mitsamt allen Entities, Zeilen und Werten
    /// aus der Datenbank. Die zugehörigen Dateien im Dateisystem bleiben
    /// unberührt – ein erneutes Einlesen stellt den Testfall wieder her.
    /// </summary>
    /// <param name="id">Primärschlüssel des zu löschenden Datei-Testfalls.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns><c>true</c>, wenn gelöscht wurde; <c>false</c>, wenn kein Testfall mit dieser Id existierte.</returns>
    Task<bool> DeleteImportedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Löscht alle Datei-Testfälle aus der Datenbank. Die Dateien im
    /// Dateisystem bleiben unberührt.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Anzahl der gelöschten Testfälle.</returns>
    Task<int> DeleteAllImportedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert die aus der Datenbank abgeleitete Schablone (bekannte Entities
    /// und ihre Attribute) für das Neuanlegen von Datei-Testfällen.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<EntityTemplate>> GetEntityTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert alle gespeicherten Testfall-Attribute in flacher Form für die
    /// Datenbankansicht.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task<IReadOnlyList<DatabaseAttributeRow>> GetDatabaseAttributesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legt einen neuen, über die Oberfläche zusammengestellten Datei-Testfall
    /// in der Datenbank an (ein Eingabeabschnitt mit den gewählten Entities,
    /// Attributen und Werten).
    /// </summary>
    /// <param name="draft">Der zusammengestellte Testfall.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <exception cref="InvalidOperationException">Wenn die Testfallnummer fehlt oder bereits vergeben ist.</exception>
    Task CreateFileTestCaseAsync(NewFileTestCase draft, CancellationToken cancellationToken = default);
}
