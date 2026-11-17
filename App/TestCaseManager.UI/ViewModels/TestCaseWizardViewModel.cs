using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// ViewModel des Assistenten zum Anlegen eines Datei-Testfalls in drei
/// Schritten: (1) Entities wählen, (2) Attribute wählen, (3) Werte eintragen.
/// Als Schablone dienen die bereits in der Datenbank vorhandenen Entities und
/// Attribute; eigene Ergänzungen sind möglich.
/// </summary>
public partial class TestCaseWizardViewModel : ObservableObject
{
    private readonly ITestCaseFileService _fileService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<TestCaseWizardViewModel> _logger;

    /// <summary>Bereits vergebene Testfallnummern (zur Eindeutigkeitsprüfung).</summary>
    private readonly HashSet<string> _existingNumbers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Signatur der zuletzt für den Werteschritt aufgebauten Auswahl.</summary>
    private string _lastValueSignature = string.Empty;

    /// <summary>Wird ausgelöst, wenn der Assistent geschlossen werden soll.</summary>
    public event EventHandler<bool>? RequestClose;

    /// <summary>Aktueller Schritt (1 = Entities, 2 = Attribute, 3 = Werte).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1), nameof(IsStep2), nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(StepTitle), nameof(CanGoBack), nameof(IsLastStep))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand), nameof(NextCommand), nameof(FinishCommand))]
    private int _currentStep = 1;

    /// <summary>Testfallnummer (fachlicher Schlüssel).</summary>
    [ObservableProperty]
    private string _number = string.Empty;

    /// <summary>Bezeichnung der erzeugten Eingabedatei.</summary>
    [ObservableProperty]
    private string _designation = "Eingabe";

    /// <summary>Name einer neu hinzuzufügenden Entity (Eingabefeld).</summary>
    [ObservableProperty]
    private string _newEntityName = string.Empty;

    /// <summary>Ob gerade gespeichert wird.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Alle wählbaren Entities (aus der Schablone und ergänzte).</summary>
    public ObservableCollection<EntityPick> Entities { get; } = [];

    /// <summary>Die Entity-Blöcke des Werteschritts.</summary>
    public ObservableCollection<ValueEntityBlock> ValueBlocks { get; } = [];

    /// <summary>Ob Schritt 1 (Entities) aktiv ist.</summary>
    public bool IsStep1 => CurrentStep == 1;

    /// <summary>Ob Schritt 2 (Attribute) aktiv ist.</summary>
    public bool IsStep2 => CurrentStep == 2;

    /// <summary>Ob Schritt 3 (Werte) aktiv ist.</summary>
    public bool IsStep3 => CurrentStep == 3;

    /// <summary>Ob der letzte Schritt erreicht ist.</summary>
    public bool IsLastStep => CurrentStep == 3;

    /// <summary>Ob ein Schritt zurück möglich ist.</summary>
    public bool CanGoBack => CurrentStep > 1;

    /// <summary>Überschrift des aktuellen Schritts.</summary>
    public string StepTitle => CurrentStep switch
    {
        1 => "Schritt 1 von 3 – Entities auswählen",
        2 => "Schritt 2 von 3 – Attribute auswählen",
        3 => "Schritt 3 von 3 – Werte eintragen",
        _ => string.Empty
    };

    /// <summary>Die im Attributschritt sichtbaren, ausgewählten Entities.</summary>
    public IEnumerable<EntityPick> SelectedEntities => Entities.Where(e => e.IsSelected);

    /// <summary>Erzeugt das ViewModel (Dependency Injection).</summary>
    /// <param name="fileService">Dienst für Schablone und Speichern.</param>
    /// <param name="dialogService">Dialogdienst (Meldungen).</param>
    /// <param name="logger">Logger für Fehlerprotokollierung.</param>
    public TestCaseWizardViewModel(ITestCaseFileService fileService, IDialogService dialogService,
        ILogger<TestCaseWizardViewModel> logger)
    {
        _fileService = fileService;
        _dialogService = dialogService;
        _logger = logger;
    }

