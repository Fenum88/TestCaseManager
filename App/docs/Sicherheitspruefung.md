# Sicherheitsprüfung vor Veröffentlichung / Einsatz im Firmennetz

Stand: 17.11.2026 · Umfang: alle Dateien, die mit den vorhandenen
`.gitignore`-Regeln ins Repository gehen würden (219 Dateien, ~1,4 MB),
zusätzlich Konfiguration der lokalen PostgreSQL-Instanz.

Begleitdokumente:
[Netzwerkverbindungen.md](Netzwerkverbindungen.md) ·
[Abhaengigkeiten.md](Abhaengigkeiten.md)

## Ergebnis auf einen Blick

| Bereich | Status |
|---------|--------|
| Geheimnisse im Repo (API-Keys, Tokens, private Schlüssel, echte Passwörter) | ✅ keine gefunden – nur das lokale Standardpasswort `postgres` (siehe S1) |
| Persönliche Daten im Repo (E-Mail, Benutzername, Firmennamen) | ✅ keine gefunden – nur Beispielpfad `d:\myprojects\KI\Testmanager\App` in `START.md` |
| Versehentlich versionierte Daten (`Workspace/`, DB-Dateien, Testfälle, Logs, `bin/`, `obj/`, `.vs/`) | ✅ durch `.gitignore` ausgeschlossen |
| Bekannte Schwachstellen in NuGet-Paketen | ✅ keine |
| Lizenzen | ✅ nur MIT / Apache-2.0 / PostgreSQL |
| Internetverbindungen der Anwendung zur Laufzeit | ✅ keine |
| Build | ✅ erfolgreich |
| Tests | ✅ 134 / 134 bestanden |
| SQL-Injection | ✅ kein Roh-SQL im Code, ausschließlich EF Core LINQ (parametrisiert) |
| Kommandozeilen-Injection | ✅ Prozessstart mit `ArgumentList`, `UseShellExecute=false` |
| Unsichere Deserialisierung | ✅ nur `System.Text.Json` auf feste Typen, kein `BinaryFormatter`/`TypeNameHandling` |

## Befunde

| ID | Schwere | Befund | Fundstelle | Empfehlung |
|----|---------|--------|------------|------------|
| S1 | Mittel (bei Firmen-DB: Hoch) | Standard-Zugangsdaten `postgres`/`postgres` im Klartext | `TestCaseManager.UI/appsettings.json:3`, `Persistence/DesignTimeDbContextFactory.cs:17`, `db-*.ps1`, `START.md` | Für die lokale Wegwerf-DB unkritisch (nur `localhost`). **Echte Firmen-Zugangsdaten nie in `appsettings.json` eintragen**, sondern per Umgebungsvariable setzen (wird vom Generic Host automatisch gelesen): `setx ConnectionStrings__TestCaseManagerDatabase "Host=…;Username=…;Password=…;SSL Mode=Require"` |
| S2 | Mittel (bei Firmen-DB) | Anwendung arbeitet mit dem Superuser `postgres` | wie S1 | Auf einem Firmenserver eigenen Benutzer mit Rechten nur auf die DB `testcasemanager` anlegen. Achtung: Beim Start werden Migrationen **automatisch** angewendet (`DatabaseInitializer.cs`) – der Benutzer braucht DDL-Rechte auf das eigene Schema, sonst nichts. |
| S3 | Mittel (bei Firmen-DB) | Keine TLS-Verschlüsselung der DB-Verbindung konfiguriert | Verbindungszeichenfolge | Bei Remote-Server `SSL Mode=VerifyFull` (oder mind. `Require`) ergänzen. |
| S4 | Mittel | .NET 9 hat seit 10.11.2026 keinen Support mehr (keine Sicherheitsupdates) | alle `.csproj` (`net9.0`) | Auf .NET 10 (LTS) umstellen. Firmen-Scanner (z. B. Defender for Endpoint, Qualys) melden EOL-Runtimes oft. |
| S5 | Niedrig–Mittel | PostgreSQL-Download ohne Prüfsummen-Verifikation; Version 16.4 ist veraltet | `db-setup.ps1:19–29` | Neuere 16.x-Version verwenden und SHA-256 nach dem Download prüfen (`Get-FileHash`). Im Firmennetz besser einen von der IT bereitgestellten PostgreSQL nutzen. |
| S6 | Niedrig | Testfallnummer/-bezeichnung aus dem Assistenten werden nicht auf ungültige Pfadzeichen geprüft. Beim „Auslesen“ in Dateien fließen sie in `Path.Combine` → mit Eingaben wie `..\..\x` könnte außerhalb des Zielordners geschrieben werden. | `UI/ViewModels/TestCaseWizardViewModel.cs:245`, `Infrastructure/Services/TestCaseFileService.cs:156–161` | Eingaben gegen `Path.GetInvalidFileNameChars()` und `..` prüfen (wie bereits in `CalculationSystem.cs:58` gemacht). Risiko gering, da nur der angemeldete Benutzer selbst betroffen ist. |
| S7 | Niedrig (by design) | Berechnungssysteme können beliebige Programme starten | `CalculationSystemService.cs`, `Workspace/Berechnungssysteme/berechnungssysteme.json` | Gewollt. Wer die JSON-Datei ändern kann, kann Code als der aktuelle Benutzer ausführen → Ordner nicht auf ein für alle beschreibbares Netzlaufwerk legen. |
| S8 | Info | Windows-Benutzername wird im Änderungsprotokoll gespeichert | `WindowsCurrentUserService.cs:12` | Personenbezogenes Datum – bei gemeinsamer Firmen-DB ggf. mit Datenschutz/Betriebsrat abstimmen. |
| S9 | Info | Fließende Paketversionen `9.0.*` | Application, UI `.csproj` | Für reproduzierbare Builds feste Versionen oder `Directory.Packages.props` (Central Package Management) verwenden. |
| S10 | Info | `xunit` 2.x ist als Legacy markiert | Tests `.csproj` | Bei Gelegenheit auf `xunit.v3` migrieren (betrifft nur Tests). |

