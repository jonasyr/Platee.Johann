namespace Platee.Johann.UI.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

/// <summary>
/// Negates a boolean. Used to disable a button while its row is busy without adding a
/// second inverted property to every row view model.
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;
}
