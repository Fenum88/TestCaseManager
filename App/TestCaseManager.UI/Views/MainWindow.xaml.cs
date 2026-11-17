using System.Windows;
using TestCaseManager.UI.ViewModels;

namespace TestCaseManager.UI.Views;

/// <summary>
/// Hauptfenster der Anwendung. Erhält sein ViewModel per Dependency
/// Injection und zeigt beim Start das Dashboard an.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Erzeugt das Fenster mit seinem ViewModel.</summary>
    /// <param name="viewModel">Das Haupt-ViewModel (Navigation).</param>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            DashboardNavButton.IsChecked = true;
            await viewModel.ShowDashboardCommand.ExecuteAsync(null);
        };
    }
}
