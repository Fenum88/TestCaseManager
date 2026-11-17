using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Services;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// ViewModel des Reiters „Testszenarien": Testfälle zu benannten Szenarien
/// zusammenfassen, die anschließend im Berechnungsreiter als Ganzes
/// durchgerechnet werden können. Die Szenarien werden dauerhaft gespeichert.
/// </summary>
public partial class TestScenariosViewModel : ObservableObject
{
    private readonly ITestScenarioStore _store;
    private readonly IDialogService _dialogService;
    private readonly ILogger<TestScenariosViewModel> _logger;
    private readonly string _testCaseRoot;

    /// <summary>Alle bearbeitbaren Szenarien.</summary>
    public ObservableCollection<ScenarioEditModel> Scenarios { get; } = [];

    /// <summary>Das aktuell ausgewählte Szenario (Detailbearbeitung).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    private ScenarioEditModel? _selectedScenario;

    /// <summary>Statuszeile.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Erzeugt das ViewModel (Dependency Injection).</summary>
    /// <param name="store">Persistenter Speicher der Szenarien.</param>
    /// <param name="dialogService">Dialogdienst (Bestätigungen, Meldungen).</param>
    /// <param name="configuration">Konfiguration (Pfadauflösung).</param>
    /// <param name="logger">Logger für Fehlerprotokollierung.</param>
    public TestScenariosViewModel(ITestScenarioStore store, IDialogService dialogService,
        IConfiguration configuration, ILogger<TestScenariosViewModel> logger)
    {
        _store = store;
        _dialogService = dialogService;
        _logger = logger;

        _testCaseRoot = TestCaseFilesPathResolver.Resolve(
            configuration["TestCaseFiles:RootPath"], AppContext.BaseDirectory);
    }

    /// <summary>Lädt die gespeicherten Szenarien und die verfügbaren Testfälle.</summary>
    public async Task LoadAsync()
    {
        try
        {
            var available = TestCaseFilesPathResolver.EnumerateTestCaseNumbers(_testCaseRoot);
            var scenarios = await _store.GetAllAsync();

            var vorher = SelectedScenario?.Name;
            Scenarios.Clear();
            foreach (var scenario in scenarios)
            {
                Scenarios.Add(new ScenarioEditModel(scenario, available));
            }

            SelectedScenario = Scenarios.FirstOrDefault(s => s.Name == vorher) ?? Scenarios.FirstOrDefault();
            StatusMessage = $"{Scenarios.Count} Szenario(s) geladen, {available.Count} Testfälle verfügbar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Testszenarien konnten nicht geladen werden.");
            _dialogService.ShowError("Fehler", "Die Testszenarien konnten nicht geladen werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Legt ein neues, leeres Szenario an.</summary>
    [RelayCommand]
    private void Add()
    {
        var available = TestCaseFilesPathResolver.EnumerateTestCaseNumbers(_testCaseRoot);
        var neu = new ScenarioEditModel(null, available) { Name = "Neues Szenario" };
        Scenarios.Add(neu);
        SelectedScenario = neu;
        StatusMessage = "Neues Szenario angelegt – Name vergeben, Testfälle wählen und speichern.";
    }

    /// <summary>Entfernt das ausgewählte Szenario.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Remove()
    {
        if (SelectedScenario is null)
        {
            return;
        }

        var name = SelectedScenario.Name;
        if (!_dialogService.Confirm("Szenario entfernen",
                $"Soll das Szenario \"{name}\" entfernt werden?"))
        {
            return;
        }

        Scenarios.Remove(SelectedScenario);
        StatusMessage = $"Szenario \"{name}\" entfernt. Zum Übernehmen speichern.";
    }

    /// <summary>Prüft die Eingaben und speichert alle Szenarien.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        var ohneName = Scenarios.Any(s => string.IsNullOrWhiteSpace(s.Name));
        if (ohneName)
        {
            _dialogService.ShowError("Name fehlt", "Jedes Szenario benötigt einen Namen.");
            return;
        }

        var doppelt = Scenarios
            .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (doppelt.Count > 0)
        {
            _dialogService.ShowError("Doppelte Namen",
                "Szenarionamen müssen eindeutig sein. Mehrfach vorhanden:"
                + Environment.NewLine + string.Join(Environment.NewLine, doppelt));
            return;
        }

        try
        {
            await _store.SaveAsync(Scenarios.Select(s => s.ToTestScenario()));
            StatusMessage = $"{Scenarios.Count} Szenario(s) gespeichert.";
            _dialogService.ShowInfo("Gespeichert",
                "Die Szenarien wurden gespeichert und stehen im Reiter „Berechnung“ zur Auswahl.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Testszenarien konnten nicht gespeichert werden.");
            _dialogService.ShowError("Fehler", "Die Testszenarien konnten nicht gespeichert werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Verwirft Änderungen und lädt den gespeicherten Stand neu.</summary>
    [RelayCommand]
    private async Task ReloadAsync() => await LoadAsync();

    /// <summary>CanExecute-Bedingung für Aktionen mit Auswahl.</summary>
    private bool HasSelection() => SelectedScenario is not null;
}
