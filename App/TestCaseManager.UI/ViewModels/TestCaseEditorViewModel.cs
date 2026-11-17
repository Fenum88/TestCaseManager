using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TestCaseManager.Application.Common.Validation;
using TestCaseManager.Application.Services;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;
using TestCaseManager.UI.Models;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// ViewModel des Testfall-Editors (Detailansicht).
/// Arbeitet auf einer von der Arbeitseinheit nachverfolgten Entität:
/// Der Dialog besitzt einen eigenen DI-Scope, alle Änderungen werden
/// erst beim Klick auf "Speichern" validiert und atomar persistiert.
/// </summary>
public partial class TestCaseEditorViewModel : ObservableObject
{
    /// <summary>Maximal zulässige Größe eines Dateianhangs (25 MB).</summary>
    private const long MaxAttachmentSize = 25 * 1024 * 1024;

    private readonly ITestCaseService _testCaseService;
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;
    private readonly Guid? _testCaseId;

    /// <summary>Die bearbeitete Entität (neu oder von der Arbeitseinheit nachverfolgt).</summary>
    private TestCase _testCase = new();

    /// <summary>
    /// Wird ausgelöst, wenn der Dialog geschlossen werden soll.
    /// <c>true</c> = es wurde gespeichert, <c>false</c> = Abbruch.
    /// </summary>
    public event EventHandler<bool>? RequestClose;

    /// <summary>Fenstertitel ("Neuer Testfall" bzw. Name des Testfalls).</summary>
    [ObservableProperty]
    private string _windowTitle = "Neuer Testfall";

    /// <summary>Name des Testfalls (Pflichtfeld, eindeutig).</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Beschreibung des Testfalls.</summary>
    [ObservableProperty]
    private string? _description;

    /// <summary>Fachliche Version.</summary>
    [ObservableProperty]
    private string _version = "1.0";

    /// <summary>Gewählter Status.</summary>
    [ObservableProperty]
    private EnumOption<TestCaseStatus> _selectedStatus = OptionLists.Statuses[0];

    /// <summary>Kategoriename (editierbar; unbekannte Kategorien werden angelegt).</summary>
    [ObservableProperty]
    private string _categoryName = string.Empty;

    /// <summary>Kommagetrennte Tags (unbekannte Tags werden angelegt).</summary>
    [ObservableProperty]
    private string _tagsText = string.Empty;

    /// <summary>Ersteller und Erstellungszeitpunkt (nur Anzeige).</summary>
    [ObservableProperty]
    private string _auditInfo = string.Empty;

    /// <summary>Gibt an, ob gerade geladen oder gespeichert wird.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Auswahlliste aller Status.</summary>
    public IReadOnlyList<EnumOption<TestCaseStatus>> StatusOptions => OptionLists.Statuses;

    /// <summary>Auswahlliste aller Parameter-Datentypen (für Grid-Spalten).</summary>
    public IReadOnlyList<EnumOption<ParameterDataType>> DataTypeOptions => OptionLists.DataTypes;

    /// <summary>Vorschläge für die Kategorieauswahl.</summary>
    public ObservableCollection<string> CategorySuggestions { get; } = [];

    /// <summary>Eingabeparameter des Testfalls (direkt editierbar).</summary>
    public ObservableCollection<TestParameter> Parameters { get; } = [];

    /// <summary>Erwartete Ergebnisse des Testfalls (direkt editierbar).</summary>
    public ObservableCollection<ExpectedResult> ExpectedResults { get; } = [];

    /// <summary>Dateianhänge des Testfalls.</summary>
    public ObservableCollection<Attachment> Attachments { get; } = [];

    /// <summary>Änderungshistorie (nur Anzeige, neueste zuerst).</summary>
    public ObservableCollection<TestCaseHistory> History { get; } = [];