## Checkliste vor dem `git push`

- [ ] **Repository auf GitHub als *privat* anlegen.** Vorher klären, ob Code, der
      für die Arbeit gedacht ist, auf einem privaten GitHub-Konto liegen darf
      (Arbeitsvertrag / IT-Richtlinie / geistiges Eigentum). Ggf. Firmen-GitHub
      oder internes GitLab/Azure DevOps nutzen.
- [ ] Repo-Wurzel ist `Testmanager/` (dort liegt die `.gitignore`, die `Workspace/` ausschließt).
- [ ] Nach `git add .` mit `git status` prüfen, dass **kein** `Workspace/`, `bin/`, `obj/`, `.vs/`, `logs/` auftaucht.
- [ ] Keine echten Testfalldaten aus der Firma in `docs/` oder Testdateien kopiert.
- [ ] `LICENSE`-Datei bewusst wählen – oder bewusst weglassen (ohne Lizenz = „alle Rechte vorbehalten“; für ein privates Repo üblich).
- [ ] Optional: `.gitattributes` mit `* text=auto` gegen Zeilenende-Chaos zwischen Rechnern.
- [ ] Optional: GitHub *Secret Scanning* und *Dependabot alerts* im Repo aktivieren (kostenlos, auch für private Repos).

## Checkliste vor dem Einsatz auf dem Firmenrechner

- [ ] **Nichts installieren, was die IT-Richtlinie verbietet.** `db-setup.ps1`
      lädt eine fremde ZIP (~323 MB) aus dem Internet und startet daraus einen
      Datenbankserver. Das braucht zwar keine Adminrechte, kann aber gegen
      Software-Richtlinien verstoßen, von AppLocker/Defender blockiert oder vom
      Proxy verhindert werden → vorher mit der IT klären oder eine vorhandene
      Firmen-PostgreSQL nutzen.
- [ ] Port **5433** ist frei (`Get-NetTCPConnection -LocalPort 5433`). Die
      lokale DB lauscht nur auf `localhost` und ist im Netz nicht sichtbar.
- [ ] Bei Firmen-DB: eigene leere Datenbank verwenden – die App führt beim Start
      **automatisch Schema-Migrationen** aus und legt Standardkategorien an.
      Niemals auf eine produktive oder fremde Datenbank zeigen lassen.
- [ ] Zugangsdaten per Umgebungsvariable statt in `appsettings.json` (siehe S1).
- [ ] .NET-Runtime-Version mit der IT abstimmen (S4).
- [ ] NuGet-Zugriff: Falls `api.nuget.org` gesperrt ist, internen Feed in einer
      `nuget.config` eintragen.
- [ ] `TestCaseFiles:RootPath` und `CalculationSystems:FolderPath` in
      `appsettings.json` auf einen Ordner zeigen lassen, auf den nur du
      Schreibrechte hast (S7).
- [ ] Python für die Berechnungssysteme ist vorhanden und erlaubt.
- [ ] Logs (`logs/` neben der EXE) enthalten Pfade und Testfallnummern – nicht in
      Tickets oder E-Mails nach außen weitergeben.

## Wie geprüft wurde

1. Dateiliste mit `git ls-files -o --exclude-standard` (ohne das Projekt zu verändern) ermittelt.
2. Volltextsuche in diesen Dateien nach Passwörtern, Tokens, Schlüsseln, E-Mail-Adressen, absoluten Pfaden, IPs, URLs.
3. Volltextsuche im Code nach Netzwerk-APIs, Prozessstart, Registry, nativen Aufrufen, Roh-SQL, Deserialisierung.
4. Manuelle Durchsicht der Fundstellen (Prozessstart, Dateischreiben, DB-Initialisierung, Startup).
5. `dotnet list package --vulnerable / --deprecated / --outdated --include-transitive`.
6. Lizenzen aus den `.nuspec`-Dateien.
7. `postgresql.conf` / `pg_hba.conf` der lokalen Instanz.
8. `dotnet test` (134 Tests, alle grün).

Nicht durchgeführt: dynamische Analyse / Penetrationstest, SAST-Werkzeug (z. B.
CodeQL – lässt sich nach dem Push kostenlos als GitHub Action aktivieren).
