namespace Random_Image_Tests.Classes.Browser;

using System.Windows;
using System.Windows.Controls;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;

public class Test_ImageBrowserStateDurable
{
    [Fact]
    public void Test_GetFileDescription()
    {
        const int image_count = 73;

        // arrange
        List<bool> actual_result = new();
        ImageBrowserStateDurable sut = new();
        sut.Initialize(image_count);

        // act
        actual_result.Add(sut.GetDefaultDescription().Contains(image_count.ToString()));

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_GetLayoutDescription()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserStateDurable sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);

        // act
        string description = sut.GetLayoutDescription();
        sut.SetLayout(9);
        actual_result.Add(description != sut.GetLayoutDescription());
        sut.SetLayout(7);
        actual_result.Add(description == sut.GetLayoutDescription());

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_GetScalingDescription()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserStateDurable sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);
        sut.Navigate(0);

        // act
        string description = sut.GetScalingDescription();
        sut.GoIn(3);
        actual_result.Add(description != sut.GetScalingDescription());
        sut.GoBack();
        actual_result.Add(description == sut.GetScalingDescription());
        sut.ToggleAutoScaling();
        actual_result.Add(description != sut.GetScalingDescription());
        sut.ToggleAutoScaling(restore: true);
        actual_result.Add(description == sut.GetScalingDescription());

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_GetOrientationDescription()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserStateDurable sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);
        sut.Navigate(0);

        // act
        string description = sut.GetOrientationDescription();
        sut.GoIn(3);
        actual_result.Add(description != sut.GetOrientationDescription());
        sut.Rotate(180, false, true);
        actual_result.Add(description != sut.GetOrientationDescription());
        sut.GoBack();
        actual_result.Add(description != sut.GetOrientationDescription());
        sut.Rotate(0, true, false);
        actual_result.Add(description == sut.GetOrientationDescription());
        sut.Rotate(0, false, true);
        actual_result.Add(description != sut.GetOrientationDescription());
        sut.Rotate(0, true, false);
        actual_result.Add(description == sut.GetOrientationDescription());

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public async Task Test_UpdateButtonTooltips()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserStateDurable sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);
        sut.Navigate(0);
        sut.Navigate(10);

        // act
        await MainThread.InvokeSTA(() =>
        {
            FrameworkElement button1 = new();
            FrameworkElement button2 = new();
            sut.UpdateButtonTooltips(button1, button2);
            actual_result.Add(button1.GetTooltipText().Contains(sut.FirstImageIndex.ToString()));
            actual_result.Add(button2.GetTooltipText().Contains((sut.FirstImageIndex + sut.ImagesOnCanvas + 1).ToString()));
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public async Task Test_GetCommandLine()
    {
        // arrange
        ImageBrowserStateDurable sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);
        sut.Navigate(0);
        sut.SetImageFolders(new string[] { "/folder1" });
        string not_expected_result = sut.GetCommandLine();

        // act
        sut.SetImageFolders(new string[] { "/folder1", "/folder2", string.Empty });
        sut.Navigate(10);
        sut.SetLayout(9);
        sut.ToggleGroupingImages();
        sut.ToggleAutoScaling();
        sut.Rotate(90, false, true);
        sut.GoIn(3);
        sut.ToggleAutoScaling();
        sut.ApplyExtraZoom(true);
        await MainThread.InvokeSTA(() =>
        {
            ImageBrowserCanvas canvas = new();
            Window window = new() { Content = canvas };
            sut.AppendBrowserState(canvas, !sut.EnableAnimations, "/temp/abc.txt");
        });
        sut = sut.Clone();  // test cloning as well
        string expected_result = sut.GetCommandLine();
        ImageBrowserStateDurable sut2 = new(expected_result.Split(" "));  // apply all changes again!
        string actual_result = sut2.GetCommandLine();

        // assert
        Assert.Equal(expected_result, actual_result);
        Assert.NotEqual(not_expected_result, actual_result);
    }
}