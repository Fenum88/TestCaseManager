# Richtet eine projekt-lokale ("virtuelle") PostgreSQL-Instanz ein:
#   1. Laedt portable PostgreSQL-Binaries herunter (einmalig, ~323 MB)
#   2. Entpackt sie nach .localdb\pgsql
#   3. Initialisiert ein Datenverzeichnis (Superuser: postgres / Passwort: postgres)
#   4. Startet den Server auf Port 5433
#   5. Legt die Datenbank "testcasemanager" an
#
# Es ist KEINE System-Installation und KEIN Administrator noetig.
# Das Skript ist idempotent - bereits erledigte Schritte werden uebersprungen.
# Verwendung:  .\db-setup.ps1

$ErrorActionPreference = "Stop"
# Die virtuelle Datenbank liegt als temporäres Arbeitsmaterial im
# Nachbarordner "Workspace"; die Skripte selbst gehören zur Anwendung ("App").
$root = Join-Path (Split-Path $PSScriptRoot -Parent) "Workspace\.localdb"
$bin  = Join-Path $root "pgsql\bin"
$data = Join-Path $root "data"
$zip  = Join-Path $root "postgres.zip"
$pgUrl = "https://get.enterprisedb.com/postgresql/postgresql-16.4-1-windows-x64-binaries.zip"
$port = 5433

New-Item -ItemType Directory -Force -Path $root | Out-Null

# --- 1. Download ---------------------------------------------------------
if (-not (Test-Path (Join-Path $bin "initdb.exe"))) {
    if (-not (Test-Path $zip)) {
        Write-Host "Lade PostgreSQL-Binaries herunter (~323 MB) ..." -ForegroundColor Cyan
        try   { Start-BitsTransfer -Source $pgUrl -Destination $zip }
        catch { (New-Object System.Net.WebClient).DownloadFile($pgUrl, $zip) }
    }
    # --- 2. Entpacken ----------------------------------------------------
    Write-Host "Entpacke Binaries ..." -ForegroundColor Cyan
    Expand-Archive -Path $zip -DestinationPath $root -Force
}
else {
    Write-Host "PostgreSQL-Binaries bereits vorhanden." -ForegroundColor Green
}

# --- 3. initdb -----------------------------------------------------------
if (-not (Test-Path (Join-Path $data "PG_VERSION"))) {
    Write-Host "Initialisiere Datenverzeichnis ..." -ForegroundColor Cyan
    $pwfile = Join-Path $root "pw.txt"
    Set-Content -Path $pwfile -Value "postgres" -NoNewline -Encoding ascii
    & "$bin\initdb.exe" -D $data -U postgres -A scram-sha-256 --pwfile=$pwfile -E UTF8 | Out-Null
    Remove-Item $pwfile -Force
}
else {
    Write-Host "Datenverzeichnis bereits initialisiert." -ForegroundColor Green
}

# --- 4. Server starten ---------------------------------------------------
& "$bin\pg_ctl.exe" -D $data status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Starte Server auf Port $port ..." -ForegroundColor Cyan
    & "$bin\pg_ctl.exe" -D $data -l (Join-Path $root "server.log") -o "-p $port" start
    Start-Sleep -Seconds 2
}
else {
    Write-Host "Server laeuft bereits." -ForegroundColor Green
}

# --- 5. Datenbank anlegen ------------------------------------------------
$env:PGPASSWORD = "postgres"
$exists = & "$bin\psql.exe" -h localhost -p $port -U postgres -tAc `
    "SELECT 1 FROM pg_database WHERE datname='testcasemanager'"
if ($exists -ne "1") {
    Write-Host "Lege Datenbank 'testcasemanager' an ..." -ForegroundColor Cyan
    & "$bin\createdb.exe" -h localhost -p $port -U postgres testcasemanager
}
else {
    Write-Host "Datenbank 'testcasemanager' existiert bereits." -ForegroundColor Green
}
Remove-Item Env:\PGPASSWORD

Write-Host ""
Write-Host "Fertig! Die virtuelle Datenbank laeuft auf Port $port." -ForegroundColor Green
Write-Host "Naechster Schritt:  dotnet run --project TestCaseManager.UI" -ForegroundColor Green
