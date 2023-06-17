namespace Random_Image_Tests.Classes.Utilities;

using System.Windows.Media.Imaging;

using Random_Image.Classes.Utilities;

public class Test_ResourceHelper
{
    [Fact]
    public void Test_LoadTextResource()
    {
        // arrange
        string? result = null;

        // act
        Exception? exception = Record.Exception(() => result = ResourceHelper.LoadTextResource(ResourceHelper.HelpHtml));

        // assert
        Assert.Null(exception);
        Assert.NotNull(result);
        Assert.True(result.Length > 100);
    }

    [Fact]
    public void Test_LoadBitmapResource()
    {
        // arrange
        BitmapSource? result = null;

        // act
        Exception? exception = Record.Exception(() =>
            result = ResourceHelper.LoadBitmapResource(ResourceHelper.LoadingPng));

        // assert
        Assert.Null(exception);
        Assert.NotNull(result);
        Assert.True(result.PixelWidth > 100 && result.PixelHeight > 100);
    }

    [Fact]
    public void Test_LoadBitmap()
    {
        // arrange
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\starting.gif");
        BitmapSource? result = null;

        // act
        Exception? exception = Record.Exception(() => result = ResourceHelper.LoadBitmap(path));

        // assert
        Assert.Null(exception);
        Assert.NotNull(result);
        Assert.True(result.PixelWidth > 100 && result.PixelHeight > 100);
    }

    [Fact]
    public void Test_LoadBitmapWithLimit()
    {
        // arrange
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\starting.gif");
        BitmapSource? result = null;
        int limit = 15;

        // act
        Exception? exception = Record.Exception(() => (result, _, _) = ResourceHelper.LoadBitmapWithLimit(path, limit));

        // assert
        Assert.Null(exception);
        Assert.NotNull(result);
        Assert.True(result.PixelWidth == limit && result.PixelHeight == limit);
    }

    [Fact]
    public void Test_IsAnimatedGif()
    {
        // arrange
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\starting.gif");

        // act
        bool result = ResourceHelper.IsAnimatedGif(path);

        // assert
        Assert.True(result);
    }
}