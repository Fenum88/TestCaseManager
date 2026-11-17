using System.Text;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.FileFormat;
using TestCaseManager.Application.Services;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Infrastructure.Services;

/// <summary>
/// Datei-basiertes Ein- und Auslesen von Testfällen. Diese Klasse kümmert
/// sich um das Dateisystem (Ordnerstruktur, Lesen/Schreiben) und delegiert
/// das eigentliche Format an die austauschbaren Module
/// <see cref="ITestCaseFileParser"/> und <see cref="ITestCaseFileWriter"/>.
///
/// Ordnerstruktur:
/// <code>
/// {root}/Input/{Nummer}_TF/{FileNr}_{Bezeichnung}.Eingabe
/// {root}/Output/{Nummer}_TF/{FileNr}_{Bezeichnung}.Ausgabe
/// </code>
/// </summary>
public class TestCaseFileService : ITestCaseFileService
{
    private const string InputFolder = "Input";
    private const string OutputFolder = "Output";
    private const string TestCaseFolderSuffix = "_TF";
    private const string InputExtension = ".Eingabe";
    private const string OutputExtension = ".Ausgabe";

    private static readonly Encoding FileEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ITestCaseFileParser _parser;
    private readonly ITestCaseFileWriter _writer;
    private readonly TestCaseFileOptions _options;
    private readonly ILogger<TestCaseFileService> _logger;

    /// <summary>Erzeugt den Dienst mit seinen Abhängigkeiten (Dependency Injection).</summary>
    /// <param name="unitOfWork">Arbeitseinheit für den Datenbankzugriff.</param>
    /// <param name="parser">Austauschbares Einlese-Modul.</param>
    /// <param name="writer">Austauschbares Auslese-Modul.</param>
    /// <param name="options">Einstellungen des Ein-/Auslesens.</param>
    /// <param name="logger">Logger für Diagnose und Fehlerprotokollierung.</param>
    public TestCaseFileService(IUnitOfWork unitOfWork, ITestCaseFileParser parser,
        ITestCaseFileWriter writer, TestCaseFileOptions options, ILogger<TestCaseFileService> logger)
    {
        _unitOfWork = unitOfWork;
        _parser = parser;
        _writer = writer;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImportResult> ImportFolderAsync(string rootPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Der Ordner \"{rootPath}\" wurde nicht gefunden.");
        }

        var result = new ImportResult();

        // 1. Dateien einlesen und nach Testfallnummer gruppieren. Der Abschnitt
        //    Output wird nur berücksichtigt, wenn er ausdrücklich konfiguriert
        //    ist – standardmäßig werden ausschließlich die Eingabewerte gespeichert.
        var byNumber = new Dictionary<string, List<TestCaseFile>>(StringComparer.OrdinalIgnoreCase);
        ReadSection(rootPath, InputFolder, TestCaseFileSection.Input, byNumber, result);

        if (_options.ImportOutputSection)
        {
            ReadSection(rootPath, OutputFolder, TestCaseFileSection.Output, byNumber, result);
        }

        result.Messages.Add($"Ordner: {rootPath}");
        if (!_options.ImportOutputSection)
        {
            result.Messages.Add("Hinweis: Ausgabewerte werden nicht gespeichert "
                + "(Einstellung TestCaseFiles:ImportOutputSection = false).");
        }

        if (byNumber.Count == 0)
        {
            var searched = _options.ImportOutputSection ? "Input/Output" : "Input";
            result.Messages.Add($"Keine Testfalldateien im Unterordner {searched} gefunden.");
            _logger.LogInformation("Keine Testfalldateien unter {Root} gefunden.", rootPath);
            return result;
        }

