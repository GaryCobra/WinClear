using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WinClear.Models;

namespace WinClear.Converters;

[ValueConversion(typeof(SafetyTag), typeof(Brush))]
public class SafetyTagColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SafetyTag tag)
        {
            return tag switch
            {
                SafetyTag.Safe => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                SafetyTag.Warning => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                SafetyTag.Danger => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
