namespace TestCaseManager.Application.Common.Interfaces;

/// <summary>
/// Unit-of-Work-Pattern: bündelt alle Repositories einer Arbeitseinheit
/// und persistiert deren Änderungen atomar in einer Transaktion.
/// Auditfelder, Historie und Audit-Log werden beim Speichern automatisch
/// von der Persistenzschicht ergänzt.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Repository für Testfälle.</summary>
    ITestCaseRepository TestCases { get; }

    /// <summary>Repository für Kategorien.</summary>
    ICategoryRepository Categories { get; }

    /// <summary>Repository für Tags.</summary>
    ITagRepository Tags { get; }

    /// <summary>Repository für das Audit-Log (nur lesend).</summary>
    IAuditLogRepository AuditLog { get; }

    /// <summary>Repository für Datei-Testfälle (Import-/Export-Subsystem).</summary>
    IImportedTestCaseRepository ImportedTestCases { get; }

    /// <summary>Repository für die gemeinsam genutzten Abgleichprofile.</summary>
    ICompareProfileRepository CompareProfiles { get; }

    /// <summary>
    /// Persistiert alle vorgemerkten Änderungen atomar.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Anzahl der geschriebenen Datensätze.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
