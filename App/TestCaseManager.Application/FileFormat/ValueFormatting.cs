using System.Globalization;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.FileFormat;

/// <summary>
/// Hilfsfunktionen zum Zerlegen und Zusammensetzen einzelner Werte
/// (<c>tuer=2</c>, <c>baujahr="01.01.1965"</c>) für das Standard-Dateiformat.
/// Kapselt die Regeln für Anführungszeichen und die Datentyp-Ableitung an
/// einer Stelle, damit Parser und Writer garantiert konsistent bleiben.
/// </summary>
internal static class ValueFormatting
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Das Ergebnis der Zerlegung eines rohen Wert-Tokens.</summary>
    /// <param name="Value">Logischer Wert ohne umschließende Anführungszeichen.</param>
    /// <param name="IsQuoted">Ob der Wert in Anführungszeichen stand.</param>
    /// <param name="DataType">Abgeleiteter Datentyp.</param>
    internal readonly record struct ParsedValue(string Value, bool IsQuoted, ParameterDataType DataType);

    /// <summary>
    /// Zerlegt das rohe Wert-Token (rechts vom ersten <c>=</c>) in logischen
    /// Wert, Anführungszeichen-Kennung und abgeleiteten Datentyp.
    /// </summary>
    /// <param name="rawValue">Das rohe Token, z. B. <c>2</c> oder <c>"01.01.1965"</c>.</param>
    internal static ParsedValue Parse(string rawValue)
    {
        var token = rawValue.Trim();

        if (token.Length >= 2 && token[0] == '"' && token[^1] == '"')
        {
            // In Anführungszeichen => bewusst als Text markiert.
            return new ParsedValue(token[1..^1], IsQuoted: true, ParameterDataType.Text);
        }

        return new ParsedValue(token, IsQuoted: false, InferDataType(token));
    }

    /// <summary>
    /// Setzt einen logischen Wert wieder in seine Textdarstellung zusammen –
    /// mit Anführungszeichen, wenn er ursprünglich welche hatte.
    /// </summary>
    /// <param name="value">Logischer Wert (ohne Anführungszeichen).</param>
    /// <param name="isQuoted">Ob Anführungszeichen zu setzen sind.</param>
    internal static string Render(string? value, bool isQuoted)
    {
        var text = value ?? string.Empty;
        return isQuoted ? $"\"{text}\"" : text;
    }

    // Für die Dezimalerkennung bewusst OHNE Tausendertrennzeichen: sonst würde
    // z. B. ein Datum wie "01.01.1965" von der deutschen Kultur als Zahl
    // (Punkte als Tausenderpunkte) fehlinterpretiert. Erlaubt sind nur
    // Vorzeichen und genau ein Dezimaltrennzeichen.
    private const NumberStyles DecimalStyle =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    /// <summary>Leitet den Datentyp eines nicht in Anführungszeichen stehenden Wertes ab.</summary>
    private static ParameterDataType InferDataType(string token)
    {
        if (token.Length == 0)
        {
            return ParameterDataType.Text;
        }

        if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return ParameterDataType.Integer;
        }

        if (decimal.TryParse(token, DecimalStyle, CultureInfo.InvariantCulture, out _)
            || decimal.TryParse(token, DecimalStyle, German, out _))
        {
            return ParameterDataType.Decimal;
        }

        return ParameterDataType.Text;
    }
}
