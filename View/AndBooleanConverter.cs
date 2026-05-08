using System.Globalization;

namespace TaskManagement.View;

public class AndBooleanConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 &&
            values[0] is bool first &&
            values[1] is bool second)
        {
            return first && second; // or use || for OR logic
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
