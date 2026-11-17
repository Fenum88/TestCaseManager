using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TestCaseManager.Persistence;

/// <summary>
/// Erzeugt den DbContext zur Designzeit (für <c>dotnet ef migrations</c>).
/// Die Verbindungszeichenfolge dient nur der Modellerstellung – für das
/// Generieren von Migrationen ist keine erreichbare Datenbank nötig.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TestCaseManagerDbContext>
{
    /// <inheritdoc />
    public TestCaseManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestCaseManagerDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=testcasemanager;Username=postgres;Password=postgres");

        return new TestCaseManagerDbContext(optionsBuilder.Options);
    }
}
