# Architekturübersicht

Die Anwendung folgt der **Clean Architecture** (Schichtenarchitektur mit
Abhängigkeiten ausschließlich nach innen). Der fachliche Kern kennt weder
Datenbank noch Oberfläche; technische Details liegen in den äußeren Schichten und
werden über Interfaces entkoppelt.

## Schichten und Abhängigkeitsrichtung

```mermaid
flowchart TD
    UI["TestCaseManager.UI<br/>(WPF, MVVM, Host, DI-Verdrahtung)"]
    INF["TestCaseManager.Infrastructure<br/>(CurrentUser, DateTime)"]
    PER["TestCaseManager.Persistence<br/>(DbContext, Repositories, UnitOfWork, Migrationen)"]
    APP["TestCaseManager.Application<br/>(Interfaces, Services, Validierung, DTOs)"]
    DOM["TestCaseManager.Domain<br/>(Entitäten, Enums, Fabriken)"]

    UI --> APP
    UI --> PER
    UI --> INF
    PER --> APP
    INF --> APP
    APP --> DOM
    PER --> DOM

    style DOM fill:#CDE2FB,stroke:#2A78D6
    style APP fill:#E5F0FC,stroke:#2A78D6
```

- **Domain** – reiner Fachkern ohne externe Abhängigkeiten: Entitäten, Enums,
  die `TestCaseFactory` und deutsche Anzeigenamen (`EnumDisplayExtensions`).
- **Application** – definiert die **Verträge** (Repository-, Unit-of-Work-,
  Service-Interfaces), die **Anwendungsfälle** (`TestCaseService` usw.), die
  **Validierung** und die **DTOs** für die Anzeige. Kennt nur die Domäne.
- **Persistence** – implementiert die Verträge mit Entity Framework Core und
  PostgreSQL: `DbContext`, Repositories, `UnitOfWork`, `DatabaseInitializer`,
  Migrationen. Erzeugt Auditfelder, Historie und Audit-Log **automatisch** beim
  Speichern.
- **Infrastructure** – Querschnittsdienste (aktueller Windows-Benutzer, Systemzeit).
- **UI** – WPF-Oberfläche nach dem **MVVM**-Muster, Host-Aufbau, Dependency
  Injection, Konfiguration und Logging.

Weil die inneren Schichten die äußeren nicht kennen, lassen sich Datenbank oder
Oberfläche austauschen, ohne die Geschäftslogik zu berühren.

## Eingesetzte Design Patterns

| Pattern | Umsetzung | Zweck |
|---------|-----------|-------|
| **Repository** | `IRepository<T>`, `ITestCaseRepository`, … in *Application*; Implementierungen in *Persistence* | Kapselt den Datenzugriff hinter fachlichen Schnittstellen |
| **Unit of Work** | `IUnitOfWork` / `UnitOfWork` | Bündelt Repositories und speichert atomar in einer Transaktion |
| **Service Layer** | `TestCaseService`, `DashboardService`, … | Bündelt die Anwendungsfälle / Geschäftslogik |
| **Factory** | `TestCaseFactory` | Erzeugt neue Testfälle und tiefe Kopien mit sinnvollen Standardwerten |
| **Dependency Injection** | `Microsoft.Extensions.DependencyInjection`, `AddApplication/AddPersistence/AddInfrastructure` | Lose Kopplung, Testbarkeit |
| **MVVM** | Views + ViewModels (CommunityToolkit.Mvvm) | Trennt Darstellung von Logik |
| **Options/Configuration** | `appsettings.json` + Host | Zentralisierte Einstellungen |

## Lebensdauern (Dependency Injection)

- **Scoped** (`DbContext`, `UnitOfWork`, alle Services): Pro Vorgang/Dialog wird
  über die `IServiceScopeFactory` ein eigener Bereich geöffnet. Alle in diesem
  Bereich geladenen Entitäten werden von **derselben** Arbeitseinheit nachverfolgt
  und beim Speichern gemeinsam persistiert. Das entspricht dem Muster
  „Unit of Work pro Bildschirm/Dialog“ und vermeidet langlebige DbContexts.
- **Singleton** (`ICurrentUserService`, `IDateTimeProvider`, `TestCaseValidator`,
  Haupt-ViewModels, `IDialogService`): zustandsarme bzw. anwendungsweite Dienste.

