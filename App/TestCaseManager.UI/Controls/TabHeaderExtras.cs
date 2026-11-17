using System.Windows;
using System.Windows.Controls;

namespace TestCaseManager.UI.Controls;

/// <summary>
/// Zusatzangaben für die Reiterleiste des Stils <c>TitledTabControl</c>.
///
/// Damit lassen sich rechts neben den Reitern Bedienelemente unterbringen
/// (etwa „Aktualisieren"), ohne dafür eine eigene Kopfzeile aufzubauen –
/// die Reiter sähen sonst je Ansicht unterschiedlich aus.
/// </summary>
public static class TabHeaderExtras
{
    /// <summary>Angehängte Eigenschaft für <see cref="GetRightContent"/>.</summary>
    public static readonly DependencyProperty RightContentProperty =
        DependencyProperty.RegisterAttached("RightContent", typeof(object),
            typeof(TabHeaderExtras), new PropertyMetadata(null));

    /// <summary>Liefert den rechtsbündigen Inhalt der Reiterleiste.</summary>
    /// <param name="element">Das Reitersteuerelement.</param>
    public static object? GetRightContent(TabControl element)
        => element.GetValue(RightContentProperty);

    /// <summary>Setzt den rechtsbündigen Inhalt der Reiterleiste.</summary>
    /// <param name="element">Das Reitersteuerelement.</param>
    /// <param name="value">Der anzuzeigende Inhalt.</param>
    public static void SetRightContent(TabControl element, object? value)
        => element.SetValue(RightContentProperty, value);
}
