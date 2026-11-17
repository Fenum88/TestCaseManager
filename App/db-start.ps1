# Startet die projekt-lokale ("virtuelle") PostgreSQL-Datenbank auf Port 5433.
# Es ist keine System-Installation und kein Administrator noetig.
# Verwendung:  .\db-start.ps1

$ErrorActionPreference = "Stop"
# Datenbank liegt als temporäres Arbeitsmaterial im Nachbarordner "Workspace".
$root = Join-Path (Split-Path $PSScriptRoot -Parent) "Workspace\.localdb"
$bin  = Join-Path $root "pgsql\bin"
$data = Join-Path $root "data"
$log  = Join-Path $root "server.log"

if (-not (Test-Path (Join-Path $bin "pg_ctl.exe"))) {
    Write-Error "PostgreSQL-Binaries fehlen unter '$bin'. Bitte zuerst db-setup.ps1 ausfuehren."
    exit 1
}

# Laeuft der Server bereits?
& "$bin\pg_ctl.exe" -D $data status *> $null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Datenbank laeuft bereits (Port 5433)." -ForegroundColor Green
    exit 0
}

Write-Host "Starte PostgreSQL auf Port 5433 ..." -ForegroundColor Cyan
& "$bin\pg_ctl.exe" -D $data -l $log -o "-p 5433" start
Write-Host "Fertig. Verbindung: Host=localhost;Port=5433;Database=testcasemanager;Username=postgres;Password=postgres" -ForegroundColor Green
