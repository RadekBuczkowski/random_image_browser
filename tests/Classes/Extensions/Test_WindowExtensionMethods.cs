namespace Random_Image_Tests.Classes.Extensions;

using System.Windows;
using System.Windows.Controls;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;

public partial class Test_WindowExtensionMethods
{
    [Fact]
    public async Task Test_SerializeWindowRectangle()
    {
        // arrange
        Rect expected_result = new(100, 50, 500, 400);
        Rect actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Window sut = new()
            {
                Left = expected_result.Left,
                Top = expected_result.Top,
            };
            sut.SetValue(FrameworkElement.WidthProperty, expected_result.Width);
            sut.SetValue(FrameworkElement.HeightProperty, expected_result.Height);
            string rectangle = sut.SerializeWindowRectangle();
            sut.SetValue(FrameworkElement.HeightProperty, 200.0);  // change to something else to check if it is overwritten
            sut.ApplyWindowRectangle(rectangle);
            actual_result = new(sut.Left, sut.Top, sut.Width, sut.Height);
        });

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public async Task Test_ToggleFullScreen()
    {
        // arrange
        List<bool> actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Window sut = new();
            actual_result.Add(sut.IsFullScreen() == false);
            sut.ToggleFullScreen();
            actual_result.Add(sut.IsFullScreen());
            sut.ToggleFullScreen();
            actual_result.Add(sut.IsFullScreen() == false);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public async Task Test_GetScalingFactor()
    {
        // arrange
        Vector expected_result = new(0, 0);  // cannot read the actual settings from a detached window
        Vector actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Window sut = new();
            Vector actual_result = sut.GetScalingFactor();
            ImageTag.SetWindowScaling(actual_result);
        });

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public async Task Test_Tooltip()
    {
        // arrange
        const string text = "Sut";
        List<bool> actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new();
            sut.SetTooltip(text);
            sut.ShowTooltip();
            actual_result.Add(sut.IsTooltipShown());
            actual_result.Add(sut.GetTooltipText() == text);
            sut.HideTooltip();
            actual_result.Add(sut.IsTooltipShown() == false);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public async Task Test_SetNoFilesFoundMessageLabel()
    {
        // arrange
        List<bool> actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            TextBlock label = new();
            label.SetNoFilesFoundMessageLabel(80);
            actual_result.Add(label.Text == string.Empty);
            actual_result.Add(label.Inlines.Count == 0);
            label.SetNoFilesFoundMessageLabel(0);
            actual_result.Add(label.Text == string.Empty);
            actual_result.Add(label.Inlines.Count > 0);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }
}