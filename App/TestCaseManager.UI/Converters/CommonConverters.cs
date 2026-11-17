using System.Globalization;
using System.Windows.Data;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.UI.Converters;

/// <summary>
/// Übersetzt einen <see cref="ChangeType"/> in seinen deutschen
/// Anzeigenamen (Historie und Audit-Log).
/// </summary>
public class ChangeTypeToTextConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ChangeType changeType ? changeType.ToDisplayName() : string.Empty;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Formatiert eine Dateigröße in Bytes als lesbare Angabe (KB/MB).
/// </summary>
public class FileSizeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long bytes)
        {
            return string.Empty;
        }

        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
            _ => $"{bytes / (1024.0 * 1024.0):0.##} MB"
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Kehrt einen Wahrheitswert um. Wird z. B. verwendet, um Schaltflächen
/// während eines laufenden Vorgangs (IsBusy) zu deaktivieren.
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

/// <summary>
/// Übersetzt einen UTC-Zeitstempel in die lokale Zeitzone und formatiert
/// ihn deutsch (Datum und Uhrzeit).
/// </summary>
public class UtcToLocalTextConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var utc = value switch
        {
            DateTime dt => dt,
            _ => (DateTime?)null
        };

        if (utc is null)
        {
            return string.Empty;
        }

        var local = DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToLocalTime();
        return local.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("de-DE"));
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
