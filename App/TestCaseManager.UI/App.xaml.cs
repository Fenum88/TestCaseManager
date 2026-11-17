using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TestCaseManager.Application;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Infrastructure;
using TestCaseManager.Infrastructure.Services;
using TestCaseManager.Persistence;
using TestCaseManager.UI.Services;
using TestCaseManager.UI.ViewModels;
using TestCaseManager.UI.Views;

namespace TestCaseManager.UI;

/// <summary>
/// Einstiegspunkt der WPF-Anwendung. Baut den Generic Host mit
/// Konfiguration (appsettings.json), Logging (Serilog) und Dependency
/// Injection auf, initialisiert die Datenbank (automatische Migrationen)
/// und zeigt anschließend das Hauptfenster an.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    /// <summary>
    /// Startet Host, Datenbankinitialisierung und Hauptfenster.
    /// Schlägt die Datenbankverbindung fehl, wird der Fehler protokolliert,
    /// dem Benutzer angezeigt und die Anwendung beendet.
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Arbeitsverzeichnis auf den Ordner der ausführbaren Datei setzen.
        // Wird die Anwendung über eine Verknüpfung oder aus einem anderen
        // Verzeichnis gestartet, findet der Host so trotzdem die dort
        // abgelegte appsettings.json und schreibt Logs neben die Anwendung.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        // Unbehandelte UI-Ausnahmen protokollieren statt Absturz ohne Diagnose.
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unbehandelte Ausnahme im UI-Thread.");
            MessageBox.Show(args.Exception.Message, "Unerwarteter Fehler",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            _host = Host.CreateDefaultBuilder(e.Args)
                .UseSerilog((context, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .WriteTo.Debug())
                .ConfigureServices((context, services) =>
                {
                    var connectionString = context.Configuration
                        .GetConnectionString("TestCaseManagerDatabase")
                        ?? throw new InvalidOperationException(
                            "Die Verbindungszeichenfolge 'TestCaseManagerDatabase' fehlt in der appsettings.json.");

                    // Einstellungen des Datei-Ein-/Auslesens aus der Konfiguration lesen.
                    var fileOptions = context.Configuration
                        .GetSection("TestCaseFiles")
                        .Get<TestCaseFileOptions>() ?? new TestCaseFileOptions();

                    // Speicherort und Erstbelegung der Berechnungssysteme festlegen.
                    var calculationFolder = TestCaseFilesPathResolver.ResolveWorkspaceSubfolder(
                        context.Configuration["CalculationSystems:FolderPath"],
                        AppContext.BaseDirectory, "Berechnungssysteme");
                    var seedSystems = context.Configuration
                        .GetSection("CalculationSystems:Systems")
                        .Get<List<CalculationSystem>>() ?? [];
                    var calculationStoreOptions = new CalculationSystemStoreOptions
                    {
                        FilePath = Path.Combine(calculationFolder, "berechnungssysteme.json"),
                        Seed = seedSystems
                    };

                    // Speicherort der Testszenarien (neben den Testfällen).
                    var testCaseRoot = TestCaseFilesPathResolver.Resolve(
                        context.Configuration["TestCaseFiles:RootPath"], AppContext.BaseDirectory);
                    var scenarioStoreOptions = new TestScenarioStoreOptions
                    {
                        FilePath = Path.Combine(testCaseRoot, "szenarien.json")
                    };

                    // Speicherort der Abgleichprofile (je Profil eigene Schlüsselregeln).
                    var compareProfileStoreOptions = new CompareProfileStoreOptions
                    {
                        FilePath = Path.Combine(testCaseRoot, "abgleich-profile.json")
                    };

                    // Schichten gemäß Clean Architecture registrieren.
                    services.AddApplication();
                    services.AddPersistence(connectionString);
                    services.AddInfrastructure(fileOptions, calculationStoreOptions, scenarioStoreOptions,
                        compareProfileStoreOptions);

                    // UI-Dienste, ViewModels und Hauptfenster.
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<DashboardViewModel>();
                    services.AddSingleton<TestCaseListViewModel>();
                    services.AddSingleton<ImportExportViewModel>();
                    services.AddSingleton<CalculationViewModel>();
                    services.AddSingleton<TestScenariosViewModel>();
                    services.AddSingleton<DatabaseViewModel>();
                    services.AddSingleton<SettingsViewModel>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            await _host.StartAsync();

            // Datenbank prüfen und Schema automatisch aktualisieren.
            using (var scope = _host.Services.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
                await initializer.InitializeAsync();
            }

            // Früher lokal gespeicherte Abgleichprofile einmalig übernehmen.
            await _host.Services.GetRequiredService<CompareProfileTakeover>().RunAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Die Anwendung konnte nicht gestartet werden.");
            MessageBox.Show(
                "Die Anwendung konnte nicht gestartet werden." + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                "Bitte prüfen Sie die Datenbankverbindung in der appsettings.json.",
                "Startfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>Fährt den Host geordnet herunter und schließt das Log.</summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
