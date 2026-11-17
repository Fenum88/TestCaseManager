using TestCaseManager.Application.Common.Interfaces;

namespace TestCaseManager.Tests.TestSupport;

/// <summary>
/// Deterministischer Zeitgeber für Tests: liefert immer denselben,
/// vorgebbaren Zeitpunkt, sodass Auditfelder überprüfbar sind.
/// </summary>
public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    /// <summary>Der von diesem Zeitgeber gelieferte Zeitpunkt.</summary>
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
}

/// <summary>
/// Fester Benutzerkontext für Tests.
/// </summary>
/// <param name="userName">Der zu liefernde Benutzername.</param>
public sealed class FakeCurrentUserService(string userName) : ICurrentUserService
{
    /// <inheritdoc />
    public string UserName { get; } = userName;
}
