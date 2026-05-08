using System.Globalization;

namespace TaskManagement.View
{
    public class IsNotNullPendingDateToAdminVisibleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            bool hasPendingDate = false;

            // 🔥 Fix: Safely handle both regular DateTime and DateTimeOffset
            if (values[0] is DateTimeOffset dto)
            {
                hasPendingDate = true; // It's a valid non-null DateTimeOffset
            }
            else if (values[0] is DateTime dt)
            {
                hasPendingDate = dt != DateTime.MinValue;
            }
            else if (values[0] != null)
            {
                // Fallback for any other boxed non-null objects
                hasPendingDate = true;
            }

            bool isAdmin = values[1] is bool boolValue && boolValue;

            // Logic: Visible ONLY if user IS an Admin AND a Pending Date exists (not null)
            return isAdmin && hasPendingDate;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
