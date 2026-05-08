using System.Globalization;

namespace TaskManagement.View
{
    public class DateTimeOffsetToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the database value is a valid DateTimeOffset, extract just the DateTime part for the DatePicker
            if (value is DateTimeOffset dto)
            {
                return dto.DateTime;
            }

            // Fallback placeholder if the database value is null
            return DateTime.Today;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // When the user picks a date in the UI, box it back up into a DateTimeOffset for Firestore
            if (value is DateTime dt)
            {
                return new DateTimeOffset(dt);
            }

            return null;
        }
    }
}