        // 2. Bestehende Testfälle mit gleicher Nummer entfernen (Upsert),
        //    getrennt gespeichert, um die Eindeutigkeit der Nummer nicht zu verletzen.
        foreach (var number in byNumber.Keys)
        {
            var existing = await _unitOfWork.ImportedTestCases
                .GetByNumberWithDetailsAsync(number, cancellationToken);
            if (existing is not null)
            {
                _unitOfWork.ImportedTestCases.Remove(existing);
            }
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Neue Testfälle anlegen.
        foreach (var (number, files) in byNumber.OrderBy(kv => kv.Key))
        {
            var importedTestCase = new ImportedTestCase { Number = number };
            ApplyFileSortOrder(files);
            importedTestCase.Files.AddRange(files);

            await _unitOfWork.ImportedTestCases.AddAsync(importedTestCase, cancellationToken);

            result.TestCaseCount++;
            result.FileCount += files.Count;

            var entityCount = files.SelectMany(f => f.Entities).Count();
            var rowCount = files.SelectMany(f => f.Entities).SelectMany(e => e.Rows).Count();
            var valueCount = files.SelectMany(f => f.Entities)
                .SelectMany(e => e.Rows).SelectMany(r => r.Values).Count();
            result.Messages.Add(
                $"Testfall {number}: {files.Count} Datei(en), {entityCount} Entities, "
                + $"{rowCount} Zeilen, {valueCount} Werte");
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Einlesen abgeschlossen: {Summary}", result.Summary);
        return result;
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportFolderAsync(string rootPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var result = new ExportResult();
        var testCases = await _unitOfWork.ImportedTestCases.GetAllWithDetailsAsync(cancellationToken);

        result.Messages.Add($"Zielordner: {rootPath}");
        if (testCases.Count == 0)
        {
            result.Messages.Add("Keine Datei-Testfälle in der Datenbank vorhanden.");
        }

        foreach (var testCase in testCases)
        {
            var writtenForTestCase = 0;
            foreach (var file in testCase.Files)
            {
                try
                {
                    var sectionFolder = file.Section == TestCaseFileSection.Input ? InputFolder : OutputFolder;
                    var targetFolder = Path.Combine(rootPath, sectionFolder,
                        testCase.Number + TestCaseFolderSuffix);
                    Directory.CreateDirectory(targetFolder);

                    var fileName = file.FileNumber + "_" + file.Designation + _writer.GetFileExtension(file);
                    var targetPath = Path.Combine(targetFolder, fileName);

                    await File.WriteAllTextAsync(targetPath, _writer.Write(file), FileEncoding, cancellationToken);
                    result.FileCount++;
                    writtenForTestCase++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    var message = $"Testfall {testCase.Number}, Datei {file.Designation}: {ex.Message}";
                    result.Errors.Add(message);
                    _logger.LogError(ex, "Fehler beim Auslesen: {Message}", message);
                }
            }

            result.TestCaseCount++;
            result.Messages.Add($"Testfall {testCase.Number}: {writtenForTestCase} Datei(en) geschrieben");
        }

        _logger.LogInformation("Auslesen abgeschlossen: {Summary}", result.Summary);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportedTestCaseListItem>> GetImportedAsync(
        CancellationToken cancellationToken = default)
        => await _unitOfWork.ImportedTestCases.GetSummariesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntityTemplate>> GetEntityTemplatesAsync(
        CancellationToken cancellationToken = default)
        => await _unitOfWork.ImportedTestCases.GetEntityTemplatesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DatabaseAttributeRow>> GetDatabaseAttributesAsync(
        CancellationToken cancellationToken = default)
        => await _unitOfWork.ImportedTestCases.GetAllAttributesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task CreateFileTestCaseAsync(NewFileTestCase draft,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var number = draft.Number?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new InvalidOperationException("Die Testfallnummer darf nicht leer sein.");
        }

        var existing = await _unitOfWork.ImportedTestCases
            .GetByNumberWithDetailsAsync(number, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Die Testfallnummer \"{number}\" ist bereits vergeben.");
        }

        var designation = string.IsNullOrWhiteSpace(draft.Designation) ? "Eingabe" : draft.Designation.Trim();

        // Ein Eingabeabschnitt mit einer Datei; Dateiname ergibt sich beim
        // Auslesen aus FileNumber und Bezeichnung.
        var file = new TestCaseFile
        {
            Section = TestCaseFileSection.Input,
            FileNumber = "0001",
            Designation = designation,
            SortOrder = 0
        };

        var entitySortOrder = 0;
        foreach (var entityDraft in draft.Entities)
        {
            var entity = new DataEntity
            {
                Name = entityDraft.Name.Trim(),
                SortOrder = entitySortOrder++
            };

            var rowIndex = 0;
            foreach (var rowDraft in entityDraft.Rows)
            {
                var row = new DataRow { RowIndex = rowIndex++ };

                for (var column = 0; column < entityDraft.Columns.Count; column++)
                {
                    var raw = column < rowDraft.Values.Count ? rowDraft.Values[column] : null;
                    // Leere Zellen werden nicht gespeichert (Zeilen dürfen
                    // unterschiedliche Spalten besitzen).
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    var manual = ManualValueFactory.Create(raw);
                    row.Values.Add(new DataValue
                    {
                        ColumnName = entityDraft.Columns[column].Trim(),
                        Value = manual.Value,
                        IsQuoted = manual.IsQuoted,
                        DataType = manual.DataType,
                        SortOrder = column
                    });
                }

                entity.Rows.Add(row);
            }

            file.Entities.Add(entity);
        }

        var importedTestCase = new ImportedTestCase { Number = number };
        importedTestCase.Files.Add(file);

        await _unitOfWork.ImportedTestCases.AddAsync(importedTestCase, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Datei-Testfall {Number} über die Oberfläche angelegt "
            + "({EntityCount} Entities).", number, draft.Entities.Count);
    }

    /// <inheritdoc />
    public async Task<ImportedTestCaseDetail?> GetImportedDetailAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        var testCase = await _unitOfWork.ImportedTestCases.GetByIdWithDetailsAsync(id, cancellationToken);
        if (testCase is null)
        {
            return null;
        }

        // Den Objektgraphen (Datei → Entity → Zeile → Wert) zu einer flachen
        // Attributliste ausrollen. Die Reihenfolge der Include-Abfrage bleibt
        // dabei erhalten, sodass die Anzeige der Dateireihenfolge entspricht.
        var attributes = testCase.Files
            .SelectMany(file => file.Entities
                .SelectMany(entity => entity.Rows
                    .SelectMany(row => row.Values
                        .Select(value => new DataAttributeItem
                        {
                            ValueId = value.Id,
                            Section = file.Section == TestCaseFileSection.Input ? "Eingabe" : "Ausgabe",
                            FileName = file.FileNumber + "_" + file.Designation,
                            EntityName = entity.Name,
                            RowIndex = row.RowIndex,
                            ColumnName = value.ColumnName,
                            Value = value.Value,
                            DataType = value.DataType.ToDisplayName(),
                            IsQuoted = value.IsQuoted
                        }))))
            .ToList();

        return new ImportedTestCaseDetail
        {
            Id = testCase.Id,
            Number = testCase.Number,
            ImportedBy = testCase.ModifiedBy ?? testCase.CreatedBy,
            ImportedAt = testCase.ModifiedAt ?? testCase.CreatedAt,
            Attributes = attributes
        };
    }

