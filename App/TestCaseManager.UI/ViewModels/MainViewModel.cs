using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// ViewModel des Hauptfensters: verwaltet die Navigation zwischen
/// Dashboard und Testfallverwaltung. Die eigentlichen Inhalte werden
/// über DataTemplates auf die jeweilige Ansicht abgebildet.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly TestCaseListViewModel _testCaseListViewModel;
    private readonly ImportExportViewModel _importExportViewModel;
    private readonly CalculationViewModel _calculationViewModel;
    private readonly TestScenariosViewModel _testScenariosViewModel;
    private readonly DatabaseViewModel _databaseViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    /// <summary>Das aktuell angezeigte Unter-ViewModel.</summary>
    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    /// <summary>Erzeugt das ViewModel mit den Unteransichten (Dependency Injection).</summary>
    /// <param name="dashboardViewModel">ViewModel des Dashboards.</param>
    /// <param name="testCaseListViewModel">ViewModel der Testfallverwaltung.</param>
    /// <param name="importExportViewModel">ViewModel für Datei-Import/-Export.</param>
    /// <param name="calculationViewModel">ViewModel für die Berechnungssysteme.</param>
    /// <param name="testScenariosViewModel">ViewModel des Testszenarien-Reiters.</param>
    /// <param name="databaseViewModel">ViewModel des Datenbank-Reiters.</param>
    /// <param name="settingsViewModel">ViewModel des Einstellungen-Reiters.</param>
    public MainViewModel(DashboardViewModel dashboardViewModel, TestCaseListViewModel testCaseListViewModel,
        ImportExportViewModel importExportViewModel, CalculationViewModel calculationViewModel,
        TestScenariosViewModel testScenariosViewModel, DatabaseViewModel databaseViewModel,
        SettingsViewModel settingsViewModel)
    {
        _dashboardViewModel = dashboardViewModel;
        _testCaseListViewModel = testCaseListViewModel;
        _importExportViewModel = importExportViewModel;
        _calculationViewModel = calculationViewModel;
        _testScenariosViewModel = testScenariosViewModel;
        _databaseViewModel = databaseViewModel;
        _settingsViewModel = settingsViewModel;
    }

    /// <summary>Zeigt das Dashboard an und aktualisiert dessen Kennzahlen.</summary>
    [RelayCommand]
    private async Task ShowDashboardAsync()
    {
        CurrentViewModel = _dashboardViewModel;
        await _dashboardViewModel.LoadAsync();
    }

    /// <summary>Zeigt die Testfallverwaltung an und aktualisiert die Liste.</summary>
    [RelayCommand]
    private async Task ShowTestCasesAsync()
    {
        CurrentViewModel = _testCaseListViewModel;
        await _testCaseListViewModel.RefreshAllAsync();
    }

    /// <summary>Zeigt die Import-/Export-Ansicht an und aktualisiert die Übersicht.</summary>
    [RelayCommand]
    private async Task ShowImportExportAsync()
    {
        CurrentViewModel = _importExportViewModel;
        await _importExportViewModel.RefreshAsync();
    }

    /// <summary>Zeigt den Reiter „Berechnung" an und lädt die Systeme neu.</summary>
    [RelayCommand]
    private async Task ShowCalculationAsync()
    {
        CurrentViewModel = _calculationViewModel;
        await _calculationViewModel.LoadAsync();
    }

    /// <summary>Zeigt den Reiter „Testszenarien" an und lädt sie neu.</summary>
    [RelayCommand]
    private async Task ShowScenariosAsync()
    {
        CurrentViewModel = _testScenariosViewModel;
        await _testScenariosViewModel.LoadAsync();
    }

    /// <summary>Zeigt den Reiter „Datenbank" an und lädt die Attribute neu.</summary>
    [RelayCommand]
    private async Task ShowDatabaseAsync()
    {
        CurrentViewModel = _databaseViewModel;
        await _databaseViewModel.LoadAsync();
    }

    /// <summary>Zeigt den Reiter „Einstellungen" an und lädt die Systeme neu.</summary>
    [RelayCommand]
    private async Task ShowSettingsAsync()
    {
        CurrentViewModel = _settingsViewModel;
        await _settingsViewModel.LoadAsync();
    }
}
