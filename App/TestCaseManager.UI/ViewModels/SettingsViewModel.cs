using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// ViewModel des Einstellungen-Reiters. Hier bindet der Anwender
/// Berechnungssysteme ein, entfernt sie und gibt vor, wie sie aufgerufen
/// werden (Programm, Skript, Version, Aufrufmuster). Die Systeme werden über
/// den <see cref="ICalculationSystemStore"/> dauerhaft gespeichert.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ICalculationSystemStore _store;
    private readonly ICompareProfileStore _compareProfileStore;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SettingsViewModel> _logger;

    /// <summary>Ordner, gegen den relative Skriptpfade aufgelöst werden.</summary>
    private readonly string _calculationFolder;

    /// <summary>Die bearbeitbaren Berechnungssysteme.</summary>
    public ObservableCollection<CalculationSystemEditModel> Systems { get; } = [];

    /// <summary>Die bearbeitbaren Abgleichprofile.</summary>
    public ObservableCollection<CompareProfileEditModel> CompareProfiles { get; } = [];

    /// <summary>Das aktuell gewählte Profil (dessen Regeln bearbeitet werden).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCompareProfileCommand), nameof(AddCompareKeyCommand),
        nameof(ExportSelectedCompareProfileCommand))]
    private CompareProfileEditModel? _selectedCompareProfile;

    /// <summary>Die aktuell gewählte Schlüsselregel des Profils.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCompareKeyCommand))]
    private CompareKeyEditModel? _selectedCompareKey;

    /// <summary>Statuszeile des Abgleich-Abschnitts.</summary>
    [ObservableProperty]
    private string _compareKeyStatus = string.Empty;

    /// <summary>Das aktuell in der Tabelle ausgewählte System.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    private CalculationSystemEditModel? _selectedSystem;

    /// <summary>Statuszeile mit dem Ergebnis der letzten Aktion.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Anzeigepfad des Berechnungssysteme-Ordners (für den Hinweistext).</summary>
    public string CalculationFolder => _calculationFolder;

    /// <summary>Erzeugt das ViewModel (Dependency Injection).</summary>
    /// <param name="store">Persistenter Speicher der Berechnungssysteme.</param>
    /// <param name="compareProfileStore">Persistenter Speicher der Abgleichprofile.</param>
    /// <param name="dialogService">Dialogdienst (Dateiauswahl, Meldungen).</param>
    /// <param name="configuration">Konfiguration (Ordnerauflösung).</param>
    /// <param name="logger">Logger für Fehlerprotokollierung.</param>
    public SettingsViewModel(ICalculationSystemStore store, ICompareProfileStore compareProfileStore,
        IDialogService dialogService, IConfiguration configuration, ILogger<SettingsViewModel> logger)
    {
        _store = store;
        _compareProfileStore = compareProfileStore;
        _dialogService = dialogService;
        _logger = logger;

        _calculationFolder = TestCaseFilesPathResolver.ResolveWorkspaceSubfolder(
            configuration["CalculationSystems:FolderPath"], AppContext.BaseDirectory, "Berechnungssysteme");
    }

    /// <summary>Lädt die gespeicherten Systeme in die Tabelle.</summary>
    public async Task LoadAsync()
    {
        try
        {
            var systems = await _store.GetAllAsync();
            Systems.Clear();
            foreach (var system in systems)
            {
                Systems.Add(new CalculationSystemEditModel(system));
            }
            StatusMessage = $"{Systems.Count} Berechnungssystem(e) geladen.";

            var vorher = SelectedCompareProfile?.Name;
            var profiles = await _compareProfileStore.GetAllAsync();
            CompareProfiles.Clear();
            foreach (var profile in profiles)
            {
                CompareProfiles.Add(new CompareProfileEditModel(profile));
            }
            SelectedCompareProfile = CompareProfiles.FirstOrDefault(p => p.Name == vorher)
                                     ?? CompareProfiles.FirstOrDefault();
            CompareKeyStatus = $"{CompareProfiles.Count} Abgleichprofil(e) geladen.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Einstellungen konnten nicht geladen werden.");
            _dialogService.ShowError("Fehler", "Die Einstellungen konnten nicht geladen werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Legt ein neues, leeres Abgleichprofil an.</summary>
    [RelayCommand]
    private void AddCompareProfile()
    {
        var neu = new CompareProfileEditModel { Name = "Neues Profil" };
        CompareProfiles.Add(neu);
        SelectedCompareProfile = neu;
        CompareKeyStatus = "Neues Profil angelegt – Regeln ergänzen und speichern.";
    }

    /// <summary>Entfernt das ausgewählte Abgleichprofil.</summary>
    [RelayCommand(CanExecute = nameof(HasCompareProfileSelection))]
    private void RemoveCompareProfile()
    {
        if (SelectedCompareProfile is null)
        {
            return;
        }

        var name = SelectedCompareProfile.Name;
        if (!_dialogService.Confirm("Profil entfernen",
                $"Soll das Abgleichprofil \"{name}\" mit allen Regeln entfernt werden?"))
        {
            return;
        }

        CompareProfiles.Remove(SelectedCompareProfile);
        SelectedCompareProfile = CompareProfiles.FirstOrDefault();
        CompareKeyStatus = $"Profil „{name}“ entfernt. Zum Übernehmen speichern.";
    }

    /// <summary>Fügt dem gewählten Profil eine neue Schlüsselregel hinzu.</summary>
    [RelayCommand(CanExecute = nameof(HasCompareProfileSelection))]
    private void AddCompareKey()
    {
        if (SelectedCompareProfile is null)
        {
            return;
        }

        var neu = new CompareKeyEditModel { Entity = "Provision", KeyColumns = "datum" };
        SelectedCompareProfile.Rules.Add(neu);
        SelectedCompareKey = neu;
        CompareKeyStatus = "Neue Regel hinzugefügt – anpassen und speichern.";
    }

    /// <summary>Entfernt die ausgewählte Schlüsselregel aus dem Profil.</summary>
    [RelayCommand(CanExecute = nameof(HasCompareKeySelection))]
    private void RemoveCompareKey()
    {
        if (SelectedCompareKey is null || SelectedCompareProfile is null)
        {
            return;
        }

        var entity = SelectedCompareKey.Entity;
        SelectedCompareProfile.Rules.Remove(SelectedCompareKey);
        CompareKeyStatus = $"Regel für „{entity}“ entfernt. Zum Übernehmen speichern.";
    }

    /// <summary>Prüft die Eingaben und speichert alle Abgleichprofile.</summary>
    [RelayCommand]
    private async Task SaveCompareProfilesAsync()
    {
        if (CompareProfiles.Any(p => string.IsNullOrWhiteSpace(p.Name)))
        {
            _dialogService.ShowError("Name fehlt", "Jedes Profil benötigt einen Namen.");
            return;
        }

        var doppelteProfile = CompareProfiles
            .GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (doppelteProfile.Count > 0)
        {
            _dialogService.ShowError("Doppelte Profile",
                "Profilnamen müssen eindeutig sein. Mehrfach vorhanden:"
                + Environment.NewLine + string.Join(Environment.NewLine, doppelteProfile));
            return;
        }

        foreach (var profil in CompareProfiles)
        {
            if (profil.Rules.Any(r => string.IsNullOrWhiteSpace(r.Entity) || r.SplitColumns().Count == 0))
            {
                _dialogService.ShowError("Unvollständige Regel",
                    $"Im Profil „{profil.Name}“ benötigt jede Regel eine Entität "
                    + "und mindestens ein Schlüsselattribut.");
                return;
            }

            var doppelteEntitaeten = profil.Rules
                .GroupBy(r => r.Entity.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (doppelteEntitaeten.Count > 0)
            {
                _dialogService.ShowError("Doppelte Entitäten",
                    $"Im Profil „{profil.Name}“ ist je Entität nur eine Regel zulässig. "
                    + "Mehrfach vorhanden:" + Environment.NewLine
                    + string.Join(Environment.NewLine, doppelteEntitaeten));
                return;
            }
        }

        try
        {
            await _compareProfileStore.SaveAsync(CompareProfiles.Select(p => p.ToProfile()));
            CompareKeyStatus = $"{CompareProfiles.Count} Abgleichprofil(e) gespeichert.";
            _dialogService.ShowInfo("Gespeichert",
                "Die Abgleichprofile wurden gespeichert und stehen im Reiter „Berechnung“ zur Auswahl.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Abgleichprofile konnten nicht gespeichert werden.");
            _dialogService.ShowError("Fehler", "Die Abgleichprofile konnten nicht gespeichert werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Exportiert alle Abgleichprofile als CSV.</summary>
    [RelayCommand]
    private void ExportAllCompareProfiles()
        => ExportCompareProfiles(CompareProfiles.Select(p => p.ToProfile()).ToList(), "Abgleichprofile");

    /// <summary>Exportiert nur das ausgewählte Abgleichprofil als CSV.</summary>
    [RelayCommand(CanExecute = nameof(HasCompareProfileSelection))]
    private void ExportSelectedCompareProfile()
    {
        if (SelectedCompareProfile is null)
        {
            return;
        }

        ExportCompareProfiles([SelectedCompareProfile.ToProfile()], SelectedCompareProfile.Name);
    }

    /// <summary>Schreibt die übergebenen Profile in eine vom Anwender gewählte CSV-Datei.</summary>
    /// <param name="profiles">Die zu exportierenden Profile.</param>
    /// <param name="suggestedName">Vorschlag für den Dateinamen (ohne Endung).</param>
    private void ExportCompareProfiles(IReadOnlyList<CompareProfile> profiles, string suggestedName)
    {
        if (profiles.Count == 0)
        {
            _dialogService.ShowInfo("Nichts zu exportieren", "Es sind keine Profile vorhanden.");
            return;
        }

        var path = _dialogService.ShowSaveFileDialog(
            Sanitize(suggestedName) + ".csv", "CSV-Datei|*.csv|Alle Dateien|*.*");
        if (path is null)
        {
            return;
        }

        try
        {
            // UTF-8 mit BOM, damit Excel Umlaute korrekt anzeigt.
            File.WriteAllText(path, CompareProfileCsv.Write(profiles), new UTF8Encoding(true));
            CompareKeyStatus = $"{profiles.Count} Profil(e) nach {Path.GetFileName(path)} exportiert.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Abgleichprofile konnten nicht exportiert werden.");
            _dialogService.ShowError("Fehler", "Der Export ist fehlgeschlagen:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>
    /// Liest Abgleichprofile aus einer CSV-Datei. Profile mit bekanntem Namen
    /// werden ersetzt, neue ergänzt; nicht enthaltene bleiben unverändert.
    /// Übernommen wird erst beim Speichern.
    /// </summary>
    [RelayCommand]
    private void ImportCompareProfiles()
    {
        var path = _dialogService.ShowOpenFileDialog("CSV-Datei|*.csv|Alle Dateien|*.*");
        if (path is null)
        {
            return;
        }

        string csv;
        try
        {
            csv = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die CSV-Datei {Path} konnte nicht gelesen werden.", path);
            _dialogService.ShowError("Fehler", "Die Datei konnte nicht gelesen werden:"
                + Environment.NewLine + ex.Message);
            return;
        }

        if (!CompareProfileCsv.TryParse(csv, out var importiert, out var error))
        {
            _dialogService.ShowError("Ungültige CSV-Datei", error ?? "Unbekannter Fehler.");
            return;
        }

        var bekannt = importiert
            .Where(p => CompareProfiles.Any(v => Same(v.Name, p.Name)))
            .Select(p => p.Name)
            .ToList();
        var neu = importiert.Count - bekannt.Count;

        var frage = $"{importiert.Count} Profil(e) gelesen: {neu} neu, {bekannt.Count} werden ersetzt.";
        if (bekannt.Count > 0)
        {
            frage += Environment.NewLine + Environment.NewLine + "Ersetzt werden:"
                     + Environment.NewLine + string.Join(Environment.NewLine, bekannt);
        }
        frage += Environment.NewLine + Environment.NewLine + "Übernehmen?";

        if (!_dialogService.Confirm("Profile importieren", frage))
        {
            return;
        }

        foreach (var profil in importiert)
        {
            var vorhanden = CompareProfiles.FirstOrDefault(v => Same(v.Name, profil.Name));
            if (vorhanden is not null)
            {
                CompareProfiles[CompareProfiles.IndexOf(vorhanden)] = new CompareProfileEditModel(profil);
            }
            else
            {
                CompareProfiles.Add(new CompareProfileEditModel(profil));
            }
        }

        SelectedCompareProfile = CompareProfiles
            .FirstOrDefault(p => Same(p.Name, importiert[0].Name));
        CompareKeyStatus = $"{importiert.Count} Profil(e) importiert ({neu} neu, {bekannt.Count} ersetzt) "
                           + "– zum Übernehmen speichern.";
    }

    /// <summary>Vergleicht Namen ohne Beachtung der Groß-/Kleinschreibung.</summary>
    private static bool Same(string left, string right)
        => string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Ersetzt für Dateinamen unzulässige Zeichen durch einen Unterstrich.</summary>
    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    /// <summary>CanExecute-Bedingung für Aktionen an der Profilauswahl.</summary>
    private bool HasCompareProfileSelection() => SelectedCompareProfile is not null;

    /// <summary>CanExecute-Bedingung für Aktionen an der Regelauswahl.</summary>
    private bool HasCompareKeySelection() => SelectedCompareKey is not null;

    /// <summary>Fügt ein neues, leeres Berechnungssystem hinzu.</summary>
    [RelayCommand]
    private void Add()
    {
        var neu = new CalculationSystemEditModel { Name = "Neues System" };
        Systems.Add(neu);
        SelectedSystem = neu;
        StatusMessage = "Neues System hinzugefügt – bitte ausfüllen und speichern.";
    }

    /// <summary>Wählt eine Skriptdatei über den Dateidialog für das gewählte System.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void BrowseScript()
    {
        if (SelectedSystem is null)
        {
            return;
        }

        var path = _dialogService.ShowOpenFileDialog(
            "Skripte (*.py;*.exe;*.bat;*.cmd)|*.py;*.exe;*.bat;*.cmd|Alle Dateien|*.*");
        if (path is null)
        {
            return;
        }

        // Liegt das Skript im Berechnungssysteme-Ordner, nur den Dateinamen
        // speichern (portabel); sonst den absoluten Pfad.
        var directory = Path.GetDirectoryName(path);
        SelectedSystem.ScriptFile =
            string.Equals(directory?.TrimEnd(Path.DirectorySeparatorChar),
                _calculationFolder.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileName(path)
                : path;
    }

    /// <summary>Entfernt das ausgewählte Berechnungssystem aus der Liste.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Remove()
    {
        if (SelectedSystem is null)
        {
            return;
        }

        var name = SelectedSystem.Name;
        if (!_dialogService.Confirm("System entfernen",
                $"Soll das Berechnungssystem \"{name}\" entfernt werden? "
                + "Die Skriptdatei selbst bleibt erhalten."))
        {
            return;
        }

        Systems.Remove(SelectedSystem);
        StatusMessage = $"System \"{name}\" entfernt. Zum Übernehmen speichern.";
    }

    /// <summary>Prüft die Eingaben und speichert alle Systeme dauerhaft.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        // Pflichtfelder prüfen.
        var fehlend = Systems
            .Where(s => string.IsNullOrWhiteSpace(s.Name) || string.IsNullOrWhiteSpace(s.ScriptFile))
            .ToList();
        if (fehlend.Count > 0)
        {
            _dialogService.ShowError("Unvollständige Angaben",
                "Jedes Berechnungssystem benötigt mindestens einen Namen und eine Skriptdatei.");
            return;
        }

        // Doppelte Namen verhindern (Ausgabe-Unterordner müssen eindeutig sein).
        var doppelt = Systems
            .GroupBy(s => $"{s.Name.Trim()}_v{s.Version.Trim()}", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (doppelt.Count > 0)
        {
            _dialogService.ShowError("Doppelte Systeme",
                "Name und Version müssen zusammen eindeutig sein. Mehrfach vorhanden:"
                + Environment.NewLine + string.Join(Environment.NewLine, doppelt));
            return;
        }

        var systems = Systems.Select(s => s.ToCalculationSystem()).ToList();

        try
        {
            await _store.SaveAsync(systems);

            // Auf fehlende Skriptdateien hinweisen (blockiert das Speichern nicht).
            var nichtGefunden = systems
                .Where(s => !File.Exists(ResolveScriptPath(s.ScriptFile)))
                .Select(s => s.Name)
                .ToList();

            StatusMessage = $"{systems.Count} Berechnungssystem(e) gespeichert.";
            if (nichtGefunden.Count > 0)
            {
                _dialogService.ShowInfo("Gespeichert – Hinweis",
                    "Gespeichert. Für folgende Systeme wurde die Skriptdatei nicht gefunden "
                    + "(bitte Pfad prüfen):" + Environment.NewLine
                    + string.Join(Environment.NewLine, nichtGefunden));
            }
            else
            {
                _dialogService.ShowInfo("Gespeichert",
                    "Die Berechnungssysteme wurden gespeichert und stehen im Reiter „Berechnung“ bereit.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Berechnungssysteme konnten nicht gespeichert werden.");
            _dialogService.ShowError("Fehler", "Die Berechnungssysteme konnten nicht gespeichert werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Verwirft alle Änderungen und lädt den gespeicherten Stand neu.</summary>
    [RelayCommand]
    private async Task ReloadAsync() => await LoadAsync();

    /// <summary>Löst einen (relativen) Skriptpfad gegen den Ordner auf.</summary>
    private string ResolveScriptPath(string scriptFile)
        => Path.IsPathRooted(scriptFile)
            ? scriptFile
            : Path.Combine(_calculationFolder, scriptFile);

    /// <summary>CanExecute-Bedingung für Aktionen, die eine Auswahl brauchen.</summary>
    private bool HasSelection() => SelectedSystem is not null;
}
