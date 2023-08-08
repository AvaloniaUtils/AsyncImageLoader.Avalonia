using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AsyncImageLoader.Avalonia.Demo.Converters;

public class GetClassNameConverter : IValueConverter {
    public static GetClassNameConverter Instance { get; } = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null) {
            return "null";
        }

        return value.GetType().Name;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}