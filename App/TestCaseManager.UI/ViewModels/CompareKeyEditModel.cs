using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// Editierbares Modell eines Abgleichprofils: Name, Beschreibung und die
/// zugehörigen Schlüsselregeln.
/// </summary>
public partial class CompareProfileEditModel : ObservableObject
{
    /// <summary>Anzeigename des Profils.</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Beschreibung des Profils.</summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>Die Schlüsselregeln dieses Profils.</summary>
    public ObservableCollection<CompareKeyEditModel> Rules { get; } = [];

    /// <summary>Anzahl der Regeln (für die Übersichtsspalte).</summary>
    public int RuleCount => Rules.Count;

    /// <summary>Erzeugt ein leeres Profil (für „Hinzufügen").</summary>
    public CompareProfileEditModel()
    {
        Rules.CollectionChanged += (_, _) => OnPropertyChanged(nameof(RuleCount));
    }

    /// <summary>Erzeugt ein Editiermodell aus einem gespeicherten Profil.</summary>
    /// <param name="profile">Die Vorlage.</param>
    public CompareProfileEditModel(CompareProfile profile) : this()
    {
        _name = profile.Name;
        _description = profile.Description;
        foreach (var rule in profile.KeyRules)
        {
            Rules.Add(new CompareKeyEditModel(rule));
        }
    }

    /// <summary>Überführt das Editiermodell in ein speicherbares Profil.</summary>
    public CompareProfile ToProfile() => new()
    {
        Name = Name.Trim(),
        Description = Description.Trim(),
        KeyRules = Rules.Select(r => r.ToRule()).ToList()
    };
}

/// <summary>
/// Editierbares Zeilenmodell einer Abgleich-Schlüsselregel: legt fest, über
/// welche Attribute die Zeilen einer Entität beim Vergleich zugeordnet werden.
/// Mehrere Schlüsselattribute werden als kommagetrennte Liste erfasst.
/// </summary>
public partial class CompareKeyEditModel : ObservableObject
{
    /// <summary>Name der Entität (z. B. "Provision").</summary>
    [ObservableProperty]
    private string _entity = string.Empty;

    /// <summary>Schlüsselattribute, kommagetrennt (z. B. "datum" oder "datum, art").</summary>
    [ObservableProperty]
    private string _keyColumns = string.Empty;

    /// <summary>Erzeugt ein leeres Editiermodell (für „Hinzufügen").</summary>
    public CompareKeyEditModel()
    {
    }

    /// <summary>Erzeugt ein Editiermodell aus einer gespeicherten Regel.</summary>
    /// <param name="rule">Die Vorlage.</param>
    public CompareKeyEditModel(CompareKeyRule rule)
    {
        _entity = rule.Entity;
        _keyColumns = string.Join(", ", rule.KeyColumns);
    }

    /// <summary>Überführt das Editiermodell in eine speicherbare Regel.</summary>
    public CompareKeyRule ToRule() => new()
    {
        Entity = Entity.Trim(),
        KeyColumns = SplitColumns()
    };

    /// <summary>Zerlegt die kommagetrennte Eingabe in einzelne Attributnamen.</summary>
    public IReadOnlyList<string> SplitColumns()
        => KeyColumns
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
