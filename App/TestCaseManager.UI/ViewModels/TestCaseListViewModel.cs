using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.Domain.Enums;
using TestCaseManager.UI.Models;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// ViewModel der Testfallverwaltung: Liste aller Testfälle mit
/// Freitextsuche, Status-, Kategorie- und Tag-Filter sowie den
/// Funktionen Neu, Bearbeiten, Kopieren und Löschen.
/// Jeder Vorgang läuft in einem eigenen DI-Scope (Unit of Work).
/// </summary>
public partial class TestCaseListViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ILogger<TestCaseListViewModel> _logger;

    /// <summary>Laufende Verzögerung der Freitextsuche (Debounce).</summary>
    private CancellationTokenSource? _searchDebounce;

    /// <summary>Alle gefundenen Testfälle (Anzeigemodelle).</summary>
    public ObservableCollection<TestCaseListItem> TestCases { get; } = [];

    /// <summary>Auswahlliste für den Statusfilter (inklusive "Alle").</summary>
    public ObservableCollection<EnumOption<TestCaseStatus?>> StatusFilters { get; } = [];

    /// <summary>Auswahlliste für den Kategoriefilter (inklusive "Alle").</summary>
    public ObservableCollection<EnumOption<Guid?>> CategoryFilters { get; } = [];

    /// <summary>Auswahlliste für den Tag-Filter (inklusive "Alle").</summary>
    public ObservableCollection<EnumOption<Guid?>> TagFilters { get; } = [];

    /// <summary>Der aktuell in der Liste ausgewählte Testfall.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    private TestCaseListItem? _selectedTestCase;

    /// <summary>Freitext für die Suche (Name, Beschreibung, Version).</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>Gewählter Statusfilter.</summary>
    [ObservableProperty]
    private EnumOption<TestCaseStatus?>? _selectedStatusFilter;

    /// <summary>Gewählter Kategoriefilter.</summary>
    [ObservableProperty]
    private EnumOption<Guid?>? _selectedCategoryFilter;

    /// <summary>Gewählter Tag-Filter.</summary>
    [ObservableProperty]
    private EnumOption<Guid?>? _selectedTagFilter;

    /// <summary>Gibt an, ob gerade Daten geladen werden.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Anzahl der aktuell angezeigten Testfälle (Statuszeile).</summary>
    [ObservableProperty]
    private int _resultCount;

    /// <summary>Erzeugt das ViewModel (Dependency Injection).</summary>
    /// <param name="scopeFactory">Fabrik für DI-Dienstbereiche.</param>
    /// <param name="dialogService">Dialogdienst (Editor, Bestätigungen, Fehler).</param>
    /// <param name="logger">Logger für Fehlerprotokollierung.</param>
    public TestCaseListViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogService,
        ILogger<TestCaseListViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _logger = logger;
    }

    /// <summary>
    /// Lädt Filterlisten und Testfallliste vollständig neu
    /// (beim Anzeigen der Ansicht und nach Änderungen).
    /// </summary>
    public async Task RefreshAllAsync()
    {
        await LoadFilterListsAsync();
        await RefreshAsync();
    }

    /// <summary>Führt die Suche mit den aktuellen Filterkriterien aus.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            var filter = new TestCaseFilter
            {
                SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                Status = SelectedStatusFilter?.Value,
                CategoryId = SelectedCategoryFilter?.Value,
                TagId = SelectedTagFilter?.Value
            };

            using var scope = _scopeFactory.CreateScope();
            var testCaseService = scope.ServiceProvider.GetRequiredService<ITestCaseService>();
            var results = (await testCaseService.SearchAsync(filter)).ToList();

            // Eingelesene Datei-Testfälle zusätzlich anzeigen. Da diese keinen
            // Status, keine Kategorie und keine Tags besitzen, werden sie nur
            // eingeblendet, wenn kein solcher Filter aktiv ist. Die Freitextsuche
            // wird auf die Testfallnummer angewendet.
            var combined = results;
            if (filter.Status is null && filter.CategoryId is null && filter.TagId is null)
            {
                var fileService = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
                var imported = await fileService.GetImportedAsync();
                var search = filter.SearchText?.Trim();

                var importedItems = imported
                    .Where(i => string.IsNullOrEmpty(search)
                                || i.Number.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .Select(MapImported);

                combined = results.Concat(importedItems)
                    .OrderByDescending(t => t.LastChangedAt)
                    .ToList();
            }

            TestCases.Clear();
            foreach (var item in combined)
            {
                TestCases.Add(item);
            }

            ResultCount = TestCases.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Testfallliste konnte nicht geladen werden.");
            _dialogService.ShowError("Fehler", "Die Testfälle konnten nicht geladen werden:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Öffnet den Assistenten zum Anlegen eines neuen Testfalls
    /// (Entities → Attribute → Werte, Schablone aus der Datenbank).
    /// </summary>
    [RelayCommand]
    private async Task NewAsync()
    {
        if (_dialogService.ShowTestCaseWizard())
        {
            await RefreshAllAsync();
        }
    }

    /// <summary>
    /// Öffnet die Detailansicht des ausgewählten Testfalls (Doppelklick).
    /// Je nach Art des Testfalls führt das zu unterschiedlichen Fenstern:
    /// interaktive Testfälle öffnen den Editor, Datei-Testfälle die nur
    /// lesende Attributansicht.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasAnySelection))]
    private async Task OpenAsync()
    {
        if (SelectedTestCase is null)
        {
            return;
        }

        if (SelectedTestCase.IsImported)
        {
            try
            {
                await _dialogService.ShowImportedTestCaseDetailAsync(SelectedTestCase.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Die Attribute des Datei-Testfalls {TestCaseId} konnten nicht "
                    + "geladen werden.", SelectedTestCase.Id);
                _dialogService.ShowError("Fehler", "Die Attribute konnten nicht geladen werden:"
                    + Environment.NewLine + ex.Message);
            }

            return;
        }

        await EditAsync();
    }

    /// <summary>Öffnet den Editor für den ausgewählten Testfall.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditAsync()
    {
        if (SelectedTestCase is not null && _dialogService.ShowTestCaseEditor(SelectedTestCase.Id))
        {
            await RefreshAllAsync();
        }
    }

    /// <summary>Kopiert den ausgewählten Testfall inklusive aller Details.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task CopyAsync()
    {
        if (SelectedTestCase is null)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var testCaseService = scope.ServiceProvider.GetRequiredService<ITestCaseService>();
            var copy = await testCaseService.CopyAsync(SelectedTestCase.Id);

            _dialogService.ShowInfo("Testfall kopiert",
                $"Der Testfall wurde als \"{copy.Name}\" kopiert.");
            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Der Testfall {TestCaseId} konnte nicht kopiert werden.", SelectedTestCase.Id);
            _dialogService.ShowError("Fehler", "Der Testfall konnte nicht kopiert werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>
    /// Löscht den ausgewählten Testfall nach Bestätigung – sowohl interaktive
    /// als auch Datei-Testfälle. Bei Datei-Testfällen wird ausschließlich der
    /// Datenbankinhalt entfernt; die Dateien im Dateisystem bleiben erhalten.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasAnySelection))]
    private async Task DeleteAsync()
    {
        if (SelectedTestCase is null)
        {
            return;
        }

        var isImported = SelectedTestCase.IsImported;
        var confirmed = _dialogService.Confirm("Testfall löschen", isImported
            ? $"Soll der Datei-Testfall \"{SelectedTestCase.Name}\" mitsamt allen Entities, "
              + "Zeilen und Werten aus der Datenbank gelöscht werden?"
              + Environment.NewLine + Environment.NewLine
              + "Die Dateien im Testfälle-Ordner bleiben erhalten – ein erneutes "
              + "Einlesen stellt den Testfall wieder her."
            : $"Soll der Testfall \"{SelectedTestCase.Name}\" mitsamt allen Parametern, "
              + "Ergebnissen und Anhängen wirklich gelöscht werden?");

        if (!confirmed)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();

            if (isImported)
            {
                var fileService = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
                await fileService.DeleteImportedAsync(SelectedTestCase.Id);
            }
            else
            {
                var testCaseService = scope.ServiceProvider.GetRequiredService<ITestCaseService>();
                await testCaseService.DeleteAsync(SelectedTestCase.Id);
            }

            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Der Testfall {TestCaseId} konnte nicht gelöscht werden.", SelectedTestCase.Id);
            _dialogService.ShowError("Fehler", "Der Testfall konnte nicht gelöscht werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Setzt alle Filter zurück und lädt die Liste neu.</summary>
    [RelayCommand]
    private async Task ResetFiltersAsync()
    {
        SearchText = string.Empty;
        SelectedStatusFilter = StatusFilters.FirstOrDefault();
        SelectedCategoryFilter = CategoryFilters.FirstOrDefault();
        SelectedTagFilter = TagFilters.FirstOrDefault();
        await RefreshAsync();
    }

    /// <summary>
    /// CanExecute-Bedingung für Bearbeiten/Kopieren/Löschen. Aktionen sind nur
    /// für interaktive Testfälle möglich – Datei-Testfälle werden ausschließlich
    /// unter „Import / Export" verwaltet.
    /// </summary>
    private bool HasSelection() => SelectedTestCase is { IsImported: false };

    /// <summary>
    /// CanExecute-Bedingung für das Öffnen der Detailansicht. Anders als
    /// <see cref="HasSelection"/> gilt sie für beide Arten von Testfällen,
    /// da auch Datei-Testfälle angesehen (nur nicht bearbeitet) werden können.
    /// </summary>
    private bool HasAnySelection() => SelectedTestCase is not null;

    /// <summary>Bildet einen Datei-Testfall auf ein Listenelement ab (nur Anzeige).</summary>
    /// <param name="imported">Übersichtsdaten des Datei-Testfalls.</param>
    private static TestCaseListItem MapImported(ImportedTestCaseListItem imported) => new()
    {
        Id = imported.Id,
        Name = imported.Number,
        Version = string.Empty,
        CreatedBy = imported.ImportedBy,
        LastChangedAt = imported.ImportedAt,
        IsImported = true
    };

    /// <summary>Verzögerte Suche bei Freitexteingabe (300 ms Debounce).</summary>
    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                    async () => await RefreshAsync());
            }
            catch (TaskCanceledException)
            {
                // Eingabe wurde fortgesetzt – veraltete Suche verwerfen.
            }
        }, token);
    }

    /// <summary>Sofortige Aktualisierung bei Statusfilter-Änderung.</summary>
    partial void OnSelectedStatusFilterChanged(EnumOption<TestCaseStatus?>? value)
        => _ = RefreshAsync();

    /// <summary>Sofortige Aktualisierung bei Kategoriefilter-Änderung.</summary>
    partial void OnSelectedCategoryFilterChanged(EnumOption<Guid?>? value)
        => _ = RefreshAsync();

    /// <summary>Sofortige Aktualisierung bei Tag-Filter-Änderung.</summary>
    partial void OnSelectedTagFilterChanged(EnumOption<Guid?>? value)
        => _ = RefreshAsync();

    /// <summary>
    /// Lädt die Auswahllisten für Status-, Kategorie- und Tag-Filter.
    /// Die aktuelle Auswahl bleibt – sofern noch vorhanden – erhalten.
    /// </summary>
    private async Task LoadFilterListsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();

            var categories = await categoryService.GetAllAsync();
            var tags = await tagService.GetAllAsync();

            var previousStatus = SelectedStatusFilter?.Value;
            var previousCategory = SelectedCategoryFilter?.Value;
            var previousTag = SelectedTagFilter?.Value;

            if (StatusFilters.Count == 0)
            {
                StatusFilters.Add(new EnumOption<TestCaseStatus?>(null, "Alle Status"));
                foreach (var option in OptionLists.Statuses)
                {
                    StatusFilters.Add(new EnumOption<TestCaseStatus?>(option.Value, option.Label));
                }
            }

            CategoryFilters.Clear();
            CategoryFilters.Add(new EnumOption<Guid?>(null, "Alle Kategorien"));
            foreach (var category in categories)
            {
                CategoryFilters.Add(new EnumOption<Guid?>(category.Id, category.Name));
            }

            TagFilters.Clear();
            TagFilters.Add(new EnumOption<Guid?>(null, "Alle Tags"));
            foreach (var tag in tags)
            {
                TagFilters.Add(new EnumOption<Guid?>(tag.Id, tag.Name));
            }

            // Auswahl ohne erneutes Auslösen der Suche wiederherstellen: Die
            // Backing-Fields werden bewusst direkt gesetzt, damit die
            // OnChanged-Handler (und damit ein weiterer Suchlauf) nicht anspringen.
#pragma warning disable MVVMTK0034
            _selectedStatusFilter = StatusFilters.FirstOrDefault(o => Equals(o.Value, previousStatus))
                                    ?? StatusFilters.First();
            _selectedCategoryFilter = CategoryFilters.FirstOrDefault(o => Equals(o.Value, previousCategory))
                                      ?? CategoryFilters.First();
            _selectedTagFilter = TagFilters.FirstOrDefault(o => Equals(o.Value, previousTag))
                                 ?? TagFilters.First();
#pragma warning restore MVVMTK0034
            OnPropertyChanged(nameof(SelectedStatusFilter));
            OnPropertyChanged(nameof(SelectedCategoryFilter));
            OnPropertyChanged(nameof(SelectedTagFilter));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Filterlisten konnten nicht geladen werden.");
        }
    }
}
