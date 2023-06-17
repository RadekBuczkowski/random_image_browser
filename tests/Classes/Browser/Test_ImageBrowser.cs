namespace Random_Image_Tests.Classes.Browser;

using System.Windows.Controls;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;

public class Test_ImageBrowser
{
    static Test_ImageBrowser()
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
            ImageBrowserCanvas canvas = new()
            {
                RenderSize = new(1920, 1080)
            };
            ImageBrowser sut = new(state, canvas);
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
    public async Task Test_Rotate_GoIn()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserStateDurable state = new();
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources");
        state.SetImageFolders(new string[] { folder });

        // act
        await MainThread.InvokeSTA(() =>
        {
            ImageBrowserCanvas canvas = new()
            {
                RenderSize = new(1920, 1080)
            };
            ImageBrowser sut = new(state, canvas);
            sut.Initialize(false, 0, null, () => { });
            Image? image = canvas.Children[0] as Image;
            sut.LoadImages(0);
            sut.SetLayout(7);
            sut.RotateImages(true);
            (_, _, int rotation) = image.ApplyRemainingTransformations(new(), new(), 0);
            actual_result.Add(rotation == 90);
            sut.NormalImages();
            (_, _, rotation) = image.ApplyRemainingTransformations(new(), new(), 0);
            actual_result.Add(rotation == 0);
            sut.GoIn(image);
            actual_result.Add(state.IsZoomed);
            sut.GoBack();
            actual_result.Add(state.IsZoomed == false);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }
}
