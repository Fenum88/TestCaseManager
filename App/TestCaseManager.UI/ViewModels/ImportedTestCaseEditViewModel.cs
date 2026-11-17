using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// Eine bearbeitbare Zeile des Attributeditors. Hält neben dem aktuellen Wert
/// auch den ursprünglich geladenen, sodass sich Änderungen erkennen
/// (<see cref="IsChanged"/>) und einzeln zurücknehmen lassen.
/// </summary>
public partial class AttributeEditRow : ObservableObject
{
    /// <summary>Der beim Öffnen geladene Wert (Grundlage für den Vergleich).</summary>
    private readonly string _originalValue;

    /// <summary>Primärschlüssel des zugrunde liegenden Wertes.</summary>
    public Guid ValueId { get; }

    /// <summary>Gruppenüberschrift (Abschnitt · Datei · Entity).</summary>
    public string GroupHeader { get; }

    /// <summary>Name der Entity/Tabelle.</summary>
    public string EntityName { get; }

    /// <summary>Nullbasierter Zeilenindex innerhalb der Entity.</summary>
    public int RowIndex { get; }

    /// <summary>Spaltenname (z. B. "tuer").</summary>
    public string ColumnName { get; }

    /// <summary>Datentyp, wie er beim Laden abgeleitet war.</summary>
    public string DataType { get; }

    /// <summary>Der bearbeitbare Wert.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChanged))]
    [NotifyPropertyChangedFor(nameof(ChangeMarker))]
    [NotifyPropertyChangedFor(nameof(OriginalDisplay))]
    private string _value;

    /// <summary>Ob der Wert gegenüber dem geladenen Stand abweicht.</summary>
    public bool IsChanged => !string.Equals(Value, _originalValue, StringComparison.Ordinal);

    /// <summary>Sichtbare Markierung geänderter Zeilen (ohne Farbcodierung lesbar).</summary>
    public string ChangeMarker => IsChanged ? "●" : string.Empty;

    /// <summary>Zeigt bei Änderungen den ursprünglichen Wert an.</summary>
    public string OriginalDisplay => IsChanged ? _originalValue : string.Empty;

    /// <summary>Erzeugt eine Bearbeitungszeile aus einem geladenen Attribut.</summary>
    /// <param name="attribute">Das geladene Attribut.</param>
    public AttributeEditRow(DataAttributeItem attribute)
    {
        ValueId = attribute.ValueId;
        GroupHeader = attribute.GroupHeader;
        EntityName = attribute.EntityName;
        RowIndex = attribute.RowIndex;
        ColumnName = attribute.ColumnName;
        DataType = attribute.DataType;

        _originalValue = attribute.Value ?? string.Empty;
        _value = _originalValue;
    }

    /// <summary>Verwirft die Änderung dieser Zeile.</summary>
    public void Revert() => Value = _originalValue;
}

