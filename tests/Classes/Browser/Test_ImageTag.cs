namespace Random_Image_Tests.Classes.Browser;

using System.Windows;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Cache;
using Random_Image.Classes.Extensions;

public class Test_ImageTag
{
    private ImageBrowserState State { get; init; } = new();

    public Test_ImageTag()
    {
        ImageTag.SetWindowScaling(new Vector(2, 2));

        State.Initialize(20);
        // set layout to 2 image rows with 5 images in a row
        State.SetLayout(7);
        State.GoIn(0);
        // when zoomed set auto-scaling mode to outside edges
        while (State.FitInsideEdges || State.UseOriginalSize)
            State.ToggleAutoScaling();
        State.ResetExtraZoom();
        State.GoBack();
        // when not zoomed set auto-scaling mode to thumbnails with rounded corners
        while (State.HasRoundedCorners == false)
            State.ToggleAutoScaling();
        // first navigation ignores the delta parameter (to simulate a slide-in of all images)
        State.Navigate(0);
        // move one image row down
        State.Navigate(5);
    }

    [Fact]
    public void Test_Constructor_FilePath()
    {
        // act
        ImageTag sut = new(new ImageCacheTag(), "c:/Pictures/file.jpeg");

        // assert
        Assert.Equal("c:/Pictures/file.jpeg", sut.FilePath);
    }

    [Fact]
    public void Test_Constructor_IsAnimated()
    {
        // act
        ImageTag sut = new(new ImageCacheTag(), "c:/Pictures/file.gif");

        // assert
        Assert.False(sut.IsAnimated);  // Note: needs to be loaded first (see Test_ResourceHelper.cs).
        Assert.False(sut.IsLoaded);
    }

    [Theory]
    [InlineData(100, 50, false)]
    [InlineData(50, 100, true)]
    public void Test_SetOriginalSize_IsPortrait(int width, int height, bool expected_result)
    {
        // act
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(width, height));
        sut.SetBitmapSize(cache);

