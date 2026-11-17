# Entwicklerdokumentation

Dieser Leitfaden beschreibt Aufbau, Konventionen und typische Erweiterungen des
Projekts.

## Lösungsaufbau

| Projekt | Verantwortung | Wichtige Typen |
|---------|---------------|----------------|
| **Domain** | Fachkern ohne externe Abhängigkeiten | `TestCase`, `TestParameter`, `ExpectedResult`, `Category`, `Tag`, `Attachment`, `TestCaseHistory`, `AuditLogEntry`, `TestCaseFactory`, `EnumDisplayExtensions` |
| **Application** | Verträge, Use-Cases, Validierung, DTOs | `ITestCaseService`, `IUnitOfWork`, `IRepository<T>`, `TestCaseValidator`, `ParameterValueParser`, `TestCaseFilter`, `TestCaseListItem` |
| **Persistence** | EF Core, PostgreSQL | `TestCaseManagerDbContext`, `*Repository`, `UnitOfWork`, `DatabaseInitializer`, `Configurations/*`, `Migrations/*` |
| **Infrastructure** | Querschnittsdienste | `WindowsCurrentUserService`, `SystemDateTimeProvider` |
| **UI** | WPF / MVVM | `App`, `MainWindow`, `*View`, `*ViewModel`, `Converters/*`, `DialogService` |
| **Tests** | xUnit + EF InMemory | `TestCaseServiceTests`, `TestCaseValidatorTests`, `ParameterValueParserTests`, `TestCaseFactoryTests` |

## Konventionen

- **Sprache:** Fachbezeichner und Kommentare auf Deutsch; technische
  Framework-Begriffe englisch. Jede Klasse und öffentliche Methode ist mit
  XML-Kommentaren dokumentiert.
- **Nullable Reference Types** und **Implicit Usings** sind aktiviert.
- **Async durchgängig:** Datenbankzugriffe sind `async` mit `CancellationToken`.
- **Sicherheit:** Ausschließlich EF Core / parametrisierte Abfragen – kein
  dynamisches SQL, damit keine SQL-Injection möglich ist.
- **MVVM:** Views enthalten keine Logik außer dem Auslösen von Ladevorgängen.
  Commands und Zustand liegen in den ViewModels (CommunityToolkit.Mvvm,
  `[ObservableProperty]`, `[RelayCommand]`).

## Erweiterungsszenarien

### 1. Neuen Parameter-Datentyp ergänzen
1. Wert zu `Domain/Enums/ParameterDataType.cs` hinzufügen.
2. Deutschen Namen in `EnumDisplayExtensions.ToDisplayName` ergänzen.
3. Prüfregel in `Application/Common/Validation/ParameterValueParser.cs` ergänzen.

Kein Datenbank- oder UI-Umbau nötig – die Auswahllisten entstehen automatisch aus
dem Enum.

### 2. Neues Feld an einem Testfall
1. Eigenschaft in der Entität (`Domain/Entities/TestCase.cs`) ergänzen.
2. Bei Bedarf Spaltenkonfiguration in `Persistence/Configurations/TestCaseConfiguration.cs`
   (Länge, Pflicht, Index) anpassen.
3. Migration erzeugen (siehe unten). Beim nächsten Start wird die Spalte automatisch angelegt.
4. Feld in `TestCaseEditorViewModel` + `TestCaseEditorWindow.xaml` einbinden.
5. Für die Historie ggf. in `TestCaseManagerDbContext.TestCaseDiffProperties`
   den deutschen Anzeigenamen registrieren.

### 3. Neuen Anwendungsfall / Service
1. Interface in `Application/Services` definieren, Implementierung daneben.
2. In `Application/DependencyInjection.cs` registrieren.
3. Im ViewModel über den DI-Scope (`IServiceScopeFactory`) auflösen und nutzen.

### 4. Neuer Testfalltyp (Spezialisierung)
Dank der Factory (`TestCaseFactory`) und der typneutralen Parameter lassen sich
Varianten ohne Schemaänderung abbilden. Für stark abweichende Typen kann die
Factory um weitere Erzeugungsmethoden (z. B. `CreateForZinsberechnung`) erweitert
werden, ohne aufrufenden Code zu brechen.

## Migrationen

```bash
# Neue Migration
dotnet ef migrations add <Name> \
  --project TestCaseManager.Persistence \
  --startup-project TestCaseManager.Persistence

# Letzte Migration zurücknehmen (nur wenn noch nicht angewendet)
dotnet ef migrations remove \
  --project TestCaseManager.Persistence \
  --startup-project TestCaseManager.Persistence

# SQL-Skript erzeugen (idempotent)
dotnet ef migrations script --idempotent \
  --project TestCaseManager.Persistence \
  --startup-project TestCaseManager.Persistence \
  --output docs/schema.sql
```

Das Werkzeug `dotnet-ef` ist als lokales Tool eingebunden (`.config/dotnet-tools.json`);
bei Bedarf einmalig `dotnet tool restore` ausführen.

Zur **Designzeit** (ohne laufende Datenbank) liefert
`DesignTimeDbContextFactory` den Kontext für das EF-Werkzeug. Im **Betrieb**
übernimmt `DatabaseInitializer.InitializeAsync` beim Start das Anwenden der
Migrationen und das Anlegen der Standardkategorien.

## Tests

- **Framework:** xUnit; Datenzugriff über **EF Core InMemory** (isolierte
  Datenbank je Test, siehe `TestSupport/InMemoryTestContext.cs`).
- **Deterministik:** `FakeDateTimeProvider` und `FakeCurrentUserService` machen
  Auditfelder und Historie überprüfbar.
- **Abdeckung:** Validierung, Wert-Parser, Factory (tiefe Kopie), Service-Logik
  (Speichern, Eindeutigkeit, Kopieren, Löschen, Suche) inklusive der automatischen
  Historien-/Audit-Erzeugung.

```bash
dotnet test
```

## Logging

Serilog schreibt nach `logs/testcasemanager-<Datum>.log` (tägliche Rotation,
14 Tage Aufbewahrung) und in die Debug-Ausgabe. Alle Fehler werden protokolliert;
unbehandelte UI-Ausnahmen fängt `App.DispatcherUnhandledException` ab und meldet
sie dem Benutzer, statt die Anwendung abstürzen zu lassen.

## Startsequenz (UI)

1. `App.OnStartup` baut den Generic Host (Konfiguration, Serilog, DI).
2. Registrierung der Schichten: `AddApplication()`, `AddPersistence(conn)`,
   `AddInfrastructure()`.
3. `DatabaseInitializer.InitializeAsync` prüft/aktualisiert das Schema.
4. `MainWindow` wird aus dem Container aufgelöst und angezeigt; es startet auf dem
   Dashboard.
