using System.Globalization;

namespace TaskManagement.View;

public class NullableDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 1. Check for DateTimeOffset (Android Firestore type)
        if (value is DateTimeOffset dateTimeOffset)
            return $"Due: {dateTimeOffset.LocalDateTime:dd/MM/yyyy}";

        // 2. Check for standard DateTime (Windows/REST type)
        if (value is DateTime date)
            return $"Due: {date.Date:dd/MM/yyyy}";

        return string.Empty; // Blank if null
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (DateTime.TryParse(value?.ToString(), out var date))
            return date;

        return null;
    }
}
