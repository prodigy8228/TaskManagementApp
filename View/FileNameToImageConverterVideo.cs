using System.Globalization;

namespace TaskManagement.View;

public class FileNameToImageConverterVideo : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string fileName && !string.IsNullOrEmpty(fileName))
        {
            // Return the "Correct" image if the filename is not empty
            return "Yes"; // Ensure the image file is in your Resources folder
        }
        return "No"; // No image if the filename is empty
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException(); // Not needed for this case
    }
}
