using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using TestCaseManager.UI.ViewModels;

namespace TestCaseManager.UI.Views;

/// <summary>
/// Ansicht des Reiters „Berechnung". Sorgt zusätzlich dafür, dass das
/// Log-Panel automatisch zum neuesten Eintrag scrollt.
/// </summary>
public partial class CalculationView : UserControl
{
    private INotifyCollectionChanged? _observedLog;

    /// <summary>Initialisiert die Komponente und richtet das Auto-Scrollen ein.</summary>
    public CalculationView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => Detach();
    }

    /// <summary>Abonniert die Log-Sammlung des jeweils gesetzten ViewModels.</summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Detach();

        if (DataContext is CalculationViewModel viewModel)
        {
            _observedLog = viewModel.LogEntries;
            _observedLog.CollectionChanged += OnLogChanged;
        }
    }

    /// <summary>Scrollt bei neuen Einträgen ans Ende des Protokolls.</summary>
    private void OnLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            LogScroll.ScrollToEnd();
        }
    }

    /// <summary>Entfernt das Abonnement der zuvor beobachteten Sammlung.</summary>
    private void Detach()
    {
        if (_observedLog is not null)
        {
            _observedLog.CollectionChanged -= OnLogChanged;
            _observedLog = null;
        }
    }
}
