namespace Random_Image_Tests.Classes.Cache;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Cache;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;

public class Test_ImageCacheFiles
{
    private static string[] ImageFiles = Array.Empty<string>();

    static Test_ImageCacheFiles()
    {
        // Don't use dispatcher or start new threads because because unit tests are multi-threaded.
        MainThread.Disable = true;

        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources");
        ImageCacheFiles.Scan(new string[] { folder }, new string[] { "gif" }, 0, null, (result) => ImageFiles = result);
    }

    [Fact]
    public void Test_Scan()
    {
        // Dummy reference because static constructors are not included in code coverage.
        ImageCacheFiles.Scan(Array.Empty<string>(), Array.Empty<string>(), 0, null, (result) => { });

        // arrange
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\starting.gif");

        // assert
        Assert.True(ImageFiles.Length == 1);
        Assert.Equal(path, ImageFiles[0]);
    }

    [Fact]
    public void Test_FileCount()
    {
        // act
        ImageCacheFiles sut = new(ImageFiles, true);

        // assert
        Assert.True(sut.FileCount == 1);
    }

    [Fact]
    public void Test_GetImageTag()
    {
        // act
        ImageCacheFiles sut = new(ImageFiles, true);
        ImageTag tag = sut.GetImageTag(0);

        // assert
        Assert.Equal(ImageFiles[0], tag.FilePath);
    }

    [Fact]
    public void Test_Serialization()
    {
        // arrange
        ImageCacheTag tag = new();
        tag.SetIndex(5);
        ImageCacheFiles expected_result = new("sut".Yield(), true);

        // act
        string tempFileName = expected_result.GetSerializedTempFile();
        ImageCacheFiles actual_result = ImageCacheFiles.DeSerializeFromTempFile(tempFileName);

        // assert
        Assert.Equal(expected_result.FileGroups[0], actual_result.FileGroups[0]);
        Assert.Equal(expected_result.FileOrder[0], actual_result.FileOrder[0]);
    }
}