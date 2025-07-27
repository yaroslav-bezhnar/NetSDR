using System.Globalization;
using System.Windows.Data;
using NetSDR.Wpf.Infrastructure;

namespace NetSDR.Wpf.Converters;

public class ThemeToIconConverter : IValueConverter
{
    #region methods

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            ThemeType.Light => "☀",
            ThemeType.Dark => "🌙",
            _ => "🌓"
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    #endregion
}