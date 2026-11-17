using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Filtering;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// Eine Zeile der Datenbankansicht: ein Testfall mit seinen Attributwerten.
/// <see cref="Values"/> ist deckungsgleich mit den Spalten-Überschriften
/// (Entität · Attribut) des ViewModels.
/// </summary>
public sealed class PivotRow
{
    /// <summary>Primärschlüssel des Testfalls (zum Öffnen der Bearbeitung).</summary>
    public Guid TestCaseId { get; }

    /// <summary>Testfallnummer (erste Spalte).</summary>
    public string Number { get; }

    /// <summary>Attributwerte in Spaltenreihenfolge (mehrere Zeilen je Entity mit „ | " verbunden).</summary>
    public string?[] Values { get; }

    /// <summary>Erzeugt eine Zeile aus Testfall, Nummer und Werten.</summary>
    /// <param name="testCaseId">Primärschlüssel des Testfalls.</param>
    /// <param name="number">Testfallnummer.</param>
    /// <param name="values">Werte in Spaltenreihenfolge.</param>
    public PivotRow(Guid testCaseId, string number, string?[] values)
    {
        TestCaseId = testCaseId;
        Number = number;
        Values = values;
    }
}

/// <summary>
/// Eine Zeile der Profilansicht: eine Schlüsselregel eines Abgleichprofils.
/// Profile ohne Regeln erscheinen mit leeren Regelspalten, damit sie in der
/// Übersicht nicht fehlen.
/// </summary>
public sealed class CompareProfileRow
{
    /// <summary>Name des Profils.</summary>
    public string Profile { get; init; } = string.Empty;

    /// <summary>Beschreibung des Profils.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Entität der Regel (leer, wenn das Profil keine Regeln hat).</summary>
    public string Entity { get; init; } = string.Empty;

