namespace Random_Image_Tests.Classes.Browser;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;

public class Test_ImageBrowserAnimated
{
    static Test_ImageBrowserAnimated()
    {
        // don't use dispatcher or start new threads because because unit tests are multi-threaded
        MainThread.Disable = true;
    }

    [Fact]
    public async Task Test_Initialize()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserStateDurable state = new();
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources");
        state.SetImageFolders(new string[] { folder });

        // act
        await MainThread.InvokeSTA(() =>
        {
            ImageBrowserCanvas canvas = new() { RenderSize = new(1920, 1080) };
            Window window = new() { Content = canvas };
            window.Resources.Add("shadowTiny", new DropShadowEffect());
            ImageBrowserAnimated sut = new(state, canvas);
            sut.Initialize(false, 0, null, () => { });
            actual_result.Add(sut.IsReady);
            actual_result.Add(canvas.Children.Count > 0);
            Image? image = canvas.Children[0] as Image;
            actual_result.Add(image.Tag().IsReady);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public async Task Test_Align()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserStateDurable state = new();
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources");
        state.SetImageFolders(new string[] { folder });

        // act
        await MainThread.InvokeSTA(() =>
        {
            ImageBrowserCanvas canvas = new() { RenderSize = new(1920, 1080) };
            Window window = new() { Content = canvas };
            window.Resources.Add("shadowTiny", new DropShadowEffect());
            ImageBrowserAnimated sut = new(state, canvas);
            sut.Initialize(false, 0, null, () => { });
            Image? image = canvas.Children[0] as Image;
            sut.LoadImages(0);
            sut.SetLayout(7);
            while (state.CanAlignMany == false)
            {
                sut.ToggleAutoScaling();
            }
            sut.AlignImageToCenter(image);
            Thread.Sleep(50);
            canvas.ResetTransformations();  // animations don't work because there is no WPF Clocks and Time Manager
            actual_result.Add(image.Tag().IsCentered);
            sut.PresentImagesByAligning(true);
            Thread.Sleep(50);
            sut.TogglePauseOrResumeAnimations();
            sut.LoadImages(0);
            actual_result.Add(image.Tag().IsCentered == false);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }
}
