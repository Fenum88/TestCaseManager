using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Führt ein externes Berechnungssystem (z. B. ein Python-Skript) über die
/// Konsole aus. Das System erhält einen Eingabe- und einen Ausgabeordner und
/// erzeugt aus den Eingaben die zugehörigen Ausgaben.
/// </summary>
public interface ICalculationSystemService
{
    /// <summary>
    /// Ruft das Berechnungssystem als eigenständigen Prozess auf und sammelt
    /// dessen Konsolenausgabe.
    /// </summary>
    /// <param name="executable">Auszuführendes Programm (z. B. <c>python</c>).</param>
    /// <param name="scriptPath">Vollständiger Pfad zur Skriptdatei.</param>
    /// <param name="inputDirectory">Ordner mit den Eingabedateien.</param>
    /// <param name="outputDirectory">Zielordner für die Ausgabedateien.</param>
    /// <param name="argumentsTemplate">
    /// Aufrufmuster mit den Platzhaltern <c>{script}</c>, <c>{input}</c> und
    /// <c>{output}</c>. Leer/<c>null</c> verwendet das Standardmuster
    /// <c>{script} {input} {output}</c>.
    /// </param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Rückgabecode und gesammelte Konsolenausgabe.</returns>
    Task<CalculationRunResult> RunAsync(string executable, string scriptPath,
        string inputDirectory, string outputDirectory, string? argumentsTemplate = null,
        CancellationToken cancellationToken = default);
}