    /// <summary>Schlüsselattribute der Regel, kommagetrennt.</summary>
    public string KeyColumns { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel des Reiters „Datenbank". Zeigt wahlweise:
/// <list type="bullet">
/// <item>die Testfälle – je Testfall eine Zeile, Spalten aus den vorhandenen
/// Entitäten und Attributen („Entität · Attribut"), filterbar über Ausdrücke
/// wie <c>tuer=2 AND nachname=Bongo</c> (Auswertung im Speicher);</item>
/// <item>die Abgleichprofile – je Schlüsselregel eine Zeile.</item>
/// </list>
/// Beide Ansichten sind lesend; gepflegt werden die Profile unter
/// „Einstellungen → Abgleich".
/// </summary>
public partial class DatabaseViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICompareProfileStore _profileStore;
    private readonly IDialogService _dialogService;
    private readonly ILogger<DatabaseViewModel> _logger;

    /// <summary>Attribute je Testfall (für die Filterauswertung).</summary>
    private readonly Dictionary<string, List<(string Column, string? Value)>> _byTestCase = new();

    /// <summary>Sichtbare Testfallnummern nach Filter (<c>null</c> = alle).</summary>
    private HashSet<string>? _visibleNumbers;

    /// <summary>Wird ausgelöst, wenn Daten (und damit die Spalten) neu geladen wurden.</summary>
    public event EventHandler? DataReloaded;

    /// <summary>Eine Zeile je Testfall.</summary>
    public ObservableCollection<PivotRow> Rows { get; } = [];

    /// <summary>Gefilterte Sicht auf die Testfälle.</summary>
    public ICollectionView RowsView { get; }

    /// <summary>Spalten-Überschriften (Entität · Attribut), deckungsgleich zu <see cref="PivotRow.Values"/>.</summary>
    public IReadOnlyList<string> ColumnHeaders { get; private set; } = [];

    /// <summary>Die aktuell in der Tabelle ausgewählte Zeile.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenSelectedCommand))]
    private PivotRow? _selectedRow;

    /// <summary>Eine Zeile je Schlüsselregel der Abgleichprofile.</summary>
    public ObservableCollection<CompareProfileRow> ProfileRows { get; } = [];

    /// <summary>
    /// Ob der Unterreiter „Abgleichprofile" gewählt ist. Die Ansicht bindet
    /// die Auswahl des Reiters beidseitig hierauf.
    /// </summary>
    [ObservableProperty]
    private bool _showProfiles;

    /// <summary>Filterausdruck (z. B. <c>tuer=2 AND nachname=Bongo</c>).</summary>
    [ObservableProperty]
    private string _filterText = string.Empty;

    /// <summary>Statuszeile mit Trefferzahl bzw. Fehlermeldung.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Ob die Statuszeile einen Fehler anzeigt (rote Darstellung).</summary>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>Erzeugt das ViewModel (Dependency Injection).</summary>
    /// <param name="scopeFactory">Fabrik für DI-Dienstbereiche.</param>
    /// <param name="profileStore">Speicher der Abgleichprofile (Datenbank).</param>
    /// <param name="dialogService">Dialogdienst (Fehlermeldungen).</param>
    /// <param name="logger">Logger für Fehlerprotokollierung.</param>
    public DatabaseViewModel(IServiceScopeFactory scopeFactory, ICompareProfileStore profileStore,
        IDialogService dialogService, ILogger<DatabaseViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _profileStore = profileStore;
        _dialogService = dialogService;
        _logger = logger;

        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.Filter = RowVisible;
    }

    /// <summary>Lädt die Daten der aktuell gewählten Ansicht neu.</summary>
    public Task LoadAsync() => ShowProfiles ? LoadProfilesAsync() : LoadTestCasesAsync();

    /// <summary>
    /// Lädt die Abgleichprofile und baut je Schlüsselregel eine Zeile auf.
    /// Profile ohne Regeln erhalten eine Zeile mit leeren Regelspalten.
    /// </summary>
    private async Task LoadProfilesAsync()
    {
        try
        {
            var profile = await _profileStore.GetAllAsync();

            ProfileRows.Clear();
            foreach (var eintrag in profile)
            {
                if (eintrag.KeyRules.Count == 0)
                {
                    ProfileRows.Add(new CompareProfileRow
                    {
                        Profile = eintrag.Name,
                        Description = eintrag.Description
                    });
                    continue;
                }

                foreach (var regel in eintrag.KeyRules)
                {
                    ProfileRows.Add(new CompareProfileRow
                    {
                        Profile = eintrag.Name,
                        Description = eintrag.Description,
                        Entity = regel.Entity,
                        KeyColumns = string.Join(", ", regel.KeyColumns)
                    });
                }
            }

            HasError = false;
            var regeln = profile.Sum(p => p.KeyRules.Count);
            StatusMessage = $"{profile.Count} Abgleichprofil(e), {regeln} Schlüsselregel(n).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Abgleichprofile konnten nicht geladen werden.");
            HasError = true;
            StatusMessage = "Die Abgleichprofile konnten nicht geladen werden: " + ex.Message;
        }
    }

    /// <summary>Lädt alle Attribute und baut die Pivot-Zeilen (ein Testfall je Zeile) auf.</summary>
    private async Task LoadTestCasesAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
            var flat = await service.GetDatabaseAttributesAsync();

            // 1. Spaltenreihenfolge: Entität + Attribut in Reihenfolge des ersten Auftretens.
            var columnKeys = new List<(string Entity, string Column)>();
            var seen = new HashSet<(string, string)>();
            foreach (var row in flat)
            {
                var key = (row.Entity, row.Column);
                if (seen.Add(key))
                {
                    columnKeys.Add(key);
                }
            }
            ColumnHeaders = columnKeys.Select(k => $"{k.Entity} · {k.Column}").ToList();

