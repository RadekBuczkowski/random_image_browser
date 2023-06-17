namespace Random_Image_Tests.Classes.Browser;

using System;
using System.Windows.Input;

using Random_Image.Classes.Browser;

public class Test_ImageBrowserState
{
    [Theory]
    [InlineData(7, 40, 30)]
    [InlineData(7, 8, 0)]
    [InlineData(5, 40, 36)]
    [InlineData(5, 7, 3)]
    [InlineData(5, 6, 0)]
    public void Test_MaximumFirstImageIndex(int layout, int file_count, int expected_result)
    {
        // arrange
        ImageBrowserState sut = new();
        sut.Initialize(file_count);
        sut.SetLayout(layout);
        sut.Navigate(0);
        sut.Navigate(10);

        // act
        int actual_result = sut.MaximumFirstImageIndex;

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Theory]
    [InlineData(0, new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 })]
    [InlineData(1, new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 })]
    [InlineData(-1, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 })]
    public void Test_GetAvailableImageIndexes(int extra_pages, int[] expected_result)
    {
        // arrange
        ImageBrowserState sut = new();
        sut.Initialize(40);
        sut.SetLayout(7);
        sut.Navigate(0);
        sut.Navigate(10);

        // act
        IEnumerable<int> actual_result = sut.GetAvailableImageIndexes(extra_pages);

        // assert
        Assert.True(actual_result.SequenceEqual(expected_result));
    }

    [Theory]
    [InlineData(4, -1, false)]
    [InlineData(5, -1, true)]
    [InlineData(24, 1, true)]
    [InlineData(25, 1, false)]
    public void Test_IsImageVisible(int index, int extra_rows, bool expected_result)
    {
        // arrange
        ImageBrowserState sut = new();
        sut.Initialize(40);
        sut.SetLayout(7);
        sut.Navigate(0);
        sut.GoIn(3);
        sut.GoBack();
        sut.Navigate(10);

        // act
        bool actual_result = sut.IsImageVisible(index, extra_rows);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Theory]
    [InlineData(4, -1, false)]
    [InlineData(5, -1, true)]
    [InlineData(24, 1, true)]
    [InlineData(25, 1, false)]
    public void Test_IsImageVisibleBeforeZoomed(int index, int extra_rows, bool expected_result)
    {
        // arrange
        ImageBrowserState sut = new();
        sut.Initialize(40);
        sut.SetLayout(7);
        sut.Navigate(0);
        sut.Navigate(10);
        sut.GoIn(10);

        // act
        bool actual_result = sut.IsImageVisibleBeforeZoomed(index, extra_rows);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Theory]
    [InlineData(19, true)]
    [InlineData(20, false)]
    [InlineData(69, false)]
    [InlineData(70, true)]
    public void Test_IsImageExpired(int index, bool expected_result)
    {
        // arrange
        ImageBrowserState sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);
        sut.Navigate(0);
        sut.Navigate(40);

        // act
        bool actual_result = sut.IsImageExpired(index);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_SetImageFolders()
    {
        // arrange
        string[] expected_result1 = new string[] { "/folder1", "/folder2" };
        string[] expected_result2 = new string[] { "jpg", "jpeg", "gif" };
        ImageBrowserState sut = new();

        // act
        sut.SetImageFolders(expected_result1, expected_result2);
        string[] actual_result1 = sut.ImageFolders;
        string[] actual_result2 = sut.ImageExtensions;

        // assert
        Assert.Equal(expected_result1, actual_result1);
        Assert.Equal(expected_result2, actual_result2);
    }

    [Fact]
    public void Test_ToggleGroupingImages()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserState sut = new();

        // act
        actual_result.Add(sut.GroupImages);
        sut.ToggleGroupingImages();
        actual_result.Add(sut.GroupImages == false);

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_GetCompletedWheelScrolls()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserState sut = new();

        // act
        actual_result.Add(sut.GetCompletedWheelScrolls(120, 120) == 1);
        actual_result.Add(sut.GetCompletedWheelScrolls(120, 120) == 1);
        actual_result.Add(sut.GetCompletedWheelScrolls(120, 120) == 0);  // too many scrolls at once
        Thread.Sleep(300);
        actual_result.Add(sut.GetCompletedWheelScrolls(60, 120) == 0);  // first half of a scroll
        actual_result.Add(sut.GetCompletedWheelScrolls(60, 120) == 1);  // full scroll completed

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_GetNavigationDelta()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserState sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);
        sut.Navigate(0);

        // act
        actual_result.Add(sut.GetNavigationDelta(Key.Down, 30) == sut.CanvasColumns);
        sut.Navigate(sut.CanvasColumns);
        actual_result.Add(sut.GetNavigationDelta(Key.Down, 30) == 0);  // too quickly, 30 ms not passed yet
        actual_result.Add(sut.GetNavigationDelta(Key.Up, 0) == -sut.CanvasColumns);
        sut.Navigate(-sut.CanvasColumns);
        actual_result.Add(sut.GetNavigationDelta(Key.Up, 0) == 0);  // already at the beginning

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_GoIn()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserState sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);
        sut.Navigate(0);

        // act
        actual_result.Add(sut.IsZoomed == false);
        actual_result.Add(sut.CanvasColumns > 1);
        actual_result.Add(sut.CanvasRows > 1);
        actual_result.Add(sut.ImagesOnCanvas > 1);
        sut.GoIn(3);
        actual_result.Add(sut.IsZoomed);
        actual_result.Add(sut.CanvasColumns == 1);
        actual_result.Add(sut.CanvasRows == 1);
        actual_result.Add(sut.ImagesOnCanvas == 1);
        sut.GoBack();
        actual_result.Add(sut.IsZoomed == false);
        actual_result.Add(sut.CanvasColumns > 1);
        actual_result.Add(sut.CanvasRows > 1);
        actual_result.Add(sut.ImagesOnCanvas > 1);

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Theory]
    [InlineData(AutoScalingModes.InsideEdges, false)]
    [InlineData(AutoScalingModes.OutsideEdges, true)]
    public void Test_SetAutoScaling(AutoScalingModes expected_result, bool isZoomed)
    {
        // arrange
        ImageBrowserState sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);
        sut.Navigate(0);

        // act
        sut.SetAutoScaling(expected_result, isZoomed);
        AutoScalingModes actual_result = isZoomed ? sut.FitWhenZoomed : sut.Fit;

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_RestoreAutoScaling()
    {
        // arrange
        List<bool> actual_result = new();
        ImageBrowserState sut = new();
        sut.Initialize(80);
        sut.SetLayout(7);
        sut.Navigate(0);

        // act
        actual_result.Add(sut.IsNonDefaultAutoScaling == false);
        sut.ToggleAutoScaling();
        actual_result.Add(sut.IsNonDefaultAutoScaling);
        sut.ToggleAutoScaling(restore: true);
        actual_result.Add(sut.IsNonDefaultAutoScaling == false);
        sut.GoIn(3);
        actual_result.Add(sut.IsNonDefault == false);
        sut.ToggleAutoScaling();
        actual_result.Add(sut.IsNonDefault);
        sut.ToggleAutoScaling(restore: true);
        actual_result.Add(sut.IsNonDefault == false);
        sut.ApplyExtraZoom(true);
        actual_result.Add(sut.IsNonDefault);
        sut.ResetExtraZoom();
        sut.ToggleAutoScaling(restore: true);
        actual_result.Add(sut.IsNonDefault == false);
        sut.GoBack();
        actual_result.Add(sut.IsNonDefault == false);

        // assert
        Assert.DoesNotContain(false, actual_result);
    }
}