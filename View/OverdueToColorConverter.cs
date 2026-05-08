using System.Globalization;

namespace TaskManagement.View;

public class OverdueToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 1. Check if the value is a standard DateTime
        if (value is DateTime dueDate)
        {
            return dueDate.Date < DateTime.Today;
        }

        // 2. 🔥 FIX: Safely check if the value is a DateTimeOffset
        if (value is DateTimeOffset dueDateTimeOffset)
        {
            return dueDateTimeOffset.Date < DateTime.Today;
        }

        // 3. Fallback: If it's loaded as a string (rare but happens with some DB parsers)
        if (value != null && DateTime.TryParse(value.ToString(), out DateTime parsedDate))
        {
            return parsedDate.Date < DateTime.Today;
        }

        return false; // Return false if data is missing or null
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
