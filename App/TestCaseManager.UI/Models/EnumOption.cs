using TestCaseManager.Domain.Enums;

namespace TestCaseManager.UI.Models;

/// <summary>
/// Auswahloption für ComboBoxen: ein Enum-Wert mit deutschem Anzeigenamen.
/// </summary>
/// <typeparam name="TValue">Der Werttyp der Option.</typeparam>
/// <param name="Value">Der eigentliche Wert (z. B. Enum oder Id).</param>
/// <param name="Label">Der deutsche Anzeigename.</param>
public record EnumOption<TValue>(TValue Value, string Label)
{
    /// <summary>Anzeige in ComboBoxen ohne DisplayMemberPath.</summary>
    public override string ToString() => Label;
}

/// <summary>
/// Stellt die Standard-Auswahllisten der Oberfläche bereit
/// (Status- und Datentyp-Optionen mit deutschen Bezeichnungen).
/// </summary>
public static class OptionLists
{
    /// <summary>Alle Testfall-Status als Auswahloptionen.</summary>
    public static IReadOnlyList<EnumOption<TestCaseStatus>> Statuses { get; } =
        Enum.GetValues<TestCaseStatus>()
            .Select(s => new EnumOption<TestCaseStatus>(s, s.ToDisplayName()))
            .ToList();

    /// <summary>Alle Parameter-Datentypen als Auswahloptionen.</summary>
    public static IReadOnlyList<EnumOption<ParameterDataType>> DataTypes { get; } =
        Enum.GetValues<ParameterDataType>()
            .Select(d => new EnumOption<ParameterDataType>(d, d.ToDisplayName()))
            .ToList();
}
