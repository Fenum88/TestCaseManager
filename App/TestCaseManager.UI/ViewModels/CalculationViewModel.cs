using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// ViewModel des Reiters „Berechnung": ruft ein einzelnes oder alle
/// konfigurierten Berechnungssysteme auf. Jedes System liest die Eingaben aus
/// dem Unterordner <c>Input</c> und schreibt seine Ausgaben in einen eigenen,
/// nach System und Version benannten Unterordner von <c>Output</c>, sodass
/// gleichzeitige Läufe sich nicht überschreiben.
/// </summary>
public partial class CalculationViewModel : ObservableObject
{
    private const string InputFolderName = "Input";
    private const string OutputFolderName = "Output";

    private readonly ICalculationSystemService _calculationService;
    private readonly ICompareService _compareService;
    private readonly ICalculationSystemStore _store;
    private readonly ITestScenarioStore _scenarioStore;
    private readonly ICompareProfileStore _compareProfileStore;
    private readonly IDialogService _dialogService;
    private readonly ILogger<CalculationViewModel> _logger;

    /// <summary>Ordner mit den Berechnungssystem-Skripten.</summary>
    private readonly string _calculationFolder;

    /// <summary>Wurzelordner der Testfalldateien (enthält Input und Output).</summary>
    [ObservableProperty]
    private string _rootPath;

    /// <summary>Gibt an, ob gerade eine Berechnung läuft.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Statuszeile mit dem Ergebnis der letzten Berechnung.</summary>
    [ObservableProperty]
    private string _statusMessage = "Bereit.";

    /// <summary>Das für die Einzelberechnung ausgewählte System.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunSelectedCommand))]
    private CalculationSystem? _selectedSystem;

    /// <summary>Erstes System des Vergleichs.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCompareCommand))]
    private CalculationSystem? _compareSystemA;

    /// <summary>Zweites System des Vergleichs.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCompareCommand))]
    private CalculationSystem? _compareSystemB;

    /// <summary>Auswahl des Testszenarios („Alle Testfälle" oder ein Szenario).</summary>
    [ObservableProperty]
    private ScenarioChoice? _selectedScenarioChoice;

    /// <summary>Auswahl des Abgleichprofils („Ohne Profil" oder ein Profil).</summary>
    [ObservableProperty]
    private CompareProfileChoice? _selectedCompareProfileChoice;

    /// <summary>Verfügbare Berechnungssysteme (aus der Konfiguration).</summary>
    public ObservableCollection<CalculationSystem> Systems { get; } = [];

    /// <summary>Auswahlmenü der Testszenarien (erster Eintrag: „Alle Testfälle").</summary>
    public ObservableCollection<ScenarioChoice> ScenarioChoices { get; } = [];

    /// <summary>Auswahlmenü der Abgleichprofile (erster Eintrag: „Ohne Profil").</summary>
    public ObservableCollection<CompareProfileChoice> CompareProfileChoices { get; } = [];

    /// <summary>Protokoll der Berechnungsläufe (Log-Panel).</summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    /// <summary>Erzeugt das ViewModel (Dependency Injection).</summary>
    /// <param name="calculationService">Dienst zum Ausführen externer Berechnungssysteme.</param>
    /// <param name="compareService">Dienst zum Vergleich zweier Ausgaben.</param>
    /// <param name="store">Persistenter Speicher der Berechnungssysteme.</param>
    /// <param name="scenarioStore">Persistenter Speicher der Testszenarien.</param>
    /// <param name="compareProfileStore">Persistenter Speicher der Abgleichprofile.</param>
    /// <param name="dialogService">Dialogdienst (Ordnerauswahl, Meldungen).</param>
    /// <param name="configuration">Konfiguration (Pfade).</param>
    /// <param name="logger">Logger für Fehlerprotokollierung.</param>
    public CalculationViewModel(ICalculationSystemService calculationService,
        ICompareService compareService, ICalculationSystemStore store, ITestScenarioStore scenarioStore,
        ICompareProfileStore compareProfileStore, IDialogService dialogService,
        IConfiguration configuration, ILogger<CalculationViewModel> logger)
    {
        _calculationService = calculationService;
        _compareService = compareService;
        _store = store;
        _scenarioStore = scenarioStore;
        _compareProfileStore = compareProfileStore;
        _dialogService = dialogService;
        _logger = logger;

        _rootPath = TestCaseFilesPathResolver.Resolve(
            configuration["TestCaseFiles:RootPath"], AppContext.BaseDirectory);

        _calculationFolder = TestCaseFilesPathResolver.ResolveWorkspaceSubfolder(
            configuration["CalculationSystems:FolderPath"], AppContext.BaseDirectory, "Berechnungssysteme");
    }

