using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EfiGuardUI.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLowerInvariant();
        return status switch
        {
            "on" or "enabled" or "running" or "auto" => new SolidColorBrush(Color.FromRgb(48, 209, 88)),
            "off" or "disabled" => new SolidColorBrush(Color.FromRgb(255, 69, 58)),
            "warn" or "pending" => new SolidColorBrush(Color.FromRgb(255, 159, 10)),
            _ => new SolidColorBrush(Color.FromRgb(110, 110, 115))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToBgConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLowerInvariant();
        return status switch
        {
            "on" or "enabled" or "running" or "auto" => new SolidColorBrush(Color.FromArgb(30, 48, 209, 88)),
            "off" or "disabled" => new SolidColorBrush(Color.FromArgb(30, 255, 69, 58)),
            "warn" or "pending" => new SolidColorBrush(Color.FromArgb(30, 255, 159, 10)),
            _ => new SolidColorBrush(Color.FromArgb(30, 110, 110, 115))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isChinese = CultureInfo.CurrentUICulture.Name.StartsWith("zh");
        var status = value?.ToString()?.ToLowerInvariant();
        return status switch
        {
            "on" or "enabled" or "running" or "auto" => isChinese ? "启用" : "ON",
            "off" or "disabled" => isChinese ? "禁用" : "OFF",
            "warn" or "pending" => isChinese ? "警告" : "WARN",
            _ => isChinese ? "未知" : "UNKNOWN"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? "on" : "off";
        return "unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null ? "unknown" : value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
