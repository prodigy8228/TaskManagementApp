using System.Globalization;

namespace TaskManagement.View
{
    public class IsNotEmptyToAdminVisibleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = Entry Text
            // values[1] = Global Role
            string text = values?.ElementAtOrDefault(0)?.ToString();


            bool hasText = !string.IsNullOrWhiteSpace(text);
            bool isAdmin = values?.ElementAtOrDefault(1) is bool b && b;

            return hasText && isAdmin;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