## Automatische Protokollierung im DbContext

Der `TestCaseManagerDbContext` überschreibt `SaveChangesAsync` und erledigt dort
drei Querschnittsaufgaben, sodass keine Codestelle sie „vergessen“ kann:

1. **Auditfelder** (`CreatedAt/By`, `ModifiedAt/By`) füllen.
2. **Änderungshistorie** (`TestCaseHistory`) je geänderter Eigenschaft mit altem
   und neuem Wert schreiben – auch für Parameter, Ergebnisse, Anhänge und Tags.
3. **Audit-Log** (`AuditLogEntry`) je betroffenem Datensatz schreiben.

## Klassendiagramm (Kernklassen)

```mermaid
classDiagram
    class EntityBase {
        +Guid Id
    }
    class AuditableEntity {
        +DateTime CreatedAt
        +string CreatedBy
        +DateTime? ModifiedAt
        +string? ModifiedBy
    }
    class TestCase {
        +string Name
        +string? Description
        +string Version
        +TestCaseStatus Status
        +Guid? CategoryId
        +List~TestParameter~ Parameters
        +List~ExpectedResult~ ExpectedResults
        +List~Attachment~ Attachments
        +List~Tag~ Tags
        +List~TestCaseHistory~ History
    }
    class TestParameter {
        +string Name
        +ParameterDataType DataType
        +string? Value
        +string? DefaultValue
        +bool IsRequired
    }
    class ExpectedResult {
        +string Name
        +ParameterDataType DataType
        +string ExpectedValue
        +string? Tolerance
    }
    class Category
    class Tag
    class Attachment
    class TestCaseHistory
    class AuditLogEntry

    EntityBase <|-- AuditableEntity
    AuditableEntity <|-- TestCase
    AuditableEntity <|-- Category
    AuditableEntity <|-- Attachment
    EntityBase <|-- TestParameter
    EntityBase <|-- ExpectedResult
    EntityBase <|-- Tag
    EntityBase <|-- TestCaseHistory
    EntityBase <|-- AuditLogEntry

    TestCase "1" --> "*" TestParameter
    TestCase "1" --> "*" ExpectedResult
    TestCase "1" --> "*" Attachment
    TestCase "1" --> "*" TestCaseHistory
    TestCase "*" --> "*" Tag
    TestCase "*" --> "0..1" Category
```

```mermaid
classDiagram
    class ITestCaseService {
        <<interface>>
        +SearchAsync(filter)
        +GetWithDetailsAsync(id)
        +SaveAsync(testCase, category, tags)
        +DeleteAsync(id)
        +CopyAsync(sourceId)
    }
    class IUnitOfWork {
        <<interface>>
        +ITestCaseRepository TestCases
        +ICategoryRepository Categories
        +ITagRepository Tags
        +IAuditLogRepository AuditLog
        +SaveChangesAsync()
    }
    class TestCaseService
    class UnitOfWork
    class TestCaseRepository
    class TestCaseValidator
    class TestCaseManagerDbContext

    ITestCaseService <|.. TestCaseService
    IUnitOfWork <|.. UnitOfWork
    TestCaseService --> IUnitOfWork
    TestCaseService --> TestCaseValidator
    UnitOfWork --> TestCaseRepository
    TestCaseRepository --> TestCaseManagerDbContext
    UnitOfWork --> TestCaseManagerDbContext
```

## Ablauf: Testfall speichern

```mermaid
sequenceDiagram
    participant V as EditorView
    participant VM as TestCaseEditorViewModel
    participant S as TestCaseService
    participant U as UnitOfWork
    participant DB as DbContext (PostgreSQL)

    V->>VM: Speichern
    VM->>S: SaveAsync(testCase, category, tags)
    S->>S: Validieren (Pflichtfelder, Datentypen, Längen)
    S->>U: NameExistsAsync? (Eindeutigkeit)
    S->>U: Kategorie/Tags auflösen (ggf. anlegen)
    S->>U: SaveChangesAsync
    U->>DB: SaveChangesAsync
    DB->>DB: Auditfelder, Historie & Audit-Log erzeugen
    DB-->>VM: Erfolg
    VM-->>V: Dialog schließen
```
