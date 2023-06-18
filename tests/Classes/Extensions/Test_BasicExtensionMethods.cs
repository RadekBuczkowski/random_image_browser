namespace Random_Image_Tests.Classes.Utilities;

using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

using Random_Image.Classes.Extensions;
using Random_Image.Classes.Wpf;

public partial class Test_BasicExtensionMethods
{
    [Fact]
    public void Test_Yield()
    {
        // arrange
        const int value = 3;
        List<int> expected_result = new() { value };

        // act
        IEnumerable<int> actual_result = value.Yield();

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_IsIn_Int()
    {
        // arrange
        const int value = 3;

        // act
        bool actual_result1 = value.IsIn(5, 3, 6);
        bool actual_result2 = value.IsIn(5, 4, 6);

        // assert
        Assert.True(actual_result1);
        Assert.False(actual_result2);
    }

    [Fact]
    public void Test_IsIn_String()
    {
        // arrange
        const string value = "Hello";

        // act
        bool actual_result1 = value.IsIn("Whole", "World", "Hello");
        bool actual_result2 = value.IsIn("Whole", "World");

        // assert
        Assert.True(actual_result1);
        Assert.False(actual_result2);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("123", "123")]
    public void Test_TrimEmpty(string value, string expected_result)
    {
        // act
        string actual_result = value.TrimEmpty();

        // assert
        Assert.Equal(actual_result, expected_result);
    }

    [Fact]
    public void Test_GetNonEmpty()
    {
        // arrange
        string?[] items = new string?[] { "1", null, "  ", "3", "", "12", "", null };
        string?[] expected_result = new string?[] { "1", "3", "12" };

        // act
        string?[] actual_result = items.GetNonEmpty().ToArray();

        // assert
        Assert.Equal(actual_result, expected_result);
    }

    [Fact]
    public void Test_ExtractItems()
    {
        // arrange
        string text = ";;1; 3 ,  12 ,";
        string[] expected_result = new string[] { "1", "3", "12" };

        // act
        string[] actual_result = text.ExtractItems();

        // assert
        Assert.Equal(actual_result, expected_result);
    }

    [Fact]
    public void Test_CombineItems()
    {
        // arrange
        string[] items = new string[] { "1", "3", "12" };
        string expected_result = "1, 3, 12";

        // act
        string actual_result = items.CombineItems();

        // assert
        Assert.Equal(actual_result, expected_result);
    }

    [Fact]
    public void Test_AreNonEmptyItemsEqual_True()
    {
        // arrange
        string items1 = "1, 2,  , 3   , ,";
        string items2 = ", , 1, ,  2, 3";

        // act
        bool actual_result = items1.AreNonEmptyItemsEqual(items2);

        // assert
        Assert.True(actual_result);
    }

    [Fact]
    public void Test_AreNonEmptyItemsEqual_False()
    {
        // arrange
        string items1 = "1, 2, 3";
        string items2 = "1, 3, 2";

        // act
        bool actual_result = items1.AreNonEmptyItemsEqual(items2);

        // assert
        Assert.False(actual_result);
    }

    [Fact]
    public void Test_AreNonEmptyItemsEqual_Collections_True()
    {
        // arrange
        string?[] items1 = new string?[] { "1", "3", null, "", "4" };
        string?[] items2 = new string?[] { "1", "3", "4", null };

        // act
        bool actual_result = items1.AreNonEmptyItemsEqual(items2);

        // assert
        Assert.True(actual_result);
    }

    [Fact]
    public void Test_AreNonEmptyItemsEqual_Collections_False()
    {
        // arrange
        string?[] items1 = new string?[] { "1", "3", "4" };
        string?[] items2 = new string?[] { "1", "4", "3" };

        // act
        bool actual_result = items1.AreNonEmptyItemsEqual(items2);

        // assert
        Assert.False(actual_result);
    }

    [Theory]
    [InlineData(3, 5, 5)]
    [InlineData(5, 5, 5)]
    [InlineData(6, 5, 10)]
    [InlineData(-3, 5, -5)]
    public void Test_MakeEvenlyDivisible(int value, int divisor, int expected_result)
    {
        // act
        double actual_result = value.MakeEvenlyDivisible(divisor);

        // assert
        Assert.True(actual_result == expected_result);
    }

    [Theory]
    [InlineData(1.5, 1, 2, 1.5)]
    [InlineData(0.5, 1, 2, 1)]
    [InlineData(2.5, 1, 2, 2)]
    public void Test_Limit_Number(double x, double low, double high, double expected_result)
    {
        // act
        double actual_result = x.Limit(low, high);

        // assert
        Assert.True(actual_result == expected_result);
    }

    [Theory]
    [InlineData(1.5, -1.5, 3, 3, 1.5, -1.5)]
    [InlineData(1.5, -1.5, 2, 2, 1.5, -1.5)]
    [InlineData(1.5, -1.5, 1, 1, 1, -1)]
    public void Test_Limit_Vector(double x, double y, double limit_x, double limit_y, double expected_x, double expected_y)
    {
        // act
        Vector actual_result = new Vector(x, y).Limit(new Vector(limit_x, limit_y));

        // assert
        Assert.True(actual_result.X == expected_x && actual_result.Y == expected_y);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, -1, 1, -1)]
    [InlineData(-0.9, 0.9, 0, 0)]
    [InlineData(0, -2, 0, -2)]
    [InlineData(0, -0.5, 0, 0)]
    [InlineData(0, 0.5, 0, 0)]
    [InlineData(0, 2, 0, 2)]
    public void Test_RoundToZero(double x1, double y1, double x2, double y2)
    {
        // act
        Vector actual_result = new Vector(x1, y1).RoundToZero();
        Vector expected_result = new(x2, y2);

        // assert
        Assert.True(actual_result == expected_result);
    }

