namespace Random_Image_Tests.Classes.Extensions;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;

public class Test_ImageBrowserCanvas
{
    [Fact]
    public async Task Test_AddNewImage()
    {
        // arrange
        bool actual_result = false;
        ImageTag tag = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            ImageBrowserCanvas sut = new() { Width = 100, Height = 50 };
            Image image = sut.AddNewImage(tag);
            actual_result = sut.Children.Contains(image);
        });

        // assert
        Assert.True(actual_result);
    }

    [Fact]
    public async Task Test_GetSize()
    {
        // arrange
        Size expected_result = new(0, 0);  // Canvas is not rendered
        Size actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            ImageBrowserCanvas sut = new() { Width = 100, Height = 50 };
            actual_result = sut.GetSize();
        });

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Test_SetCache(bool expected_result)
    {
        // arrange
        bool actual_result = false;

        // act
        await MainThread.InvokeSTA(() =>
        {
            ImageBrowserCanvas sut = new() { Width = 100, Height = 50 };
            sut.ResetCache(expected_result);
            object old_value = sut.CacheMode;
            sut.ResetCache(expected_result);
            actual_result = sut.CacheMode is BitmapCache && sut.CacheMode == old_value;
        });

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public async Task Test_SetCache_Default()
    {
        // arrange
        bool actual_result = false;

        // act
        await MainThread.InvokeSTA(() =>
        {
            ImageBrowserCanvas sut = new() { Width = 100, Height = 50 };
            sut.ResetCache();
            actual_result = sut.CacheMode == null;
        });

        // assert
        Assert.True(actual_result);
    }

    [Fact]
    public async Task Test_CloneAndResetCanvas()
    {
        // arrange
        BitmapSource bitmap = ResourceHelper.LoadBitmapResource(ResourceHelper.LoadingPng);
        ImageTag tag = new();
        bool actual_result = false;

        // act
        await MainThread.InvokeSTA(() =>
        {
            Grid parent = new();
            ImageBrowserCanvas sut = new() { Width = 100, Height = 50 };
            parent.Children.Add(sut);
            Image image = new();
            image.SetTag(tag);
            image.AssignBitmap(new(bitmap));
            sut.Children.Add(image);
            sut.CloneAndResetCanvas();
            actual_result = sut.Children.Count == 0;
        });

        // assert
        Assert.True(actual_result);
    }
}