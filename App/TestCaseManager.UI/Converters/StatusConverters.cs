using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.UI.Converters;

/// <summary>
/// Übersetzt einen <see cref="TestCaseStatus"/> in seinen deutschen
/// Anzeigenamen. Wird überall dort verwendet, wo Status als Text
/// erscheint (Grid, Badges, Dashboard) – Status wird nie nur über
/// Farbe kommuniziert.
/// </summary>
public class StatusToTextConverter : IValueConverter
{
    /// <summary>Liefert den deutschen Anzeigenamen eines Status (aus der Domäne).</summary>
    public static string GetText(TestCaseStatus status) => status.ToDisplayName();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TestCaseStatus status ? GetText(status) : string.Empty;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Liefert die reservierte Statusfarbe eines <see cref="TestCaseStatus"/>
/// (Punkt-Markierung neben dem Textlabel bzw. Balkenfarbe im Dashboard).
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    private static readonly Dictionary<TestCaseStatus, Brush> Brushes = new()
    {
        [TestCaseStatus.Draft] = Freeze("#898781"),
        [TestCaseStatus.InReview] = Freeze("#FAB219"),
        [TestCaseStatus.Active] = Freeze("#0CA30C"),
        [TestCaseStatus.Inactive] = Freeze("#EC835A"),
        [TestCaseStatus.Deprecated] = Freeze("#D03B3B")
    };

    /// <summary>Liefert die Statusfarbe als eingefrorenen Brush.</summary>
    public static Brush GetBrush(TestCaseStatus status)
        => Brushes.TryGetValue(status, out var brush) ? brush : Freeze("#898781");

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TestCaseStatus status ? GetBrush(status) : GetBrush(TestCaseStatus.Draft);

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    /// <summary>Erzeugt einen eingefrorenen (thread-sicheren) Brush aus einem Hexwert.</summary>
    private static SolidColorBrush Freeze(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
