using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.FileFormat;
using TestCaseManager.Application.Services;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Infrastructure.Services;

/// <summary>
/// Vergleicht die Ausgaben zweier Berechnungssysteme. Für jedes einander
/// entsprechende Dateipaar entsteht eine lesbare Vergleichsdatei in gleicher
/// Ordnerstruktur. Zusätzlich werden eine deutsche CSV (Zahlen mit Komma) und
/// eine Excel-Mappe mit den Blättern „Zusammenfassung" und „Abgleich"
/// geschrieben. Bei numerischen Attributen wird die Differenz (A − B) ausgewiesen.
/// </summary>
public class CompareService : ICompareService
{
    private const string OutputExtension = ".Ausgabe";
    private const string CompareExtension = ".vergleich";
    private const char CsvSeparator = ';';

    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly Encoding PlainUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    // CSV mit BOM, damit deutsche Umlaute in Excel korrekt erscheinen.
    private static readonly Encoding CsvUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private readonly ITestCaseFileParser _parser;
    private readonly ILogger<CompareService> _logger;

    /// <summary>Erzeugt den Dienst (Dependency Injection).</summary>
    /// <param name="parser">Parser für das Dateiformat (Ausgaben einlesen).</param>
    /// <param name="logger">Logger für Diagnose und Fehlerprotokollierung.</param>
    public CompareService(ITestCaseFileParser parser, ILogger<CompareService> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CompareResult> CompareOutputsAsync(CompareOutputsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messages = new List<string>();
        var errors = new List<string>();
        var rows = new List<CompareRow>();

        if (!Directory.Exists(request.DirectoryA) || !Directory.Exists(request.DirectoryB))
        {
            errors.Add("Mindestens einer der Ausgabeordner fehlt – wurde die Berechnung ausgeführt?");
            return await FinishAsync(request, rows, messages, errors, 0, cancellationToken);
        }

        var filesA = Directory.EnumerateFiles(request.DirectoryA, "*" + OutputExtension,
            SearchOption.AllDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        var matchedRelatives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var filesCompared = 0;

        foreach (var fileA in filesA)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(request.DirectoryA, fileA);
            var fileB = Path.Combine(request.DirectoryB, relative);
            matchedRelatives.Add(relative);

            if (!File.Exists(fileB))
            {
                var baseName = Path.GetFileNameWithoutExtension(relative);
                errors.Add($"Nur in A vorhanden: {relative}");
                rows.Add(CompareRow.MissingFile(ExtractTestCaseNumber(relative),
                    GeVoFromFileName(baseName), baseName, presentInA: true));
                continue;
            }

            try
            {
                var fileRows = await CompareFileAsync(request, relative, fileA, fileB, cancellationToken);
                rows.AddRange(fileRows);
                filesCompared++;
                var diffs = fileRows.Count(r => r.Status != "gleich");
                messages.Add($"{relative}: {fileRows.Count} Attribute, {diffs} Unterschied(e)");
            }
            catch (Exception ex) when (ex is IOException or FormatException)
            {
                var message = $"{relative}: {ex.Message}";
                errors.Add(message);
                _logger.LogError(ex, "Fehler beim Vergleich: {Message}", message);
            }
        }

        // Dateien, die es nur in B gibt.
        foreach (var fileB in Directory.EnumerateFiles(request.DirectoryB, "*" + OutputExtension,
                     SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(request.DirectoryB, fileB);
            if (!matchedRelatives.Contains(relative))
            {
                var baseName = Path.GetFileNameWithoutExtension(relative);
                errors.Add($"Nur in B vorhanden: {relative}");
                rows.Add(CompareRow.MissingFile(ExtractTestCaseNumber(relative),
                    GeVoFromFileName(baseName), baseName, presentInA: false));
            }
        }

        return await FinishAsync(request, rows, messages, errors, filesCompared, cancellationToken);
    }

    /// <summary>Schreibt CSV und Excel und stellt das Ergebnis zusammen.</summary>
    private async Task<CompareResult> FinishAsync(CompareOutputsRequest request, List<CompareRow> rows,
        List<string> messages, List<string> errors, int filesCompared, CancellationToken cancellationToken)
    {
        var differences = rows.Count(r => r.Status != "gleich");

        if (!string.IsNullOrWhiteSpace(request.CsvPath))
        {
            await WriteCsvAsync(request.CsvPath, rows, cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(request.ExcelPath))
        {
            WriteExcel(request, rows, filesCompared, differences);
        }

        _logger.LogInformation("Vergleich abgeschlossen: {Files} Dateien, {Diff} Unterschiede.",
            filesCompared, differences);

        return new CompareResult
        {
            FilesCompared = filesCompared,
            TotalAttributes = rows.Count(r => !r.IsFileLevel),
            Differences = differences,
            CsvPath = request.CsvPath,
            ExcelPath = request.ExcelPath,
            Messages = messages,
            Errors = errors
        };
    }

    /// <summary>Vergleicht ein Dateipaar und schreibt die lesbare Vergleichsdatei.</summary>
    private async Task<List<CompareRow>> CompareFileAsync(CompareOutputsRequest request, string relative,
        string fileA, string fileB, CancellationToken cancellationToken)
    {
        var contentA = await File.ReadAllTextAsync(fileA, PlainUtf8, cancellationToken);
        var contentB = await File.ReadAllTextAsync(fileB, PlainUtf8, cancellationToken);

        var baseName = Path.GetFileNameWithoutExtension(relative);
        var parsedA = _parser.Parse(string.Empty, baseName, TestCaseFileSection.Output, contentA);
        var parsedB = _parser.Parse(string.Empty, baseName, TestCaseFileSection.Output, contentB);

        var rowsA = ExtractRows(parsedA);
        var rowsB = ExtractRows(parsedB);
        var testCaseNumber = ExtractTestCaseNumber(relative);
        var gevo = ExtractGeVo(parsedA, baseName);
        if (string.IsNullOrEmpty(gevo))
        {
            gevo = ExtractGeVo(parsedB, baseName);
        }

        var report = new StringBuilder();
        report.AppendLine($"Vergleich: {request.LabelA}  vs  {request.LabelB}");
        report.AppendLine($"Datei: {relative}");
        report.AppendLine($"Geschäftsvorfall: {gevo}");
        report.AppendLine();

        var fileRows = new List<CompareRow>();

        // Entitäten in Reihenfolge von A, danach die nur in B vorhandenen.
        var entityNames = rowsA.Select(r => r.Entity)
            .Concat(rowsB.Select(r => r.Entity))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var entity in entityNames)
        {
            var entityRowsA = rowsA.Where(r => Same(r.Entity, entity)).ToList();
            var entityRowsB = rowsB.Where(r => Same(r.Entity, entity)).ToList();
            var keyColumns = FindKeyColumns(request.KeyRules, entity);

            foreach (var pair in PairRows(entityRowsA, entityRowsB, keyColumns))
            {
                var label = pair.Label(keyColumns);
                report.AppendLine($"[{entity} ({label})]");

                // Spalten in Reihenfolge von A, danach die nur in B vorhandenen.
                var columns = new List<string>();
                if (pair.A is not null)
                {
                    columns.AddRange(pair.A.Columns);
                }
                if (pair.B is not null)
                {
                    columns.AddRange(pair.B.Columns.Where(
                        c => pair.A is null || !pair.A.Values.ContainsKey(c)));
                }

                foreach (var column in columns)
                {
                    var hasA = pair.A is not null && pair.A.Values.TryGetValue(column, out _);
                    var hasB = pair.B is not null && pair.B.Values.TryGetValue(column, out _);
                    var valueA = hasA ? pair.A!.Values[column] : default;
                    var valueB = hasB ? pair.B!.Values[column] : default;

                    var row = CompareRow.Attribute(testCaseNumber, gevo, baseName,
                        new AttributeKey(entity, pair.DisplayIndex, column), pair.KeyDisplay,
                        hasA, valueA, hasB, valueB);
                    fileRows.Add(row);

                    var diffText = row.Difference.HasValue
                        ? $"Δ {row.Difference.Value.ToString(German)}" : string.Empty;
                    report.AppendLine(
                        $"  {column,-16} {row.DisplayA,-14} | {row.DisplayB,-14}  {diffText,-16} {StatusLabel(row.Status)}");
                }
            }
        }

        report.AppendLine();
        report.AppendLine(
            $"Zusammenfassung: {fileRows.Count} Attribut(e), {fileRows.Count(r => r.Status != "gleich")} Unterschied(e).");

        var relativeNoExt = relative[..^OutputExtension.Length];
        var comparePath = Path.Combine(request.CompareDirectory, relativeNoExt + CompareExtension);
        Directory.CreateDirectory(Path.GetDirectoryName(comparePath)!);
        await File.WriteAllTextAsync(comparePath, report.ToString(), PlainUtf8, cancellationToken);

        return fileRows;
    }

    /// <summary>Schreibt die deutsche Gesamt-CSV (Zahlen mit Komma, Semikolon-getrennt).</summary>
    private static async Task WriteCsvAsync(string csvPath, List<CompareRow> rows,
        CancellationToken cancellationToken)
    {
        var csv = new StringBuilder();
        csv.Append("Testfall").Append(CsvSeparator).Append("GeVo").Append(CsvSeparator)
           .Append("Datei").Append(CsvSeparator)
           .Append("Entity").Append(CsvSeparator).Append("Schlüssel").Append(CsvSeparator)
           .Append("Zeile").Append(CsvSeparator)
           .Append("Attribut").Append(CsvSeparator).Append("Wert A").Append(CsvSeparator)
           .Append("Wert B").Append(CsvSeparator).Append("Differenz (A−B)").Append(CsvSeparator)
           .Append("Status").AppendLine();

        foreach (var row in rows)
        {
            csv.Append(CsvField(row.TestCaseNumber)).Append(CsvSeparator)
               .Append(CsvField(row.GeVo)).Append(CsvSeparator)
               .Append(CsvField(row.File)).Append(CsvSeparator)
               .Append(CsvField(row.Entity)).Append(CsvSeparator)
               .Append(CsvField(row.KeyDisplay)).Append(CsvSeparator)
               .Append(row.IsFileLevel ? string.Empty : row.RowIndex.ToString(CultureInfo.InvariantCulture))
               .Append(CsvSeparator)
               .Append(CsvField(row.Column)).Append(CsvSeparator)
               .Append(CsvField(row.DisplayA)).Append(CsvSeparator)
               .Append(CsvField(row.DisplayB)).Append(CsvSeparator)
               .Append(CsvField(row.Difference?.ToString(German) ?? string.Empty)).Append(CsvSeparator)
               .Append(CsvField(row.Status)).AppendLine();
        }

        var directory = Path.GetDirectoryName(csvPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        await File.WriteAllTextAsync(csvPath, csv.ToString(), CsvUtf8, cancellationToken);
    }

    /// <summary>Schreibt die Excel-Mappe mit Zusammenfassung und Abgleich.</summary>
    private static void WriteExcel(CompareOutputsRequest request, List<CompareRow> rows,
        int filesCompared, int differences)
    {
        using var workbook = new XLWorkbook();
        BuildSummarySheet(workbook, request, rows, filesCompared, differences);
        BuildReconciliationSheet(workbook, rows);

        var directory = Path.GetDirectoryName(request.ExcelPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        workbook.SaveAs(request.ExcelPath);
    }

    /// <summary>Tabellenblatt 1: Zusammenfassung (Kennzahlen, Warnungen/Fehler je System).</summary>
    private static void BuildSummarySheet(XLWorkbook workbook, CompareOutputsRequest request,
        List<CompareRow> rows, int filesCompared, int differences)
    {
        var ws = workbook.Worksheets.Add("Zusammenfassung");
        ws.Cell(1, 1).Value = "Vergleich";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Value = $"{request.LabelA}  vs  {request.LabelB}";
        ws.Cell(2, 1).Value = "Erstellt am";
        ws.Cell(2, 2).Value = DateTime.Now;

        ws.Cell(4, 1).Value = "Gerechnete Testfälle";
        ws.Cell(4, 2).Value = request.TestCaseCount;
        ws.Cell(5, 1).Value = "Verglichene Dateien";
        ws.Cell(5, 2).Value = filesCompared;
        ws.Cell(6, 1).Value = "Attribute gesamt";
        ws.Cell(6, 2).Value = rows.Count(r => !r.IsFileLevel);
        ws.Cell(7, 1).Value = "Unterschiede";
        ws.Cell(7, 2).Value = differences;

        // Warnungen/Fehler je Berechnungssystem.
        var headerRow = 9;
        ws.Cell(headerRow, 1).Value = "Berechnungssystem";
        ws.Cell(headerRow, 2).Value = "Testfälle mit Warnung";
        ws.Cell(headerRow, 3).Value = "Testfälle mit Fehler";
        ws.Cell(headerRow, 4).Value = "Exit-Code";
        ws.Range(headerRow, 1, headerRow, 4).Style.Font.Bold = true;

        var infos = new[] { request.InfoA, request.InfoB }.Where(i => i is not null).Cast<CompareRunInfo>().ToList();
        var line = headerRow + 1;
        foreach (var info in infos)
        {
            var warnCases = info.Notes.Where(n => n.Severity == "Warnung")
                .Select(n => n.TestCaseNumber).Distinct().Count();
            var errorCases = info.Notes.Where(n => n.Severity == "Fehler")
                .Select(n => n.TestCaseNumber).Distinct().Count();
            ws.Cell(line, 1).Value = info.Label;
            ws.Cell(line, 2).Value = warnCases;
            ws.Cell(line, 3).Value = errorCases;
            ws.Cell(line, 4).Value = info.ExitCode;
            line++;
        }

        // Detailtabelle der Meldungen.
        line += 1;
        ws.Cell(line, 1).Value = "Details der Warnungen/Fehler";
        ws.Cell(line, 1).Style.Font.Bold = true;
        line++;
        ws.Cell(line, 1).Value = "Testfall";
        ws.Cell(line, 2).Value = "Berechnungssystem";
        ws.Cell(line, 3).Value = "Schweregrad";
        ws.Cell(line, 4).Value = "Meldung";
        ws.Range(line, 1, line, 4).Style.Font.Bold = true;
        line++;

        var anyNotes = false;
        foreach (var info in infos)
        {
            foreach (var note in info.Notes)
            {
                ws.Cell(line, 1).Value = note.TestCaseNumber;
                ws.Cell(line, 2).Value = info.Label;
                ws.Cell(line, 3).Value = note.Severity;
                ws.Cell(line, 4).Value = note.Message;
                line++;
                anyNotes = true;
            }
        }
        if (!anyNotes)
        {
            ws.Cell(line, 1).Value = "Keine Warnungen oder Fehler.";
        }

        ws.Columns().AdjustToContents();
    }

    /// <summary>Tabellenblatt 2: der eigentliche Abgleich (eine Zeile je Attribut).</summary>
    private static void BuildReconciliationSheet(XLWorkbook workbook, List<CompareRow> rows)
    {
        var ws = workbook.Worksheets.Add("Abgleich");
        string[] headers =
        [
            "Testfall", "GeVo", "Datei", "Entity", "Schlüssel", "Zeile", "Attribut",
            "Wert A", "Wert B", "Differenz (A−B)", "Status"
        ];
        for (var c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
        }
        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        ws.SheetView.FreezeRows(1);

        var r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.TestCaseNumber;
            ws.Cell(r, 2).Value = row.GeVo;
            ws.Cell(r, 3).Value = row.File;
            ws.Cell(r, 4).Value = row.Entity;
            ws.Cell(r, 5).Value = row.KeyDisplay;
            if (!row.IsFileLevel)
            {
                ws.Cell(r, 6).Value = row.RowIndex;
            }
            ws.Cell(r, 7).Value = row.Column;
            SetValueCell(ws.Cell(r, 8), row.NumericA, row.DisplayA);
            SetValueCell(ws.Cell(r, 9), row.NumericB, row.DisplayB);
            if (row.Difference.HasValue)
            {
                ws.Cell(r, 10).Value = row.Difference.Value;
            }
            ws.Cell(r, 11).Value = row.Status;
            r++;
        }

        ws.Columns().AdjustToContents();
    }

    /// <summary>
    /// Setzt eine Wertzelle: numerisch, wenn eine Zahl vorliegt (Excel zeigt sie
    /// dann im Gebietsschema des Anwenders, also mit Komma), sonst als Text.
    /// </summary>
    private static void SetValueCell(IXLCell cell, decimal? number, string display)
    {
        if (number.HasValue)
        {
            cell.Value = number.Value;
        }
        else
        {
            cell.Value = display;
        }
    }

    private const string GeVoEntityName = "GeVo";

    /// <summary>
    /// Zerlegt eine geparste Datei in ihre Zeilen (Entity + Zeilenindex +
    /// Werte). Die Marker-Entity „GeVo" wird ausgelassen, da der
    /// Geschäftsvorfall als eigene Spalte geführt wird.
    /// </summary>
    private static List<EntityRow> ExtractRows(TestCaseFile file)
    {
        var rows = new List<EntityRow>();
        foreach (var entity in file.Entities)
        {
            if (entity.Name.Equals(GeVoEntityName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            foreach (var row in entity.Rows)
            {
                var entityRow = new EntityRow(entity.Name, row.RowIndex);
                foreach (var value in row.Values)
                {
                    entityRow.Add(value.ColumnName, new ValuePair(value.Value ?? string.Empty, value.IsQuoted));
                }
                rows.Add(entityRow);
            }
        }
        return rows;
    }

    /// <summary>Ermittelt die Schlüsselattribute einer Entität aus den Regeln.</summary>
    private static IReadOnlyList<string> FindKeyColumns(IReadOnlyList<CompareKeyRule> rules, string entity)
    {
        var rule = rules.FirstOrDefault(r => Same(r.Entity, entity));
        if (rule is null)
        {
            return [];
        }
        return rule.KeyColumns.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
    }

    /// <summary>
    /// Ordnet die Zeilen zweier Systeme einander zu: über die Schlüsselwerte,
    /// wenn Schlüsselattribute konfiguriert sind – sonst über den Zeilenindex.
    /// Zeilen ohne Gegenstück ergeben ein Paar mit nur einer Seite.
    /// </summary>
    private static IEnumerable<RowPair> PairRows(List<EntityRow> rowsA, List<EntityRow> rowsB,
        IReadOnlyList<string> keyColumns)
    {
        if (keyColumns.Count == 0)
        {
            // Ohne Vorgabe zählt die Position: Index 0 gegen Index 0 usw.
            var indices = rowsA.Select(r => r.RowIndex)
                .Concat(rowsB.Select(r => r.RowIndex))
                .Distinct()
                .OrderBy(i => i);

            foreach (var index in indices)
            {
                yield return new RowPair
                {
                    A = rowsA.FirstOrDefault(r => r.RowIndex == index),
                    B = rowsB.FirstOrDefault(r => r.RowIndex == index)
                };
            }
            yield break;
        }

        // Mit Vorgabe zählt der Schlüsselwert – die Position ist dann egal.
        var groupsA = Group(rowsA, keyColumns);
        var groupsB = Group(rowsB, keyColumns);

        var orderedKeys = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rowsA.Concat(rowsB))
        {
            var key = BuildKey(row, keyColumns);
            if (seen.Add(key))
            {
                orderedKeys.Add(key);
            }
        }

        foreach (var key in orderedKeys)
        {
            groupsA.TryGetValue(key, out var listA);
            groupsB.TryGetValue(key, out var listB);

            // Mehrere Zeilen mit gleichem Schlüssel werden der Reihe nach gepaart.
            var count = Math.Max(listA?.Count ?? 0, listB?.Count ?? 0);
            for (var i = 0; i < count; i++)
            {
                yield return new RowPair
                {
                    A = listA is not null && i < listA.Count ? listA[i] : null,
                    B = listB is not null && i < listB.Count ? listB[i] : null,
                    KeyDisplay = key
                };
            }
        }
    }

    /// <summary>Gruppiert Zeilen nach ihrem zusammengesetzten Schlüsselwert.</summary>
    private static Dictionary<string, List<EntityRow>> Group(List<EntityRow> rows,
        IReadOnlyList<string> keyColumns)
        => rows.GroupBy(r => BuildKey(r, keyColumns), StringComparer.OrdinalIgnoreCase)
               .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Bildet den zusammengesetzten Schlüsselwert einer Zeile.</summary>
    private static string BuildKey(EntityRow row, IReadOnlyList<string> keyColumns)
        => string.Join(" | ", keyColumns.Select(
            c => row.Values.TryGetValue(c, out var value) ? value.Value : string.Empty));

    /// <summary>Vergleicht Namen ohne Beachtung der Groß-/Kleinschreibung.</summary>
    private static bool Same(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Ermittelt den Geschäftsvorfall einer Datei: bevorzugt aus der Entity
    /// „GeVo" (Spalte „gevo"), ersatzweise aus dem Dateinamenpräfix.
    /// </summary>
    private static string ExtractGeVo(TestCaseFile file, string baseName)
    {
        var gevoEntity = file.Entities.FirstOrDefault(
            e => e.Name.Equals(GeVoEntityName, StringComparison.OrdinalIgnoreCase));
        var value = gevoEntity?.Rows.FirstOrDefault()?.Values
            .FirstOrDefault(v => v.ColumnName.Equals("gevo", StringComparison.OrdinalIgnoreCase))?.Value;
        return !string.IsNullOrWhiteSpace(value) ? value : GeVoFromFileName(baseName);
    }

    /// <summary>Leitet den Geschäftsvorfall aus dem Dateinamenpräfix ab ("{GeVo}_…").</summary>
    private static string GeVoFromFileName(string baseName)
    {
        var index = baseName.IndexOf('_');
        return index > 0 ? baseName[..index] : string.Empty;
    }

    private static string StatusLabel(string status) => status switch
    {
        "gleich" => "gleich",
        "unterschied" => "UNTERSCHIED",
        "nur A" => "NUR A",
        _ => "NUR B"
    };

    /// <summary>Ermittelt die Testfallnummer aus dem relativen Pfad ("0001_TF/…").</summary>
    private static string ExtractTestCaseNumber(string relative)
    {
        var firstSegment = relative.Replace('\\', '/').Split('/')[0];
        return firstSegment.EndsWith("_TF", StringComparison.OrdinalIgnoreCase)
            ? firstSegment[..^3]
            : firstSegment;
    }

    /// <summary>Maskiert ein CSV-Feld (Trennzeichen, Anführungszeichen, Zeilenumbrüche).</summary>
    private static string CsvField(string value)
    {
        if (value.IndexOf(CsvSeparator) < 0 && value.IndexOf('"') < 0
            && value.IndexOf('\n') < 0 && value.IndexOf('\r') < 0)
        {
            return value;
        }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>Schlüssel eines Attributs innerhalb einer Datei.</summary>
    private readonly record struct AttributeKey(string Entity, int RowIndex, string Column);

    /// <summary>Wert eines Attributs samt Anführungszeichen-Kennung.</summary>
    private readonly record struct ValuePair(string Value, bool IsQuoted);

    /// <summary>Eine Datenzeile einer Entität mit ihren Werten in Spaltenreihenfolge.</summary>
    private sealed class EntityRow(string entity, int rowIndex)
    {
        /// <summary>Name der Entität.</summary>
        public string Entity { get; } = entity;

        /// <summary>Zeilenindex innerhalb der Entität.</summary>
        public int RowIndex { get; } = rowIndex;

        /// <summary>Spaltennamen in der Reihenfolge der Datei.</summary>
        public List<string> Columns { get; } = [];

        /// <summary>Werte je Spalte (Spaltennamen ohne Beachtung der Groß-/Kleinschreibung).</summary>
        public Dictionary<string, ValuePair> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Übernimmt einen Wert und merkt sich die Spaltenreihenfolge.</summary>
        /// <param name="column">Spaltenname.</param>
        /// <param name="value">Wert der Spalte.</param>
        public void Add(string column, ValuePair value)
        {
            if (!Values.ContainsKey(column))
            {
                Columns.Add(column);
            }
            Values[column] = value;
        }
    }

    /// <summary>Ein einander zugeordnetes Zeilenpaar (eine Seite darf fehlen).</summary>
    private sealed class RowPair
    {
        /// <summary>Zeile aus System A (oder <c>null</c>, wenn nur in B vorhanden).</summary>
        public EntityRow? A { get; init; }

        /// <summary>Zeile aus System B (oder <c>null</c>, wenn nur in A vorhanden).</summary>
        public EntityRow? B { get; init; }

        /// <summary>Der Schlüsselwert (leer bei Zuordnung über den Index).</summary>
        public string KeyDisplay { get; init; } = string.Empty;

        /// <summary>Anzuzeigender Zeilenindex (bevorzugt aus A).</summary>
        public int DisplayIndex => A?.RowIndex ?? B?.RowIndex ?? 0;

        /// <summary>Beschriftung der Zeile für die lesbare Vergleichsdatei.</summary>
        /// <param name="keyColumns">Die konfigurierten Schlüsselattribute.</param>
        public string Label(IReadOnlyList<string> keyColumns)
            => keyColumns.Count > 0
                ? $"{string.Join(" | ", keyColumns)} = {KeyDisplay}"
                : $"Zeile {DisplayIndex}";
    }

    /// <summary>Eine Zeile des Abgleichs (ein Attribut oder eine fehlende Datei).</summary>
    private sealed class CompareRow
    {
        public string TestCaseNumber { get; private init; } = string.Empty;
        public string GeVo { get; private init; } = string.Empty;
        public string File { get; private init; } = string.Empty;

        /// <summary>Der Schlüsselwert der Zeile (leer bei Zuordnung über den Index).</summary>
        public string KeyDisplay { get; private init; } = string.Empty;
        public string Entity { get; private init; } = string.Empty;
        public int RowIndex { get; private init; }
        public string Column { get; private init; } = string.Empty;
        public string DisplayA { get; private init; } = "—";
        public string DisplayB { get; private init; } = "—";
        public decimal? NumericA { get; private init; }
        public decimal? NumericB { get; private init; }
        public decimal? Difference { get; private init; }
        public string Status { get; private init; } = string.Empty;

        /// <summary>Ob die Zeile eine ganze Datei betrifft (nicht ein einzelnes Attribut).</summary>
        public bool IsFileLevel { get; private init; }

        /// <summary>Erzeugt eine Attributzeile inklusive numerischer Differenz.</summary>
        public static CompareRow Attribute(string testCase, string gevo, string file, AttributeKey key,
            string keyDisplay, bool hasA, ValuePair valueA, bool hasB, ValuePair valueB)
        {
            var numA = hasA ? ParseNumber(valueA) : null;
            var numB = hasB ? ParseNumber(valueB) : null;

            string status;
            if (hasA && hasB)
            {
                status = string.Equals(valueA.Value, valueB.Value, StringComparison.Ordinal)
                    ? "gleich" : "unterschied";
            }
            else
            {
                status = hasA ? "nur A" : "nur B";
            }

            return new CompareRow
            {
                TestCaseNumber = testCase,
                GeVo = gevo,
                KeyDisplay = keyDisplay,
                File = file,
                Entity = key.Entity,
                RowIndex = key.RowIndex,
                Column = key.Column,
                DisplayA = Display(hasA, valueA, numA),
                DisplayB = Display(hasB, valueB, numB),
                NumericA = numA,
                NumericB = numB,
                Difference = numA.HasValue && numB.HasValue ? numA.Value - numB.Value : null,
                Status = status
            };
        }

        /// <summary>Erzeugt eine Zeile für eine nur einseitig vorhandene Datei.</summary>
        public static CompareRow MissingFile(string testCase, string gevo, string file, bool presentInA) => new()
        {
            TestCaseNumber = testCase,
            GeVo = gevo,
            File = file,
            Entity = string.Empty,
            Column = "(ganze Datei)",
            DisplayA = presentInA ? "vorhanden" : "—",
            DisplayB = presentInA ? "—" : "vorhanden",
            Status = presentInA ? "nur A" : "nur B",
            IsFileLevel = true
        };

        /// <summary>Numerischer Wert eines nicht in Anführungszeichen stehenden Wertes.</summary>
        private static decimal? ParseNumber(ValuePair value)
        {
            if (value.IsQuoted)
            {
                return null;
            }
            return decimal.TryParse(value.Value,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        /// <summary>Anzeigedarstellung eines Wertes (Zahl deutsch mit Komma, Text mit Anführungszeichen).</summary>
        private static string Display(bool has, ValuePair value, decimal? number)
        {
            if (!has)
            {
                return "—";
            }
            if (number.HasValue)
            {
                return number.Value.ToString(German);
            }
            return value.IsQuoted ? $"\"{value.Value}\"" : value.Value;
        }
    }
}
