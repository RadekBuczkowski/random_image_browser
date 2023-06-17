namespace Random_Image.Classes.Wpf;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

using Random_Image.Classes.Browser;

/// <summary>
/// This file provides conversion classes that are used exclusively from XAML.
/// All classes are very simple and they are in the same file for simplicity.
/// </summary>

/// <summary>
/// Changes the color from Black to White and vice versa.
/// </summary>
public class SwapColors : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (Color)value == Colors.White ? Colors.Black : Colors.White;

    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

/// <summary>
/// Multiplies a value by another value.
/// </summary>
public class Multiply : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Math.Ceiling((double)value * double.Parse(parameter.ToString(),
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo));

    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Math.Floor((double)value / double.Parse(parameter.ToString(),
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo));
    }
}

/// <summary>
/// Multiplies a value by four values specified in the parameter, and returns a value of type <see cref="Thickness"/>
/// that can be used in the Margin, Padding, and BorderThickness properties in XAML.
/// </summary>
public class ThicknessMultiply : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        static double Parse(string text) =>
            double.Parse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);

        char[] delimiters = new char[] { ';', ',' };
        string[] values = parameter.ToString().Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length != 4)
            throw new ArgumentException("Wrong number of parameters in ThicknessMultiply");
        double left = Math.Ceiling((double)value * Parse(values[0]));
        double top = Math.Ceiling((double)value * Parse(values[1]));
        double right = Math.Ceiling((double)value * Parse(values[2]));
        double bottom = Math.Ceiling((double)value * Parse(values[3]));
        return new Thickness(left, top, right, bottom);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

/// <summary>
/// Multiplies a value by four values specified in the parameter, and returns a value of type
/// <see cref="CornerRadius"/> that can be used in the CornerRadius property in XAML.
/// </summary>
public class CornerRadiusMultiply : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        static double Parse(string text) =>
            double.Parse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);

        char[] delimiters = new char[] { ';', ',' };
        string[] values = parameter.ToString().Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length != 4)
            throw new ArgumentException("Wrong number of parameters in CornerRadiusMultiply");
        double topLeft = Math.Ceiling((double)value * Parse(values[0]));
        double topRight = Math.Ceiling((double)value * Parse(values[1]));
        double bottomRight = Math.Ceiling((double)value * Parse(values[2]));
        double bottomLeft = Math.Ceiling((double)value * Parse(values[3]));
        return new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

/// <summary>
/// Converts the IsChecked property in a <see cref="CheckBox"/> to the specified parameter.
/// </summary>
public class RadioButtonCheckedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.Equals(parameter) == true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.Equals(true) == true ? parameter : Binding.DoNothing;
    }
}

/// <summary>
/// Converts an image browser layout number to a layout description text.
/// </summary>
public class LayoutNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int layout = ((int)parameter).LimitLayout();
        (int columns, int rows) = ImageBrowserConstants.LayoutArrangements[layout - 1];
        return $"{columns} × {rows}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

/// <summary>
/// Converts an integer to string and vice versa where 0 is converted to the parameter value (e.g. "Disabled").
/// </summary>
public class IntegerSettingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((value as int?) == 0) ? parameter : value?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value == parameter) ? 0 : int.TryParse(value?.ToString().Trim(), out int result) ? result : 0;
    }
}

/// <summary>
/// Allows specifying values in XAML as integers.
/// </summary>
public sealed class IntegerExtension : MarkupExtension
{
    public IntegerExtension(int value)
    {
        Value = value;
    }

    public int Value { get; set; }

    public override object ProvideValue(IServiceProvider sp) => Value;
}