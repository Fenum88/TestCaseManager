namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Liefert den aktuell angemeldeten Benutzer für Auditfelder,
/// Historie und Audit-Log. Die Implementierung liegt in der
/// Infrastrukturschicht (z. B. Windows-Anmeldename).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Anzeigename bzw. Anmeldename des aktuellen Benutzers.</summary>
    string UserName { get; }
}