        // assert
        Assert.Equal(expected_result, sut.IsPortrait);
    }

    [Theory]
    [InlineData(100, 50, false)]
    [InlineData(2000, 1500, true)]
    public void Test_SetOriginalSize_IsLarge(int width, int height, bool expected_result)
    {
        // act
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(width * 2, height * 2));
        sut.SetBitmapSize(cache);  // compensate for Windows scaling (2, 2)

        // assert
        Assert.Equal(expected_result, sut.IsLarge);
    }

    [Theory]
    [InlineData(100, 50)]
    [InlineData(2000, 1500)]
    public void Test_SetOriginalSize_OriginalSize(int width, int height)
    {
        // act
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(width * 2, height * 2));
        sut.SetBitmapSize(cache);  // compensate for Windows scaling (2, 2)

        // assert
        Assert.True(sut.BitmapSize.RoughlyEquals(new Size(width, height)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Test_Resize_IsReady(bool isLoaded)
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(100, 50));
        if (isLoaded)
            sut.SetBitmapSize(cache);

        // act
        sut.Resize(State, new(100, 50), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.Equal(sut.IsLoaded, sut.IsReady);
    }


    [Fact]
    public void Test_Resize_PositionOnCanvas()
    {
        // arrange
        ImageTag sut = new();
        sut.SetIndex(7);

        // act
        sut.Resize(State, new(100, 50), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.Equal(2, sut.PositionOnCanvas);  // 7 - 5
    }

    [Fact]
    public void Test_Resize_OccupiesWholeArea_WhenNotZoomed()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);  // Note: a canvas slot has a different ratio than the image and the whole canvas.
        sut.SetIndex(5);

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.OccupiesWholeArea);
        Assert.False(sut.IsZoomed);
    }

    [Fact]
    public void Test_Resize_OccupiesWholeArea_WhenZoomed()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);  // with Windows scaling (2, 2) the zoomed image will fit the entire canvas
        sut.SetIndex(7);
        State.GoIn(7);
        State.Navigate(State.SelectedPositionOnCanvas);

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.OccupiesWholeArea);
        Assert.True(sut.IsZoomed);
        Assert.False(sut.IsMovable);
    }

    [Fact]
    public void Test_Resize_Angle_90Degrees()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(100, 50));
        sut.SetBitmapSize(cache);
        State.Rotate(90, true, false);  // 90 degrees to the right

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.Equal(90, sut.Angle);
    }

    [Fact]
    public void Test_Resize_Mirror()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(100, 50));
        sut.SetBitmapSize(cache);
        State.Rotate(0, true, true);  // mirror

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.IsMirror);
    }

    [Fact]
    public void Test_Resize_ImageRectangle_Thumbnails()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);
        sut.SetIndex(7);

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.Radius > 0);
        Assert.True(sut.ImageRectangle.RoughlyEquals(new Rect(251.25, 0, 497.5, 398)));  // read with a debugger
    }

    [Fact]
    public void Test_Resize_ImageRectangle_Thumbnails_Error()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2), new Exception("Loading error!"));
        sut.SetBitmapSize(cache);
        sut.SetIndex(7);

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.Radius > 0);
        Assert.True(sut.ImageRectangle.RoughlyEquals(new Rect(375.625, 99.5, 248.75, 199)));  // read with a debugger
    }

    [Fact]
    public void Test_Resize_ImageRectangle_OutsideEdges()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);
        sut.SetIndex(8);
        State.SetLayout(8);
        State.ToggleAutoScaling();

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.Radius == 0);
        Assert.True(sut.ImageRectangle.RoughlyEquals(new Rect(503.5, 133.917, 160.833, 128.667)));  // from a debugger
    }

    [Fact]
    public void Test_Resize_CanvasRectangle_Thumbnails()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);
        sut.SetIndex(7);

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.CanvasRectangle.RoughlyEquals(new Rect(401.6, 0, 196.8, 398)));  // read with a debugger
    }

    [Fact]
    public void Test_Resize_CanvasRectangle_OutsideEdges()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);
        sut.SetIndex(8);
        State.SetLayout(8);
        State.ToggleAutoScaling();

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.CanvasRectangle.RoughlyEquals(new Rect(503.5, 0, 160.833, 396.5)));  // read with a debugger
    }

    [Fact]
    public void Test_Resize_CropRectangle_Thumbnails()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);
        sut.SetIndex(7);

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.CropRectangle.RoughlyEquals(new Rect(150.35, 0, 196.8, 398)));  // read with a debugger
    }

    [Fact]
    public void Test_Resize_CropRectangle_OutsideEdges()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);
        sut.SetIndex(8);
        State.SetLayout(8);
        State.ToggleAutoScaling();

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);

        // assert
        Assert.True(sut.CropRectangle.RoughlyEquals(new Rect(0, 0, 160.833, 396.5)));  // read with a debugger
    }

    [Fact]
    public void Test_Resize_Alignment_Thumbnails()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);
        sut.SetIndex(5);

        // act
        sut.Resize(State, new(1000, 800), new(100, 100), new(0, 0), Reasons.Align);  // mouse moved

        // assert
        Assert.True(sut.Alignment.RoughlyEquals(new Vector(0.0, 0.0)));  // always centered in the thumbnails mode
        Assert.True(sut.IsCentered);
        Assert.Equal(Reasons.Align, sut.Reason);
    }

    [Fact]
    public void Test_Resize_Alignment_InsideEdges()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);
        sut.SetIndex(5);
        State.ToggleAutoScaling();
        State.ToggleAutoScaling();  // inside edges where mouse movement is active

        // act
        sut.Resize(State, new(1000, 800), new(100, 100), new(0, 0), Reasons.Align);  // mouse moved

        // assert
        Assert.True(sut.Alignment.RoughlyEquals(new Vector(0.014, -0.415)));  // read with a debugger
        Assert.True(sut.IsMovable);
    }

    [Fact]
    public void Test_Resize_Alignment_InsideEdges_RemainingAnimations()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(1000 * 2, 800 * 2));
        sut.SetBitmapSize(cache);
        sut.SetIndex(5);
        State.ToggleAutoScaling();
        State.ToggleAutoScaling();  // inside edges where mouse movement is active

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(20, 0), Reasons.Navigate);  // canceled alignment animations

        // assert
        Assert.True(sut.Alignment.RoughlyEquals(new Vector(-0.067, 0)));  // read with a debugger
    }

    [Fact]
    public void Test_GetNavigationShift_OneImageLeft()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(100, 50));
        sut.SetBitmapSize(cache);

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);
        Vector actual_result = sut.GetNavigationShift(State, -1);

        // assert
        Assert.True(actual_result.RoughlyEquals(new Vector(-200.8, 0)));  // read with a debugger
    }

    [Fact]
    public void Test_GetNavigationShift_ImageWholePageDown()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(100, 50));
        sut.SetBitmapSize(cache);

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);
        Vector actual_result = sut.GetNavigationShift(State, 2 * 5);

        // assert
        Assert.True(actual_result.RoughlyEquals(new Vector(0, 804)));  // read with a debugger
    }

    [Fact]
    public void Test_GetNavigationDelta()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(100, 50));
        sut.SetBitmapSize(cache);
        Size canvasSize = new(1000, 800);
        int expected_result = (5 * 5 + 3).MakeEvenlyDivisible(5);  // six pages down

        // act
        sut.Resize(State, canvasSize, new(0, 0), new(0, 0), Reasons.Navigate);
        Vector shift = sut.GetNavigationShift(State, expected_result);
        int actual_result = ImageTag.GetNavigationDelta(State, canvasSize, shift);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_GetShiftedCrop_Angle_90Degrees_Mirror()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(100, 50));
        sut.SetBitmapSize(cache);
        State.Rotate(90, true, true);  // 90 degrees to the right and mirror

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);
        Rect actual_result = sut.GetShiftedCrop(sut.CropRectangle, new(50, -20));

        // assert
        Assert.True(actual_result.RoughlyEquals(new Rect(20, -48.9, 398, 196.8)));  // read with a debugger
        Assert.True(sut.Angle == 90);
        Assert.True(sut.IsMirror);
    }

    [Fact]
    public void Test_GetImageDescription()
    {
        // arrange
        ImageTag sut = new();
        ImageCacheBitmap cache = new(TestHelper.MockBitmap(100, 50));
        sut.SetBitmapSize(cache);
        State.Rotate(90, true, true);  // 90 degrees to the right and mirror

        // act
        sut.Resize(State, new(1000, 800), new(0, 0), new(0, 0), Reasons.Navigate);
        string description = sut.GetImageDescription();
        bool actual_result = description.Contains("Rotate right") && description.Contains("Mirror");

        // assert
        Assert.True(actual_result);
    }

}