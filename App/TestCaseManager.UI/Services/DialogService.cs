using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.UI.ViewModels;
using TestCaseManager.UI.Views;

namespace TestCaseManager.UI.Services;

/// <summary>
/// WPF-Implementierung des Dialogdienstes. Der Testfall-Editor erhält
/// einen eigenen DI-Scope: Alle darin geladenen Entitäten werden von
/// derselben Arbeitseinheit nachverfolgt und erst beim Speichern
/// atomar persistiert (Unit of Work pro Dialog).
/// </summary>
public class DialogService : IDialogService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Erzeugt den Dienst mit der Scope-Fabrik des Hosts.</summary>
    /// <param name="scopeFactory">Fabrik für DI-Dienstbereiche.</param>
    /// <param name="loggerFactory">Fabrik für Logger der erzeugten ViewModels.</param>
    public DialogService(IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public void ShowError(string title, string message)
        => MessageBox.Show(GetOwner(), message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    /// <inheritdoc />
    public void ShowInfo(string title, string message)
        => MessageBox.Show(GetOwner(), message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    /// <inheritdoc />
    public bool Confirm(string title, string message)
        => MessageBox.Show(GetOwner(), message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
           == MessageBoxResult.Yes;

    /// <inheritdoc />
    public bool ShowTestCaseEditor(Guid? testCaseId)
    {
        using var scope = _scopeFactory.CreateScope();

        var viewModel = new TestCaseEditorViewModel(
            scope.ServiceProvider.GetRequiredService<ITestCaseService>(),
            scope.ServiceProvider.GetRequiredService<ICategoryService>(),
            this,
            testCaseId);

        var window = new TestCaseEditorWindow
        {
            DataContext = viewModel,
            Owner = GetOwner()
        };

        // Das ViewModel meldet über dieses Ereignis, ob gespeichert wurde.
        viewModel.RequestClose += (_, saved) =>
        {
            window.DialogResult = saved;
        };

        return window.ShowDialog() == true;
    }

    /// <inheritdoc />
    public bool ShowTestCaseWizard()
    {
        using var scope = _scopeFactory.CreateScope();

        var viewModel = new TestCaseWizardViewModel(
            scope.ServiceProvider.GetRequiredService<ITestCaseFileService>(),
            this,
            scope.ServiceProvider.GetRequiredService<ILogger<TestCaseWizardViewModel>>());

        var window = new TestCaseWizardWindow
        {
            DataContext = viewModel,
            Owner = GetOwner()
        };

        viewModel.RequestClose += (_, created) =>
        {
            window.DialogResult = created;
        };

        return window.ShowDialog() == true;
    }

    /// <inheritdoc />
    public async Task ShowImportedTestCaseDetailAsync(Guid importedTestCaseId)
    {
        // Die Daten werden innerhalb des Scopes vollständig in ein Detailmodell
        // übertragen. Das Fenster selbst hält keinen DbContext, weshalb der
        // Scope bereits vor dem Anzeigen wieder freigegeben werden kann.
        ImportedTestCaseDetail? detail;
        using (var scope = _scopeFactory.CreateScope())
        {
            var fileService = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
            detail = await fileService.GetImportedDetailAsync(importedTestCaseId);
        }

        if (detail is null)
        {
            ShowError("Testfall nicht gefunden",
                "Der Datei-Testfall existiert nicht mehr in der Datenbank. "
                + "Bitte aktualisieren Sie die Liste.");
            return;
        }

        var window = new ImportedTestCaseDetailWindow
        {
            DataContext = new ImportedTestCaseDetailViewModel(detail),
            Owner = GetOwner()
        };

        window.ShowDialog();
    }

    /// <inheritdoc />
    public async Task<bool> ShowImportedTestCaseEditorAsync(Guid importedTestCaseId)
    {
        // Das ViewModel öffnet für Laden und Speichern jeweils einen eigenen
        // Scope. Ein über die Lebensdauer des Fensters offener DbContext wäre
        // hier unangebracht: Das Fenster steht beliebig lange offen.
        var viewModel = new ImportedTestCaseEditViewModel(
            _scopeFactory,
            this,
            _loggerFactory.CreateLogger<ImportedTestCaseEditViewModel>(),
            importedTestCaseId);

        if (!await viewModel.LoadAsync())
        {
            return false;
        }

        var window = new ImportedTestCaseEditWindow
        {
            DataContext = viewModel,
            Owner = GetOwner()
        };

        viewModel.RequestClose += (_, saved) =>
        {
            window.DialogResult = saved;
        };

        return window.ShowDialog() == true;
    }

    /// <inheritdoc />
    public string? ShowOpenFileDialog(string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter };
        return dialog.ShowDialog(GetOwner()) == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? ShowSaveFileDialog(string suggestedFileName, string filter)
    {
        var dialog = new SaveFileDialog { FileName = suggestedFileName, Filter = filter };
        return dialog.ShowDialog(GetOwner()) == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? ShowFolderDialog(string? initialPath)
    {
        var dialog = new OpenFolderDialog { Title = "Ordner auswählen" };
        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
        {
            dialog.InitialDirectory = initialPath;
        }

        return dialog.ShowDialog(GetOwner()) == true ? dialog.FolderName : null;
    }

    /// <summary>Ermittelt das aktive Fenster als Besitzer für modale Dialoge.</summary>
    private static Window? GetOwner()
        => System.Windows.Application.Current?.Windows
               .OfType<Window>()
               .FirstOrDefault(w => w.IsActive)
           ?? System.Windows.Application.Current?.MainWindow;
}
