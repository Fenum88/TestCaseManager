using TestCaseManager.Domain.Common;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Ein einzelner Wert (eine „Zelle") einer Zeile – im Dateiformat als
/// <c>{Spalte}={Wert}</c> notiert (z. B. <c>tuer=2</c> oder
/// <c>baujahr="01.01.1965"</c>).
///
/// Für eine verlustfreie Rekonstruktion wird neben dem logischen Wert auch
/// festgehalten, ob er in der Datei in Anführungszeichen stand
/// (<see cref="IsQuoted"/>), sowie ein abgeleiteter Datentyp für Anzeige
/// und Auswertung.
/// </summary>
public class DataValue : EntityBase
{
    /// <summary>Fremdschlüssel zur enthaltenden Zeile.</summary>
    public Guid DataRowId { get; set; }

    /// <summary>Enthaltende Zeile (Navigationseigenschaft).</summary>
    public DataRow? DataRow { get; set; }

    /// <summary>Spaltenname (z. B. "tuer", "baujahr").</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>Logischer Wert ohne umschließende Anführungszeichen.</summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gibt an, ob der Wert in der Datei in Anführungszeichen stand.
    /// Steuert die exakte Rekonstruktion (<c>key="wert"</c> vs. <c>key=wert</c>).
    /// </summary>
    public bool IsQuoted { get; set; }

    /// <summary>Aus dem Wert abgeleiteter Datentyp (für Anzeige/Auswertung).</summary>
    public ParameterDataType DataType { get; set; } = ParameterDataType.Text;

    /// <summary>Reihenfolge des Wertes innerhalb der Zeile.</summary>
    public int SortOrder { get; set; }
}
