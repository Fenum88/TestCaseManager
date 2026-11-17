using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestCaseManager.Application.Common.Interfaces;

namespace TestCaseManager.Persistence;

/// <summary>
/// Registriert alle Dienste der Persistenzschicht im
/// Dependency-Injection-Container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Fügt DbContext (PostgreSQL/Npgsql), Unit of Work und
    /// Datenbank-Initialisierer dem Container hinzu.
    /// </summary>
    /// <param name="services">Die Dienstsammlung des Hosts.</param>
    /// <param name="connectionString">PostgreSQL-Verbindungszeichenfolge aus der appsettings.json.</param>
    /// <returns>Dieselbe Dienstsammlung (Fluent-API).</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<TestCaseManagerDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }
}
