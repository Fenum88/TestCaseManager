using System.Windows.Controls;

namespace TestCaseManager.UI.Views;

/// <summary>
/// Ansicht des Dashboards. Das ViewModel wird über den DataContext
/// (DataTemplate im Hauptfenster) zugewiesen.
/// </summary>
public partial class DashboardView : UserControl
{
    /// <summary>Initialisiert die Komponente.</summary>
    public DashboardView()
    {
        InitializeComponent();
    }
}
