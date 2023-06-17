namespace Random_Image_Tests.Classes.Cache;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Cache;
using Random_Image.Classes.Extensions;

public class Test_ImageCache
{
    private static string ImagePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\starting.gif");

    [Fact]
    public void Test_NormalConstructor()
    {
        // act
        ImageCache sut = new(ImagePath.Yield(), false);

        // assert
        Assert.True(sut.Files.FileCount == 1);
        Assert.Equal(ImagePath, sut.Files.FileGroups[0][0]);
    }

    [Fact]
    public void Test_SerializedConstructor()
    {
        // act
        ImageCache sut1 = new(ImagePath.Yield(), false);
        ImageCache sut2 = new(sut1.Files.GetSerializedTempFile());

        // assert
        Assert.True(sut2.Files.FileCount == 1);
        Assert.Equal(ImagePath, sut2.Files.FileGroups[0][0]);
    }

    [Fact]
    public void Test_LoadImage()
    {
        // act
        ImageCache sut = new(ImagePath.Yield(), false);
        ImageTag tag = sut.Files.GetImageTag(0);
        ImageCacheBitmap image = sut.LoadImage(tag.FilePath);

        // assert
        Assert.True(image.Bitmap.Width > 0 && image.Bitmap.Height > 0);
    }

    [Fact]
    public void Test_LoadImage_IsAsynchronous()
    {
        // arrange
        List<bool> actual_result = new();

        // act
        ImageCache sut = new(ImagePath.Yield(), false);
        ImageTag tag = sut.Files.GetImageTag(0);
        ImageCacheBitmap image = new();
        _ = sut.LoadImage(tag.FilePath, (result) => image = result);
        Thread.Sleep(500);
        actual_result.Add(image.IsAsynchronous);
        _ = sut.LoadImage(tag.FilePath, (result) => image = result);
        actual_result.Add(image.IsAsynchronous == false); // already in the cache
        actual_result.Add(image.Bitmap.Width > 0 && image.Bitmap.Height > 0);

        // assert
        Assert.DoesNotContain(false, actual_result);
    }
}