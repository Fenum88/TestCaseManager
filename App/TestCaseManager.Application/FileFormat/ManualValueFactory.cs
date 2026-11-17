using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.FileFormat;

/// <summary>
/// Erzeugt aus einem manuell eingegebenen Wert die für die Speicherung
/// benötigten Angaben: logischer Wert, Anführungszeichen-Kennung und
/// abgeleiteter Datentyp. Nutzt dieselben Ableitungsregeln wie das Einlesen
/// (<see cref="ValueFormatting"/>), damit von Hand angelegte und eingelesene
/// Testfälle identisch behandelt werden. Öffentlich, damit die
/// Infrastrukturschicht beim Anlegen darauf zugreifen kann.
/// </summary>
public static class ManualValueFactory
{
    /// <summary>Das Ergebnis der Ableitung für einen manuell eingegebenen Wert.</summary>
    /// <param name="Value">Logischer Wert ohne umschließende Anführungszeichen.</param>
    /// <param name="IsQuoted">Ob der Wert beim Schreiben in Anführungszeichen steht.</param>
    /// <param name="DataType">Abgeleiteter Datentyp.</param>
    public readonly record struct ManualValue(string Value, bool IsQuoted, ParameterDataType DataType);

    /// <summary>
    /// Leitet aus dem eingegebenen Rohtext den zu speichernden Wert ab. Zahlen
    /// werden ohne Anführungszeichen als Ganzzahl bzw. Dezimalzahl erkannt;
    /// alles andere gilt als Text und wird beim Schreiben in Anführungszeichen
    /// gesetzt (wie im Dateiformat üblich).
    /// </summary>
    /// <param name="raw">Der eingegebene Wert (mit oder ohne Anführungszeichen).</param>
    public static ManualValue Create(string? raw)
    {
        var text = (raw ?? string.Empty).Trim();

        // Vom Anwender getippte Anführungszeichen entfernen – sie werden beim
        // Schreiben anhand des Datentyps ohnehin wieder gesetzt.
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
        {
            text = text[1..^1];
        }

        var parsed = ValueFormatting.Parse(text);
        var isQuoted = parsed.DataType == ParameterDataType.Text && text.Length > 0;
        return new ManualValue(text, isQuoted, parsed.DataType);
    }
}