/// <summary>
/// ViewModel des Bearbeitungsfensters eines Datei-Testfalls. Zeigt alle
/// gespeicherten Attribute gruppiert an und schreibt geänderte Werte zurück
/// in die Datenbank. Gespeichert werden ausschließlich die tatsächlich
/// geänderten Werte.
/// </summary>
public partial class ImportedTestCaseEditViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ImportedTestCaseEditViewModel> _logger;
    private readonly Guid _testCaseId;

    /// <summary>Meldet dem Fenster, dass es geschlossen werden soll (<c>true</c> = gespeichert).</summary>
    public event EventHandler<bool>? RequestClose;

    /// <summary>Alle Attribute des Testfalls als bearbeitbare Zeilen.</summary>
    public ObservableCollection<AttributeEditRow> Attributes { get; } = [];

    /// <summary>Gruppierte und filterbare Sicht auf die Attribute.</summary>
    public ICollectionView AttributesView { get; }

    /// <summary>Fenstertitel mit der Testfallnummer.</summary>
    public string WindowTitle => $"Testfall {Number} – bearbeiten";

    /// <summary>Testfallnummer.</summary>
    [ObservableProperty]
    private string _number = string.Empty;

    /// <summary>Herkunfts- und Änderungsangaben für die Kopfzeile.</summary>
    [ObservableProperty]
    private string _auditInfo = string.Empty;

    /// <summary>Die aktuell ausgewählte Zeile.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RevertRowCommand))]
    private AttributeEditRow? _selectedRow;

    /// <summary>Freitextfilter über Entity, Spalte und Wert.</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>Statuszeile mit Hinweisen und Fehlermeldungen.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Ob die Statuszeile einen Fehler anzeigt (rote Darstellung).</summary>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>Anzahl der geänderten Werte (Statuszeile und Speichern-Bedingung).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertAllCommand))]
    private int _changedCount;

    /// <summary>Erzeugt das ViewModel (Dependency Injection).</summary>
    /// <param name="scopeFactory">Fabrik für DI-Dienstbereiche.</param>
    /// <param name="dialogService">Dialogdienst für Meldungen.</param>
    /// <param name="logger">Logger für Fehlerprotokollierung.</param>
    /// <param name="testCaseId">Primärschlüssel des zu bearbeitenden Testfalls.</param>
    public ImportedTestCaseEditViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogService,
        ILogger<ImportedTestCaseEditViewModel> logger, Guid testCaseId)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _logger = logger;
        _testCaseId = testCaseId;

        AttributesView = CollectionViewSource.GetDefaultView(Attributes);
        AttributesView.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(AttributeEditRow.GroupHeader)));
        AttributesView.Filter = MatchesSearch;
    }

    /// <summary>Lädt den Testfall mit allen Attributen.</summary>
    /// <returns><c>true</c>, wenn der Testfall geladen werden konnte.</returns>
    public async Task<bool> LoadAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
            var detail = await service.GetImportedDetailAsync(_testCaseId);

            if (detail is null)
            {
                _dialogService.ShowError("Testfall nicht gefunden",
                    "Der Testfall existiert nicht mehr in der Datenbank. "
                    + "Bitte die Ansicht aktualisieren.");
                return false;
            }

            Number = detail.Number;
            AuditInfo = $"Datei-Testfall · zuletzt geändert von {detail.ImportedBy} am "
                        + $"{detail.ImportedAt.ToLocalTime():dd.MM.yyyy HH:mm}";
            OnPropertyChanged(nameof(WindowTitle));

            Attributes.Clear();
            foreach (var attribute in detail.Attributes)
            {
                var row = new AttributeEditRow(attribute);
                // Jede Wertänderung meldet sich, damit Zähler und Befehle folgen.
                row.PropertyChanged += OnRowPropertyChanged;
                Attributes.Add(row);
            }

            ChangedCount = 0;
            HasError = false;
            StatusMessage = $"{Attributes.Count} Attribut(e) geladen.";
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Der Testfall {TestCaseId} konnte nicht geladen werden.", _testCaseId);
            _dialogService.ShowError("Fehler", "Die Attribute konnten nicht geladen werden:"
                + Environment.NewLine + ex.Message);
            return false;
        }
    }

    /// <summary>Schreibt die geänderten Werte in die Datenbank.</summary>
    [RelayCommand(CanExecute = nameof(HasChanges))]
    private async Task SaveAsync()
    {
        var updates = Attributes
            .Where(row => row.IsChanged)
            .Select(row => new AttributeValueUpdate(row.ValueId, row.Value))
            .ToList();

        if (updates.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestCaseFileService>();
            var changed = await service.UpdateAttributeValuesAsync(_testCaseId, updates);

            _logger.LogInformation("Testfall {TestCaseId}: {Count} Wert(e) über die Oberfläche geändert.",
                _testCaseId, changed);

            RequestClose?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Änderungen am Testfall {TestCaseId} konnten nicht "
                + "gespeichert werden.", _testCaseId);
            HasError = true;
            StatusMessage = "Speichern fehlgeschlagen: " + ex.Message;
            _dialogService.ShowError("Fehler", "Die Änderungen konnten nicht gespeichert werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Schließt das Fenster; fragt bei ungespeicherten Änderungen nach.</summary>
    [RelayCommand]
    private void Cancel()
    {
        if (ChangedCount > 0 && !_dialogService.Confirm("Änderungen verwerfen",
                $"{ChangedCount} geänderte(r) Wert(e) wurden noch nicht gespeichert. "
                + "Sollen die Änderungen verworfen werden?"))
        {
            return;
        }

        RequestClose?.Invoke(this, false);
    }

    /// <summary>Nimmt die Änderung der ausgewählten Zeile zurück.</summary>
    [RelayCommand(CanExecute = nameof(HasChangedSelection))]
    private void RevertRow() => SelectedRow?.Revert();

    /// <summary>Nimmt alle Änderungen zurück.</summary>
    [RelayCommand(CanExecute = nameof(HasChanges))]
    private void RevertAll()
    {
        foreach (var row in Attributes.Where(r => r.IsChanged).ToList())
        {
            row.Revert();
        }
    }

    /// <summary>Setzt den Freitextfilter zurück.</summary>
    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    /// <summary>Ob es ungespeicherte Änderungen gibt.</summary>
    private bool HasChanges() => ChangedCount > 0;

    /// <summary>Ob die ausgewählte Zeile geändert wurde.</summary>
    private bool HasChangedSelection() => SelectedRow is { IsChanged: true };

    /// <summary>Aktualisiert den Änderungszähler, sobald ein Wert bearbeitet wurde.</summary>
    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AttributeEditRow.Value))
        {
            return;
        }

        ChangedCount = Attributes.Count(row => row.IsChanged);
        RevertRowCommand.NotifyCanExecuteChanged();

        HasError = false;
        StatusMessage = ChangedCount == 0
            ? $"{Attributes.Count} Attribut(e) geladen."
            : $"{ChangedCount} von {Attributes.Count} Wert(en) geändert – noch nicht gespeichert.";
    }

    /// <summary>Wendet den Filter bei jeder Eingabe erneut an.</summary>
    partial void OnSearchTextChanged(string value) => AttributesView.Refresh();

    /// <summary>
    /// Filterprädikat: leerer Suchtext zeigt alles, sonst wird in Entity-Name,
    /// Spaltenname und Wert ohne Beachtung der Groß-/Kleinschreibung gesucht.
    /// </summary>
    /// <param name="item">Die zu prüfende Zeile.</param>
    private bool MatchesSearch(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        if (item is not AttributeEditRow row)
        {
            return false;
        }

        var search = SearchText.Trim();
        return row.EntityName.Contains(search, StringComparison.OrdinalIgnoreCase)
               || row.ColumnName.Contains(search, StringComparison.OrdinalIgnoreCase)
               || row.Value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
