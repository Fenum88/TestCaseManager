using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// ViewModel der Detailansicht eines Datei-Testfalls. Zeigt alle in der
/// Datenbank gespeicherten Attribute (Entity → Zeile → Spalte → Wert)
/// gruppiert nach Herkunft an. Die Ansicht ist ausschließlich lesend:
/// Datei-Testfälle werden über „Import / Export" gepflegt.
/// </summary>
public partial class ImportedTestCaseDetailViewModel : ObservableObject
{
    private readonly ImportedTestCaseDetail _detail;

    /// <summary>
    /// Gruppierte und filterbare Sicht auf die Attribute. Die Gruppierung
    /// nach <see cref="DataAttributeItem.GroupHeader"/> bildet je Entity
    /// einen eigenen Block (Abschnitt · Datei · Entity).
    /// </summary>
    public ICollectionView Attributes { get; }

    /// <summary>Fenstertitel mit der Testfallnummer.</summary>
    public string WindowTitle => $"Testfall {_detail.Number} – Attribute";

    /// <summary>Herkunfts- und Änderungsangaben für die Kopfzeile.</summary>
    public string AuditInfo =>
        $"Datei-Testfall · eingelesen von {_detail.ImportedBy} am "
        + $"{_detail.ImportedAt.ToLocalTime():dd.MM.yyyy HH:mm}";

    /// <summary>Anzahl der Entities (Tabellen) über alle Dateien.</summary>
    public int EntityCount => _detail.Attributes
        .Select(a => a.GroupHeader).Distinct().Count();

    /// <summary>Anzahl der Zeilen über alle Entities.</summary>
    public int RowCount => _detail.Attributes
        .Select(a => (a.GroupHeader, a.RowIndex)).Distinct().Count();

    /// <summary>Anzahl der Attribute (Zellen) insgesamt.</summary>
    public int AttributeCount => _detail.Attributes.Count;

    /// <summary>Freitextfilter über Entity, Spalte und Wert.</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>Anzahl der aktuell angezeigten Attribute (Statuszeile).</summary>
    [ObservableProperty]
    private int _visibleCount;

    /// <summary>Erzeugt das ViewModel aus den geladenen Detaildaten.</summary>
    /// <param name="detail">Vollständige Detaildaten des Datei-Testfalls.</param>
    public ImportedTestCaseDetailViewModel(ImportedTestCaseDetail detail)
    {
        _detail = detail;

        Attributes = CollectionViewSource.GetDefaultView(detail.Attributes);
        Attributes.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(DataAttributeItem.GroupHeader)));
        Attributes.Filter = MatchesSearch;

        VisibleCount = detail.Attributes.Count;
    }

    /// <summary>Setzt den Freitextfilter zurück.</summary>
    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    /// <summary>Wendet den Filter bei jeder Eingabe erneut an.</summary>
    partial void OnSearchTextChanged(string value)
    {
        Attributes.Refresh();
        VisibleCount = Attributes.Cast<DataAttributeItem>().Count();
    }

    /// <summary>
    /// Filterprädikat: leerer Suchtext zeigt alles, sonst wird in Entity-Name,
    /// Spaltenname und Wert ohne Beachtung der Groß-/Kleinschreibung gesucht.
    /// </summary>
    /// <param name="item">Das zu prüfende Attribut.</param>
    private bool MatchesSearch(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        if (item is not DataAttributeItem attribute)
        {
            return false;
        }

        var search = SearchText.Trim();
        return attribute.EntityName.Contains(search, StringComparison.OrdinalIgnoreCase)
               || attribute.ColumnName.Contains(search, StringComparison.OrdinalIgnoreCase)
               || (attribute.Value?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