    /// <inheritdoc />
    public async Task<int> UpdateAttributeValuesAsync(Guid id, IReadOnlyList<AttributeValueUpdate> updates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        var testCase = await _unitOfWork.ImportedTestCases.GetByIdWithDetailsAsync(id, cancellationToken);
        if (testCase is null)
        {
            throw new InvalidOperationException(
                "Der Testfall existiert nicht mehr in der Datenbank.");
        }

        if (updates.Count == 0)
        {
            return 0;
        }

        // Nur die Werte dieses Testfalls sind adressierbar. Eine Änderung kann
        // dadurch selbst bei manipulierter Id nie einen fremden Testfall treffen.
        var ownValues = testCase.Files
            .SelectMany(file => file.Entities)
            .SelectMany(entity => entity.Rows)
            .SelectMany(row => row.Values)
            .ToDictionary(value => value.Id);

        var changed = 0;
        foreach (var update in updates)
        {
            if (!ownValues.TryGetValue(update.ValueId, out var value))
            {
                throw new InvalidOperationException(
                    "Ein zu änderndes Attribut gehört nicht zu diesem Testfall. "
                    + "Bitte die Ansicht aktualisieren und erneut versuchen.");
            }

            // Datentyp nach denselben Regeln ableiten wie beim Einlesen –
            // sonst driften Datei und Datenbank auseinander.
            var manual = ManualValueFactory.Create(update.NewValue);
            var isQuoted = DeriveQuoting(value, manual);

            if (value.Value == manual.Value && value.IsQuoted == isQuoted
                && value.DataType == manual.DataType)
            {
                continue;
            }

            value.Value = manual.Value;
            value.IsQuoted = isQuoted;
            value.DataType = manual.DataType;
            changed++;
        }

        if (changed == 0)
        {
            return 0;
        }

        // Der Testfall selbst wird als geändert markiert, damit „geändert von/am"
        // die Bearbeitung widerspiegelt. Die konkreten Werte setzt der DbContext
        // beim Speichern (Auditfelder).
        testCase.ModifiedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Testfall {Number} ({TestCaseId}): {Count} Attributwert(e) geändert.",
            testCase.Number, testCase.Id, changed);
        return changed;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteImportedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Vollständig laden, damit der ChangeTracker die abhängigen Entitäten
        // kennt. Die Fremdschlüssel sind auf Cascade konfiguriert, sodass
        // Entities, Zeilen und Werte mitgelöscht werden.
        var testCase = await _unitOfWork.ImportedTestCases.GetByIdWithDetailsAsync(id, cancellationToken);
        if (testCase is null)
        {
            _logger.LogWarning("Datei-Testfall {TestCaseId} sollte gelöscht werden, existiert aber nicht.", id);
            return false;
        }

