using TestCaseManager.Domain.Common;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Eine Entity innerhalb einer Testfalldatei – entspricht einer
/// „Tabelle" (im Dateiformat als <c>b_{Name}</c> notiert, z. B.
/// <c>b_Gebaeude</c>). Eine Entity enthält beliebig viele Zeilen.
/// </summary>
public class DataEntity : EntityBase
{
    /// <summary>Fremdschlüssel zur enthaltenden Datei.</summary>
    public Guid TestCaseFileId { get; set; }

    /// <summary>Enthaltende Datei (Navigationseigenschaft).</summary>
    public TestCaseFile? TestCaseFile { get; set; }

    /// <summary>Name der Entity/Tabelle (z. B. "Gebaeude").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Reihenfolge der Entity innerhalb der Datei.</summary>
    public int SortOrder { get; set; }

    /// <summary>Die Zeilen dieser Entity.</summary>
    public List<DataRow> Rows { get; set; } = [];
}
