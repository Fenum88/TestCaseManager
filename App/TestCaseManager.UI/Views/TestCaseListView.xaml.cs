using System.Windows.Controls;

namespace TestCaseManager.UI.Views;

/// <summary>
/// Ansicht der Testfallverwaltung (Liste, Suche, Filter, Aktionen).
/// Das ViewModel wird über den DataContext zugewiesen.
/// </summary>
public partial class TestCaseListView : UserControl
{
    /// <summary>Initialisiert die Komponente.</summary>
    public TestCaseListView()
    {
        InitializeComponent();
    }
}
