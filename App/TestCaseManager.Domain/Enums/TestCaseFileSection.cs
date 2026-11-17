namespace TestCaseManager.Domain.Enums;

/// <summary>
/// Abschnitt eines Datei-Testfalls. Ein Testfall besteht aus Eingabe-
/// und Ausgabedateien, die getrennt gespeichert werden.
/// </summary>
public enum TestCaseFileSection
{
    /// <summary>Eingabedaten (Dateiendung <c>.Eingabe</c>, Ordner <c>Input</c>).</summary>
    Input = 0,

    /// <summary>Ausgabe-/Erwartungsdaten (Dateiendung <c>.Ausgabe</c>, Ordner <c>Output</c>).</summary>
    Output = 1
}
