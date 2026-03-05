using System.Globalization;

namespace TaskManagement.View
{
    public class TaskTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() != "All List" && value?.ToString() != "Default" && value?.ToString() != "Completed Tasks List"; // Hide icons if TaskType is "All Task"
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
