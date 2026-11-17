using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Services;
using TestCaseManager.Application.Validation;
using TestCaseManager.Persistence;

namespace TestCaseManager.Tests.TestSupport;

/// <summary>
/// Baut eine vollständig verdrahtete Testumgebung auf Basis der
/// EF-Core-InMemory-Datenbank auf: DbContext, Unit of Work und
/// Testfall-Service teilen sich denselben Kontext (wie im echten
/// DI-Scope). Jede Instanz erhält eine eigene, isolierte Datenbank.
/// </summary>
public sealed class InMemoryTestContext : IDisposable
{
    private readonly string _databaseName;

    /// <summary>Der gemeinsam genutzte EF-Core-Kontext.</summary>
    public TestCaseManagerDbContext DbContext { get; }

    /// <summary>Die Arbeitseinheit über dem Kontext.</summary>
    public IUnitOfWork UnitOfWork { get; }

    /// <summary>Der zu testende Testfall-Service.</summary>
    public ITestCaseService TestCaseService { get; }

    /// <summary>Der deterministische Zeitgeber (frei einstellbar).</summary>
    public FakeDateTimeProvider DateTimeProvider { get; }

    /// <summary>Der aktuell konfigurierte Benutzername.</summary>
    public string UserName { get; }

    /// <summary>Erzeugt eine isolierte Testumgebung.</summary>
    /// <param name="userName">Benutzername für Auditfelder und Historie.</param>
    public InMemoryTestContext(string userName = "tester")
    {
        UserName = userName;
        _databaseName = $"tcm-tests-{Guid.NewGuid()}";
        DateTimeProvider = new FakeDateTimeProvider();

        DbContext = BuildContext();
        UnitOfWork = new UnitOfWork(DbContext);
        TestCaseService = new TestCaseService(
            UnitOfWork,
            new TestCaseValidator(),
            NullLogger<TestCaseService>.Instance);
    }

    /// <summary>
    /// Erzeugt einen frischen Kontext auf derselben Datenbank, um Zustände
    /// unabhängig vom Change-Tracker der Hauptinstanz erneut zu laden.
    /// </summary>
    public TestCaseManagerDbContext CreateFreshContext() => BuildContext();

    /// <summary>Erzeugt einen Kontext auf der gemeinsamen InMemory-Datenbank.</summary>
    private TestCaseManagerDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<TestCaseManagerDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        return new TestCaseManagerDbContext(
            options,
            new FakeCurrentUserService(UserName),
            DateTimeProvider);
    }

    /// <inheritdoc />
    public void Dispose() => DbContext.Dispose();
}