            // 2. Je Testfall eine Zeile; mehrere Entity-Zeilen werden je Attribut verbunden.
            _byTestCase.Clear();
            Rows.Clear();
            foreach (var group in flat.GroupBy(r => r.Number))
            {
                var attributes = group.ToList();
                _byTestCase[group.Key] = attributes.Select(r => (r.Column, r.Value)).ToList();

                var values = new string?[columnKeys.Count];
                for (var i = 0; i < columnKeys.Count; i++)
                {
                    var (entity, column) = columnKeys[i];
                    var matches = attributes
                        .Where(r => r.Entity == entity && r.Column == column)
                        .OrderBy(r => r.RowIndex)
                        .Select(r => r.Value);
                    var joined = string.Join(" | ", matches);
                    values[i] = joined.Length == 0 ? null : joined;
                }
                Rows.Add(new PivotRow(attributes[0].TestCaseId, group.Key, values));
            }

            _visibleNumbers = null;
            HasError = false;
            RowsView.Refresh();
            UpdateStatus();
            DataReloaded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Datenbankansicht konnte nicht geladen werden.");
            _dialogService.ShowError("Fehler", "Die Attribute konnten nicht geladen werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Wendet den Filterausdruck an.</summary>
    [RelayCommand]
    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            ResetFilter();
            return;
        }

        if (!AttributeFilter.TryParse(FilterText, out var filter, out var error))
        {
            HasError = true;
            StatusMessage = "Ungültiger Filter: " + error;
            return;
        }

        _visibleNumbers = _byTestCase
            .Where(kv => filter!.Matches(kv.Value))
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        RowsView.Refresh();
        UpdateStatus();
    }

    /// <summary>Setzt den Filter zurück (alle Testfälle anzeigen).</summary>
    [RelayCommand]
    private void ResetFilter()
    {
        FilterText = string.Empty;
        _visibleNumbers = null;
        HasError = false;
        RowsView.Refresh();
        UpdateStatus();
    }

    /// <summary>Lädt die Ansicht neu.</summary>
    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();


    /// <summary>
    /// Öffnet den ausgewählten Testfall zum Bearbeiten (Doppelklick). Nach dem
    /// Speichern wird die Ansicht neu geladen, damit die geänderten Werte und
    /// gegebenenfalls neue Spalten sichtbar werden.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task OpenSelectedAsync()
    {
        if (SelectedRow is null)
        {
            return;
        }

        try
        {
            if (await _dialogService.ShowImportedTestCaseEditorAsync(SelectedRow.TestCaseId))
            {
                await LoadAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Der Testfall {TestCaseId} konnte nicht zum Bearbeiten "
                + "geöffnet werden.", SelectedRow.TestCaseId);
            _dialogService.ShowError("Fehler", "Der Testfall konnte nicht geöffnet werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>CanExecute-Bedingung für das Öffnen: eine Zeile muss gewählt sein.</summary>
    private bool HasSelection() => SelectedRow is not null && !ShowProfiles;

    /// <summary>Lädt beim Wechsel des Unterreiters die passenden Daten nach.</summary>
    partial void OnShowProfilesChanged(bool value)
    {
        OpenSelectedCommand.NotifyCanExecuteChanged();
        _ = LoadAsync();
    }

    /// <summary>Filterprädikat der Sammlungssicht.</summary>
    /// <param name="item">Die zu prüfende Zeile.</param>
    private bool RowVisible(object item)
        => _visibleNumbers is null
           || (item is PivotRow row && _visibleNumbers.Contains(row.Number));

    /// <summary>Aktualisiert die Statuszeile mit Trefferzahlen.</summary>
    private void UpdateStatus()
    {
        HasError = false;
        var testCases = RowsView.Cast<PivotRow>().Count();
        StatusMessage = _visibleNumbers is null
            ? $"{testCases} Testfall/-fälle, {ColumnHeaders.Count} Attributspalte(n)."
            : $"Filter aktiv: {testCases} von {_byTestCase.Count} Testfall/-fällen.";
    }
}
