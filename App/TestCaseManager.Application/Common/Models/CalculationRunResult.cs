namespace TestCaseManager.Application.Common.Models;

/// <summary>Eine einzelne Ausgabezeile eines Berechnungslaufs (stdout/stderr).</summary>
/// <param name="Text">Der Zeileninhalt.</param>
/// <param name="IsError">Ob die Zeile von der Fehlerausgabe (stderr) stammt.</param>
public record CalculationOutputLine(string Text, bool IsError);

/// <summary>
/// Ergebnis eines Berechnungslaufs: gesammelte Konsolenausgabe und der
/// Rückgabecode des aufgerufenen Prozesses.
/// </summary>
public class CalculationRunResult
{
    /// <summary>Rückgabecode des Prozesses (0 = Erfolg).</summary>
    public int ExitCode { get; init; }

    /// <summary>Gibt an, ob der Lauf erfolgreich war (Rückgabecode 0).</summary>
    public bool Success => ExitCode == 0;

    /// <summary>Alle Ausgabezeilen (stdout und stderr) in Reihenfolge des Eintreffens.</summary>
    public IReadOnlyList<CalculationOutputLine> Output { get; init; } = [];
}
