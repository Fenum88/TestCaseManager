using System.Text;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.FileFormat;

/// <summary>
/// Standard-Implementierung von <see cref="ITestCaseFileWriter"/>, das
/// Gegenstück zu <see cref="IndentedKeyValueFileParser"/>. Erzeugt exakt
/// das eingerückte Schlüssel-Wert-Format wieder, sodass eine eingelesene
/// und anschließend ausgelesene Datei inhaltsgleich ist.
///
/// Entitäten werden mit dem Standardpräfix <c>b_</c> geschrieben – Präfixe
/// werden beim Einlesen abgeschnitten und sind im Namen nicht enthalten.
/// </summary>
public class IndentedKeyValueFileWriter : ITestCaseFileWriter
{
    /// <summary>Einrückung je Ebene (4 Leerzeichen).</summary>
    private const string Indent = "    ";

    /// <inheritdoc />
    public string Write(TestCaseFile file)
    {
        var builder = new StringBuilder();

        var entities = file.Entities.OrderBy(e => e.SortOrder).ToList();
        for (var entityIndex = 0; entityIndex < entities.Count; entityIndex++)
        {
            var entity = entities[entityIndex];

            // Leerzeile trennt aufeinanderfolgende Entities.
            if (entityIndex > 0)
            {
                builder.AppendLine();
            }

            builder.Append("b_").AppendLine(entity.Name);

            foreach (var row in entity.Rows.OrderBy(r => r.RowIndex))
            {
                builder.Append(Indent)
                       .Append('(').Append(row.RowIndex).Append(")_")
                       .AppendLine(entity.Name);

                foreach (var value in row.Values.OrderBy(v => v.SortOrder))
                {
                    builder.Append(Indent).Append(Indent)
                           .Append(value.ColumnName).Append('=')
                           .AppendLine(ValueFormatting.Render(value.Value, value.IsQuoted));
                }
            }
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public string GetFileExtension(TestCaseFile file) => file.Section switch
    {
        TestCaseFileSection.Input => ".Eingabe",
        TestCaseFileSection.Output => ".Ausgabe",
        _ => ".txt"
    };
}
