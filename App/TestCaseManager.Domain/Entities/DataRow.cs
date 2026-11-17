using TestCaseManager.Domain.Common;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Eine Zeile einer Entity – im Dateiformat als <c>({Index})_{Entity}</c>
/// notiert (z. B. <c>(0)_Gebaeude</c>). Eine Zeile enthält beliebig viele
/// Werte (Spalte=Wert); verschiedene Zeilen derselben Entity dürfen
/// unterschiedliche Spalten besitzen.
/// </summary>
public class DataRow : EntityBase
{
    /// <summary>Fremdschlüssel zur enthaltenden Entity.</summary>
    public Guid DataEntityId { get; set; }

    /// <summary>Enthaltende Entity (Navigationseigenschaft).</summary>
    public DataEntity? DataEntity { get; set; }

    /// <summary>Nullbasierter Zeilenindex innerhalb der Entity.</summary>
    public int RowIndex { get; set; }

    /// <summary>Die Werte (Spalten) dieser Zeile.</summary>
    public List<DataValue> Values { get; set; } = [];
}
