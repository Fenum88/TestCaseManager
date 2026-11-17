namespace TestCaseManager.Domain.Enums;

/// <summary>
/// Liefert deutsche Anzeigenamen für die Aufzählungstypen der Domäne.
/// Zentrale, einzige Quelle dieser Bezeichnungen: Sowohl die
/// Persistenzschicht (lesbare Historie) als auch die Benutzeroberfläche
/// greifen darauf zu, damit Statustexte überall identisch sind.
/// </summary>
public static class EnumDisplayExtensions
{
    /// <summary>Deutscher Anzeigename eines Testfall-Status.</summary>
    /// <param name="status">Der Status.</param>
    public static string ToDisplayName(this TestCaseStatus status) => status switch
    {
        TestCaseStatus.Draft => "Entwurf",
        TestCaseStatus.InReview => "In Prüfung",
        TestCaseStatus.Active => "Aktiv",
        TestCaseStatus.Inactive => "Inaktiv",
        TestCaseStatus.Deprecated => "Veraltet",
        _ => status.ToString()
    };

    /// <summary>Deutscher Anzeigename eines Parameter-Datentyps.</summary>
    /// <param name="dataType">Der Datentyp.</param>
    public static string ToDisplayName(this ParameterDataType dataType) => dataType switch
    {
        ParameterDataType.Text => "Text",
        ParameterDataType.Integer => "Ganzzahl",
        ParameterDataType.Decimal => "Dezimalzahl",
        ParameterDataType.Boolean => "Ja/Nein",
        ParameterDataType.Date => "Datum",
        ParameterDataType.DateTime => "Datum + Uhrzeit",
        ParameterDataType.Json => "JSON",
        _ => dataType.ToString()
    };

    /// <summary>Deutscher Anzeigename einer Änderungsart.</summary>
    /// <param name="changeType">Die Änderungsart.</param>
    public static string ToDisplayName(this ChangeType changeType) => changeType switch
    {
        ChangeType.Created => "Angelegt",
        ChangeType.Modified => "Geändert",
        ChangeType.Deleted => "Gelöscht",
        _ => changeType.ToString()
    };
}
