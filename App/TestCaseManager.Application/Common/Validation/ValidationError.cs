namespace TestCaseManager.Application.Common.Validation;

/// <summary>
/// Ein einzelner Validierungsfehler mit Bezug auf das betroffene Feld.
/// </summary>
/// <param name="PropertyName">Betroffenes Feld (z. B. "Name" oder "Parameter 'Zinssatz'").</param>
/// <param name="Message">Verständliche Fehlermeldung für den Benutzer.</param>
public record ValidationError(string PropertyName, string Message)
{
    /// <summary>Formatierte Darstellung für Meldungslisten.</summary>
    public override string ToString() => $"{PropertyName}: {Message}";
}
