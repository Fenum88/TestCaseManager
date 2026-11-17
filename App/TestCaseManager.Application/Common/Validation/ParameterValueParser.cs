using System.Globalization;
using System.Text.Json;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.Common.Validation;

/// <summary>
/// Prüft, ob ein als Zeichenkette gespeicherter Wert zu einem
/// <see cref="ParameterDataType"/> passt. Zahlen werden sowohl in
/// deutscher (Komma) als auch invarianter (Punkt) Schreibweise akzeptiert.
/// </summary>
public static class ParameterValueParser
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly string[] TrueValues = ["true", "ja", "wahr", "1"];
    private static readonly string[] FalseValues = ["false", "nein", "falsch", "0"];

    /// <summary>
    /// Prüft, ob <paramref name="value"/> ein gültiger Wert für den
    /// angegebenen Datentyp ist. Leere Werte gelten als gültig –
    /// Pflichtprüfungen übernimmt der Validator.
    /// </summary>
    /// <param name="value">Zu prüfender Wert.</param>
    /// <param name="dataType">Erwarteter Datentyp.</param>
    /// <returns><c>true</c>, wenn der Wert dem Datentyp entspricht.</returns>
    public static bool IsValid(string? value, ParameterDataType dataType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();

        return dataType switch
        {
            ParameterDataType.Text => true,
            ParameterDataType.Integer => long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            ParameterDataType.Decimal => decimal.TryParse(trimmed, NumberStyles.Number, German, out _)
                                      || decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            ParameterDataType.Boolean => TrueValues.Contains(trimmed, StringComparer.OrdinalIgnoreCase)
                                      || FalseValues.Contains(trimmed, StringComparer.OrdinalIgnoreCase),
            ParameterDataType.Date => DateOnly.TryParse(trimmed, German, out _)
                                   || DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, out _),
            ParameterDataType.DateTime => System.DateTime.TryParse(trimmed, German, DateTimeStyles.None, out _)
                                       || System.DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            ParameterDataType.Json => IsValidJson(trimmed),
            _ => true
        };
    }

    /// <summary>
    /// Prüft, ob die Zeichenkette wohlgeformtes JSON enthält.
    /// </summary>
    private static bool IsValidJson(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
