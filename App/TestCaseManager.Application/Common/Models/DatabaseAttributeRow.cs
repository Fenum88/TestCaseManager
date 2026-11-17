using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Ein einzelnes, in der Datenbank gespeichertes Testfall-Attribut in flacher
/// Form für die Datenbankansicht: Testfallnummer, Entity, Zeile, Spalte und
/// Wert. Speist sowohl die Anzeige als auch die Filterauswertung.
/// </summary>
public class DatabaseAttributeRow
{
    /// <summary>
    /// Primärschlüssel des Testfalls. Wird benötigt, um aus der Ansicht heraus
    /// den passenden Testfall zum Bearbeiten zu öffnen.
    /// </summary>
    public Guid TestCaseId { get; init; }

    /// <summary>Testfallnummer (z. B. "0001").</summary>
    public string Number { get; init; } = string.Empty;

    /// <summary>Name der Entity/Tabelle (z. B. "Gebaeude").</summary>
    public string Entity { get; init; } = string.Empty;

    /// <summary>Nullbasierter Zeilenindex innerhalb der Entity.</summary>
    public int RowIndex { get; init; }

    /// <summary>Spaltenname (z. B. "tuer", "nachname").</summary>
    public string Column { get; init; } = string.Empty;

    /// <summary>Logischer Wert ohne umschließende Anführungszeichen.</summary>
    public string? Value { get; init; }

    /// <summary>Abgeleiteter Datentyp.</summary>
    public ParameterDataType DataType { get; init; }

    /// <summary>Deutscher Anzeigename des Datentyps.</summary>
    public string DataTypeDisplay => DataType.ToDisplayName();
}
