using System.Text.RegularExpressions;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.FileFormat;

/// <summary>
/// Standard-Implementierung von <see cref="ITestCaseFileParser"/> für das
/// eingerückte Schlüssel-Wert-Format des Berechnungssystems:
/// <code>
/// b_Gebaeude
///     (0)_Gebaeude
///         tuer=2
///         baujahr="01.01.1965"
/// </code>
/// Die Erkennung erfolgt inhaltsbasiert (Entity-Präfix, Muster
/// <c>(N)_Name</c>, <c>Schlüssel=Wert</c>) und ist damit tolerant gegenüber
/// abweichender Einrückung.
///
/// Das Entity-Präfix ist reine Formatkennzeichnung und wird einheitlich
/// abgeschnitten: <c>b_Gebaeude</c> → <c>Gebaeude</c>, <c>a_Raeume</c> →
/// <c>Raeume</c>. Die Entität heißt in Datenbank und Abgleich also immer ohne
/// Präfix.
/// </summary>
public partial class IndentedKeyValueFileParser : ITestCaseFileParser
{
    /// <summary>Erkennt eine Zeilendefinition wie <c>(0)_Gebaeude</c>.</summary>
    [GeneratedRegex(@"^\((?<index>\d+)\)_(?<name>.+)$")]
    private static partial Regex RowPattern();

    /// <summary>Erkennt einen Entity-Kopf wie <c>b_Gebaeude</c> oder <c>a_Raeume</c>.</summary>
    [GeneratedRegex(@"^(?<prefix>[A-Za-z])_(?<name>.+)$")]
    private static partial Regex EntityPattern();

    /// <inheritdoc />
    public TestCaseFile Parse(string fileNumber, string designation,
        TestCaseFileSection section, string content)
    {
        var file = new TestCaseFile
        {
            FileNumber = fileNumber,
            Designation = designation,
            Section = section
        };

        DataEntity? currentEntity = null;
        DataRow? currentRow = null;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue; // Leerzeilen trennen nur optisch.
            }

            // 1) Zeilen-Kopf: (N)_Name
            var rowMatch = RowPattern().Match(line);
            if (rowMatch.Success && currentEntity is not null)
            {
                currentRow = new DataRow
                {
                    RowIndex = int.Parse(rowMatch.Groups["index"].Value)
                };
                currentEntity.Rows.Add(currentRow);
                continue;
            }

            // 2) Entity-Kopf: {Präfix}_Name (vor der Wertprüfung, aber nach der
            //    Zeilenprüfung; Werte enthalten stets ein "=").
            var separator = line.IndexOf('=');
            if (separator < 0)
            {
                var entityMatch = EntityPattern().Match(line);
                if (entityMatch.Success)
                {
                    // Präfix (b_, a_, …) ist Formatkennzeichnung und entfällt im Namen.
                    currentEntity = new DataEntity
                    {
                        Name = entityMatch.Groups["name"].Value,
                        SortOrder = file.Entities.Count
                    };
                    file.Entities.Add(currentEntity);
                    currentRow = null;
                }
                continue;
            }

            // 3) Wert: Schlüssel=Wert
            if (separator > 0 && currentRow is not null)
            {
                var columnName = line[..separator].Trim();
                var parsed = ValueFormatting.Parse(line[(separator + 1)..]);

                currentRow.Values.Add(new DataValue
                {
                    ColumnName = columnName,
                    Value = parsed.Value,
                    IsQuoted = parsed.IsQuoted,
                    DataType = parsed.DataType,
                    SortOrder = currentRow.Values.Count
                });
            }
        }

        return file;
    }
}
