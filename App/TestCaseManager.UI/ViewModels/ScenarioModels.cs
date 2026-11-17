using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// Auswahleintrag für den Szenario-Filter im Berechnungsreiter. Ein Eintrag mit
/// <see cref="Scenario"/> = <c>null</c> steht für „Alle Testfälle".
/// </summary>
public sealed class ScenarioChoice
{
    /// <summary>Anzeigetext im Auswahlmenü.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Das zugehörige Szenario oder <c>null</c> für „Alle Testfälle".</summary>
    public TestScenario? Scenario { get; init; }

    /// <summary>Ob dieser Eintrag „Alle Testfälle" bedeutet.</summary>
    public bool IsAll => Scenario is null;
}

/// <summary>
/// Auswahleintrag für das Abgleichprofil im Berechnungsreiter. Ein Eintrag mit
/// <see cref="Profile"/> = <c>null</c> steht für „Ohne Profil" – dann zählt
/// beim Vergleich allein die Position der Zeilen.
/// </summary>
public sealed class CompareProfileChoice
{
    /// <summary>Anzeigetext im Auswahlmenü.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Das zugehörige Profil oder <c>null</c> für „Ohne Profil".</summary>
    public CompareProfile? Profile { get; init; }

    /// <summary>Die Schlüsselregeln des Profils (leer ohne Profil).</summary>
    public IReadOnlyList<CompareKeyRule> KeyRules => Profile?.KeyRules ?? [];
}

/// <summary>Ein auswählbarer Testfall (Mitglied) eines Szenarios.</summary>
public partial class TestCaseMemberPick : ObservableObject
{
    /// <summary>Testfallnummer (z. B. "0003").</summary>
    public string Number { get; }

    /// <summary>Ob der Testfall zum Szenario gehört.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Erzeugt ein Mitglied mit Nummer und Vorauswahl.</summary>
    /// <param name="number">Testfallnummer.</param>
    /// <param name="isSelected">Vorauswahl.</param>
    public TestCaseMemberPick(string number, bool isSelected)
    {
        Number = number;
        _isSelected = isSelected;
    }
}

/// <summary>
/// Editierbares Modell eines Testszenarios für den Testszenarien-Reiter.
/// Führt neben Name und Beschreibung die Auswahl der enthaltenen Testfälle.
/// </summary>
public partial class ScenarioEditModel : ObservableObject
{
    /// <summary>Name des Szenarios.</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Beschreibung des Szenarios.</summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>Alle verfügbaren Testfälle mit ihrer Zugehörigkeit.</summary>
    public ObservableCollection<TestCaseMemberPick> Members { get; } = [];

    /// <summary>Anzahl der aktuell ausgewählten Testfälle.</summary>
    public int SelectedCount => Members.Count(m => m.IsSelected);

    /// <summary>
    /// Erzeugt ein Editiermodell aus einem gespeicherten Szenario und den aktuell
    /// verfügbaren Testfallnummern. Testfälle aus dem Szenario, die es nicht mehr
    /// gibt, werden dennoch angeboten (damit die Zuordnung nicht verloren geht).
    /// </summary>
    /// <param name="scenario">Gespeichertes Szenario (oder <c>null</c> für ein neues).</param>
    /// <param name="availableNumbers">Aktuell im Input vorhandene Testfallnummern.</param>
    public ScenarioEditModel(TestScenario? scenario, IEnumerable<string> availableNumbers)
    {
        _name = scenario?.Name ?? string.Empty;
        _description = scenario?.Description ?? string.Empty;

        var selected = new HashSet<string>(
            scenario?.TestCaseNumbers ?? [], StringComparer.OrdinalIgnoreCase);

        // Vereinigung aus verfügbaren und bereits zugeordneten Nummern.
        var numbers = availableNumbers
            .Concat(selected)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

        foreach (var number in numbers)
        {
            var member = new TestCaseMemberPick(number, selected.Contains(number));
            member.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TestCaseMemberPick.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedCount));
                }
            };
            Members.Add(member);
        }
    }

    /// <summary>Überführt das Editiermodell in ein speicherbares Szenario.</summary>
    public TestScenario ToTestScenario() => new()
    {
        Name = Name.Trim(),
        Description = Description.Trim(),
        TestCaseNumbers = Members.Where(m => m.IsSelected).Select(m => m.Number).ToList()
    };
}
