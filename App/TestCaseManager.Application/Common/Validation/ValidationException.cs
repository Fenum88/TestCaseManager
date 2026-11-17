namespace TestCaseManager.Application.Common.Validation;

/// <summary>
/// Wird von der Serviceschicht geworfen, wenn eine Entität die
/// Validierung nicht besteht. Enthält alle Einzelfehler, damit die
/// Oberfläche sie gesammelt anzeigen kann.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>Alle festgestellten Validierungsfehler.</summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Erzeugt die Ausnahme aus einer Liste von Validierungsfehlern.
    /// </summary>
    /// <param name="errors">Die festgestellten Fehler (mindestens einer).</param>
    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base("Die Eingaben sind ungültig:" + Environment.NewLine +
               string.Join(Environment.NewLine, errors.Select(e => "• " + e)))
    {
        Errors = errors;
    }
}