    /// <summary>
    /// Lädt die konfigurierten Berechnungssysteme aus dem Speicher neu. Wird
    /// beim Öffnen des Reiters aufgerufen, damit Änderungen aus den
    /// Einstellungen sofort sichtbar sind.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            var systems = await _store.GetAllAsync();
            var vorher = SelectedSystem?.Name;

            Systems.Clear();
            foreach (var system in systems.Where(s => !string.IsNullOrWhiteSpace(s.Name)))
            {
                Systems.Add(system);
            }

            SelectedSystem = Systems.FirstOrDefault(s => s.Name == vorher) ?? Systems.FirstOrDefault();

            // Für den Vergleich sinnvolle Vorauswahl: erstes und zweites System.
            CompareSystemA = Systems.FirstOrDefault(s => s.Name == CompareSystemA?.Name) ?? Systems.FirstOrDefault();
            CompareSystemB = Systems.FirstOrDefault(s => s.Name == CompareSystemB?.Name)
                             ?? Systems.FirstOrDefault(s => s != CompareSystemA)
                             ?? Systems.LastOrDefault();

            await LoadScenariosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Berechnungssysteme konnten nicht geladen werden.");
            _dialogService.ShowError("Fehler", "Die Berechnungssysteme konnten nicht geladen werden:"
                + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Lädt die Testszenarien neu und baut das Auswahlmenü auf.</summary>
    private async Task LoadScenariosAsync()
    {
        var vorher = SelectedScenarioChoice?.Scenario?.Name;
        var scenarios = await _scenarioStore.GetAllAsync();

        ScenarioChoices.Clear();
        ScenarioChoices.Add(new ScenarioChoice { Label = "Alle Testfälle" });
        foreach (var scenario in scenarios)
        {
            ScenarioChoices.Add(new ScenarioChoice
            {
                Label = $"{scenario.Name} ({scenario.TestCaseNumbers.Count} Testfälle)",
                Scenario = scenario
            });
        }

        SelectedScenarioChoice = ScenarioChoices.FirstOrDefault(c => c.Scenario?.Name == vorher)
                                 ?? ScenarioChoices[0];

        await LoadCompareProfilesAsync();
    }

    /// <summary>Lädt die Abgleichprofile neu und baut das Auswahlmenü auf.</summary>
    private async Task LoadCompareProfilesAsync()
    {
        var vorher = SelectedCompareProfileChoice?.Profile?.Name;
        var profiles = await _compareProfileStore.GetAllAsync();

        CompareProfileChoices.Clear();
        CompareProfileChoices.Add(new CompareProfileChoice { Label = "Ohne Profil (nur Position)" });
        foreach (var profile in profiles)
        {
            CompareProfileChoices.Add(new CompareProfileChoice
            {
                Label = $"{profile.Name} ({profile.KeyRules.Count} Regeln)",
                Profile = profile
            });
        }

        // Vorauswahl: zuletzt gewähltes Profil, sonst das erste echte Profil.
        SelectedCompareProfileChoice =
            CompareProfileChoices.FirstOrDefault(c => c.Profile?.Name == vorher)
            ?? CompareProfileChoices.FirstOrDefault(c => c.Profile is not null)
            ?? CompareProfileChoices[0];
    }

    /// <summary>Öffnet die Ordnerauswahl für den Wurzelordner.</summary>
    [RelayCommand]
    private void Browse()
    {
        var selected = _dialogService.ShowFolderDialog(RootPath);
        if (selected is not null)
        {
            RootPath = selected;
        }
    }

    /// <summary>Führt nur das ausgewählte Berechnungssystem aus.</summary>
    [RelayCommand(CanExecute = nameof(CanRunSelected))]
    private async Task RunSelectedAsync()
    {
        if (SelectedSystem is null)
        {
            return;
        }

        await RunSystemsAsync([SelectedSystem]);
    }

    /// <summary>Führt alle konfigurierten Berechnungssysteme gleichzeitig aus.</summary>
    [RelayCommand]
    private async Task RunAllAsync()
    {
        if (Systems.Count == 0)
        {
            _dialogService.ShowInfo("Keine Systeme",
                "Es sind keine Berechnungssysteme konfiguriert (Abschnitt CalculationSystems in der appsettings.json).");
            return;
        }

        await RunSystemsAsync(Systems.ToList());
    }

    /// <summary>
    /// Führt die übergebenen Systeme gleichzeitig aus (jeweils in einen eigenen
    /// Ausgabe-Unterordner) und schreibt deren Konsolenausgabe geordnet ins
    /// Protokoll.
    /// </summary>
    /// <param name="systems">Die auszuführenden Systeme.</param>
    private async Task RunSystemsAsync(IReadOnlyList<CalculationSystem> systems)
    {
        if (IsBusy || !TryPrepareRun(out var run))
        {
            return;
        }

        var mehrzahl = systems.Count > 1;

        IsBusy = true;
        StatusMessage = mehrzahl
            ? $"Berechne {systems.Count} Systeme gleichzeitig …"
            : $"Berechnung „{systems[0].Name}“ läuft …";
        AppendLog(mehrzahl
            ? $"───── {systems.Count} Berechnungen gestartet ({run.ScenarioLabel}) ─────"
            : $"───── Berechnung „{systems[0].Name}“ gestartet ({run.ScenarioLabel}) ─────");
        AppendLog($"Eingabe: {run.InputDisplay}");

        try
        {
            // Alle Systeme parallel starten; jedes erhält seinen eigenen
            // Ausgabe-Unterordner, daher gibt es keine Schreibkonflikte.
            var tasks = systems.Select(async system =>
            {
                var outputDirectory = Path.Combine(run.OutputBase, system.OutputSubfolderName);
                var scriptPath = ResolveScript(system);
                var result = await _calculationService.RunAsync(
                    system.Executable, scriptPath, run.InputDirectory, outputDirectory, system.ArgumentsTemplate);
                return (system, result);
            }).ToList();

            var runs = await Task.WhenAll(tasks);

            // Ausgabe je System als zusammenhängenden Block protokollieren.
            var erfolgreich = 0;
            foreach (var (system, result) in runs)
            {
                AppendLog($"■ {system.Name} (v{system.Version}) → {run.OutputDisplay}\\{system.OutputSubfolderName}");
                foreach (var line in result.Output)
                {
                    AppendLog("    " + line.Text, line.IsError);
                }

                if (result.Success)
                {
                    erfolgreich++;
                }
                else
                {
                    AppendLog($"    Fehlercode {result.ExitCode}.", isError: true);
                }
            }

            StatusMessage = $"{erfolgreich}/{systems.Count} Berechnung(en) erfolgreich ({run.ScenarioLabel}).";
            AppendLog("Fertig: " + StatusMessage, isError: erfolgreich < systems.Count);

            if (erfolgreich < systems.Count)
            {
                _dialogService.ShowError("Berechnung mit Fehlern",
                    "Nicht alle Berechnungssysteme waren erfolgreich. Details stehen im Protokoll.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Ausführen der Berechnungssysteme.");
            StatusMessage = "Berechnung fehlgeschlagen.";
            AppendLog("Berechnung fehlgeschlagen: " + ex.Message, isError: true);
            _dialogService.ShowError("Fehler", "Die Berechnung ist fehlgeschlagen:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            run.Cleanup();
            IsBusy = false;
        }
    }

    /// <summary>
    /// Führt einen Vergleich zweier Systeme aus: beide berechnen, danach die
    /// Ausgaben attributweise vergleichen. Das Ergebnis liegt in derselben
    /// Ordnerstruktur; zusätzlich entsteht eine Gesamt-CSV.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRunCompare))]
    private async Task RunCompareAsync()
    {
        if (IsBusy || CompareSystemA is null || CompareSystemB is null)
        {
            return;
        }

        if (CompareSystemA.OutputSubfolderName == CompareSystemB.OutputSubfolderName)
        {
            _dialogService.ShowError("Gleiche Systeme",
                "Bitte zwei unterschiedliche Berechnungssysteme für den Vergleich wählen.");
            return;
        }

        if (!TryPrepareRun(out var run))
        {
            return;
        }

        var systemA = CompareSystemA;
        var systemB = CompareSystemB;
        var dirA = Path.Combine(run.OutputBase, systemA.OutputSubfolderName);
        var dirB = Path.Combine(run.OutputBase, systemB.OutputSubfolderName);

        var compareFolderName = $"Vergleich_{systemA.OutputSubfolderName}_vs_{systemB.OutputSubfolderName}";
        var compareDirectory = Path.Combine(run.OutputBase, compareFolderName);
        var csvPath = Path.Combine(compareDirectory, "_Gesamtvergleich.csv");
        var excelPath = Path.Combine(compareDirectory, "_Gesamtvergleich.xlsx");
        var testCaseCount = Directory.Exists(run.InputDirectory)
            ? Directory.GetDirectories(run.InputDirectory).Length
            : 0;

        IsBusy = true;
        StatusMessage = $"Vergleich „{systemA.Name}“ ↔ „{systemB.Name}“ läuft …";
        AppendLog($"───── Vergleich „{systemA.Name}“ ↔ „{systemB.Name}“ gestartet ({run.ScenarioLabel}) ─────");
        AppendLog($"Eingabe: {run.InputDisplay}");

        try
        {
            // 1. Beide Systeme parallel berechnen (Reihenfolge A, B bleibt erhalten).
            var runs = await Task.WhenAll(new[] { systemA, systemB }.Select(async system =>
            {
                var outputDirectory = Path.Combine(run.OutputBase, system.OutputSubfolderName);
                var scriptPath = ResolveScript(system);
                var result = await _calculationService.RunAsync(
                    system.Executable, scriptPath, run.InputDirectory, outputDirectory, system.ArgumentsTemplate);
                return (system, result);
            }));

            foreach (var (system, result) in runs)
            {
                AppendLog($"■ {system.Name} (v{system.Version}) → {run.OutputDisplay}\\{system.OutputSubfolderName}");
                foreach (var line in result.Output)
                {
                    AppendLog("    " + line.Text, line.IsError);
                }
                if (!result.Success)
                {
                    AppendLog($"    Fehlercode {result.ExitCode}.", isError: true);
                }
            }

            var infoA = BuildRunInfo($"{systemA.Name} v{systemA.Version}", runs[0].result);
            var infoB = BuildRunInfo($"{systemB.Name} v{systemB.Version}", runs[1].result);

            // 2. Ausgaben vergleichen (auch bei Fehlläufen, damit die
            //    Zusammenfassung Warnungen und Fehler dokumentiert).
            var profil = SelectedCompareProfileChoice;
            var keyRules = profil?.KeyRules ?? [];
            AppendLog($"Abgleichprofil: {profil?.Label ?? "Ohne Profil (nur Position)"}");
            foreach (var regel in keyRules)
            {
                AppendLog($"    {regel.Entity} → {string.Join(" | ", regel.KeyColumns)}");
            }
            AppendLog("Vergleiche Ausgaben …");
            var compare = await _compareService.CompareOutputsAsync(new CompareOutputsRequest
            {
                DirectoryA = dirA,
                DirectoryB = dirB,
                LabelA = $"{systemA.Name} v{systemA.Version}",
                LabelB = $"{systemB.Name} v{systemB.Version}",
                CompareDirectory = compareDirectory,
                CsvPath = csvPath,
                ExcelPath = excelPath,
                TestCaseCount = testCaseCount,
                InfoA = infoA,
                InfoB = infoB,
                KeyRules = keyRules
            });

            foreach (var message in compare.Messages)
            {
                AppendLog("    " + message);
            }
            foreach (var error in compare.Errors)
            {
                AppendLog("    " + error, isError: true);
            }

            AppendLog($"Ergebnis: {run.OutputDisplay}\\{compareFolderName}");
            AppendLog($"Excel (2 Blätter): {excelPath}");
            AppendLog($"Gesamt-CSV (deutsch): {csvPath}");
            StatusMessage = $"{compare.Summary} ({run.ScenarioLabel})";
            AppendLog("Fertig: " + StatusMessage, isError: compare.Errors.Count > 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Vergleich der Berechnungssysteme.");
            StatusMessage = "Vergleich fehlgeschlagen.";
            AppendLog("Vergleich fehlgeschlagen: " + ex.Message, isError: true);
            _dialogService.ShowError("Fehler", "Der Vergleich ist fehlgeschlagen:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            run.Cleanup();
            IsBusy = false;
        }
    }

    /// <summary>CanExecute-Bedingung für den Vergleich (zwei Systeme gewählt).</summary>
    private bool CanRunCompare() => CompareSystemA is not null && CompareSystemB is not null;

    /// <summary>
    /// Wertet die Konsolenausgabe eines Berechnungslaufs aus und leitet daraus
    /// die Warn- und Fehlermeldungen je Testfall ab (für die Zusammenfassung).
    /// Zeilen der Fehlerausgabe gelten als Fehler, Zeilen mit „WARN" als Warnung;
    /// ein Rückgabecode ungleich 0 ergibt einen zusätzlichen Fehlereintrag.
    /// </summary>
    private static CompareRunInfo BuildRunInfo(string label, CalculationRunResult result)
    {
        var notes = new List<CompareRunNote>();
        foreach (var line in result.Output)
        {
            string? severity = line.IsError
                ? "Fehler"
                : line.Text.Contains("WARN", StringComparison.OrdinalIgnoreCase) ? "Warnung" : null;
            if (severity is null)
            {
                continue;
            }

            notes.Add(new CompareRunNote
            {
                TestCaseNumber = ExtractTestCaseNumber(line.Text),
                Severity = severity,
                Message = line.Text.Trim()
            });
        }

        if (!result.Success && !notes.Any(n => n.Severity == "Fehler"))
        {
            notes.Add(new CompareRunNote
            {
                TestCaseNumber = "—",
                Severity = "Fehler",
                Message = $"Rückgabecode {result.ExitCode}"
            });
        }

        return new CompareRunInfo { Label = label, ExitCode = result.ExitCode, Notes = notes };
    }

    /// <summary>Extrahiert eine Testfallnummer aus einer Textzeile ("…0002_TF…" → "0002").</summary>
    private static string ExtractTestCaseNumber(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)_TF");
        return match.Success ? match.Groups[1].Value : "—";
    }

    /// <summary>CanExecute-Bedingung für die Einzelberechnung.</summary>
    private bool CanRunSelected() => SelectedSystem is not null;

    /// <summary>
    /// Bereitet einen Lauf vor: Für „Alle Testfälle" werden der Eingabeordner
    /// <c>Input</c> und der Ausgabeordner <c>Output</c> verwendet. Für ein
    /// Szenario wird ein temporärer Eingabeordner nur mit den enthaltenen
    /// Testfällen angelegt und in einen szenario-eigenen Ausgabeordner
    /// <c>Output/Szenario_{Name}</c> geschrieben.
    /// </summary>
    /// <param name="run">Der vorbereitete Lauf-Kontext.</param>
    /// <returns><c>false</c>, wenn ein gewähltes Szenario keine Testfälle enthält.</returns>
    private bool TryPrepareRun(out RunContext run)
    {
        var inputRoot = Path.Combine(RootPath, InputFolderName);
        var outputRoot = Path.Combine(RootPath, OutputFolderName);
        var scenario = SelectedScenarioChoice?.Scenario;

        if (scenario is null)
        {
            run = new RunContext(inputRoot, outputRoot, OutputFolderName, "Alle Testfälle", null);
            return true;
        }

        if (scenario.TestCaseNumbers.Count == 0)
        {
            _dialogService.ShowInfo("Leeres Szenario",
                $"Das Szenario „{scenario.Name}“ enthält keine Testfälle.");
            run = default!;
            return false;
        }

        // Temporären Eingabeordner nur mit den Testfällen des Szenarios anlegen.
        var tempInput = Path.Combine(Path.GetTempPath(), "tcm-szenario-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempInput);
        foreach (var number in scenario.TestCaseNumbers)
        {
            var source = Path.Combine(inputRoot, number + "_TF");
            if (Directory.Exists(source))
            {
                CopyDirectory(source, Path.Combine(tempInput, number + "_TF"));
            }
        }

        var scenarioFolder = "Szenario_" + Sanitize(scenario.Name);
        var outputBase = Path.Combine(outputRoot, scenarioFolder);
        run = new RunContext(tempInput, outputBase, OutputFolderName + "\\" + scenarioFolder,
            $"Szenario „{scenario.Name}“", tempInput);
        return true;
    }

    /// <summary>Löst den (relativen) Skriptpfad eines Systems auf.</summary>
    private string ResolveScript(CalculationSystem system)
        => Path.IsPathRooted(system.ScriptFile)
            ? system.ScriptFile
            : Path.Combine(_calculationFolder, system.ScriptFile);

    /// <summary>Kopiert einen Ordner rekursiv (für den temporären Szenario-Eingabeordner).</summary>
    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }
        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }

    /// <summary>Ersetzt für Ordnernamen unzulässige Zeichen durch einen Unterstrich.</summary>
    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    /// <summary>
    /// Kontext eines Berechnungslaufs: Eingabe-/Ausgabeordner, Anzeigetexte und
    /// ggf. ein aufzuräumender temporärer Ordner (bei Szenarien).
    /// </summary>
    private sealed record RunContext(string InputDirectory, string OutputBase, string OutputDisplay,
        string ScenarioLabel, string? TempInput)
    {
        /// <summary>Anzeigepfad des Eingabeordners.</summary>
        public string InputDisplay => TempInput is null ? InputDirectory : $"{ScenarioLabel} (Teilmenge)";

        /// <summary>Entfernt den temporären Eingabeordner, falls einer angelegt wurde.</summary>
        public void Cleanup()
        {
            if (TempInput is not null && Directory.Exists(TempInput))
            {
                try
                {
                    Directory.Delete(TempInput, recursive: true);
                }
                catch (IOException)
                {
                    // Temporärer Ordner wird beim nächsten Aufräumen entfernt.
                }
            }
        }
    }

    /// <summary>Leert das Log-Panel.</summary>
    [RelayCommand]
    private void ClearLog() => LogEntries.Clear();

    /// <summary>Fügt einen Eintrag zum Log hinzu (mit Begrenzung der Größe).</summary>
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
