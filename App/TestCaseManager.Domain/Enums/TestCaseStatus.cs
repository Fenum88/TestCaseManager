namespace TestCaseManager.Domain.Enums;

/// <summary>
/// Lebenszyklus-Status eines Testfalls.
/// Neue Statuswerte können gefahrlos ergänzt werden, da der Wert
/// als Zahl persistiert wird und die UI die Anzeige über einen
/// Konverter auflöst.
/// </summary>
public enum TestCaseStatus
{
    /// <summary>Testfall befindet sich in Erstellung und ist noch nicht freigegeben.</summary>
    Draft = 0,

    /// <summary>Testfall wird fachlich geprüft.</summary>
    InReview = 1,

    /// <summary>Testfall ist freigegeben und aktiv im Einsatz.</summary>
    Active = 2,

    /// <summary>Testfall ist vorübergehend deaktiviert.</summary>
    Inactive = 3,

    /// <summary>Testfall ist veraltet und wird nicht mehr verwendet.</summary>
    Deprecated = 4
}
