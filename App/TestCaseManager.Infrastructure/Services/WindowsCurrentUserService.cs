using TestCaseManager.Application.Common.Interfaces;

namespace TestCaseManager.Infrastructure.Services;

/// <summary>
/// Liefert den Windows-Anmeldenamen des aktuellen Benutzers für
/// Auditfelder, Historie und Audit-Log.
/// </summary>
public class WindowsCurrentUserService : ICurrentUserService
{
    /// <inheritdoc />
    public string UserName { get; } = Environment.UserName;
}