    /// <summary>Lädt die Schablone (Entities/Attribute) und vorhandene Nummern.</summary>
    public async Task LoadAsync()
    {
        try
        {
            var templates = await _fileService.GetEntityTemplatesAsync();
            Entities.Clear();
            foreach (var template in templates)
            {
                Entities.Add(new EntityPick(template.Name, template.Attributes));
            }

            var imported = await _fileService.GetImportedAsync();
            _existingNumbers.Clear();
            foreach (var item in imported)
            {
                _existingNumbers.Add(item.Number);
            }

            if (string.IsNullOrWhiteSpace(Number))
            {
                Number = SuggestNextNumber();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Schablone für den Testfall-Assistenten konnte nicht geladen werden.");
            _dialogService.ShowError("Fehler", "Die Vorlage aus der Datenbank konnte nicht geladen werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Fügt eine eigene, in der Schablone fehlende Entity hinzu.</summary>
    [RelayCommand]
    private void AddEntity()
    {
        var name = NewEntityName.Trim();
        if (name.Length == 0)
        {
            return;
        }

        var existing = Entities.FirstOrDefault(
            e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            existing = new EntityPick(name, []);
            Entities.Add(existing);
        }
        existing.IsSelected = true;

        NewEntityName = string.Empty;
    }

    /// <summary>Wechselt einen Schritt vor (mit Prüfung des aktuellen Schritts).</summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (CurrentStep == 1 && !ValidateStep1())
        {
            return;
        }

        if (CurrentStep == 2)
        {
            if (!ValidateStep2())
            {
                return;
            }
            BuildValueStep();
        }

        if (CurrentStep < 3)
        {
            CurrentStep++;
            OnPropertyChanged(nameof(SelectedEntities));
        }
    }

    /// <summary>Wechselt einen Schritt zurück.</summary>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
        }
    }

    /// <summary>Legt den Testfall an und schließt den Assistenten bei Erfolg.</summary>
    [RelayCommand(CanExecute = nameof(IsLastStep))]
    private async Task FinishAsync()
    {
        if (IsBusy || !ValidateStep3())
        {
            return;
        }

        var draft = new NewFileTestCase
        {
            Number = Number.Trim(),
            Designation = Designation.Trim(),
            Entities = ValueBlocks.Select(block => new NewFileEntity
            {
                Name = block.EntityName,
                Columns = block.Columns,
                Rows = block.Rows.Select(row => new NewFileRow
                {
                    Values = row.Cells.Select(c => (string?)c.Value).ToList()
                }).ToList()
            }).ToList()
        };

        IsBusy = true;
        try
        {
            await _fileService.CreateFileTestCaseAsync(draft);
            RequestClose?.Invoke(this, true);
        }
        catch (InvalidOperationException ex)
        {
            // Fachliche Prüfung (z. B. Nummer bereits vergeben).
            _dialogService.ShowError("Testfall nicht angelegt", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Der Datei-Testfall konnte nicht angelegt werden.");
            _dialogService.ShowError("Fehler", "Der Testfall konnte nicht angelegt werden:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Bricht den Assistenten ab.</summary>
    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);

    /// <summary>Der Vorwärts-Button ist nur bis zum vorletzten Schritt aktiv.</summary>
    private bool CanGoNext() => CurrentStep < 3;

    /// <summary>Prüft die Angaben aus Schritt 1 (Nummer und Entity-Auswahl).</summary>
    private bool ValidateStep1()
    {
        var number = Number.Trim();
        if (number.Length == 0)
        {
            _dialogService.ShowError("Angabe fehlt", "Bitte eine Testfallnummer eingeben.");
            return false;
        }

        if (_existingNumbers.Contains(number))
        {
            _dialogService.ShowError("Nummer vergeben",
                $"Die Testfallnummer \"{number}\" ist bereits vorhanden. Bitte eine andere wählen.");
            return false;
        }

        if (!Entities.Any(e => e.IsSelected))
        {
            _dialogService.ShowError("Keine Entity gewählt",
                "Bitte mindestens eine Entity auswählen.");
            return false;
        }

        return true;
    }

    /// <summary>Prüft, dass jede gewählte Entity mindestens ein Attribut hat.</summary>
    private bool ValidateStep2()
    {
        var ohneAttribut = SelectedEntities
            .Where(e => e.SelectedAttributes.Count == 0)
            .Select(e => e.Name)
            .ToList();

        if (ohneAttribut.Count > 0)
        {
            _dialogService.ShowError("Attribute fehlen",
                "Bitte für jede gewählte Entity mindestens ein Attribut auswählen. Betroffen:"
                + Environment.NewLine + string.Join(", ", ohneAttribut));
            return false;
        }

        return true;
    }

    /// <summary>Prüft, dass mindestens ein Wert eingetragen wurde.</summary>
    private bool ValidateStep3()
    {
        var hatWert = ValueBlocks
            .SelectMany(b => b.Rows)
            .SelectMany(r => r.Cells)
            .Any(c => !string.IsNullOrWhiteSpace(c.Value));

        if (!hatWert)
        {
            _dialogService.ShowError("Keine Werte",
                "Bitte mindestens einen Wert eintragen.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Baut die Entity-Blöcke des Werteschritts auf. Bleibt die Auswahl seit
    /// dem letzten Aufbau unverändert, werden die bereits eingetragenen Werte
    /// behalten (kein erneutes Zurücksetzen bei Vor/Zurück).
    /// </summary>
    private void BuildValueStep()
    {
        var signature = string.Join("|", SelectedEntities
            .Select(e => e.Name + ":" + string.Join(",", e.SelectedAttributes)));

        if (signature == _lastValueSignature && ValueBlocks.Count > 0)
        {
            return;
        }

        ValueBlocks.Clear();
        foreach (var entity in SelectedEntities)
        {
            ValueBlocks.Add(new ValueEntityBlock(entity.Name, entity.SelectedAttributes));
        }
        _lastValueSignature = signature;
    }

    /// <summary>Schlägt die nächste freie, vierstellige Testfallnummer vor.</summary>
    private string SuggestNextNumber()
    {
        var max = _existingNumbers
            .Select(n => int.TryParse(n, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value : 0)
            .DefaultIfEmpty(0)
            .Max();
        return (max + 1).ToString("D4", CultureInfo.InvariantCulture);
    }
}
