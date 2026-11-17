using TestCaseManager.Domain.Common;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Dateianhang eines Testfalls (Dokument, Bild, PDF usw.).
/// Der Inhalt wird als Binärdaten direkt in der Datenbank gespeichert,
/// damit keine externen Dateipfade gepflegt werden müssen.
/// </summary>
public class Attachment : AuditableEntity
{
    /// <summary>Fremdschlüssel zum zugehörigen Testfall.</summary>
    public Guid TestCaseId { get; set; }

    /// <summary>Zugehöriger Testfall (Navigationseigenschaft).</summary>
    public TestCase? TestCase { get; set; }

    /// <summary>Ursprünglicher Dateiname inklusive Erweiterung.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME-Typ der Datei (z. B. "application/pdf").</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Dateigröße in Bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Binärer Dateiinhalt.</summary>
    public byte[] Content { get; set; } = [];

    /// <summary>Optionale Beschreibung des Anhangs.</summary>
    public string? Description { get; set; }
}
