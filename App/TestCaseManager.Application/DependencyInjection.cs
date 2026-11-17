using Microsoft.Extensions.DependencyInjection;
using TestCaseManager.Application.FileFormat;
using TestCaseManager.Application.Services;
using TestCaseManager.Application.Validation;

namespace TestCaseManager.Application;

/// <summary>
/// Registriert alle Dienste der Application-Schicht im
/// Dependency-Injection-Container. Die Services sind "scoped", damit
/// pro Dialog bzw. Vorgang eine eigene Arbeitseinheit verwendet wird.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Fügt die Application-Dienste dem Container hinzu.
    /// </summary>
    /// <param name="services">Die Dienstsammlung des Hosts.</param>
    /// <returns>Dieselbe Dienstsammlung (Fluent-API).</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<TestCaseValidator>();

        services.AddScoped<ITestCaseService, TestCaseService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Format-Module für das dateibasierte Ein-/Auslesen. Diese beiden
        // Registrierungen sind der unternehmensindividuelle Anpassungspunkt:
        // Für ein abweichendes Dateiformat wird hier eine eigene
        // Parser-/Writer-Implementierung eingetragen.
        services.AddSingleton<ITestCaseFileParser, IndentedKeyValueFileParser>();
        services.AddSingleton<ITestCaseFileWriter, IndentedKeyValueFileWriter>();

        return services;
    }
}
