using System.Windows;
using TestCaseManager.UI.ViewModels;

namespace TestCaseManager.UI.Views;

/// <summary>
/// Modaler Assistent zum Anlegen eines Datei-Testfalls in drei Schritten
/// (Entities → Attribute → Werte). Lädt die Schablone nach dem Öffnen.
/// </summary>
public partial class TestCaseWizardWindow : Window
{
    /// <summary>Initialisiert das Fenster und startet das Laden bei Anzeige.</summary>
    public TestCaseWizardWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            if (DataContext is TestCaseWizardViewModel viewModel)
            {
                await viewModel.LoadAsync();
            }
        };
    }
}
