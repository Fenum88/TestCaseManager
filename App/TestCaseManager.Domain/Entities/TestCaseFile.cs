using TestCaseManager.Domain.Common;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Eine einzelne Datei eines Datei-Testfalls (Eingabe oder Ausgabe).
/// Speichert genug Metadaten, um Dateinamen und Ablageort exakt
/// wiederherzustellen: aus <see cref="FileNumber"/> und
/// <see cref="Designation"/> ergibt sich der Dateiname
/// <c>{FileNumber}_{Designation}.Eingabe|.Ausgabe</c>.
/// </summary>
public class TestCaseFile : EntityBase
{
    /// <summary>Fremdschlüssel zum übergeordneten Datei-Testfall.</summary>
    public Guid ImportedTestCaseId { get; set; }

    /// <summary>Übergeordneter Datei-Testfall (Navigationseigenschaft).</summary>
    public ImportedTestCase? ImportedTestCase { get; set; }

    /// <summary>Abschnitt der Datei (Eingabe/Ausgabe).</summary>
    public TestCaseFileSection Section { get; set; }

    /// <summary>Nummerpräfix der Datei (z. B. "0001").</summary>
    public string FileNumber { get; set; } = string.Empty;

    /// <summary>Fachliche Bezeichnung der Datei (z. B. "Vertragsbeginn").</summary>
    public string Designation { get; set; } = string.Empty;

    /// <summary>Reihenfolge der Datei innerhalb ihres Abschnitts.</summary>
    public int SortOrder { get; set; }

    /// <summary>Die in der Datei enthaltenen Entities (Tabellen).</summary>
    public List<DataEntity> Entities { get; set; } = [];
}
