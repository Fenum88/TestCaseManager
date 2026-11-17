namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Einstellungen für das dateibasierte Ein-/Auslesen. Werden aus dem
/// Abschnitt <c>TestCaseFiles</c> der <c>appsettings.json</c> befüllt und
/// erlauben es, das Verhalten unternehmensindividuell anzupassen, ohne
/// Code zu ändern.
/// </summary>
public class TestCaseFileOptions
{
    /// <summary>
    /// Gibt an, ob der Abschnitt <c>Output</c> (die Ausgabewerte) beim
    /// Einlesen in der Datenbank gespeichert wird.
    ///
    /// Standard ist <c>false</c>: Es werden ausschließlich die Eingabewerte
    /// gespeichert. Die Ausgabedateien bleiben im Dateisystem unberührt und
    /// können durch Umstellen auf <c>true</c> jederzeit wieder mit
    /// eingelesen werden – das Datenmodell unterstützt sie unverändert.
    /// </summary>
    public bool ImportOutputSection { get; init; }
}
