# Netzwerkverbindungen und externe Zugriffe

Stand: 17.11.2026 – ermittelt durch Code-Analyse aller versionierten Dateien
(C#, XAML, PowerShell, JSON, Markdown).

**Kurzfassung:** Die Anwendung selbst baut **keine Verbindung ins Internet** auf.
Es gibt keinen `HttpClient`, keine Telemetrie, kein Update-Check und keinen
Cloud-Dienst. Die einzige Netzwerkverbindung zur Laufzeit geht zur
PostgreSQL-Datenbank (Standard: `localhost`). Internetzugriffe passieren nur
bei der **Einrichtung** (PostgreSQL-Download) und beim **Bauen** (NuGet).

---

## 1. Verbindungen nach extern (Internet)

| # | Wann | Von | Ziel | Protokoll / Port | Zweck | Abschaltbar? |
|---|------|-----|------|------------------|-------|--------------|
| E1 | Einmalig bei `db-setup.ps1`, nur wenn die Binaries noch fehlen | `App/db-setup.ps1` (Zeile 19, 28–29) | `https://get.enterprisedb.com/postgresql/postgresql-16.4-1-windows-x64-binaries.zip` | HTTPS / 443 (BITS, Fallback `System.Net.WebClient`) | Download portabler PostgreSQL-Binaries (~323 MB) | Ja – Binaries manuell bereitstellen oder vorhandenen Firmen-PostgreSQL nutzen |
| E2 | Manuelle Alternative zu E1 | `App/START.md` (Zeile 57) | wie E1 | HTTPS / 443 | wie E1 | – |
| E3 | `dotnet restore` / `build` / `test` (Build-Zeit) | .NET SDK / NuGet | `https://api.nuget.org/v3/index.json` (+ CDN `*.nuget.org`) | HTTPS / 443 | Laden der NuGet-Pakete (siehe [Abhaengigkeiten.md](Abhaengigkeiten.md)) | Ja – internen NuGet-Feed/Proxy per `nuget.config` eintragen |
| E4 | `dotnet tool restore` | `App/.config/dotnet-tools.json` | `api.nuget.org` | HTTPS / 443 | Werkzeug `dotnet-ef` 9.0.17 (nur für Entwickler, die Migrationen erzeugen) | Ja – nur bei Bedarf ausführen |
| E5 | Jeder `dotnet`-CLI-Aufruf | .NET SDK (nicht die App!) | Microsoft-Telemetrie-Endpunkte | HTTPS / 443 | SDK-Nutzungstelemetrie | Ja – Umgebungsvariable `DOTNET_CLI_TELEMETRY_OPTOUT=1` |
| E6 | `git push` | Git | `github.com` | HTTPS / 443 oder SSH / 22 | Quellcode-Upload | – |

> Hinweis zu E1: Der Download wird **nicht per Prüfsumme verifiziert**.
> Siehe Empfehlung in [Sicherheitspruefung.md](Sicherheitspruefung.md).

## 2. Lokale Netzwerkverbindungen (Loopback)

| # | Von | Ziel | Protokoll / Port | Authentifizierung | Zweck |
|---|-----|------|------------------|-------------------|-------|
| L1 | Anwendung (`TestCaseManager.UI`, über Npgsql/EF Core) | `localhost:5433` | TCP (PostgreSQL-Protokoll), **ohne TLS** | Benutzer `postgres` / Passwort `postgres` aus `TestCaseManager.UI/appsettings.json` | Alle Daten der Anwendung |
| L2 | `dotnet ef` (Design-Time, `DesignTimeDbContextFactory.cs:17`) | `localhost:5433` | TCP | wie L1 (hart kodiert) | Nur beim Erzeugen/Anwenden von Migrationen durch Entwickler |
| L3 | `db-setup.ps1`, `db-psql.ps1` (`psql.exe`, `createdb.exe`) | `localhost:5433` | TCP | `postgres`/`postgres` via `PGPASSWORD` (wird danach entfernt) | Datenbank anlegen / SQL-Konsole |
| L4 | PostgreSQL-Server (lauscht) | – | TCP 5433 | `scram-sha-256` | Server-Seite von L1–L3 |

**Lauschende Ports:** Nur der portable PostgreSQL-Server öffnet einen Port.
Geprüfte Konfiguration in `Workspace/.localdb/data`:

- `listen_addresses` = Standard `localhost` → **nur von diesem Rechner erreichbar**
- `pg_hba.conf` erlaubt ausschließlich `127.0.0.1/32` und `::1/128`
- Port 5433 (nicht der Standardport 5432 → kein Konflikt mit einem evtl. installierten PostgreSQL)

Die WPF-Anwendung selbst öffnet **keinen** Port.

> **Achtung beim Einsatz auf der Arbeit:** Wird in `appsettings.json` statt
> `localhost` ein Firmen-Datenbankserver eingetragen, wird L1 zu einer echten
> Netzwerkverbindung. Dann TLS aktivieren (`SSL Mode=Require` bzw. `VerifyFull`),
> einen eigenen, nicht privilegierten DB-Benutzer verwenden und das Passwort
> nicht im Repository speichern (siehe [Sicherheitspruefung.md](Sicherheitspruefung.md)).

## 3. Weitere Zugriffe außerhalb der Anwendung (kein Netzwerk)

| # | Was | Wo im Code | Details |
|---|-----|------------|---------|
| P1 | **Start externer Prozesse** | `Infrastructure/Services/CalculationSystemService.cs:56` | Startet das konfigurierte Programm (Standard `python` aus dem `PATH`) mit Skripten aus `Workspace/Berechnungssysteme`. Argumente werden einzeln übergeben (`ArgumentList`, `UseShellExecute=false`) → keine Shell-Injektion. Die aktuell mitgelieferten Skripte (`substanzwert.py`, `ertragswert.py`) importieren nur `copy`, `os`, `sys` – **kein Netzwerk**. Welche Programme gestartet werden, ist aber frei konfigurierbar. |
| P2 | Windows-Benutzername | `Infrastructure/Services/WindowsCurrentUserService.cs:12` | `Environment.UserName` wird als „Geändert von“ im Änderungsprotokoll der Datenbank gespeichert (personenbezogenes Datum). |
| P3 | Dateisystem – Lesen/Schreiben | `TestCaseFileService`, `CompareService`, `Json*Store`, ViewModels | `Workspace/Testfälle` (Input/Output, `szenarien.json`), `Workspace/Berechnungssysteme/berechnungssysteme.json`, Export-/Importordner per Dialog, CSV/XLSX-Vergleichsberichte. |
| P4 | Temporäre Dateien | `UI/ViewModels/CalculationViewModel.cs:524` | `%TEMP%\tcm-szenario-<GUID>` – wird nach dem Lauf wieder gelöscht. |
| P5 | Logdateien | `appsettings.json` → Serilog | `logs/testcasemanager-JJJJMMTT.log` neben der EXE, 14 Tage Aufbewahrung; zusätzlich Debug-Ausgabe. **Kein** Versand an Server. |
| P6 | Konfiguration | Generic Host (`Host.CreateDefaultBuilder`) | Liest `appsettings.json`, Umgebungsvariablen und Kommandozeile. Im Entwicklungsmodus zusätzlich User Secrets (lokal unter `%APPDATA%\Microsoft\UserSecrets`). |

## 4. Was es ausdrücklich **nicht** gibt

Geprüft per Volltextsuche (`HttpClient`, `WebClient`, `WebRequest`, `Socket`,
`TcpClient`, `Dns`, `Smtp`, `Ftp`, `NamedPipe`, `Registry`, `DllImport`,
`ApplicationInsights`, `Telemetry`, URLs, IP-Adressen):

- keine HTTP-/REST-Aufrufe aus der Anwendung
- keine Telemetrie, kein Crash-Reporting an Dritte, kein Auto-Update
- keine E-Mail-, FTP- oder Netzlaufwerk-Zugriffe im Code
- keine Registry-Zugriffe, keine nativen DLL-Aufrufe
- keine hart kodierten IP-Adressen oder fremden Hostnamen
- Die `http://schemas.microsoft.com/...`-Einträge in den XAML-Dateien sind
  XML-Namensräume, **keine** Verbindungen.
