using System.Globalization;

namespace TaskManagement.View
{
    public class NullableDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
                return $"Due: {date:dd/MM/yyyy}";
            return string.Empty; // blank if null
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DateTime.TryParse(value?.ToString(), out var date))
                return date;
            return null;
        }
    }
}
