using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.FileFormat;

/// <summary>
/// Liest den Textinhalt einer Testfalldatei in das Domänenmodell ein
/// (Entities → Zeilen → Werte). Dieses Interface ist der
/// <b>unternehmensindividuell gestaltbare</b> Erweiterungspunkt für das
/// <b>Einlesen</b>: Ein Unternehmen mit abweichendem Dateiformat registriert
/// einfach eine eigene Implementierung im DI-Container.
/// </summary>
public interface ITestCaseFileParser
{
    /// <summary>
    /// Zerlegt den Dateiinhalt in die enthaltenen Entities, Zeilen und Werte.
    /// </summary>
    /// <param name="fileNumber">Nummerpräfix der Datei (z. B. "0001").</param>
    /// <param name="designation">Bezeichnung der Datei (z. B. "Vertragsbeginn").</param>
    /// <param name="section">Abschnitt (Eingabe/Ausgabe).</param>
    /// <param name="content">Vollständiger Textinhalt der Datei.</param>
    /// <returns>Die geparste Datei als Domänenobjekt (noch ohne Schlüssel-Verknüpfung zum Testfall).</returns>
    TestCaseFile Parse(string fileNumber, string designation, TestCaseFileSection section, string content);
}
