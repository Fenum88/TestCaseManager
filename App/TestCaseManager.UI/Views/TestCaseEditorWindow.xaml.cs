using System.Windows;
using TestCaseManager.UI.ViewModels;

namespace TestCaseManager.UI.Views;

/// <summary>
/// Modales Editor-Fenster für einen Testfall (Detailansicht mit Tabs
/// für Stammdaten, Parameter, Ergebnisse, Anhänge und Historie).
/// Lädt seinen Inhalt nach dem Öffnen asynchron über das ViewModel.
/// </summary>
public partial class TestCaseEditorWindow : Window
{
    /// <summary>Initialisiert das Fenster und startet das Laden bei Anzeige.</summary>
    public TestCaseEditorWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            if (DataContext is TestCaseEditorViewModel viewModel)
            {
                await viewModel.LoadAsync();
            }
        };
    }
}
