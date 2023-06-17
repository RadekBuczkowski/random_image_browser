using Random_Image.Classes.Extensions;

namespace Random_Image.Classes.Browser;

/// <summary>
/// Provides a class with constant parameters for the image browser.
/// </summary>
public static class ImageBrowserConstants
{
    /// <summary>
    /// Sets the expected animation duration of most animations.
    /// </summary>
    public const int AnimationStepDuration = 600;

    /// <summary>
    /// Maximum number of loaded image bitmaps kept in the image cache at any given time.
    /// </summary>
    public const int MaxLoadedImageBitmaps = 100;

    /// <summary>
    /// Radius in degrees of rounded corners when showing images as thumbnails.
    /// </summary>
    public const double RoundedCornerRadius = 45;

    /// <summary>
    /// Maximum image scaling when not zoomed (when several images are visible).
    /// The minimum is equal to 1/MaxScaling.
    /// </summary>
    public const double MaxScaling = 3.0;

    /// <summary>
    /// Maximum image scaling when zoomed (when only one image is visible).
    /// The minimum is equal to 1/MaxScalingWhenZoomed
    /// </summary>
    public const double MaxScalingWhenZoomed = 10.0;

    /// <summary>
    /// When increasing or decreasing ExtraZoom this constant is used as the multiplier or divisor.
    /// </summary>
    private const double ZoomStepFactor = 1.05;

    /// <summary>
    /// All images after this number of pages in either direction are recycled and deleted
    /// (one page is the size of canvas).
    /// </summary>
    public const int ExpireAfterPage = 2;

        /// <summary>
    /// If the mouse is moved by the specified distance since a tooltip was shown, the tooltip will be hidden.
    /// </summary>
    public const int HiteTooltipWhenMouseMovedBy = 30;

    /// <summary>
    /// List of system folders in the system that can be used as an image folder for loading images.
    /// </summary>
    public static string[] SystemFolders => new string[]
    {
        "system: MyPictures",
        "system: CommonPictures",
        "system: Desktop",
        "system: Programs",
        "system: MyDocuments",
        "system: ApplicationData",
        "system: LocalApplicationData",
        "system: InternetCache",
        "system: CommonApplicationData",
        "system: Windows",
        "system: ProgramFiles",
        "system: ProgramFilesX86"
    };

    /// <summary>
    /// Layout arrangements converting image layout numbers to the number of columns and rows
    /// of images visible on canvas.
    /// </summary>
    public static readonly (int Columns, int Rows)[] LayoutArrangements = new[]
        { (1,1), (2,1), (3,1), (2,2), (3,2), (4,2), (5,2), (6,2), (5,3), (6,3), (7,3), (8,3) };

    /// <summary>
    /// Adjusts the layout to make room for images changing from portrait to landscape with preserving the image scale.
    /// </summary>
    public static int AdjustLayout(int layout) =>
        layout switch { 3 => 4, 7 => 6, 8 => 7, 11 => 10, 12 => 11, _ => layout };

    /// <summary>
    /// Limits this <paramref name="layout"/> number so that it does not exceed the allowed boundaries.
    /// </summary>
    public static int LimitLayout(this int layout) => layout.Limit(2, LayoutArrangements.Length);

    /// <summary>
    /// Limits this scaling <paramref name="zoom"/> value so that it does not exceed the allowed boundaries.
    /// </summary>
    public static double LimitScalingZoom(this double zoom) =>
        zoom.Limit(1.0 / MaxScalingWhenZoomed, MaxScalingWhenZoomed);

    /// <summary>
    /// When increasing or decreasing <see cref="ImageBrowserState.ExtraZoom"/>,
    /// this constant is used as the multiplier or divisor.
    /// </summary>
    public static double GetZoomStepFactor(bool direction) => direction ? ZoomStepFactor : 1 / ZoomStepFactor;
}