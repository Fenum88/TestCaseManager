using System.IO;
using System.Text.Json.Serialization;

namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Beschreibt ein externes Berechnungssystem, das die Anwendung über die
/// Konsole aufrufen kann (z. B. ein Python-Skript). Die Systeme werden über
/// den Einstellungen-Reiter der Oberfläche gepflegt und in einer JSON-Datei
/// gespeichert; die <c>appsettings.json</c> liefert nur die Erstbelegung.
///
/// Wie das System aufgerufen wird, bestimmt <see cref="ArgumentsTemplate"/>.
/// </summary>
public class CalculationSystem
{
    /// <summary>Standard-Aufrufmuster: Skript, dann Eingabe- und Ausgabeordner.</summary>
    public const string DefaultArgumentsTemplate = "{script} {input} {output}";

    /// <summary>Anzeigename in der Oberfläche (z. B. "Substanzwertverfahren").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Auszuführendes Programm. Standard ist <c>python</c>; kann auf einen
    /// absoluten Interpreterpfad gesetzt werden.
    /// </summary>
    public string Executable { get; init; } = "python";

    /// <summary>
    /// Skriptdatei relativ zum Berechnungssysteme-Ordner (oder absoluter Pfad),
    /// z. B. <c>substanzwert.py</c>.
    /// </summary>
    public string ScriptFile { get; init; } = string.Empty;

    /// <summary>Versionsnummer des Berechnungssystems (z. B. "1.0").</summary>
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// Aufrufmuster für die Argumente. Die Platzhalter <c>{script}</c>,
    /// <c>{input}</c> und <c>{output}</c> werden durch den Skriptpfad, den
    /// Eingabe- und den Ausgabeordner ersetzt. Damit lässt sich vorgeben, wie
    /// das System aufgerufen wird, z. B. <c>{script} --in {input} --out {output}</c>.
    /// </summary>
    public string ArgumentsTemplate { get; init; } = DefaultArgumentsTemplate;

    /// <summary>
    /// Name des Ausgabe-Unterordners für dieses System, damit gleichzeitige
    /// Läufe mehrerer Systeme sich nicht überschreiben. Setzt sich aus Name
    /// und Version zusammen (dateisystemsicher), z. B.
    /// <c>Substanzwertverfahren_v1.0</c>.
    /// </summary>
    [JsonIgnore]
    public string OutputSubfolderName => Sanitize($"{Name}_v{Version}");

    /// <summary>Ersetzt für Dateinamen unzulässige Zeichen durch einen Unterstrich.</summary>
    /// <param name="value">Der zu bereinigende Text.</param>
    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
