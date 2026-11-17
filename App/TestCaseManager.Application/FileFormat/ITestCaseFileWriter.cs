using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.FileFormat;

/// <summary>
/// Serialisiert eine Testfalldatei aus dem Domänenmodell zurück in ihren
/// Textinhalt. Dieses Interface ist der <b>unternehmensindividuell
/// gestaltbare</b> Erweiterungspunkt für das <b>Auslesen</b>: Zusammen mit
/// <see cref="ITestCaseFileParser"/> bildet es einen verlustfreien
/// Round-Trip (Datei → Datenbank → inhaltsgleiche Datei).
/// </summary>
public interface ITestCaseFileWriter
{
    /// <summary>
    /// Erzeugt den Textinhalt einer Datei aus ihrem Domänenmodell.
    /// </summary>
    /// <param name="file">Die zu serialisierende Datei inklusive Entities, Zeilen und Werten.</param>
    /// <returns>Der vollständige Dateiinhalt als Zeichenkette.</returns>
    string Write(TestCaseFile file);

    /// <summary>
    /// Liefert die Dateiendung für den Abschnitt der Datei
    /// (z. B. ".Eingabe" oder ".Ausgabe").
    /// </summary>
    /// <param name="file">Die Datei, deren Endung ermittelt werden soll.</param>
    string GetFileExtension(TestCaseFile file);
}
