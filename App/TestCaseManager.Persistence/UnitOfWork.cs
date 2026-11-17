using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Persistence.Repositories;

namespace TestCaseManager.Persistence;

/// <summary>
/// Implementierung des Unit-of-Work-Patterns: Alle Repositories teilen
/// sich denselben <see cref="TestCaseManagerDbContext"/>, sodass
/// <see cref="SaveChangesAsync"/> sämtliche Änderungen einer
/// Arbeitseinheit in einer einzigen Datenbanktransaktion persistiert.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly TestCaseManagerDbContext _context;

    private ITestCaseRepository? _testCases;
    private ICategoryRepository? _categories;
    private ITagRepository? _tags;
    private IAuditLogRepository? _auditLog;
    private IImportedTestCaseRepository? _importedTestCases;
    private ICompareProfileRepository? _compareProfiles;

    /// <summary>Erzeugt die Arbeitseinheit für den übergebenen Kontext.</summary>
    /// <param name="context">Der von DI bereitgestellte, scoped DbContext.</param>
    public UnitOfWork(TestCaseManagerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public ITestCaseRepository TestCases => _testCases ??= new TestCaseRepository(_context);

    /// <inheritdoc />
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);

    /// <inheritdoc />
    public ITagRepository Tags => _tags ??= new TagRepository(_context);

    /// <inheritdoc />
    public IAuditLogRepository AuditLog => _auditLog ??= new AuditLogRepository(_context);

    /// <inheritdoc />
    public IImportedTestCaseRepository ImportedTestCases
        => _importedTestCases ??= new ImportedTestCaseRepository(_context);

    /// <inheritdoc />
    public ICompareProfileRepository CompareProfiles
        => _compareProfiles ??= new CompareProfileRepository(_context);

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
