using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;
using TestCaseManager.UI.Converters;
using TestCaseManager.UI.Services;

namespace TestCaseManager.UI.ViewModels;

/// <summary>
/// Anzeigemodell einer Statuszeile im Dashboard (Label, Anzahl und
/// Balken; die Farbe verstärkt das immer vorhandene Textlabel).
/// </summary>
/// <param name="Label">Deutscher Statusname.</param>
/// <param name="Count">Anzahl Testfälle mit diesem Status.</param>
/// <param name="MaxCount">Bezugsgröße für die Balkenbreite.</param>
/// <param name="Brush">Reservierte Statusfarbe.</param>
public record StatusStatItem(string Label, int Count, int MaxCount, Brush Brush);

/// <summary>
/// Anzeigemodell einer Kategoriezeile im Dashboard.
/// </summary>
/// <param name="Name">Kategoriename.</param>
/// <param name="Count">Anzahl Testfälle in der Kategorie.</param>
/// <param name="MaxCount">Bezugsgröße für die Balkenbreite.</param>
public record CategoryStatItem(string Name, int Count, int MaxCount);

/// <summary>
/// ViewModel des Dashboards: lädt Kennzahlen (Anzahl Testfälle,
/// Statusverteilung, Kategorien, letzte Änderungen) über einen eigenen
/// DI-Scope und bereitet sie für die Anzeige auf.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ILogger<DashboardViewModel> _logger;

    /// <summary>Gesamtanzahl aller Testfälle.</summary>
    [ObservableProperty]
    private int _totalTestCases;

    /// <summary>Anzahl der Kategorien.</summary>
    [ObservableProperty]
    private int _totalCategories;

    /// <summary>Anzahl der Tags.</summary>
    [ObservableProperty]
    private int _totalTags;

    /// <summary>Anzahl der Dateianhänge.</summary>
    [ObservableProperty]
    private int _totalAttachments;

    /// <summary>Gibt an, ob gerade Daten geladen werden.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Statusverteilung für die Balkendarstellung.</summary>
    public ObservableCollection<StatusStatItem> StatusStats { get; } = [];

    /// <summary>Kategorieverteilung für die Balkendarstellung.</summary>
    public ObservableCollection<CategoryStatItem> CategoryStats { get; } = [];

    /// <summary>Die zuletzt geänderten Testfälle.</summary>
    public ObservableCollection<TestCaseListItem> RecentTestCases { get; } = [];

    /// <summary>Die jüngsten Audit-Log-Einträge.</summary>
    public ObservableCollection<AuditLogEntry> RecentChanges { get; } = [];

    /// <summary>Erzeugt das ViewModel (Dependency Injection).</summary>
    /// <param name="scopeFactory">Fabrik für DI-Dienstbereiche (Scope pro Ladevorgang).</param>
    /// <param name="dialogService">Dialogdienst für Fehlermeldungen.</param>
    /// <param name="logger">Logger für Fehlerprotokollierung.</param>
    public DashboardViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogService,
        ILogger<DashboardViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _logger = logger;
    }

    /// <summary>Lädt alle Kennzahlen neu.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
            var statistics = await dashboardService.GetStatisticsAsync();

            ApplyStatistics(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard-Kennzahlen konnten nicht geladen werden.");
            _dialogService.ShowError("Fehler", "Die Kennzahlen konnten nicht geladen werden:"
                + Environment.NewLine + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Überträgt die geladenen Kennzahlen in die Anzeigelisten.</summary>
    private void ApplyStatistics(DashboardStatistics statistics)
    {
        TotalTestCases = statistics.TotalTestCases;
        TotalCategories = statistics.TotalCategories;
        TotalTags = statistics.TotalTags;
        TotalAttachments = statistics.TotalAttachments;

        StatusStats.Clear();
        var statusMax = Math.Max(1, statistics.StatusCounts.Values.DefaultIfEmpty(0).Max());
        foreach (var status in Enum.GetValues<TestCaseStatus>())
        {
            var count = statistics.StatusCounts.GetValueOrDefault(status);
            StatusStats.Add(new StatusStatItem(
                StatusToTextConverter.GetText(status),
                count,
                statusMax,
                StatusToBrushConverter.GetBrush(status)));
        }

        CategoryStats.Clear();
        var categoryMax = Math.Max(1, statistics.CategoryCounts.Select(c => c.Count).DefaultIfEmpty(0).Max());
        foreach (var category in statistics.CategoryCounts)
        {
            CategoryStats.Add(new CategoryStatItem(category.Name, category.Count, categoryMax));
        }

        RecentTestCases.Clear();
        foreach (var testCase in statistics.RecentTestCases)
        {
            RecentTestCases.Add(testCase);
        }

        RecentChanges.Clear();
        foreach (var change in statistics.RecentChanges)
        {
            RecentChanges.Add(change);
        }
    }
}
