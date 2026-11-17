using Microsoft.Extensions.DependencyInjection;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.Infrastructure.Services;

namespace TestCaseManager.Infrastructure;

/// <summary>
/// Registriert alle Dienste der Infrastrukturschicht im
/// Dependency-Injection-Container (Querschnittsdienste wie Benutzer
/// und Systemzeit).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Fügt die Infrastrukturdienste dem Container hinzu.
    /// </summary>
    /// <param name="services">Die Dienstsammlung des Hosts.</param>
    /// <param name="fileOptions">
    /// Einstellungen des Ein-/Auslesens. Wird nichts übergeben, gelten die
    /// Standardwerte (Ausgabewerte werden nicht gespeichert).
    /// </param>
    /// <param name="calculationStoreOptions">
    /// Speicherort und Erstbelegung der Berechnungssysteme. Wird nichts
    /// übergeben, gelten leere Standardwerte.
    /// </param>
    /// <param name="scenarioStoreOptions">
    /// Speicherort der Testszenarien. Wird nichts übergeben, gelten leere
    /// Standardwerte.
    /// </param>
    /// <param name="compareProfileStoreOptions">
    /// Speicherort der Abgleichprofile. Wird nichts übergeben, gelten leere
    /// Standardwerte.
    /// </param>
    /// <returns>Dieselbe Dienstsammlung (Fluent-API).</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        TestCaseFileOptions? fileOptions = null,
        CalculationSystemStoreOptions? calculationStoreOptions = null,
        TestScenarioStoreOptions? scenarioStoreOptions = null,
        CompareProfileStoreOptions? compareProfileStoreOptions = null)
    {
        services.AddSingleton<ICurrentUserService, WindowsCurrentUserService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Einstellungen des Datei-Ein-/Auslesens (unternehmensindividuell).
        services.AddSingleton(fileOptions ?? new TestCaseFileOptions());

        // Datei-Service für das Ein-/Auslesen (greift auf das Dateisystem zu).
        services.AddScoped<ITestCaseFileService, TestCaseFileService>();

        // Aufruf externer Berechnungssysteme (zustandslos, daher Singleton).
        services.AddSingleton<ICalculationSystemService, CalculationSystemService>();

        // Vergleich der Ausgaben zweier Berechnungssysteme (zustandslos).
        services.AddSingleton<ICompareService, CompareService>();

        // Persistenter Speicher der Berechnungssysteme (JSON-Datei).
        services.AddSingleton(calculationStoreOptions ?? new CalculationSystemStoreOptions());
        services.AddSingleton<ICalculationSystemStore, JsonCalculationSystemStore>();

        // Persistenter Speicher der Testszenarien (JSON-Datei).
        services.AddSingleton(scenarioStoreOptions ?? new TestScenarioStoreOptions());
        services.AddSingleton<ITestScenarioStore, JsonTestScenarioStore>();

        // Abgleichprofile liegen in der Datenbank, damit alle Nutzer denselben
        // Bestand sehen. Die Optionen halten nur noch den Pfad der früheren
        // JSON-Datei für die einmalige Übernahme.
        services.AddSingleton(compareProfileStoreOptions ?? new CompareProfileStoreOptions());
        services.AddSingleton<ICompareProfileStore, DatabaseCompareProfileStore>();
        services.AddSingleton<CompareProfileTakeover>();

        return services;
    }
}
