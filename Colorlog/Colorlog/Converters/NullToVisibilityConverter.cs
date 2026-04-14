using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Colorlog.Converter;

/// <summary>값이 null이 아니면 Visible, null이면 Collapsed. ConverterParameter=Inverse면 반대.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasValue = value is not null;
        if (parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
        {
            hasValue = !hasValue;
        }

        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
