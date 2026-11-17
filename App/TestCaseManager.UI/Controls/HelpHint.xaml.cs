using System.Windows;
using System.Windows.Controls;

namespace TestCaseManager.UI.Controls;

/// <summary>
/// Kleines Fragezeichen, das den erklärenden Text erst beim Überfahren zeigt.
/// Ersetzt fest sichtbare Beschreibungstexte unter den Reiterüberschriften und
/// hält die Ansichten damit ruhiger.
///
/// Verwendung: <c>&lt;controls:HelpHint Text="Erklärung …" /&gt;</c>.
/// Mehrere Absätze lassen sich mit Zeilenumbrüchen im Text abbilden;
/// <see cref="DetailText"/> ergänzt optional einen Block in fester
/// Schrittweite (z. B. eine Operatorliste).
/// </summary>
public partial class HelpHint : UserControl
{
    /// <summary>Abhängigkeitseigenschaft für <see cref="Text"/>.</summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(HelpHint),
            new PropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>Abhängigkeitseigenschaft für <see cref="DetailText"/>.</summary>
    public static readonly DependencyProperty DetailTextProperty =
        DependencyProperty.Register(nameof(DetailText), typeof(string), typeof(HelpHint),
            new PropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>Der Hilfetext, der im Tooltip erscheint.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Optionaler Zusatztext in fester Schrittweite. Bleibt er leer, wird der
    /// Block ausgeblendet und erzeugt keinen Leerraum im Tooltip.
    /// </summary>
    public string DetailText
    {
        get => (string)GetValue(DetailTextProperty);
        set => SetValue(DetailTextProperty, value);
    }

    /// <summary>Initialisiert das Steuerelement.</summary>
    public HelpHint()
    {
        InitializeComponent();
        UpdateTexts();
    }

    /// <summary>Überträgt geänderte Texte in den Tooltip.</summary>
    private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        => ((HelpHint)sender).UpdateTexts();

    /// <summary>
    /// Schreibt die Texte in die Tooltip-Elemente und blendet den Detailblock
    /// aus, solange er keinen Inhalt hat.
    /// </summary>
    private void UpdateTexts()
    {
        TextBlockMain.Text = Text;
        TextBlockDetail.Text = DetailText;
        TextBlockDetail.Visibility = string.IsNullOrWhiteSpace(DetailText)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}