    /// <summary>Erzeugt das ViewModel für einen neuen oder bestehenden Testfall.</summary>
    /// <param name="testCaseService">Testfall-Service (scoped, gehört zum Dialog-Scope).</param>
    /// <param name="categoryService">Kategorien-Service für die Vorschlagsliste.</param>
    /// <param name="dialogService">Dialogdienst für Meldungen und Dateiauswahl.</param>
    /// <param name="testCaseId"><c>null</c> für "Neu", sonst die Id des zu bearbeitenden Testfalls.</param>
    public TestCaseEditorViewModel(ITestCaseService testCaseService, ICategoryService categoryService,
        IDialogService dialogService, Guid? testCaseId)
    {
        _testCaseService = testCaseService;
        _categoryService = categoryService;
        _dialogService = dialogService;
        _testCaseId = testCaseId;
    }

    /// <summary>
    /// Lädt den Testfall (bzw. legt eine neue Entität an) und befüllt
    /// alle Anzeigelisten. Wird vom Fenster nach dem Öffnen aufgerufen.
    /// </summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            foreach (var category in await _categoryService.GetAllAsync())
            {
                CategorySuggestions.Add(category.Name);
            }

            if (_testCaseId is null)
            {
                _testCase = _testCaseService.CreateNew();
                WindowTitle = "Neuer Testfall";
            }
            else
            {
                _testCase = await _testCaseService.GetWithDetailsAsync(_testCaseId.Value)
                    ?? throw new InvalidOperationException("Der Testfall wurde nicht gefunden.");
                WindowTitle = $"Testfall bearbeiten – {_testCase.Name}";
            }

