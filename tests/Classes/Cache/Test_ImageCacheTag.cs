namespace Random_Image_Tests.Classes.Cache;

using Random_Image.Classes.Cache;

public class Test_ImageCacheTag
{
    [Fact]
    public void Test_Equals()
    {
        // arrange
        List<bool> actual_result = new();
        ImageCacheTag sut1 = new();
        ImageCacheTag sut2 = new();
        ImageCacheTag sut3 = new();

        // act
        sut1.SetIndex(7);
        sut2.SetIndex(7);
        sut3.SetIndex(5);
        actual_result.Add(sut1.GetHashCode() == sut2.GetHashCode());
        actual_result.Add(sut1.GetHashCode() != sut3.GetHashCode());
        actual_result.Add(sut1.Equals(sut2));
        actual_result.Add(sut1.Equals(sut3) == false);

        // assert
        Assert.DoesNotContain(false, actual_result);
    }


    [Fact]
    public void Test_Expire()
    {
        // arrange
        ImageCacheTag sut = new();
        sut.Expire();

        // assert
        Assert.Equal(int.MinValue, sut.Index);

    }

    [Theory]
    [InlineData(0, "1.1")]
    [InlineData(4, "2.2")]
    [InlineData(7, "3.3")]
    [InlineData(10, "5.1")]
    [InlineData(14, "2.1")]
    public void Test_GoToNeighbor(int delta, string expected_result)
    {
        // arrange
        List<List<string>> groups = new()
        {
            new List<string>() { "1.1", "1.2", "1.3" },
            new List<string>() { "2.1", "2.2" },
            new List<string>() { "3.1", "3.2", "3.3", "3.4" },
            new List<string>() { "4.1" },
            new List<string>() { "5.1" }
        };
        ImageCacheTag sut = new(0, 0, 3);
        sut.SetIndex(0);

        // act
        sut.GoToNeighbor(delta, groups);

        // assert
        Assert.Equal(expected_result, groups[sut.GroupIndex][sut.GroupItemIndex]);
    }

    [Fact]
    public void Test_GoToNeighbor_BlockMouseWheel()
    {
        // arrange
        List<bool> actual_result = new();
        List<List<string>> groups = new()
        {
            new List<string>() { "1.1", "1.2" },
            new List<string>() { "2.1" },
            new List<string>() { "3.1" }
        };
        ImageCacheTag sut = new(0, 1, 2);
        sut.SetIndex(1);

        // act
        actual_result.Add(sut.GroupIndex == 0);
        actual_result.Add(sut.GroupItemIndex == 1);
        sut.GoToNeighbor(1, groups);  // accepted 1
        actual_result.Add(sut.GroupIndex == 1);
        actual_result.Add(sut.GroupItemIndex == 0);
        sut.GoToNeighbor(1, groups);  // accepted 1
        actual_result.Add(sut.GroupIndex == 2);
        actual_result.Add(sut.GroupItemIndex == 0);
        sut.GoToNeighbor(-3, groups);  // only -2 is accepted to get back to where we started
        actual_result.Add(sut.GroupIndex == 0);
        actual_result.Add(sut.GroupItemIndex == 1);
        sut.GoToNeighbor(-1, groups);  // ignored because the next change after the reset is too soon
        actual_result.Add(sut.GroupIndex == 0);
        actual_result.Add(sut.GroupItemIndex == 1);

        // assert
        Assert.DoesNotContain(false, actual_result);
    }
}