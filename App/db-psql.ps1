# Öffnet die Datenbank-Konsole (psql) auf der virtuellen Datenbank.
#
# Beispiele:
#   .\db-psql.ps1                          # interaktiver Prompt (SQL eintippen)
#   .\db-psql.ps1 -f docs\testfaelle-lesen.sql   # SQL-Datei ausführen
#
# Im interaktiven Prompt nützlich:
#   \dt                       Tabellen auflisten
#   SELECT * FROM "TestCases";  (Semikolon am Ende nicht vergessen)
#   \q                        beenden

param([string]$f)

# Datenbank liegt als temporäres Arbeitsmaterial im Nachbarordner "Workspace".
$bin = Join-Path (Split-Path $PSScriptRoot -Parent) "Workspace\.localdb\pgsql\bin\psql.exe"
$env:PGPASSWORD = "postgres"
try {
    if ($f) {
        & $bin -h localhost -p 5433 -U postgres -d testcasemanager -f $f
    } else {
        & $bin -h localhost -p 5433 -U postgres -d testcasemanager
    }
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}
