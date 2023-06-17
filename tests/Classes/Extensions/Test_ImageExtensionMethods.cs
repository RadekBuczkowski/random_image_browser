namespace Random_Image_Tests.Classes.Extensions;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Cache;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;

public class Test_ImageExtensionMethods
{
    [Fact]
    public async Task Test_GetSetTag()
    {
        // arrange
        ImageTag expected_result = new(new ImageCacheTag(), "c:/Pictures/file.gif");
        ImageTag actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new();
            sut.SetTag(expected_result);
            actual_result = sut.Tag();
        });

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public async Task Test_GetSetImageRectangle()
    {
        // arrange
        Rect expected_result = new(1, 2, 5, 7);
        Rect actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new();
            sut.SetImageRectangle(expected_result);
            actual_result = sut.GetImageRectangle();
        });

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public async Task Test_AssignBitmap()
    {
        // arrange
        BitmapSource bitmap = ResourceHelper.LoadBitmapResource(ResourceHelper.LoadingPng);
        ImageTag tag = new();
        Size expected_result = new(bitmap.PixelWidth, bitmap.PixelHeight);
        Size actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new();
            sut.SetTag(tag);
            sut.AssignBitmap(new(bitmap));
            actual_result = sut.Tag().BitmapPixelSize;
            sut.RecycleImage();
        });

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public async Task Test_AssignBitmap_IsAnimated()
    {
        // arrange
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\starting.gif");
        BitmapSource bitmap = ResourceHelper.LoadBitmap(path);
        ImageTag tag = new(new ImageCacheTag(), path);
        Size expected_result = new(bitmap.PixelWidth, bitmap.PixelHeight);
        Size actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new();
            sut.SetTag(tag);
            sut.AssignBitmap(new(bitmap) { IsAnimated = true });  // animated GIF, start animation
            actual_result = sut.Tag().BitmapPixelSize;
            sut.RecycleImage();  // stop animation
        });

        // assert
        Assert.Equal(expected_result, actual_result);
    }
}