    [Theory]
    [InlineData(1.0001, 1.0001, 1, 1)]
    [InlineData(-1.0001, 1, -1.0001, 1)]
    [InlineData(-1.1, 1, -1.1, 1)]
    [InlineData(-1, 1, -1, 1)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0.9, 1, 0.9, 1)]
    [InlineData(0.9999, 1, 1, 1)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(1.001, 1, 1, 1)]
    public void Test_RoundToOne(double x1, double y1, double x2, double y2)
    {
        // act
        Vector actual_result = new Vector(x1, y1).RoundToOne();
        Vector expected_result = new(x2, y2);

        // assert
        Assert.True(actual_result == expected_result);
    }

    [Theory]
    [InlineData(0, 2, 0, 3, true)]
    [InlineData(0, -2, 0, -3, true)]
    [InlineData(0, -2, 0, 3, false)]
    [InlineData(0, -1, 0, 5, false)]
    [InlineData(2, 0, 1, 0, true)]
    [InlineData(-2, 0, -5, 0, true)]
    [InlineData(-2, 0, 1, 0, false)]
    [InlineData(3, 0, -1, 0, false)]
    public void Test_IsSameDirection(double x1, double y1, double x2, double y2, bool expected_result)
    {
        // act
        bool actual_result = new Vector(x1, y1).IsSameDirectionAs(new Vector(x2, y2));

        // assert
        Assert.True(actual_result == expected_result);
    }

    [Fact]
    public void Test_MatchFirst()
    {
        // arrange
        string expected_result = "Hello";

        // act
        string actual_result = GetFirstWord().MatchFirst("Hello world!");

        // assert
        Assert.Equal(actual_result, expected_result);
    }

    [Fact]
    public void Test_RectangleDifference()
    {
        // arrange
        Rect rectangle1 = new(1, 2, 5, 7);
        Rect rectangle2 = new(3, 4, 10, 21);
        Vector expected_shift = new(4.5, 9);
        Vector expected_scale = new(2, 3);

        //act
        (Vector actual_shift, Vector actual_scale) = rectangle1.RectangleDifference(rectangle2);

        // assert
        Assert.Equal(expected_shift, actual_shift);
        Assert.Equal(expected_scale, actual_scale);
    }

    [Fact]
    public void Test_DistanceTo()
    {
        // arrange
        Point point1 = new(0, 0);
        Point point2 = new(3, 4);
        double expected_result = 5;

        // act
        double actual_result = point1.DistanceTo(point2);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_MinDistanceTo()
    {
        // arrange
        Point point1 = new(0, 0);
        Point point2 = new(3, 4);
        Point point3 = new(5, 0.0001);
        double expected_result = 5;

        // act
        double actual_result = point1.MinDistanceTo((point2.X, point2.Y), (point3.X, point3.Y));

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_UndoScale()
    {
        // arrange
        Rect expected_result = new(2, 5, 200, 100);
        Rect actual_result = new(2 * 2, 5 * 3, 200 * 2, 100 * 3);
        Vector scale = new(2, 3);

        // act
        actual_result = actual_result.UndoScale(scale);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_Shuffle()
    {
        // arrange
        List<int> list1 = new() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        List<int> list2 = new(list1);  // make a copy

        // act
        list2.Shuffle();
        bool actual_result1 = list1.SequenceEqual(list2);  // compare elements and their order
        bool actual_result2 = new HashSet<int>(list1).SetEquals(new HashSet<int>(list2));  // compare elements ignoring order

        // assert
        Assert.Equal(list1.Count, list2.Count);  // equal sizes
        Assert.False(actual_result1);
        Assert.True(actual_result2);
    }

    [GeneratedRegex("^([\\w]+)")]
    private static partial Regex GetFirstWord();
}