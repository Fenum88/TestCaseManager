namespace TestCaseManager.UI.Services;

/// <summary>
/// Abstraktion aller Dialoge (Meldungen, Bestätigungen, Dateiauswahl,
/// Testfall-Editor), damit ViewModels ohne direkte Fensterreferenzen
/// auskommen und testbar bleiben.
/// </summary>
public interface IDialogService
{
    /// <summary>Zeigt eine Fehlermeldung an.</summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="message">Meldungstext.</param>
    void ShowError(string title, string message);

    /// <summary>Zeigt einen Hinweis an.</summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="message">Meldungstext.</param>
    void ShowInfo(string title, string message);

    /// <summary>Fordert eine Ja/Nein-Bestätigung an.</summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="message">Frage an den Benutzer.</param>
    /// <returns><c>true</c>, wenn der Benutzer bestätigt hat.</returns>
    bool Confirm(string title, string message);

    /// <summary>
    /// Öffnet den modalen Testfall-Editor. Der Editor arbeitet in einem
    /// eigenen Dienstbereich (Unit of Work pro Dialog).
    /// </summary>
    /// <param name="testCaseId"><c>null</c> für einen neuen Testfall, sonst die Id des zu bearbeitenden Testfalls.</param>
    /// <returns><c>true</c>, wenn gespeichert wurde.</returns>
    bool ShowTestCaseEditor(Guid? testCaseId);

    /// <summary>
    /// Öffnet die nur lesende Detailansicht eines Datei-Testfalls mit allen
    /// gespeicherten Attributen.
    /// </summary>
    /// <param name="importedTestCaseId">Id des anzuzeigenden Datei-Testfalls.</param>
    Task ShowImportedTestCaseDetailAsync(Guid importedTestCaseId);

    /// <summary>
    /// Öffnet das Bearbeitungsfenster eines Datei-Testfalls, in dem die
    /// gespeicherten Attributwerte geändert werden können.
    /// </summary>
    /// <param name="importedTestCaseId">Id des zu bearbeitenden Datei-Testfalls.</param>
    /// <returns><c>true</c>, wenn Änderungen gespeichert wurden.</returns>
    Task<bool> ShowImportedTestCaseEditorAsync(Guid importedTestCaseId);

    /// <summary>
    /// Öffnet den Assistenten zum Anlegen eines neuen Datei-Testfalls
    /// (Entities → Attribute → Werte).
    /// </summary>
    /// <returns><c>true</c>, wenn ein Testfall angelegt wurde.</returns>
    bool ShowTestCaseWizard();

    /// <summary>Öffnet einen Datei-Öffnen-Dialog.</summary>
    /// <param name="filter">Dateifilter (z. B. "Alle Dateien|*.*").</param>
    /// <returns>Der gewählte Pfad oder <c>null</c> bei Abbruch.</returns>
    string? ShowOpenFileDialog(string filter);

    /// <summary>Öffnet einen Datei-Speichern-Dialog.</summary>
    /// <param name="suggestedFileName">Vorgeschlagener Dateiname.</param>
    /// <param name="filter">Dateifilter.</param>
    /// <returns>Der gewählte Pfad oder <c>null</c> bei Abbruch.</returns>
    string? ShowSaveFileDialog(string suggestedFileName, string filter);

    /// <summary>Öffnet einen Ordnerauswahl-Dialog.</summary>
    /// <param name="initialPath">Anfangsordner (optional).</param>
    /// <returns>Der gewählte Ordner oder <c>null</c> bei Abbruch.</returns>
    string? ShowFolderDialog(string? initialPath);
}
