# Abhängigkeiten (Bibliotheken, Werkzeuge, Laufzeit)

Stand: 17.11.2026 – ermittelt mit `dotnet list package --include-transitive`,
`--vulnerable`, `--deprecated`, `--outdated`. Lizenzen aus den `.nuspec`-Dateien
der Pakete im lokalen NuGet-Cache.

**Ergebnis der Prüfung**

| Prüfung | Ergebnis |
|---------|----------|
| Bekannte Sicherheitslücken (NuGet Advisory DB, inkl. transitiv) | ✅ keine |
| Abgekündigte Pakete | ⚠️ `xunit` 2.x als „Legacy“ markiert (Nachfolger `xunit.v3`) – nur Testprojekt |
| Lizenzen | ✅ ausschließlich permissiv: MIT, Apache-2.0, PostgreSQL-Lizenz. **Keine** Copyleft-Lizenz (GPL/LGPL/AGPL) |
| Laufzeit | ⚠️ .NET 9 ist seit 10.11.2026 außerhalb des Supports (siehe unten) |

---

## 1. Laufzeit und Werkzeuge

| Komponente | Version | Lizenz | Verwendung | Hinweis |
|------------|---------|--------|------------|---------|
| .NET SDK / Runtime | 9.0 (SDK 9.0.201) | MIT | Alle Projekte (`net9.0`, UI: `net9.0-windows`) | **Support-Ende 10.11.2026** – keine Sicherheitsupdates mehr. Upgrade auf .NET 10 (LTS, Support bis Nov. 2028) empfohlen |
| WPF (Windows Desktop Runtime) | 9.0 | MIT | Oberfläche | Nur unter Windows lauffähig |
| dotnet-ef (lokales Tool) | 9.0.17 | MIT | Nur für Entwickler: Migrationen erzeugen | `App/.config/dotnet-tools.json` |
| PostgreSQL (portable Binaries von EDB) | 16.4 | PostgreSQL-Lizenz | Datenbankserver (optional, per `db-setup.ps1`) | 16.4 ist veraltet; Sicherheitskorrekturen sind in neueren 16.x-Versionen enthalten |
| Python | beliebig, aus `PATH` | PSF | Ausführen der Berechnungssysteme | Nicht Teil des Repos; Skripte nutzen nur Standardbibliothek (`copy`, `os`, `sys`) |

## 2. Direkte NuGet-Pakete

| Paket | Angefordert | Aufgelöst | Lizenz | Projekt | Zweck | Neueste verfügbar |
|-------|-------------|-----------|--------|---------|-------|-------------------|
| ClosedXML | 0.105.0 | 0.105.0 | MIT | Infrastructure, Tests | Excel-Export (`_Gesamtvergleich.xlsx`) | 0.105.1 |
| CommunityToolkit.Mvvm | 8.4.2 | 8.4.2 | MIT | UI | MVVM (ObservableObject, RelayCommand) | aktuell |
| Microsoft.EntityFrameworkCore.Design | 9.0.1 | 9.0.1 | MIT | Persistence | Migrationen (nur Design-Time, `PrivateAssets=all`) | 11.0.0 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 9.0.* | 9.0.23 | MIT | Application | DI | 11.0.0 |
| Microsoft.Extensions.Logging.Abstractions | 9.0.* | 9.0.23 | MIT | Application | Logging-Schnittstellen | 11.0.0 |
| Microsoft.Extensions.Hosting | 9.0.* | 9.0.23 | MIT | UI | Generic Host, Konfiguration | 11.0.0 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.4 | 9.0.4 | PostgreSQL | Persistence | EF-Core-Provider für PostgreSQL | 10.0.3 |
| Serilog.Extensions.Hosting | 10.0.0 | 10.0.0 | Apache-2.0 | UI | Logging-Integration | aktuell |
| Serilog.Settings.Configuration | 10.0.1 | 10.0.1 | Apache-2.0 | UI | Logging-Konfiguration aus `appsettings.json` | aktuell |
| Serilog.Sinks.Debug | 3.0.0 | 3.0.0 | Apache-2.0 | UI | Log-Ausgabe im Debugger | aktuell |
| Serilog.Sinks.File | 7.0.0 | 7.0.0 | Apache-2.0 | UI | Logdateien | aktuell |
| *Nur Testprojekt:* | | | | | | |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.1 | 9.0.1 | MIT | Tests | In-Memory-DB für Tests | 11.0.0 |
| Microsoft.NET.Test.Sdk | 17.12.0 | 17.12.0 | MIT | Tests | Testinfrastruktur | 18.12.0 |
| xunit | 2.9.2 | 2.9.2 | Apache-2.0 | Tests | Testframework (**Legacy**) | 2.9.3 / xunit.v3 |
| xunit.runner.visualstudio | 2.8.2 | 2.8.2 | Apache-2.0 | Tests | Testrunner | 4.0.1 |
| coverlet.collector | 6.0.2 | 6.0.2 | MIT | Tests | Codeabdeckung | 10.1.1 |

