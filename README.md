# TestCaseManager – Projektüberblick

Dieses Verzeichnis ist in zwei Bereiche gegliedert:

| Ordner | Inhalt |
|--------|--------|
| **[App/](App/)** | Die vollständige Anwendung: Visual-Studio-Solution, alle Projekte, Dokumentation, Datenbank-Skripte, README. **Hier wird gearbeitet.** |
| **Workspace/** | Temporäres Arbeitsmaterial: die projekt-lokale („virtuelle") PostgreSQL-Instanz (`.localdb`, Binärdateien + Daten). Regenerierbar, nicht versioniert. |

## Schnellstart

Alle Befehle werden im Ordner **`App`** ausgeführt:

```powershell
cd App
.\db-setup.ps1                          # Virtuelle Datenbank einrichten + starten (einmalig)
dotnet run --project TestCaseManager.UI # Anwendung starten
```

Ausführliche Anleitungen:

- **[App/START.md](App/START.md)** – Schnellstart mit virtueller Datenbank, Schritt für Schritt
- **[App/README.md](App/README.md)** – Funktions- und Technikübersicht, Installation mit eigenem PostgreSQL
- **[App/docs/](App/docs/)** – Architektur, Datenmodell, Entwicklerdokumentation

> Die Anwendung selbst enthält keinen Pfadbezug auf `Workspace/`. Sie verbindet
> sich über die Verbindungszeichenfolge in
> `App/TestCaseManager.UI/appsettings.json` mit PostgreSQL auf `localhost:5433`.
> Nur die Datenbank-Skripte (`App/db-*.ps1`) greifen auf `Workspace/.localdb` zu.
