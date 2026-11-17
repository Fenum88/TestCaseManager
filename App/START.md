# Schnellstart – Testfallverwaltung mit virtueller Datenbank

Diese Anleitung bringt die Anwendung mit einer **projekt-lokalen („virtuellen")
PostgreSQL-Datenbank** zum Laufen – ohne System-Installation, ohne
Administratorrechte. Die Datenbank liegt als temporäres Arbeitsmaterial im
Ordner `..\Workspace\.localdb` (Binärdateien + Daten) und kann jederzeit
rückstandslos gelöscht werden.

> **Arbeitsverzeichnis:** Alle Befehle werden im Ordner **`App`** ausgeführt
> (`d:\myprojects\KI\Testmanager\App`). Die Datenbank-Skripte legen die virtuelle
> Datenbank automatisch im Nachbarordner `Workspace` an.

---

## Voraussetzungen

| Voraussetzung | Prüfen mit | Erfüllt? |
|---------------|-----------|----------|
| **.NET 9 SDK** | `dotnet --version` (≥ 9.0) | Bereits vorhanden |
| **Windows** (für WPF) | – | ✔ |
| **Internetzugang** (einmalig für DB-Download ~323 MB) | – | ✔ |

> PostgreSQL muss **nicht** installiert sein – das Setup-Skript lädt eine
> portable Version herunter.

---

## Variante A – Alles automatisch (empfohlen)

Drei Befehle in einer **PowerShell**:

```powershell
# In den App-Ordner wechseln
cd d:\myprojects\KI\Testmanager\App

# 1. Virtuelle Datenbank einrichten und starten (einmalig ~1-2 Min. wg. Download)
.\db-setup.ps1

# 2. Anwendung starten
dotnet run --project TestCaseManager.UI
```

Das war’s. Beim Start legt die Anwendung Tabellen, Beziehungen und Indizes
selbst an und erzeugt ein paar Standardkategorien. Das Fenster
**„Testfallverwaltung"** öffnet sich mit dem Dashboard.

---

## Variante B – Schritt für Schritt (zum Nachvollziehen)

Alle Schritte, die `db-setup.ps1` automatisiert:

### 1. Portable PostgreSQL herunterladen und entpacken
```powershell
# Zielort ist der Workspace-Ordner (temporäres Arbeitsmaterial)
$root = "..\Workspace\.localdb"; New-Item -ItemType Directory -Force $root | Out-Null
Start-BitsTransfer "https://get.enterprisedb.com/postgresql/postgresql-16.4-1-windows-x64-binaries.zip" "$root\postgres.zip"
Expand-Archive "$root\postgres.zip" $root -Force
```

### 2. Datenverzeichnis initialisieren (Benutzer `postgres`, Passwort `postgres`)
```powershell
"postgres" | Out-File "$root\pw.txt" -NoNewline -Encoding ascii
& "$root\pgsql\bin\initdb.exe" -D "$root\data" -U postgres -A scram-sha-256 --pwfile="$root\pw.txt" -E UTF8
Remove-Item "$root\pw.txt"
```

### 3. Server auf Port 5433 starten
```powershell
& "$root\pgsql\bin\pg_ctl.exe" -D "$root\data" -l "$root\server.log" -o "-p 5433" start
```

### 4. Datenbank anlegen
```powershell
$env:PGPASSWORD="postgres"
& "$root\pgsql\bin\createdb.exe" -h localhost -p 5433 -U postgres testcasemanager
```

### 5. Anwendung starten
```powershell
dotnet run --project TestCaseManager.UI
```

---

## Datenbank im Alltag steuern

| Aktion | Befehl |
|--------|--------|
| Datenbank **starten** | `.\db-start.ps1` |
| Datenbank **stoppen** | `.\db-stop.ps1` |
| Neu **einrichten** (idempotent) | `.\db-setup.ps1` |
| **Zurücksetzen** (alles löschen) | `.\db-stop.ps1` &nbsp;→&nbsp; `Remove-Item ..\Workspace\.localdb -Recurse -Force` &nbsp;→&nbsp; `.\db-setup.ps1` |

> Nach einem Windows-Neustart ist die Datenbank gestoppt – einfach vor dem
> App-Start `.\db-start.ps1` ausführen.

---

## Verbindungsdaten

Die Anwendung ist bereits auf die virtuelle Datenbank eingestellt
([`TestCaseManager.UI/appsettings.json`](TestCaseManager.UI/appsettings.json)):

```
Host=localhost;Port=5433;Database=testcasemanager;Username=postgres;Password=postgres
```

Möchtest du stattdessen einen **eigenen** PostgreSQL-Server verwenden, trage hier
einfach dessen Daten ein (üblicherweise Port `5432`).

---

## In die Datenbank hineinschauen (optional)

Am einfachsten über das mitgelieferte Skript (öffnet die Datenbank-Konsole):

```powershell
.\db-psql.ps1                          # interaktiver Prompt
.\db-psql.ps1 -f docs\testfaelle-lesen.sql   # fertige Lese-Abfragen ausführen
# im Prompt z. B.:  \dt      SELECT * FROM "TestCases";      \q
```

Alternativ direkt mit `psql` oder einem Werkzeug wie **pgAdmin** / **DBeaver**
(Verbindung: `localhost:5433`, DB `testcasemanager`, `postgres`/`postgres`).

---

## Fehlerbehebung

| Symptom | Ursache / Lösung |
|---------|------------------|
| App zeigt „Startfehler / Datenbankverbindung" | DB nicht gestartet → `.\db-start.ps1` |
| `Port 5433 already in use` | Server läuft schon – nichts zu tun, oder `.\db-stop.ps1` |
| Download bricht ab | `db-setup.ps1` erneut ausführen (setzt vorhandene Schritte fort) |
| Datenbank soll komplett neu | Ordner `..\Workspace\.localdb` löschen und `db-setup.ps1` erneut ausführen |
