using CommunityToolkit.Mvvm.ComponentModel;
using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// Editierbares Zeilenmodell eines Berechnungssystems für den
/// Einstellungen-Reiter. Kapselt die Felder als änderbare, meldende
/// Eigenschaften (MVVM) und lässt sich in ein unveränderliches
/// <see cref="CalculationSystem"/> überführen.
/// </summary>
public partial class CalculationSystemEditModel : ObservableObject
{
    /// <summary>Anzeigename des Systems.</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Auszuführendes Programm (z. B. <c>python</c>).</summary>
    [ObservableProperty]
    private string _executable = "python";

    /// <summary>Skriptdatei (relativ zum Berechnungssysteme-Ordner oder absolut).</summary>
    [ObservableProperty]
    private string _scriptFile = string.Empty;

    /// <summary>Versionsnummer.</summary>
    [ObservableProperty]
    private string _version = "1.0";

    /// <summary>Aufrufmuster mit den Platzhaltern {script}, {input}, {output}.</summary>
    [ObservableProperty]
    private string _argumentsTemplate = CalculationSystem.DefaultArgumentsTemplate;

    /// <summary>Erzeugt ein leeres Editiermodell (für „Hinzufügen").</summary>
    public CalculationSystemEditModel()
    {
    }

    /// <summary>Erzeugt ein Editiermodell aus einem gespeicherten System.</summary>
    /// <param name="system">Die Vorlage.</param>
    public CalculationSystemEditModel(CalculationSystem system)
    {
        _name = system.Name;
        _executable = system.Executable;
        _scriptFile = system.ScriptFile;
        _version = system.Version;
        _argumentsTemplate = string.IsNullOrWhiteSpace(system.ArgumentsTemplate)
            ? CalculationSystem.DefaultArgumentsTemplate
            : system.ArgumentsTemplate;
    }

    /// <summary>Überführt das Editiermodell in ein unveränderliches System.</summary>
    public CalculationSystem ToCalculationSystem() => new()
    {
        Name = Name.Trim(),
        Executable = string.IsNullOrWhiteSpace(Executable) ? "python" : Executable.Trim(),
        ScriptFile = ScriptFile.Trim(),
        Version = string.IsNullOrWhiteSpace(Version) ? "1.0" : Version.Trim(),
        ArgumentsTemplate = string.IsNullOrWhiteSpace(ArgumentsTemplate)
            ? CalculationSystem.DefaultArgumentsTemplate
            : ArgumentsTemplate.Trim()
    };
}
