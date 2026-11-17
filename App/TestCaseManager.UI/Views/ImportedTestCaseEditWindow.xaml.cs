using System.Windows;

namespace TestCaseManager.UI.Views;

/// <summary>
/// Modales Fenster zum Bearbeiten der Attribute eines Datei-Testfalls.
/// Die Werte werden über das ViewModel im DataContext geladen und beim
/// Speichern in die Datenbank zurückgeschrieben.
/// </summary>
public partial class ImportedTestCaseEditWindow : Window
{
    /// <summary>Initialisiert das Fenster.</summary>
    public ImportedTestCaseEditWindow()
    {
        InitializeComponent();
    }
}
