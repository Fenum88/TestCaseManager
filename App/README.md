# TestCaseManager – Testfallverwaltung für ein Berechnungssystem

Eine moderne, wartbare und erweiterbare Desktop-Anwendung (**.NET 9 / WPF**) zur
Verwaltung fiktiver Testfälle für ein Berechnungssystem. Testfälle werden in einer
**PostgreSQL**-Datenbank gespeichert; das Datenbankschema wird beim Start
automatisch angelegt und aktualisiert (Entity Framework Core mit Migrationen).

---

## Funktionsüberblick

- **Dashboard** – Anzahl Testfälle, Statusverteilung, Kategorien, letzte Änderungen
  und Audit-Log auf einen Blick.
- **Testfallverwaltung** – Testfälle anlegen, bearbeiten, kopieren, löschen, suchen,
  filtern und sortieren.
- **Detailansicht** mit Stammdaten, **beliebig vielen Eingabeparametern**,
  **beliebig vielen erwarteten Ergebnissen**, **Anhängen** (Dokumente, Bilder, PDFs)
  und vollständiger **Änderungshistorie**.
- **Validierung** – Pflichtfelder, Datentypen, Maximallängen, eindeutige Namen,
  eindeutige Parameternamen.
- **Historie & Audit-Log** – jede Änderung wird mit Benutzer, Zeitpunkt sowie altem
  und neuem Wert automatisch protokolliert.
- **Automatische Schemaverwaltung** – Tabellen, Spalten, Beziehungen und Indizes
  werden ohne manuelle SQL-Skripte erzeugt und aktualisiert.
- **Datei-Import / -Export** – dateibasierte Testfälle (Ordner `Workspace/Testfälle`)
  in die Datenbank einlesen und inhaltsgleich wieder auslesen; das Dateiformat ist
  über austauschbare Module unternehmensindividuell anpassbar.
  Siehe **[docs/DateiImportExport.md](docs/DateiImportExport.md)**.

---

## Technologie-Stack

| Bereich            | Technologie |
|--------------------|-------------|
| Sprache / Runtime  | C# 13, .NET 9 |
| Oberfläche         | WPF, MVVM, CommunityToolkit.Mvvm |
| Datenzugriff       | Entity Framework Core 9, Npgsql (PostgreSQL) |
| Dependency Injection / Host | Microsoft.Extensions.Hosting |
| Logging            | Serilog (Datei + Debug) |
| Konfiguration      | appsettings.json |
| Tests              | xUnit, EF Core InMemory |

---

## Projektstruktur (Clean Architecture)

```
TestCaseManager.sln
├── TestCaseManager.Domain          Kern: Entitäten, Enums, Fabriken (keine Abhängigkeiten)
├── TestCaseManager.Application      Interfaces, Services, Validierung (DTOs, Use-Cases)
├── TestCaseManager.Persistence      EF Core: DbContext, Repositories, Unit of Work, Migrationen
├── TestCaseManager.Infrastructure   Querschnittsdienste (aktueller Benutzer, Systemzeit)
├── TestCaseManager.UI               WPF-Oberfläche (Views, ViewModels, Konverter, Host)
└── TestCaseManager.Tests            Unit- und Integrationstests
```

Die Abhängigkeiten zeigen ausschließlich nach innen (UI → Application → Domain).
Details siehe **[docs/Architecture.md](docs/Architecture.md)** und
**[docs/DataModel.md](docs/DataModel.md)**.

---

## Voraussetzungen

- [.NET 9 SDK](https://dotnet.microsoft.com/download) oder neuer
- **PostgreSQL 13+** (lokal oder im Netzwerk)
- Windows (WPF-Oberfläche)

> Die Datenbank kann administrativ mit **pgAdmin**, **SSMS mit PostgreSQL-Treiber**
> oder einem beliebigen SQL-Werkzeug verwaltet werden. Die Anwendung kommuniziert
> direkt über den PostgreSQL-Treiber (Npgsql) und benötigt **keine** manuellen
> SQL-Skripte.

---

## Installation & Start

> **Schnellster Weg (ohne eigene PostgreSQL-Installation):** siehe
> **[START.md](START.md)** – richtet mit `.\db-setup.ps1` eine projekt-lokale,
> „virtuelle" PostgreSQL-Datenbank ein (Port 5433) und startet die App.
> Die folgenden Schritte beschreiben die Einrichtung mit einem **eigenen**
> PostgreSQL-Server.

1. **Repository beziehen** und in das Projektverzeichnis wechseln.

2. **PostgreSQL-Datenbank bereitstellen.** Es genügt ein leerer, erreichbarer
   Datenbankserver samt Zugangsdaten. Die eigentliche Datenbank und alle Tabellen
   legt die Anwendung beim ersten Start selbst an. Eine leere Zieldatenbank
   anlegen (Beispiel):

   ```sql
   CREATE DATABASE testcasemanager;
   ```

3. **Verbindungszeichenfolge anpassen** in
   [`TestCaseManager.UI/appsettings.json`](TestCaseManager.UI/appsettings.json):

   ```json
   "ConnectionStrings": {
     "TestCaseManagerDatabase": "Host=localhost;Port=5432;Database=testcasemanager;Username=postgres;Password=DEIN_PASSWORT"
   }
   ```

4. **Anwendung starten:**

   ```bash
   dotnet run --project TestCaseManager.UI
   ```

   Beim Start prüft die Anwendung das Schema, wendet ausstehende Migrationen an
   (Tabellen/Spalten/Indizes) und legt beim allerersten Start einige
   Standardkategorien an.

---

## Build & Tests

```bash
# Gesamte Projektmappe bauen
dotnet build

# Alle Tests ausführen
dotnet test
```

---

## Datenbankschema aktualisieren (für Entwickler)

Das Schema wird über EF-Core-Migrationen verwaltet. Nach einer Modelländerung:

```bash
# Neue Migration erzeugen
dotnet ef migrations add <Name> --project TestCaseManager.Persistence --startup-project TestCaseManager.Persistence

# Optional: reines SQL-Skript erzeugen (idempotent)
dotnet ef migrations script --idempotent --project TestCaseManager.Persistence --startup-project TestCaseManager.Persistence --output docs/schema.sql
```

Die Anwendung wendet neue Migrationen beim nächsten Start automatisch an –
ein manueller Schritt ist im Betrieb nicht erforderlich.

Ein aktuelles, vollständiges Schemaskript liegt unter
**[docs/schema.sql](docs/schema.sql)**.

---

## Konfiguration

Alle Einstellungen liegen in `TestCaseManager.UI/appsettings.json`:

- `ConnectionStrings:TestCaseManagerDatabase` – PostgreSQL-Verbindung
- `Serilog` – Log-Level und Log-Dateien (Standard: `logs/testcasemanager-*.log`,
  tägliche Rotation, 14 Tage Aufbewahrung)

---

## Dokumentation

| Dokument | Inhalt |
|----------|--------|
| [docs/Architecture.md](docs/Architecture.md) | Schichtenarchitektur, Design Patterns, Klassendiagramm |
| [docs/DataModel.md](docs/DataModel.md)        | ER-Diagramm, Tabellen, Beziehungen, Indizes |
| [docs/Developer.md](docs/Developer.md)        | Entwicklerleitfaden, Erweiterbarkeit, Konventionen |
| [docs/schema.sql](docs/schema.sql)            | Generiertes SQL-Schema (Referenz) |
