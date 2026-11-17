using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// Ein Eintrag im Log-Panel der Import-/Export-Ansicht.
/// </summary>
/// <param name="Timestamp">Zeitpunkt des Eintrags.</param>
/// <param name="Message">Meldungstext.</param>
/// <param name="IsError">Ob es sich um eine Fehlermeldung handelt (rote Darstellung).</param>
public record LogEntry(DateTime Timestamp, string Message, bool IsError)
{
    /// <summary>Formatierte Zeit für die Anzeige.</summary>
    public string Time => Timestamp.ToString("HH:mm:ss");
}

/// <summary>
/// ViewModel der Import-/Export-Ansicht: Testfälle aus dem Ordner
/// <c>Workspace/Testfälle</c> in die Datenbank <b>einlesen</b> und aus der
/// Datenbank wieder als Dateien <b>auslesen</b> (wiederherstellen).
/// Jeder Vorgang läuft in einem eigenen DI-Scope (Unit of Work) und wird
/// im Log-Panel nachvollziehbar protokolliert.
/// </summary>
public partial class ImportExportViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ImportExportViewModel> _logger;

    /// <summary>Wurzelordner der Testfalldateien (enthält Input und Output).</summary>
    [ObservableProperty]
    private string _rootPath;

    /// <summary>Gibt an, ob gerade ein Vorgang läuft.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Statuszeile mit dem Ergebnis des letzten Vorgangs.</summary>
    [ObservableProperty]
    private string _statusMessage = "Bereit.";

    /// <summary>Der in der Übersicht ausgewählte Datei-Testfall.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    private ImportedTestCaseListItem? _selectedTestCase;

    /// <summary>Übersicht der in der Datenbank gespeicherten Datei-Testfälle.</summary>
    public ObservableCollection<ImportedTestCaseListItem> ImportedTestCases { get; } = [];

    /// <summary>Protokoll der Vorgänge (Log-Panel).</summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    /// <summary>Erzeugt das ViewModel (Dependency Injection).</summary>
    /// <param name="scopeFactory">Fabrik für DI-Dienstbereiche.</param>
    /// <param name="dialogService">Dialogdienst (Ordnerauswahl, Bestätigungen, Meldungen).</param>
    /// <param name="configuration">Konfiguration (optionaler fester Pfad).</param>
    /// <param name="logger">Logger für Fehlerprotokollierung.</param>
    public ImportExportViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogService,
        IConfiguration configuration, ILogger<ImportExportViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _logger = logger;

        _rootPath = TestCaseFilesPathResolver.Resolve(
            configuration["TestCaseFiles:RootPath"], AppContext.BaseDirectory);
    }

    /// <summary>Lädt die Übersicht der bereits eingelesenen Testfälle.</summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
            var items = await service.GetImportedAsync();

            ImportedTestCases.Clear();
            foreach (var item in items)
            {
                ImportedTestCases.Add(item);
            }

            AppendLog($"Übersicht geladen: {items.Count} Datei-Testfall/-fälle in der Datenbank.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Übersicht der Datei-Testfälle konnte nicht geladen werden.");
            AppendLog("Übersicht konnte nicht geladen werden: " + ex.Message, isError: true);
            _dialogService.ShowError("Fehler", "Die Übersicht konnte nicht geladen werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Öffnet die Ordnerauswahl für den Wurzelordner.</summary>
    [RelayCommand]
    private void Browse()
    {
        var selected = _dialogService.ShowFolderDialog(RootPath);
        if (selected is not null)
        {
            RootPath = selected;
            AppendLog("Ordner gewählt: " + selected);
        }
    }

    /// <summary>Liest alle Testfälle aus dem Ordner in die Datenbank ein.</summary>
    [RelayCommand]
    private async Task ImportAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Lese ein …";
        AppendLog("───── Einlesen gestartet ─────");
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
            var result = await service.ImportFolderAsync(RootPath);

            foreach (var message in result.Messages)
            {
                AppendLog(message);
            }
            foreach (var error in result.Errors)
            {
                AppendLog(error, isError: true);
            }

            StatusMessage = result.Summary;
            AppendLog("Fertig: " + result.Summary, isError: result.Errors.Count > 0);

            if (result.Errors.Count > 0)
            {
                _dialogService.ShowError("Einlesen mit Fehlern",
                    string.Join(Environment.NewLine, result.Errors));
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Einlesen aus {RootPath}.", RootPath);
            StatusMessage = "Einlesen fehlgeschlagen.";
            AppendLog("Einlesen fehlgeschlagen: " + ex.Message, isError: true);
            _dialogService.ShowError("Fehler", "Das Einlesen ist fehlgeschlagen:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Schreibt alle Testfälle aus der Datenbank wieder als Dateien.</summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var confirmed = _dialogService.Confirm("Auslesen",
            "Alle in der Datenbank gespeicherten Datei-Testfälle werden als Dateien nach"
            + Environment.NewLine + $"\"{RootPath}\"" + Environment.NewLine
            + "geschrieben. Vorhandene Dateien werden dabei überschrieben. Fortfahren?");
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Lese aus …";
        AppendLog("───── Auslesen gestartet ─────");
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
            var result = await service.ExportFolderAsync(RootPath);

            foreach (var message in result.Messages)
            {
                AppendLog(message);
            }
            foreach (var error in result.Errors)
            {
                AppendLog(error, isError: true);
            }

            StatusMessage = result.Summary;
            AppendLog("Fertig: " + result.Summary, isError: result.Errors.Count > 0);

            if (result.Errors.Count > 0)
            {
                _dialogService.ShowError("Auslesen mit Fehlern",
                    string.Join(Environment.NewLine, result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Auslesen nach {RootPath}.", RootPath);
            StatusMessage = "Auslesen fehlgeschlagen.";
            AppendLog("Auslesen fehlgeschlagen: " + ex.Message, isError: true);
            _dialogService.ShowError("Fehler", "Das Auslesen ist fehlgeschlagen:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Löscht den ausgewählten Datei-Testfall nach Bestätigung aus der
    /// Datenbank. Die Dateien im Wurzelordner bleiben erhalten.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedTestCase is null || IsBusy)
        {
            return;
        }

        var number = SelectedTestCase.Number;
        var confirmed = _dialogService.Confirm("Datei-Testfall löschen",
            $"Soll der Testfall \"{number}\" mit {SelectedTestCase.EntityCount} Entities, "
            + $"{SelectedTestCase.RowCount} Zeilen und {SelectedTestCase.ValueCount} Werten "
            + "aus der Datenbank gelöscht werden?"
            + Environment.NewLine + Environment.NewLine
            + "Die Dateien im Testfälle-Ordner bleiben erhalten.");
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
            var deleted = await service.DeleteImportedAsync(SelectedTestCase.Id);

            StatusMessage = deleted
                ? $"Testfall {number} aus der Datenbank gelöscht."
                : $"Testfall {number} war bereits gelöscht.";
            AppendLog(StatusMessage);

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Der Datei-Testfall {Number} konnte nicht gelöscht werden.", number);
            StatusMessage = "Löschen fehlgeschlagen.";
            AppendLog($"Löschen von {number} fehlgeschlagen: " + ex.Message, isError: true);
            _dialogService.ShowError("Fehler", "Der Testfall konnte nicht gelöscht werden:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Löscht alle Datei-Testfälle nach Bestätigung aus der Datenbank
    /// (Leeren der Datenbank ohne die Dateien anzutasten).
    /// </summary>
    [RelayCommand]
    private async Task DeleteAllAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (ImportedTestCases.Count == 0)
        {
            _dialogService.ShowInfo("Nichts zu löschen",
                "In der Datenbank sind keine Datei-Testfälle gespeichert.");
            return;
        }

        var confirmed = _dialogService.Confirm("Alle Datei-Testfälle löschen",
            $"Sollen wirklich alle {ImportedTestCases.Count} Datei-Testfälle mitsamt allen "
            + "Entities, Zeilen und Werten aus der Datenbank gelöscht werden?"
            + Environment.NewLine + Environment.NewLine
            + "Die Dateien im Testfälle-Ordner bleiben erhalten – ein erneutes "
            + "Einlesen stellt alle Testfälle wieder her.");
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        AppendLog("───── Löschen gestartet ─────");
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
            var count = await service.DeleteAllImportedAsync();

            StatusMessage = $"{count} Datei-Testfall/-fälle aus der Datenbank gelöscht.";
            AppendLog("Fertig: " + StatusMessage);

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Datei-Testfälle konnten nicht gelöscht werden.");
            StatusMessage = "Löschen fehlgeschlagen.";
            AppendLog("Löschen fehlgeschlagen: " + ex.Message, isError: true);
            _dialogService.ShowError("Fehler", "Die Testfälle konnten nicht gelöscht werden:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>CanExecute-Bedingung für das Löschen eines einzelnen Testfalls.</summary>
    private bool HasSelection() => SelectedTestCase is not null;

    /// <summary>Leert das Log-Panel.</summary>
    [RelayCommand]
    private void ClearLog() => LogEntries.Clear();

    /// <summary>
    /// Fügt einen Eintrag zum Log-Panel hinzu. Die Größe wird begrenzt,
    /// damit das Log bei häufiger Nutzung nicht unbegrenzt wächst.
    /// </summary>
    /// <param name="message">Meldungstext.</param>
    /// <param name="isError">Ob es eine Fehlermeldung ist.</param>
    private void AppendLog(string message, bool isError = false)
    {
        const int maxEntries = 500;
        LogEntries.Add(new LogEntry(DateTime.Now, message, isError));
        while (LogEntries.Count > maxEntries)
        {
            LogEntries.RemoveAt(0);
        }
    }
}
