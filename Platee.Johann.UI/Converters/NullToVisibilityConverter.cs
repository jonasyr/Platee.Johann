namespace Platee.Johann.UI.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// Returns Visible when value is non-null/non-empty, Collapsed otherwise.
/// Pass "Inverse" as ConverterParameter to flip the logic.
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasValue = value is string s ? !string.IsNullOrWhiteSpace(s) : value is not null;
        bool inverse = parameter is string p && p.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
        return (hasValue ^ inverse) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
