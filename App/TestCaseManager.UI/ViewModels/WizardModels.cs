using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TestCaseManager.UI.ViewModels;

/// <summary>Auswählbares Attribut (Spalte) einer Entity im Assistenten.</summary>
public partial class AttributePick : ObservableObject
{
    /// <summary>Spaltenname (z. B. "tuer").</summary>
    public string Name { get; }

    /// <summary>Ob das Attribut für den neuen Testfall verwendet wird.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Erzeugt ein Attribut mit Namen und Vorauswahl.</summary>
    /// <param name="name">Spaltenname.</param>
    /// <param name="isSelected">Vorauswahl.</param>
    public AttributePick(string name, bool isSelected = false)
    {
        Name = name;
        _isSelected = isSelected;
    }
}

/// <summary>
/// Auswählbare Entity im Assistenten samt ihrer bekannten Attribute. Erlaubt
/// zusätzlich das Ergänzen eigener, in der Schablone noch nicht vorhandener
/// Attribute.
/// </summary>
public partial class EntityPick : ObservableObject
{
    /// <summary>Name der Entity (z. B. "Gebaeude").</summary>
    public string Name { get; }

    /// <summary>Ob die Entity für den neuen Testfall verwendet wird.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Name eines neu hinzuzufügenden Attributs (Eingabefeld).</summary>
    [ObservableProperty]
    private string _newAttributeName = string.Empty;

    /// <summary>Alle bekannten und ergänzten Attribute der Entity.</summary>
    public ObservableCollection<AttributePick> Attributes { get; } = [];

    /// <summary>Erzeugt eine Entity aus Namen und bekannten Attributen.</summary>
    /// <param name="name">Entity-Name.</param>
    /// <param name="attributes">Bekannte Attribute (aus der Schablone).</param>
    public EntityPick(string name, IEnumerable<string> attributes)
    {
        Name = name;
        foreach (var attribute in attributes)
        {
            Attributes.Add(new AttributePick(attribute));
        }
    }

    /// <summary>Die aktuell ausgewählten Attribute in Anzeigereihenfolge.</summary>
    public IReadOnlyList<string> SelectedAttributes =>
        Attributes.Where(a => a.IsSelected).Select(a => a.Name).ToList();

    /// <summary>Fügt das im Eingabefeld genannte Attribut hinzu (falls neu).</summary>
    [RelayCommand]
    private void AddAttribute()
    {
        var name = NewAttributeName.Trim();
        if (name.Length == 0)
        {
            return;
        }

        if (!Attributes.Any(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            Attributes.Add(new AttributePick(name, isSelected: true));
        }

        NewAttributeName = string.Empty;
    }
}

/// <summary>Eine einzelne Zelle im Werteschritt (Spalte + eingegebener Wert).</summary>
public partial class ValueCell : ObservableObject
{
    /// <summary>Spaltenname dieser Zelle.</summary>
    public string Column { get; }

    /// <summary>Der eingegebene Wert.</summary>
    [ObservableProperty]
    private string _value = string.Empty;

    /// <summary>Erzeugt eine Zelle für die angegebene Spalte.</summary>
    /// <param name="column">Spaltenname.</param>
    public ValueCell(string column)
    {
        Column = column;
    }
}

/// <summary>Eine Datenzeile im Werteschritt (eine Zelle je Spalte).</summary>
public partial class ValueRow : ObservableObject
{
    /// <summary>Die Zellen der Zeile.</summary>
    public ObservableCollection<ValueCell> Cells { get; } = [];

    /// <summary>Erzeugt eine Zeile mit je einer leeren Zelle pro Spalte.</summary>
    /// <param name="columns">Die Spaltennamen.</param>
    public ValueRow(IEnumerable<string> columns)
    {
        foreach (var column in columns)
        {
            Cells.Add(new ValueCell(column));
        }
    }
}

/// <summary>
/// Ein Entity-Block im Werteschritt: die gewählten Spalten und beliebig viele
/// Datenzeilen, die der Anwender ausfüllt.
/// </summary>
public partial class ValueEntityBlock : ObservableObject
{
    /// <summary>Name der Entity.</summary>
    public string EntityName { get; }

    /// <summary>Die gewählten Spalten dieser Entity.</summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>Die Datenzeilen.</summary>
    public ObservableCollection<ValueRow> Rows { get; } = [];

    /// <summary>Erzeugt einen Block mit einer ersten, leeren Zeile.</summary>
    /// <param name="entityName">Entity-Name.</param>
    /// <param name="columns">Gewählte Spalten.</param>
    public ValueEntityBlock(string entityName, IReadOnlyList<string> columns)
    {
        EntityName = entityName;
        Columns = columns;
        AddRow();
    }

    /// <summary>Fügt eine weitere Datenzeile hinzu.</summary>
    [RelayCommand]
    private void AddRow() => Rows.Add(new ValueRow(Columns));

    /// <summary>Entfernt die angegebene Zeile (mindestens eine bleibt erhalten).</summary>
    /// <param name="row">Die zu entfernende Zeile.</param>
    [RelayCommand]
    private void RemoveRow(ValueRow row)
    {
        if (Rows.Count > 1)
        {
            Rows.Remove(row);
        }
    }
}
