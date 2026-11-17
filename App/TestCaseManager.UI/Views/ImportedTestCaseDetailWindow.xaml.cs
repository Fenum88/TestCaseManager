using System.Windows;

namespace TestCaseManager.UI.Views;

/// <summary>
/// Modales Fenster mit allen Attributen eines Datei-Testfalls (nur lesend).
/// Die Daten werden bereits vor dem Öffnen geladen und über das ViewModel
/// im DataContext bereitgestellt.
/// </summary>
public partial class ImportedTestCaseDetailWindow : Window
{
    /// <summary>Initialisiert das Fenster.</summary>
    public ImportedTestCaseDetailWindow()
    {
        InitializeComponent();
    }
}
