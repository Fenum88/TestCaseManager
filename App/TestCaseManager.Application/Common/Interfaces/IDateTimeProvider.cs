namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Abstraktion der Systemzeit, damit Zeitstempel in Tests
/// deterministisch gesetzt werden können.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Aktueller Zeitpunkt in UTC.</summary>
    DateTime UtcNow { get; }
}
