using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TestCaseManager.UI.ViewModels;

namespace TestCaseManager.UI.Views;

/// <summary>
/// Ansicht des Datenbank-Reiters. Baut die Spalten des DataGrids dynamisch aus
/// den Attributen auf (eine Spalte je Entität · Attribut, eine Zeile je Testfall).
/// </summary>
public partial class DatabaseView : UserControl
{
    private DatabaseViewModel? _observed;

    /// <summary>Initialisiert die Komponente und richtet die Spaltenerzeugung ein.</summary>
    public DatabaseView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => Detach();
    }

    /// <summary>Abonniert das Neuladen des jeweils gesetzten ViewModels.</summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Detach();

        if (DataContext is DatabaseViewModel viewModel)
        {
            _observed = viewModel;
            viewModel.DataReloaded += OnDataReloaded;
            // Falls die Daten bereits geladen wurden, sofort Spalten aufbauen.
            BuildColumns(viewModel);
        }
    }

    /// <summary>Baut die Spalten nach dem Laden neu auf.</summary>
    private void OnDataReloaded(object? sender, EventArgs e)
    {
        if (DataContext is DatabaseViewModel viewModel)
        {
            BuildColumns(viewModel);
        }
    }

    /// <summary>
    /// Erzeugt die Spalten: erste Spalte „Testfall", danach je Attribut eine
    /// Spalte mit der Überschrift „Entität · Attribut", gebunden an den Wert.
    /// </summary>
    /// <param name="viewModel">Das ViewModel mit den Spalten-Überschriften.</param>
    private void BuildColumns(DatabaseViewModel viewModel)
    {
        AttributeGrid.Columns.Clear();

        AttributeGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Testfall",
            Binding = new Binding(nameof(PivotRow.Number)),
            MinWidth = 90
        });

        for (var i = 0; i < viewModel.ColumnHeaders.Count; i++)
        {
            AttributeGrid.Columns.Add(new DataGridTextColumn
            {
                Header = viewModel.ColumnHeaders[i],
                Binding = new Binding($"{nameof(PivotRow.Values)}[{i}]"),
                MinWidth = 90
            });
        }
    }

    /// <summary>Entfernt das Abonnement des zuvor beobachteten ViewModels.</summary>
    private void Detach()
    {
        if (_observed is not null)
        {
            _observed.DataReloaded -= OnDataReloaded;
            _observed = null;
        }
    }
}
