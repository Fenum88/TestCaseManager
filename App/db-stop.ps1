# Stoppt die projekt-lokale PostgreSQL-Datenbank.
# Verwendung:  .\db-stop.ps1

$ErrorActionPreference = "Stop"
# Datenbank liegt als temporäres Arbeitsmaterial im Nachbarordner "Workspace".
$root = Join-Path (Split-Path $PSScriptRoot -Parent) "Workspace\.localdb"
$bin  = Join-Path $root "pgsql\bin"
$data = Join-Path $root "data"

if (-not (Test-Path (Join-Path $bin "pg_ctl.exe"))) {
    Write-Error "PostgreSQL-Binaries fehlen unter '$bin'."
    exit 1
}

& "$bin\pg_ctl.exe" -D $data status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Datenbank ist bereits gestoppt." -ForegroundColor Yellow
    exit 0
}

Write-Host "Stoppe PostgreSQL ..." -ForegroundColor Cyan
& "$bin\pg_ctl.exe" -D $data -m fast stop
Write-Host "Datenbank gestoppt." -ForegroundColor Green
