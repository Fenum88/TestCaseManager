-- Testfälle aus der Datenbank auslesen.
-- Ausführen mit:  .\db-psql.ps1 -f docs\testfaelle-lesen.sql
-- (oder im interaktiven psql-Prompt einzeln einfügen)

\echo ==================== Alle Testfälle ====================
SELECT "Name", "Version", "Status", "CreatedBy", "CreatedAt"
FROM   "TestCases"
ORDER  BY "CreatedAt" DESC;

\echo ==================== Eingabeparameter je Testfall ====================
SELECT t."Name"       AS testfall,
       p."Name"       AS parameter,
       p."DataType"   AS datentyp,
       p."Value"      AS wert,
       p."IsRequired" AS pflicht
FROM   "TestParameters" p
JOIN   "TestCases" t ON t."Id" = p."TestCaseId"
ORDER  BY t."Name", p."SortOrder";

\echo ==================== Erwartete Ergebnisse je Testfall ====================
SELECT t."Name"          AS testfall,
       r."Name"          AS ergebnis,
       r."ExpectedValue" AS erwartet,
       r."Tolerance"     AS toleranz
FROM   "ExpectedResults" r
JOIN   "TestCases" t ON t."Id" = r."TestCaseId"
ORDER  BY t."Name", r."SortOrder";

\echo ==================== Änderungshistorie (neueste zuerst) ====================
SELECT t."Name"        AS testfall,
       h."ChangeType"  AS aktion,
       h."PropertyName" AS feld,
       h."OldValue"    AS alt,
       h."NewValue"    AS neu,
       h."ChangedBy"   AS benutzer,
       h."ChangedAt"   AS zeitpunkt
FROM   "TestCaseHistories" h
JOIN   "TestCases" t ON t."Id" = h."TestCaseId"
ORDER  BY h."ChangedAt" DESC
LIMIT  20;