            ApplyEntityToViewModel();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError("Fehler", "Der Testfall konnte nicht geladen werden:"
                + Environment.NewLine + ex.Message);
            RequestClose?.Invoke(this, false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Fügt einen neuen, leeren Parameter hinzu.</summary>
    [RelayCommand]
    private void AddParameter()
        => Parameters.Add(new TestParameter
        {
            TestCaseId = _testCase.Id,
            DataType = ParameterDataType.Text,
            SortOrder = Parameters.Count
        });

    /// <summary>Entfernt den übergebenen Parameter.</summary>
    /// <param name="parameter">Die zu entfernende Zeile.</param>
    [RelayCommand]
    private void RemoveParameter(TestParameter? parameter)
    {
        if (parameter is not null)
        {
            Parameters.Remove(parameter);
        }
    }

    /// <summary>Fügt ein neues, leeres erwartetes Ergebnis hinzu.</summary>
    [RelayCommand]
    private void AddExpectedResult()
        => ExpectedResults.Add(new ExpectedResult
        {
            TestCaseId = _testCase.Id,
            DataType = ParameterDataType.Text,
            SortOrder = ExpectedResults.Count
        });

    /// <summary>Entfernt das übergebene erwartete Ergebnis.</summary>
    /// <param name="result">Die zu entfernende Zeile.</param>
    [RelayCommand]
    private void RemoveExpectedResult(ExpectedResult? result)
    {
        if (result is not null)
        {
            ExpectedResults.Remove(result);
        }
    }

    /// <summary>Fügt eine Datei als Anhang hinzu (Dokumente, Bilder, PDFs).</summary>
    [RelayCommand]
    private void AddAttachment()
    {
        var path = _dialogService.ShowOpenFileDialog(
            "Alle Dateien|*.*|PDF|*.pdf|Bilder|*.png;*.jpg;*.jpeg|Dokumente|*.docx;*.xlsx;*.txt");
        if (path is null)
        {
            return;
        }

        try
        {
            var info = new FileInfo(path);
            if (info.Length > MaxAttachmentSize)
            {
                _dialogService.ShowError("Anhang zu groß",
                    "Die Datei überschreitet die maximale Anhangsgröße von 25 MB.");
                return;
            }

            Attachments.Add(new Attachment
            {
                TestCaseId = _testCase.Id,
                FileName = info.Name,
                ContentType = GetMimeType(info.Extension),
                FileSize = info.Length,
                Content = File.ReadAllBytes(path)
            });
        }
        catch (IOException ex)
        {
            _dialogService.ShowError("Fehler", "Die Datei konnte nicht gelesen werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Entfernt den übergebenen Anhang.</summary>
    /// <param name="attachment">Der zu entfernende Anhang.</param>
    [RelayCommand]
    private void RemoveAttachment(Attachment? attachment)
    {
        if (attachment is not null)
        {
            Attachments.Remove(attachment);
        }
    }

    /// <summary>Speichert den übergebenen Anhang als Datei auf dem Rechner.</summary>
    /// <param name="attachment">Der zu exportierende Anhang.</param>
    [RelayCommand]
    private void ExportAttachment(Attachment? attachment)
    {
        if (attachment is null)
        {
            return;
        }

        var path = _dialogService.ShowSaveFileDialog(attachment.FileName, "Alle Dateien|*.*");
        if (path is null)
        {
            return;
        }

        try
        {
            File.WriteAllBytes(path, attachment.Content);
            _dialogService.ShowInfo("Anhang gespeichert", $"Die Datei wurde gespeichert:\n{path}");
        }
        catch (IOException ex)
        {
            _dialogService.ShowError("Fehler", "Die Datei konnte nicht geschrieben werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>
    /// Validiert und speichert den Testfall. Bei Validierungsfehlern
    /// bleibt der Dialog geöffnet und zeigt alle Fehler gesammelt an.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            ApplyViewModelToEntity();

            var tagNames = TagsText
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            await _testCaseService.SaveAsync(_testCase, CategoryName, tagNames);
            RequestClose?.Invoke(this, true);
        }
        catch (ValidationException ex)
        {
            _dialogService.ShowError("Eingaben prüfen", ex.Message);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError("Fehler", "Der Testfall konnte nicht gespeichert werden:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Schließt den Dialog ohne zu speichern.</summary>
    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);

    /// <summary>Befüllt die ViewModel-Eigenschaften aus der geladenen Entität.</summary>
    private void ApplyEntityToViewModel()
    {
        Name = _testCase.Name;
        Description = _testCase.Description;
        Version = _testCase.Version;
        SelectedStatus = OptionLists.Statuses.First(o => o.Value == _testCase.Status);
        CategoryName = _testCase.Category?.Name ?? string.Empty;
        TagsText = string.Join(", ", _testCase.Tags.Select(t => t.Name).OrderBy(n => n));

        AuditInfo = _testCase.CreatedBy.Length == 0
            ? string.Empty
            : $"Erstellt von {_testCase.CreatedBy} am {_testCase.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}"
              + (_testCase.ModifiedAt is null
                  ? string.Empty
                  : $" · Zuletzt geändert von {_testCase.ModifiedBy} am {_testCase.ModifiedAt.Value.ToLocalTime():dd.MM.yyyy HH:mm}");

        Parameters.Clear();
        foreach (var parameter in _testCase.Parameters)
        {
            Parameters.Add(parameter);
        }

        ExpectedResults.Clear();
        foreach (var result in _testCase.ExpectedResults)
        {
            ExpectedResults.Add(result);
        }

        Attachments.Clear();
        foreach (var attachment in _testCase.Attachments)
        {
            Attachments.Add(attachment);
        }

        History.Clear();
        foreach (var entry in _testCase.History)
        {
            History.Add(entry);
        }
    }

    /// <summary>
    /// Überträgt die ViewModel-Eigenschaften zurück in die Entität.
    /// Entfernte Zeilen werden dadurch zu "Waisen" und von Entity
    /// Framework Core beim Speichern automatisch gelöscht.
    /// </summary>
    private void ApplyViewModelToEntity()
    {
        _testCase.Name = Name;
        _testCase.Description = Description;
        _testCase.Version = Version;
        _testCase.Status = SelectedStatus.Value;

        _testCase.Parameters.Clear();
        var sortOrder = 0;
        foreach (var parameter in Parameters)
        {
            parameter.SortOrder = sortOrder++;
            _testCase.Parameters.Add(parameter);
        }

        _testCase.ExpectedResults.Clear();
        sortOrder = 0;
        foreach (var result in ExpectedResults)
        {
            result.SortOrder = sortOrder++;
            _testCase.ExpectedResults.Add(result);
        }

        _testCase.Attachments.Clear();
        foreach (var attachment in Attachments)
        {
            _testCase.Attachments.Add(attachment);
        }
    }

    /// <summary>Ermittelt den MIME-Typ anhand der Dateierweiterung.</summary>
    /// <param name="extension">Dateierweiterung inklusive Punkt (z. B. ".pdf").</param>
    private static string GetMimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };
}
