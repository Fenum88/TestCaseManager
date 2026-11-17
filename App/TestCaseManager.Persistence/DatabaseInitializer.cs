using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence;

/// <summary>
/// Prüft beim Anwendungsstart das Datenbankschema und bringt es über
/// EF-Core-Migrationen automatisch auf den aktuellen Stand:
/// <list type="bullet">
/// <item>Legt die Datenbank an, falls sie noch nicht existiert.</item>
/// <item>Erzeugt fehlende Tabellen, Spalten, Beziehungen und Indizes.</item>
/// <item>Befüllt Stammdaten (Standardkategorien) beim ersten Start.</item>
/// </list>
/// Manuelle SQL-Skripte sind nicht erforderlich.
/// </summary>
public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly TestCaseManagerDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    /// <summary>Erzeugt den Initialisierer.</summary>
    /// <param name="context">Der EF-Core-Kontext.</param>
    /// <param name="logger">Logger für Statusmeldungen.</param>
    public DatabaseInitializer(TestCaseManagerDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Prüfe Datenbankschema ...");

        var pendingMigrations = (await _context.Database
            .GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count > 0)
        {
            _logger.LogInformation(
                "Wende {Count} ausstehende Migration(en) an: {Migrations}",
                pendingMigrations.Count, string.Join(", ", pendingMigrations));

            await _context.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Datenbankschema ist jetzt aktuell.");
        }
        else
        {
            _logger.LogInformation("Datenbankschema ist aktuell – keine Migration erforderlich.");
        }

        await SeedDefaultDataAsync(cancellationToken);
    }

    /// <summary>
    /// Legt beim allerersten Start sinnvolle Standardkategorien an,
    /// damit die Anwendung nicht mit leeren Auswahllisten beginnt.
    /// </summary>
    private async Task SeedDefaultDataAsync(CancellationToken cancellationToken)
    {
        if (await _context.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Lege Standardkategorien an ...");

        _context.Categories.AddRange(
            new Category { Name = "Allgemein", Description = "Nicht näher klassifizierte Testfälle" },
            new Category { Name = "Zinsberechnung", Description = "Testfälle für Zins- und Barwertberechnungen" },
            new Category { Name = "Rundungsregeln", Description = "Testfälle für kaufmännische und mathematische Rundung" },
            new Category { Name = "Grenzwerte", Description = "Testfälle für Rand- und Extremwerte" });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
