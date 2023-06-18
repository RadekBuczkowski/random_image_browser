namespace Random_Image_Tests.Classes.Wpf;

using System.Globalization;
using System.Windows;
using System.Windows.Media;

using Random_Image.Classes.Wpf;

public class Test_Converters
{
    [Theory]
    [InlineData("White", "Black")]
    [InlineData("Black", "White")]
    [InlineData("Gray", "White")]
    public void Test_SwapColors_Convert(string from_color, string to_color)
    {
        // arrange
        Color value = (Color)ColorConverter.ConvertFromString(from_color);
        Color expected_result = (Color)ColorConverter.ConvertFromString(to_color);

        // act
        SwapColors sut = new();
        Color actual_result = (Color)sut.Convert(value, typeof(Color), null, CultureInfo.InvariantCulture);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Theory]
    [InlineData(2, 1, 2)]
    [InlineData(3, 4, 12)]
    public void Test_Multiply_Convert(double value, double multiplier, double expected_result)
    {
        // act
        Multiply sut = new();
        double actual_result = (double)sut.Convert(value, typeof(double),
            multiplier.ToString(), CultureInfo.InvariantCulture);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Theory]
    [InlineData(2, 1, 2)]
    [InlineData(12, 4, 3)]
    public void Test_Multiply_ConvertBack(double value, double multiplier, double expected_result)
    {
        // act
        Multiply sut = new();
        double actual_result = (double)sut.ConvertBack(value, typeof(double),
            multiplier.ToString(), CultureInfo.InvariantCulture);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_ThicknessMultiply_Convert()
    {
        // arrange
        double value = 5;
        string multipliers = "0.5,1,1.5,0";
        Rect expected_result = new(3, 5, 8, 0);  // Note: Math.Ceiling() is applied.

        // act
        ThicknessMultiply sut = new();
        Thickness thickness = (Thickness)sut.Convert(value, typeof(double), multipliers, CultureInfo.InvariantCulture);
        Rect actual_result = new(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_CornerRadiusMultiply_Convert()
    {
        // arrange
        double value = 5;
        string multipliers = "0.5,1,1.5,0";
        Rect expected_result = new(3, 5, 8, 0);  // Note: Math.Ceiling() is applied.

        // act
        CornerRadiusMultiply sut = new();
        CornerRadius radius = (CornerRadius)sut.Convert(value, typeof(double), multipliers, CultureInfo.InvariantCulture);
        Rect actual_result = new(radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_RadioButtonCheckedConverter_Convert()
    {
        // arrange
        double value = 37;

        // act
        RadioButtonCheckedConverter sut = new();
        bool actual_result = (bool)sut.Convert(value, typeof(int), value, CultureInfo.InvariantCulture);

        // assert
        Assert.True(actual_result);
    }

    [Fact]
    public void Test_RadioButtonCheckedConverter_ConvertBack()
    {
        // arrange
        int expected_result = 37;

        // act
        RadioButtonCheckedConverter sut = new();
        int actual_result = (int)sut.ConvertBack(true, typeof(bool), expected_result, CultureInfo.InvariantCulture);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_LayoutNameConverter_Convert()
    {
        // act
        LayoutNameConverter sut = new();
        string actual_result = (string)sut.Convert(null, typeof(int), 2, CultureInfo.InvariantCulture);

        // assert
        Assert.StartsWith("2", actual_result);
    }

    [Fact]
    public void Test_IntegerSettingConverter_Convert()
    {
        // arrange
        string property = "DisabledOption";
        string expected_result = "Disabled";
        int value = 0;

        // act
        IntegerSettingConverter sut = new();
        string actual_result = (string)sut.Convert(value, typeof(int), property, CultureInfo.InvariantCulture);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_IntegerSettingConverter_ConvertBack()
    {
        // arrange
        int expected_result = 0;
        string property = "DisabledOption";
        string value = "Disabled";

        // act
        IntegerSettingConverter sut = new();
        int actual_result = (int)sut.ConvertBack(value, typeof(int), property, CultureInfo.InvariantCulture);

        // assert
        Assert.Equal(expected_result, actual_result);
    }
}