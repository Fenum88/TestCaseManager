using TestCaseManager.Domain.Common;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Ein aus Dateien eingelesener Testfall (Datei-Testfall).
/// Dies ist ein eigenständiges Subsystem, das unabhängig von den
/// interaktiv gepflegten <see cref="TestCase"/>-Objekten arbeitet und
/// ausschließlich über die <see cref="Number">Testfallnummer</see>
/// verknüpft wird. Die Nummer ist der fachliche Schlüssel, der alle
/// zugehörigen Entitäten (Dateien, Tabellen, Zeilen, Werte) verbindet.
///
/// Gespeichert wird ein generisches Meta-Modell (Entity → Zeile → Wert),
/// das beliebige und je Zeile unterschiedliche Spalten verlustfrei abbildet
/// und den Dateiinhalt vollständig rekonstruierbar macht.
/// </summary>
public class ImportedTestCase : AuditableEntity
{
    /// <summary>
    /// Fachliche Testfallnummer (z. B. "0001"), abgeleitet aus dem
    /// Ordnernamen <c>{Nummer}_TF</c>. Systemweit eindeutig.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>Eingabe- und Ausgabedateien dieses Testfalls.</summary>
    public List<TestCaseFile> Files { get; set; } = [];
}
