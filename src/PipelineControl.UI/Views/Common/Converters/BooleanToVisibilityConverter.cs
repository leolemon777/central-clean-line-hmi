using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PipelineControl.UI.Views.Common.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isVisible = value is bool boolValue && boolValue;
        if (Invert)
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isVisible = value is Visibility visibility && visibility == Visibility.Visible;
        return Invert ? !isVisible : isVisible;
    }
}