        _unitOfWork.ImportedTestCases.Remove(testCase);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Datei-Testfall {Number} ({TestCaseId}) wurde gelöscht.",
            testCase.Number, testCase.Id);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> DeleteAllImportedAsync(CancellationToken cancellationToken = default)
    {
        var testCases = await _unitOfWork.ImportedTestCases.GetAllWithDetailsAsync(cancellationToken);
        if (testCases.Count == 0)
        {
            return 0;
        }

        foreach (var testCase in testCases)
        {
            _unitOfWork.ImportedTestCases.Remove(testCase);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alle {Count} Datei-Testfälle wurden gelöscht.", testCases.Count);
        return testCases.Count;
    }

    /// <summary>
    /// Bestimmt, ob ein geänderter Wert in Anführungszeichen geschrieben wird.
    ///
    /// Das Dateiformat kennt keine feste Regel „Text ist immer gequotet": In den
    /// Testfällen steht <c>baujahr="01.01.2010"</c> mit, <c>nachname=Weber</c>
    /// dagegen ohne Anführungszeichen. Bleibt ein Feld daher Text, wird seine
    /// bisherige Schreibweise beibehalten; nur bei einem Typwechsel entscheidet
    /// die Ableitung neu. Zahlen stehen nie in Anführungszeichen.
    /// </summary>
    /// <param name="current">Der bisher gespeicherte Wert.</param>
    /// <param name="manual">Die Ableitung aus dem neuen Rohwert.</param>
    private static bool DeriveQuoting(DataValue current, ManualValueFactory.ManualValue manual)
    {
        if (manual.DataType != ParameterDataType.Text)
        {
            return false;
        }

        return current.DataType == ParameterDataType.Text ? current.IsQuoted : manual.IsQuoted;
    }

    /// <summary>
    /// Liest alle Dateien eines Abschnitts (Input/Output) ein und ordnet sie
    /// der jeweiligen Testfallnummer zu.
    /// </summary>
    private void ReadSection(string rootPath, string sectionFolder, TestCaseFileSection section,
        Dictionary<string, List<TestCaseFile>> byNumber, ImportResult result)
    {
        var sectionPath = Path.Combine(rootPath, sectionFolder);
        if (!Directory.Exists(sectionPath))
        {
            return;
        }

        var extension = section == TestCaseFileSection.Input ? InputExtension : OutputExtension;

        foreach (var testCaseFolder in Directory.EnumerateDirectories(sectionPath))
        {
            var number = ExtractNumberFromFolder(Path.GetFileName(testCaseFolder));

            foreach (var filePath in Directory.EnumerateFiles(testCaseFolder, "*" + extension))
            {
                try
                {
                    var (fileNumber, designation) = SplitFileName(Path.GetFileNameWithoutExtension(filePath));
                    var content = File.ReadAllText(filePath, FileEncoding);
                    var file = _parser.Parse(fileNumber, designation, section, content);

                    if (!byNumber.TryGetValue(number, out var files))
                    {
                        files = [];
                        byNumber[number] = files;
                    }
                    files.Add(file);
                }
                catch (Exception ex) when (ex is IOException or FormatException)
                {
                    var message = $"{Path.GetFileName(filePath)}: {ex.Message}";
                    result.Errors.Add(message);
                    _logger.LogError(ex, "Fehler beim Einlesen: {Message}", message);
                }
            }
        }
    }

    /// <summary>Vergibt fortlaufende Reihenfolgen je Abschnitt.</summary>
    private static void ApplyFileSortOrder(List<TestCaseFile> files)
    {
        var perSection = new Dictionary<TestCaseFileSection, int>();
        foreach (var file in files.OrderBy(f => f.Section).ThenBy(f => f.FileNumber))
        {
            var order = perSection.GetValueOrDefault(file.Section);
            file.SortOrder = order;
            perSection[file.Section] = order + 1;
        }
    }

    /// <summary>Ermittelt die Testfallnummer aus einem Ordnernamen wie "0001_TF".</summary>
    private static string ExtractNumberFromFolder(string folderName)
        => folderName.EndsWith(TestCaseFolderSuffix, StringComparison.OrdinalIgnoreCase)
            ? folderName[..^TestCaseFolderSuffix.Length]
            : folderName;

    /// <summary>
    /// Zerlegt einen Dateinamen ohne Endung (z. B. "0001_Vertragsbeginn") in
    /// Nummerpräfix und Bezeichnung.
    /// </summary>
    private static (string FileNumber, string Designation) SplitFileName(string baseName)
    {
        var separator = baseName.IndexOf('_');
        return separator > 0
            ? (baseName[..separator], baseName[(separator + 1)..])
            : (string.Empty, baseName);
    }
}
