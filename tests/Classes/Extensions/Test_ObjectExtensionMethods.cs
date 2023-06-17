namespace Random_Image_Tests.Classes.Utilities;

using System.Windows.Controls;

using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;

public partial class Test_ObjectExtensionMethods
{
    private class RefreshTest
    {
        public int _value = 5;
        public int Value
        {
            get => 37;
            set => _value = value;
        }
        public int RealValue => _value;
    }

    [Fact]
    public void Test_GetPropertyValue()
    {
        // arrange
        int expected_result = 37;
        IntegerExtension test = new(expected_result);

        // act
        int actual_result = test.GetPropertyValue<int>("Value");

        // assert
        Assert.Equal(actual_result, expected_result);
    }

    [Fact]
    public void Test_SetPropertyValue()
    {
        // arrange
        int expected_result = 37;
        IntegerExtension test = new(-1);

        // act
        test.SetPropertyValue("Value", expected_result);
        int actual_result = test.Value;

        // assert
        Assert.Equal(actual_result, expected_result);
    }

    [Fact]
    public void Test_RefreshProperties()
    {
        // arrange
        int expected_result = 37;
        RefreshTest test = new();

        // act
        int actual_result1 = test.RealValue;
        test.RefreshProperties();
        int actual_result2 = test.RealValue;

        // assert
        Assert.NotEqual(actual_result1, expected_result);
        Assert.Equal(actual_result2, expected_result);
    }

    [Fact]
    public async Task Test_FindVisualParent()
    {
        // arrange
        bool actual_result = false;

        // act
        await MainThread.InvokeSTA(() =>
        {
            Canvas sut = new();
            Image image = new();
            sut.Children.Add(image);
            actual_result = image.FindVisualParent<Canvas>() == sut;
        });

        // assert
        Assert.True(actual_result);
    }

    [Fact]
    public async Task Test_FindVisualChild()
    {
        // arrange
        bool actual_result = false;

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new() { Name = "sut" };
            Canvas canvas = new();
            canvas.Children.Add(sut);
            actual_result = canvas.FindVisualChild<Image>("sut") == sut;
        });

        // assert
        Assert.True(actual_result);
    }
}