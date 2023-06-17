namespace Random_Image_Tests.Classes.Utilities;

using Random_Image.Classes.Utilities;

public class Test_PathHelper
{
    [Theory]
    [InlineData(@"c:\abc123", @"c:\abc12", true)]
    [InlineData(@"c:\abc123", @"c:\abc123a", true)]
    [InlineData(@"c:\abc123", @"c:\abd", false)]
    [InlineData(@"c:\123-abcdef", @"c:\124-abcdef", true)]
    [InlineData(@"c:\123abcdef", @"c:\124abcdef", false)]
    [InlineData(@"c:\123", @"c:\124", true)]
    [InlineData(@"c:\_0123456789abcdef0123456789abcdef_123", @"c:\_9876543210fedcba9876543210fedcba_123", true)]
    [InlineData(@"c:\abcd_0123456789abcdef0123456789abcdef_abc", @"c:\abcd_0123456789abcdef0123456789abcdef_abd", true)]
    [InlineData(@"c:\0123456789abcdef0123456789abcde0", @"c:\0123456789abcdef0123456789abcdef", false)]
    public void Test_IsSameFileGroup(string path1, string path2, bool expected_result)
    {
        // act
        bool actual_result = PathHelper.IsSameFileGroup(path1, path2);

        // assert
        Assert.Equal(expected_result, actual_result);
    }


    [Theory]
    [InlineData(@"c:\abc123", @"c:\abc", 123)]
    [InlineData(@"c:\123-abcdef", @"c:\-abcdef", 123)]
    [InlineData(@"c:\123abcdef", @"c:\123abcdef", 0)]
    public void Test_SplitNumberedPath(string path, string expected_base_path, long expected_index)
    {
        // act
        (string actual_base_path, long actual_index) = PathHelper.GetFileOrder(path);

        // assert
        Assert.Equal(expected_base_path, actual_base_path);
        Assert.Equal(expected_index, actual_index);
    }

    [Fact]
    public void Test_LimitPathLength()
    {
        // act
        string path = PathHelper.LimitPathLength("c:\\this is a long name\\this is another long name\\this is a yet another long name", 50);

        // assert
        Assert.Equal("c:/this is a long name/this is another long name/...", path);
    }

    [Theory]
    [InlineData("system: MyPictures")]
    [InlineData("system:Desktop")]
    [InlineData("c:\\")]
    public void Test_ResolveSystemFolder(string folder)
    {
        // arrange
        string path = PathHelper.ResolveSystemFolder(folder);

        // act
        bool exists = Directory.Exists(path);

        // assert
        Assert.True(exists);
    }

    [Fact]
    public void Test_GetFileSize()
    {
        // arrange
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\starting.gif");

        // act
        (long actual_result, _) = PathHelper.GetFileSizeAndTime(path);

        // assert
        Assert.True(actual_result > 0);
    }

    [Fact]
    public void Test_GetIrfanViewPath()
    {
        // arrange
        string path = PathHelper.GetIrfanViewPath();

        // act
        bool exists = File.Exists(path);

        // assert
        Assert.True(exists, "You need IrfanView installed to develop this application!");
    }
}