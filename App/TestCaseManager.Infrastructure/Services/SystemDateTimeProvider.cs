using TestCaseManager.Application.Common.Interfaces;

namespace TestCaseManager.Infrastructure.Services;

/// <summary>
/// Standardimplementierung des Zeitgebers auf Basis der Systemuhr.
/// In Tests wird stattdessen eine deterministische Implementierung verwendet.
/// </summary>
public class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
