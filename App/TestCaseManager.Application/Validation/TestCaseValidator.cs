using TestCaseManager.Application.Common.Validation;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Validation;

/// <summary>
/// Zentrale Validierung für Testfälle: Pflichtfelder, Maximallängen,
/// Datentyp-Prüfung der Parameterwerte und Eindeutigkeit der
/// Parameternamen. Die Eindeutigkeit des Testfallnamens gegen die
/// Datenbank prüft die Serviceschicht, da hierfür ein Repository nötig ist.
/// </summary>
public class TestCaseValidator
{
    /// <summary>Maximale Länge des Testfallnamens.</summary>
    public const int MaxNameLength = 200;

    /// <summary>Maximale Länge der Beschreibung.</summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>Maximale Länge der Versionsangabe.</summary>
    public const int MaxVersionLength = 50;

    /// <summary>Maximale Länge von Parameter- und Ergebnisnamen.</summary>
    public const int MaxItemNameLength = 100;

    /// <summary>
    /// Validiert den übergebenen Testfall vollständig.
    /// </summary>
    /// <param name="testCase">Der zu prüfende Testfall.</param>
    /// <returns>Liste aller Fehler; leer, wenn der Testfall gültig ist.</returns>
    public IReadOnlyList<ValidationError> Validate(TestCase testCase)
    {
        ArgumentNullException.ThrowIfNull(testCase);

        var errors = new List<ValidationError>();

        ValidateMasterData(testCase, errors);
        ValidateParameters(testCase, errors);
        ValidateExpectedResults(testCase, errors);

        return errors;
    }

    /// <summary>Prüft die Stammdaten (Name, Version, Beschreibung).</summary>
    private static void ValidateMasterData(TestCase testCase, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(testCase.Name))
        {
            errors.Add(new ValidationError("Name", "Der Name ist ein Pflichtfeld."));
        }
        else if (testCase.Name.Length > MaxNameLength)
        {
            errors.Add(new ValidationError("Name", $"Der Name darf höchstens {MaxNameLength} Zeichen lang sein."));
        }

        if (string.IsNullOrWhiteSpace(testCase.Version))
        {
            errors.Add(new ValidationError("Version", "Die Version ist ein Pflichtfeld."));
        }
        else if (testCase.Version.Length > MaxVersionLength)
        {
            errors.Add(new ValidationError("Version", $"Die Version darf höchstens {MaxVersionLength} Zeichen lang sein."));
        }

        if (testCase.Description is { Length: > MaxDescriptionLength })
        {
            errors.Add(new ValidationError("Beschreibung",
                $"Die Beschreibung darf höchstens {MaxDescriptionLength} Zeichen lang sein."));
        }
    }

    /// <summary>Prüft alle Eingabeparameter.</summary>
    private static void ValidateParameters(TestCase testCase, List<ValidationError> errors)
    {
        var duplicateNames = testCase.Parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var name in duplicateNames)
        {
            errors.Add(new ValidationError($"Parameter '{name}'",
                "Der Parametername ist innerhalb des Testfalls mehrfach vergeben."));
        }

        foreach (var parameter in testCase.Parameters)
        {
            var display = string.IsNullOrWhiteSpace(parameter.Name)
                ? "Parameter"
                : $"Parameter '{parameter.Name}'";

            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                errors.Add(new ValidationError(display, "Der Parametername ist ein Pflichtfeld."));
            }
            else if (parameter.Name.Length > MaxItemNameLength)
            {
                errors.Add(new ValidationError(display,
                    $"Der Parametername darf höchstens {MaxItemNameLength} Zeichen lang sein."));
            }

            if (!ParameterValueParser.IsValid(parameter.Value, parameter.DataType))
            {
                errors.Add(new ValidationError(display,
                    $"Der Wert \"{parameter.Value}\" entspricht nicht dem Datentyp {parameter.DataType}."));
            }

            if (!ParameterValueParser.IsValid(parameter.DefaultValue, parameter.DataType))
            {
                errors.Add(new ValidationError(display,
                    $"Der Standardwert \"{parameter.DefaultValue}\" entspricht nicht dem Datentyp {parameter.DataType}."));
            }

            if (parameter.IsRequired
                && string.IsNullOrWhiteSpace(parameter.Value)
                && string.IsNullOrWhiteSpace(parameter.DefaultValue))
            {
                errors.Add(new ValidationError(display,
                    "Der Parameter ist als Pflichtfeld markiert, besitzt aber weder Wert noch Standardwert."));
            }
        }
    }

    /// <summary>Prüft alle erwarteten Ergebnisse.</summary>
    private static void ValidateExpectedResults(TestCase testCase, List<ValidationError> errors)
    {
        foreach (var result in testCase.ExpectedResults)
        {
            var display = string.IsNullOrWhiteSpace(result.Name)
                ? "Erwartetes Ergebnis"
                : $"Erwartetes Ergebnis '{result.Name}'";

            if (string.IsNullOrWhiteSpace(result.Name))
            {
                errors.Add(new ValidationError(display, "Der Name des Ergebnisses ist ein Pflichtfeld."));
            }
            else if (result.Name.Length > MaxItemNameLength)
            {
                errors.Add(new ValidationError(display,
                    $"Der Name darf höchstens {MaxItemNameLength} Zeichen lang sein."));
            }

            if (string.IsNullOrWhiteSpace(result.ExpectedValue))
            {
                errors.Add(new ValidationError(display, "Der erwartete Wert ist ein Pflichtfeld."));
            }
            else if (!ParameterValueParser.IsValid(result.ExpectedValue, result.DataType))
            {
                errors.Add(new ValidationError(display,
                    $"Der erwartete Wert \"{result.ExpectedValue}\" entspricht nicht dem Datentyp {result.DataType}."));
            }

            if (!string.IsNullOrWhiteSpace(result.Tolerance)
                && !ParameterValueParser.IsValid(result.Tolerance, Domain.Enums.ParameterDataType.Decimal))
            {
                errors.Add(new ValidationError(display,
                    $"Die Toleranz \"{result.Tolerance}\" muss eine Zahl sein."));
            }
        }
    }
}
