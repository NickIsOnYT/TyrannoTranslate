using System.Globalization;
using System.Windows.Data;

namespace TyrannoTranslate.Converters;

public sealed class LineDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index)
            return (index + 1).ToString(culture);
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