> Pakete der Version 10.x/11.x von Microsoft.* setzen teils .NET 10/11 voraus –
> sie gehören zum Laufzeit-Upgrade, nicht zu einem einfachen Paket-Update.

## 3. Transitive Pakete (indirekt mitgeladen)

Alle unten genannten Pakete werden automatisch über die direkten Pakete
eingebunden. Mehrfachversionen entstehen, weil Serilog 10 einige
`Microsoft.Extensions.*`-Pakete in Version 10.0.0 nachzieht; NuGet löst je
Projekt die höchste Version auf.

| Paket | Version(en) | Lizenz | Kommt über |
|-------|-------------|--------|------------|
| ClosedXML.Parser | 2.0.0 | MIT | ClosedXML |
| DocumentFormat.OpenXml, …Framework | 3.1.1 | MIT | ClosedXML |
| ExcelNumberFormat | 1.1.0 | MIT | ClosedXML |
| RBush.Signed | 4.0.0 | MIT | ClosedXML |
| SixLabors.Fonts | 1.0.0 | Apache-2.0 | ClosedXML |
| System.IO.Packaging | 8.0.1 | MIT | DocumentFormat.OpenXml |
| Npgsql | 9.0.3 | PostgreSQL | Npgsql.EntityFrameworkCore.PostgreSQL |
| Microsoft.EntityFrameworkCore, …Abstractions, …Analyzers, …Relational | 9.0.1 | MIT | EF Core / Npgsql |
| Microsoft.Extensions.Caching.Abstractions, …Caching.Memory | 9.0.1 | MIT | EF Core |
| Microsoft.Extensions.Configuration (+ .Abstractions, .Binder, .CommandLine, .EnvironmentVariables, .FileExtensions, .Json, .UserSecrets) | 9.0.1 – 10.0.0 | MIT | Hosting / Serilog |
| Microsoft.Extensions.DependencyInjection, …DependencyModel | 9.0.1, 10.0.0 | MIT | Hosting / EF Core |
| Microsoft.Extensions.Diagnostics, …Diagnostics.Abstractions | 9.0.23, 10.0.0 | MIT | Hosting |
| Microsoft.Extensions.FileProviders.Abstractions, …Physical, …FileSystemGlobbing | 9.0.23, 10.0.0 | MIT | Hosting |
| Microsoft.Extensions.Hosting.Abstractions | 10.0.0 | MIT | Serilog.Extensions.Hosting |
| Microsoft.Extensions.Logging (+ .Configuration, .Console, .Debug, .EventLog, .EventSource) | 9.0.1 – 10.0.0 | MIT | Hosting |
| Microsoft.Extensions.Options, …ConfigurationExtensions, …Primitives | 9.0.1 – 10.0.0 | MIT | Hosting |
| Serilog | 4.3.0 | Apache-2.0 | Serilog.* |
| Serilog.Extensions.Logging | 10.0.0 | Apache-2.0 | Serilog.Extensions.Hosting |
| System.Diagnostics.DiagnosticSource | 10.0.0 | MIT | Hosting |
| System.Diagnostics.EventLog | 9.0.23 | MIT | Logging.EventLog |
| System.Text.Json, System.Text.Encodings.Web | 9.0.1, 10.0.0 | MIT | diverse |
| System.IO.Pipelines | 7.0.0, 10.0.0 | MIT | diverse |
| *Nur Build-/Design-Time (EF-Tools, landen nicht in der App):* | | | |
| Microsoft.CodeAnalysis.* (Analyzers, Common, CSharp, Workspaces, MSBuild) | 3.3.4 / 4.8.0 | MIT | EF Core Design |
| Microsoft.Build.Framework, Microsoft.Build.Locator | 17.8.3, 1.7.8 | MIT | EF Core Design |
| Humanizer.Core | 2.14.1 | MIT | EF Core Design |
| Mono.TextTemplating, System.CodeDom | 3.0.0, 6.0.0 | MIT | EF Core Design |
| System.Composition.* | 7.0.0 | MIT | Roslyn |
| System.Collections.Immutable, System.Reflection.Metadata, System.Threading.Channels, Microsoft.Bcl.AsyncInterfaces, System.Runtime.CompilerServices.Unsafe | 1.6.0 – 7.0.0 | MIT | Roslyn |
| *Nur Testprojekt:* | | | |
| Microsoft.CodeCoverage, Microsoft.TestPlatform.ObjectModel, …TestHost | 17.12.0 | MIT | Microsoft.NET.Test.Sdk |
| Newtonsoft.Json | 13.0.1 | MIT | TestPlatform |
| xunit.abstractions, .analyzers, .assert, .core, .extensibility.* | 2.0.3 – 2.9.2 | Apache-2.0 | xunit |

Gesamt: 16 direkte + 78 transitive Pakete.

## 4. Prüfung wiederholen

```powershell
cd App
dotnet list TestCaseManager.sln package --vulnerable --include-transitive
dotnet list TestCaseManager.sln package --deprecated
dotnet list TestCaseManager.sln package --outdated
```
