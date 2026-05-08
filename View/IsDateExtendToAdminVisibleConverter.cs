using System.Globalization;

namespace TaskManagement.View
{
    public class IsDateExtendToAdminVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? taskDueDate = null;

            // Extract the date
            if (value is DateTime date)
            {
                taskDueDate = date;
            }
            else if (value is string dateStr && DateTime.TryParse(dateStr, out DateTime parsedDate))
            {
                taskDueDate = parsedDate;
            }

            // Return true (Visible) if the date is in the past
            if (taskDueDate.HasValue)
            {
                return taskDueDate.Value.Date < DateTime.Today;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
