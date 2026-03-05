using System.Globalization;

namespace TaskManagement.View
{
    public class NullableDateToDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If value is DateTime?, return actual date; otherwise fallback to Today
            if (value is DateTime date)
                return date;

            return DateTime.Today; // fallback for null
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // DatePicker.Date is always DateTime, so cast safely
            if (value is DateTime date)
                return (DateTime?)date;

            return null;
        }
    }
}
