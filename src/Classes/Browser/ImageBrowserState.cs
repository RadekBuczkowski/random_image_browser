namespace Random_Image.Classes.Browser;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;
using Random_Image.Properties;

/// <summary>
/// Implements the global state of the image browser.
/// </summary>
public class ImageBrowserState
{
    /// <summary>
    /// Returns the count of image files found when scanning image folders and available to load.
    /// </summary>
    public int FileCount { get; private set; } = 0;

    /// <summary>
    /// Index of the top left image visible on screen (the corresponding image has this value in
    /// <see cref="Image.Tag().Index"/>).
    /// </summary>
    public int FirstImageIndex { get; protected set; } = -1;  // Start an animation scrolling to the first page.

    /// <summary>
    /// Number of images visible on screen.
    /// </summary>
    public int ImagesOnCanvas { get; private set; }

    /// <summary>
    /// Number of images visible on canvas horizontally.
    /// </summary>
    public int CanvasColumns { get; private set; }

    /// <summary>
    /// Number of images visible on canvas vertically.
    /// </summary>
    public int CanvasRows { get; private set; }

    /// <summary>
    /// A saved value of ImagesOnCanvas from before an image was clicked and zoomed (<see cref="Reasons.GoIn"/>).
    /// </summary>
    public int ImagesOnCanvasBeforeZoomed { get; private set; }

    /// <summary>
    /// A saved value of CanvasColumns from before an image was clicked and zoomed (<see cref="Reasons.GoIn"/>).
    /// </summary>
    public int CanvasColumnsBeforeZoomed { get; private set; }

    /// <summary>
    /// Position of the image on canvas that was selected and zoomed (the top-most and left-most image has position 0).
    /// </summary>
    public int SelectedPositionOnCanvas { get; private set; }

    /// <summary>
    /// An image was clicked and zoomed in (see <see cref="Reasons.GoIn"/>).
    /// </summary>
    public bool IsZoomed { get; private set; } = false;

    /// <summary>
    /// Extra zoom factor which is additionally applied on top of the active auto-scaling.
    /// Used only when <see cref="IsZoomed"/> == <see langword="true"/>.
    /// </summary>
    public double ExtraZoom { get; protected set; } = DefaultExtraZoom;

    /// <summary>
    /// Type of auto-scaling to fit images to their canvas slots.
    /// Used only when <see cref="IsZoomed"/> == <see langword="false"/>.
    /// </summary>
    public AutoScalingModes Fit { get; protected set; } = DefaultFit;

    /// <summary>
    /// Type of auto-scaling to fit images to the entire canvas.
    /// Used only when <see cref="IsZoomed"/> == <see langword="true"/>
    /// </summary>
    public AutoScalingModes FitWhenZoomed { get; protected set; } = DefaultFitWhenZoomed;

    /// <summary>
    /// The arrangement and number of images visible on canvas. It is an integer between 2..12
    /// corresponding to 2..24 images.
    /// </summary>
    public int Layout { get; private set; } = DefaultLayout;

    /// <summary>
    /// Rotation angle in degrees: 0, 90, 180, -90.
    /// </summary>
    public int Angle { get; protected set; } = 0;

    /// <summary>
    /// <see langword="true"/> if images are mirrored horizontally.
    /// </summary>
    public bool IsMirror { get; protected set; } = false;

    /// <summary>
    /// When Angle is set to 90 or -90, make all images portrait (vertical) by rotating only landscape images,
    /// or if Portrait == false, make all images landscape (horizontal) by rotating only portrait images.
    /// </summary>
    public bool IsPortrait { get; protected set; } = true;

    /// <summary>
    /// <see langword="true"/> for black window background, <see langword="false"/> for white.
    /// Default value specified in the app.config file in "IsBlackBackground".
    /// </summary>
    public bool IsBlackBackground { get; protected set; } = Settings.Default.IsBlackBackground;

    /// <summary>
    /// <see langword="true"/> if the application window is in full-screen mode, or <see langword="false"/>
    /// if in normal window mode. Default value specified in the app.config file in "IsFullScreen".
    /// </summary>
    public bool IsFullScreen { get; protected set; } = Settings.Default.IsFullScreen;

    /// <summary>
    /// <see langword="true"/> if animations are enabled.
    /// Default value specified in the app.config file in "EnableAnimations".
    /// </summary>
    public bool EnableAnimations { get; protected set; } = DefaultEnableAnimations;

    /// <summary>
    /// <see langword="true"/> if images should be grouped by similar, consecutive file names
    /// and only the groups are to be shuffled. Files in each group are ordered alphabetically.
    /// If <see langword="false"/>, all files are in random order.
    /// Default value specified in the app.config file in "GroupImages".
    /// </summary>
    public bool GroupImages { get; protected set; } = Settings.Default.GroupImages;

    /// <summary>
    /// Serialized position, width and height of the main window to preserve the window location
    /// and dimensions after a restart.
    /// </summary>
    public string SerializedWindowRectangle { get; protected set; } = string.Empty;

    /// <summary>
    /// Name of temporary JSON file containing a serialized image cache to preserve scanned files
    /// and their order after a restart.
    /// </summary>
    public string SerializedCacheFile { get; protected set; } = string.Empty;

    /// <summary>
    /// Restarts the application after the specified number of images to solve the problem of stuttering animations.
    /// 0 means disabled.
    /// </summary>
    public int AutoRestartEveryNImages { get; set; } = Settings.Default.AutoRestartEveryNImages;

    /// <summary>
    /// Threshold for both width and height in pixels of images classified as large images (e.g. larger than FHD).
    /// Images larger than this will use low quality bitmap scaling algorithm to improve performance.
    /// </summary>
    public static int LargeImagePixelDimension => Settings.Default.LargeImagePixelDimension;

    /// <summary>
    /// Images with either width or height larger than this value (e.g. larger than 4K) will have bitmaps scaled down
    /// when loading to improve performance.
    /// </summary>
    public static int MaxImagePixelDimension { get; set; } = Settings.Default.MaxImagePixelDimension;

    /// <summary>
    /// The desired frame rate for all animations (WPF default is 60, but animations are performance heavy).
    /// </summary>
    public static int DesiredFrameRate { get; set; } = Settings.Default.DesiredFrameRate;

    /// <summary>
    /// The language of the application. If empty, the language should be taken from the operating system.
    /// </summary>
    public static string Language => Settings.Default.Language;

    /// <summary>
    /// <see langword="true"/> if several images are presented on canvas and they can be aligned with the mouse.
    /// </summary>
    public bool CanAlignMany => Fit == AutoScalingModes.InsideEdges && IsZoomed == false;

    /// <summary>
    /// <see langword="true"/> if several images are presented on canvas as thumbnails with rounded corners.
    /// </summary>
    public bool HasRoundedCorners => Fit == AutoScalingModes.Thumbnails && IsZoomed == false;

    /// <summary>
    /// <see langword="true"/> if images are zoomed and shown with the original size and unscaled.
    /// </summary>
    public bool UseOriginalSize => FitWhenZoomed == AutoScalingModes.OriginalSize && IsZoomed;

    /// <summary>
    /// <see langword="true"/> if images fit to inside edges both when zoomed and not zoomed.
    /// </summary>
    public bool FitInsideEdges => Fit != AutoScalingModes.OutsideEdges && IsZoomed == false ||
                                  FitWhenZoomed == AutoScalingModes.InsideEdges && IsZoomed;

    /// <summary>
    /// <see langword="true"/> if images need to be shown with an animated shadow, both when zoomed and not zoomed.
    /// </summary>
    public bool HasShadow => Fit == AutoScalingModes.OutsideEdges;

    /// <summary>
    /// The margin between between two adjacent canvas slots with images.
    /// The canvas slots have a small empty space between them.
    /// </summary>
    public double SlotMargin => (ImagesOnCanvas == 1) ? 0 : (Fit == AutoScalingModes.OutsideEdges) ? 7 : 4;

    /// <summary>
    /// Image folders as specified in the app.config file in "ImageFolder1", "ImageFolder2" and "ImageFolder3".
    /// </summary>
    public string[] ImageFolders { get; private set; } = DefaultImageFolders;

    /// <summary>
    /// Supported image file extensions as specified in the app.config file in "ImageExtensions".
    /// </summary>
    public string[] ImageExtensions { get; private set; } =
        Settings.Default.ImageExtensions.Replace(" ", string.Empty).ToLower().Split(new char[] { ',', ';' });

    /// <summary>
    /// Default values for image folders as specified in the app.config file in "Folder1", "Folder2", and "Folder3".
    /// </summary>
    public static string[] DefaultImageFolders => new string[]
        { Settings.Default.ImageFolder1, Settings.Default.ImageFolder2, Settings.Default.ImageFolder3 };

    /// <summary>
    /// Default value for Layout as specified in the app.config file in "Layout".
    /// </summary>
    public static int DefaultLayout => ((int)Settings.Default.Layout).LimitLayout();

    /// <summary>
    /// Default value for auto-scaling as specified in the app.config file in "AutoScaling".
    /// </summary>
    private static AutoScalingModes DefaultFit => (AutoScalingModes)((int)Settings.Default.AutoScaling).Limit(1, 3);

    /// <summary>
    /// Default value for auto-scaling when zoomed as specified in the app.config file in "AutoScalingWhenZoomed".
    /// </summary>
    private static AutoScalingModes DefaultFitWhenZoomed =>
        (AutoScalingModes)((int)Settings.Default.AutoScalingWhenZoomed).Limit(1, 3);

    /// <summary>
    /// Default value for ExtraZoom as specified in the app.config file in "ExtraZoom".
    /// </summary>
    private static double DefaultExtraZoom => Settings.Default.ExtraZoom.LimitScalingZoom();

    /// <summary>
    /// Default value for animations enabled in the app.config file in "EnableAnimations".
    /// </summary>
    public static bool DefaultEnableAnimations => Settings.Default.EnableAnimations;

    /// <summary>
    /// <see langword="true"/> if the layout or auto-scaling have been changed since the program started
    /// or image rotation or mirror have been applied.
    /// </summary>
    public bool IsNonDefault => IsNonDefaultLayout || IsNonDefaultAutoScaling || IsNonDefaultOrientation;

    /// <summary>
    /// <see langword="true"/> if the layout has been changed since the program started.
    /// </summary>
    public bool IsNonDefaultLayout => Layout != DefaultLayout;

    /// <summary>
    /// <see langword="true"/> if auto-scaling has been changed since the program started.
    /// </summary>
    public bool IsNonDefaultAutoScaling => Fit != DefaultFit ||
        FitWhenZoomed != DefaultFitWhenZoomed || ExtraZoom != DefaultExtraZoom;

    /// <summary>
    /// <see langword="true"/> if an image rotation or mirror has been applied.
    /// </summary>
    public bool IsNonDefaultOrientation => Angle != 0 || IsPortrait == false || IsMirror;

    /// <summary>
    /// The navigation delta to go to the first image (image with index 0).
    /// </summary>
    public int DeltaHome => -FirstImageIndex;

    /// <summary>
    /// The navigation delta to go to the last ever shown image on screen.
    /// </summary>
    private int DeltaEnd => DeltaHome + _firstUnseenImageIndex - ImagesOnCanvas;

    /// <summary>
    /// The navigation delta to go to an image with a random index.
    /// </summary>
    public int DeltaRandom => DeltaHome + Rand.Next(MaximumFirstImageIndex + 1);

    /// <summary>
    /// Allows removing expired images from canvas if the image count exceeds this limit.
    /// </summary>
    public int MaximumImagesLoaded => (IsZoomed ? ImagesOnCanvasBeforeZoomed : ImagesOnCanvas) *
        (ImageBrowserConstants.ExpireAfterPage + 1) + (IsZoomed ? CanvasColumnsBeforeZoomed : CanvasColumns);

    /// <summary>
    /// Returns the highest allowed value for the FirstImageIndex property.
    /// </summary>
    public int MaximumFirstImageIndex =>
        (FileCount.MakeEvenlyDivisible(CanvasColumns) - ImagesOnCanvas).Limit(0, int.MaxValue);

    /// <summary>
    /// The common random number generator.
    /// </summary>
    public Random Rand { get; } = new();

    /// <summary>
    /// The previous arrangement of images on canvas from before a rotation was applied.
    /// Allows restoring the original layout.
    /// </summary>
    protected int _previousLayout = DefaultLayout;

    /// <summary>
    /// The first index of images never screen on screen
    /// (the corresponding image has the same value in <see cref="ImageTag.Index"/>).
    /// </summary>
    protected int _firstUnseenImageIndex = 0;

    /// <summary>
    /// A saved value of <see cref="FirstImageIndex"/> from before an image was clicked and zoomed
    /// (<see cref="Reasons.GoIn"/>).
    /// </summary>
    private int _firstImageIndexBeforeZoomed = 0;

    /// <summary>
    /// Position pointed by the mouse wheel. Enables using mice with high-precision wheels.
    /// </summary>
    private int _wheelPosition = 0;

    /// <summary>
    /// Number of recent wheel activations and their times.
    /// </summary>
    private readonly Queue<long> _wheelActivations = new();

    /// <summary>
    /// Last time when a navigation key was pressed. We use it to prevent scrolling the content too fast.
    /// </summary>
    private long _navigationTime = 0;

    /// <summary>
    /// Last time when the extra zoom was changed. We use it to prevent crossing the 100% threshold too fast
    /// when zooming in or out.
    /// </summary>
    private long _extraZoomResetTime = 0;

    /// <summary>
    /// Initializes properties of the <see cref="ImageBrowserState"/> object after scanning folders for images.
    /// </summary>
    public void Initialize(int fileCount, bool keepImageIndex = false)
    {
        FileCount = fileCount;
        IsZoomed = false;
        if (keepImageIndex == false)
        {
            FirstImageIndex = -1;
            _firstUnseenImageIndex = 0;
        }
    }

    /// <summary>
    /// Changes image folders and optionally extensions.
    /// </summary>
    public void SetImageFolders(string[] folders, string[] extensions = null)
    {
        if (folders != null)
            ImageFolders = folders;
        if (extensions != null)
            ImageExtensions = extensions;
    }

    /// <summary>
    /// Sets browser state parameters handled outside of the <see cref="ImageBrowserState"/> class.
    /// </summary>
    public void AppendBrowserState(ImageBrowserCanvas canvas, bool enableAnimations, string cacheFile)
    {
        IsBlackBackground = canvas.IsBlackBackground;
        IsFullScreen = canvas.Window.IsFullScreen();
        EnableAnimations = enableAnimations;
        SerializedWindowRectangle = canvas.Window.SerializeWindowRectangle();
        SerializedCacheFile = cacheFile;
    }

    /// <summary>
    /// Restores the window size and location and window background to the state
    /// set by the <see cref="AppendBrowserState"/> method or specified in the app.config file.
    /// </summary>
    public void RestoreWindow(Window window)
    {
        if (IsFullScreen)
            window.ToggleFullScreen();
        else
            window.ApplyWindowRectangle(SerializedWindowRectangle);
        if (IsBlackBackground)
            window.Background = Brushes.Black;
    }

    /// <summary>
    /// Toggles the value of the <see cref="GroupImages"/> property (which specifies if images are grouped or not).
    /// </summary>
    public void ToggleGroupingImages()
    {
        GroupImages = !GroupImages;
    }

    /// <summary>
    /// Applies the specified image <paramref name="layout"/> defining the number of image columns and rows on canvas.
    /// </summary>
    public bool SetLayout(int layout)
    {
        if (layout < 2 || layout > ImageBrowserConstants.LayoutArrangements.Length)
            return false;
        Layout = layout;
        (int columns, int rows) = ImageBrowserConstants.LayoutArrangements[layout - 1];
        ImagesOnCanvas = columns * rows;
        CanvasColumns = columns;
        CanvasRows = rows;
        return true;
    }

    /// <summary>
    /// Applies the specified rotation and/or mirror effects.
    /// </summary>
    public int Rotate(int angle, bool isPortrait, bool isMirror)
    {
        bool previous = IsPortrait;
        Angle = angle;
        IsMirror = isMirror;
        IsPortrait = isPortrait;
        int layout = Layout;
        if (isPortrait != previous && IsZoomed == false)
        {
            if (isPortrait == false)
            {
                _previousLayout = layout;
                layout = ImageBrowserConstants.AdjustLayout(layout);
            }
            else if (layout == ImageBrowserConstants.AdjustLayout(_previousLayout))
                layout = _previousLayout;
        }
        return layout;
    }

    /// <summary>
    /// Returns indexes of images visible on canvas from top left to bottom right. Extra pages preceding (i.e. &lt; 0)
    /// or following (i.e. &gt; 0) the visible images can be included as well to cache images in either direction.
    /// </summary>
    public IEnumerable<int> GetAvailableImageIndexes(int extraPages = 0)
    {
        int first = FirstImageIndex;
        int last = first + ImagesOnCanvas - 1;
        if (extraPages < 0)
            first += ImagesOnCanvas * extraPages;
        else if (extraPages > 0)
            last += ImagesOnCanvas * extraPages;
        first = first.Limit(0, FileCount - 1);
        last = last.Limit(0, FileCount - 1);
        for (int index = first; index <= last; index++)
            yield return index;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the image with the specified index is either visible on canvas or is within
    /// the specified extra rows either preceding (i.e. &lt; 0) or following (i.e. &gt; 0) all visible images.
    /// </summary>
    public bool IsImageVisible(int index, int extraRows = 0)
    {
        if (IsImageExpired(index))
            return false;
        return index >= FirstImageIndex + CanvasColumns * ((extraRows < 0) ? extraRows : 0)
            && index < FirstImageIndex + ImagesOnCanvas + CanvasColumns * ((extraRows > 0) ? extraRows : 0);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the image with the specified index is either visible on canvas or would
    /// be visible when zoomed out (<see cref="Reasons.GoBack"/>). The specified extra rows are either preceding
    /// (i.e. &lt; 0) or following (i.e. &gt; 0) all visible images.
    /// </summary>
    public bool IsImageVisibleBeforeZoomed(int index, int extraRows = 0)
    {
        if (IsZoomed == false)
            return IsImageVisible(index, extraRows);
        return index >= _firstImageIndexBeforeZoomed + CanvasColumnsBeforeZoomed * ((extraRows < 0) ? extraRows : 0)
            && index < _firstImageIndexBeforeZoomed + ImagesOnCanvasBeforeZoomed
                + CanvasColumnsBeforeZoomed * ((extraRows > 0) ? extraRows : 0);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the image index is several pages away from visible images
    /// in either direction, and the image can be safely recycled or removed from canvas.
    /// </summary>
    public bool IsImageExpired(int index)
    {
        int count = IsZoomed ? ImagesOnCanvasBeforeZoomed : ImagesOnCanvas;
        int pages = ImageBrowserConstants.ExpireAfterPage;
        return index < FirstImageIndex - count * pages || index >= FirstImageIndex + count * (pages + 1);
    }

    /// <summary>
    /// Prevents from scrolling too fast when turning the mouse wheel with a free spin (e.g. Logitech mice)
    /// Note: unless highResolutionScrollingAware or ultraHighResolutionScrollingAware in app.manifest is true, 
    /// the delta value returned by Windows is always 120 or -120.
    /// </summary>
    public int GetCompletedWheelScrolls(int delta, int deltaThreshold = 120)
    {
        if (deltaThreshold > 0)
        {
            _wheelPosition += delta;
            int lines = _wheelPosition / deltaThreshold;
            if (lines != 0)
            {
                _wheelPosition = 0;
                int speedUpTime = (_wheelActivations.CountRunningTimers(300) - 3) * 20;
                int delay = speedUpTime switch { < 0 => 0, > 300 => 100, _ => 400 - speedUpTime };
                if (SimpleTimer.IsCycleCompleted(ref _navigationTime, delay))
                    return lines;
            }
        }
        return 0;
    }

    /// <summary>
    /// Returns the navigation delta based on the pressed key. Ignores the key if it is pressed too quickly.
    /// </summary>
    public int GetNavigationDelta(Key key, int minimumDelay = 0)
    {
        if (minimumDelay > 0 && SimpleTimer.IsCycleCompleted(ref _navigationTime, minimumDelay) == false)
            return 0;
        if (FirstImageIndex < 0)
            return 0;
        return (key switch
        {
            Key.Left => -1,
            Key.Right => 1,
            Key.Up => -CanvasColumns,
            Key.Down => CanvasColumns,
            Key.PageUp => -ImagesOnCanvas,
            Key.PageDown => ImagesOnCanvas,
            Key.Home => DeltaHome,
            Key.End => DeltaEnd,
            _ => 0
        }).Limit(DeltaHome, DeltaHome + MaximumFirstImageIndex);
    }

    /// <summary>
    /// Navigates to a different image by the specified delta. Makes sure the delta doesn't exceed valid boundaries,
    /// and adds the delta to the <see cref="FirstImageIndex"/>.
    /// </summary>
    public int Navigate(int delta)
    {
        if (FirstImageIndex < 0)  // Initialize.
        {
            FirstImageIndex = -ImagesOnCanvas;  // Animate loading the first page.
            delta = ImagesOnCanvas;
        }
        delta = delta.Limit(DeltaHome, DeltaHome + MaximumFirstImageIndex);
        FirstImageIndex += delta;
        _firstUnseenImageIndex = _firstUnseenImageIndex.Limit(FirstImageIndex + ImagesOnCanvas, int.MaxValue);
        return delta;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the specified navigation <paramref name="delta"/> has an effect,
    /// i.e. it is not exceeding the boundaries. If delta is zero, it is accepted because zero is a special case 
    /// used to reload images.
    /// </summary>
    public bool HasEffect(int delta)
    {
        return delta == 0 || delta.Limit(DeltaHome, DeltaHome + MaximumFirstImageIndex) != 0;
    }

    /// <summary>
    /// Zooms in an image when clicked (<see cref="Reasons.GoIn"/>).
    /// </summary>
    public void GoIn(int index)
    {
        if (IsZoomed == false)
        {
            // If the image was clicked during a navigation animation and is disappearing from the screen,
            // make it visible again.
            if (IsImageVisible(index) == false)
            {
                int delta = index - FirstImageIndex;
                if (delta > 0)
                    delta -= ImagesOnCanvas - 1;
                Navigate(delta.MakeEvenlyDivisible(CanvasColumns));
            }
            IsZoomed = true;
            _firstImageIndexBeforeZoomed = FirstImageIndex;
            if (FitWhenZoomed != AutoScalingModes.OutsideEdges)
                ExtraZoom = 1.0;
            ImagesOnCanvasBeforeZoomed = ImagesOnCanvas;
            CanvasColumnsBeforeZoomed = CanvasColumns;
            ImagesOnCanvas = CanvasColumns = CanvasRows = 1;
            SelectedPositionOnCanvas = index - FirstImageIndex;
        }
    }

    /// <summary>
    /// Goes back from zooming an image (<see cref="Reasons.GoBack"/>).
    /// </summary>
    public void GoBack()
    {
        if (IsZoomed)
        {
            IsZoomed = false;
            int index = FirstImageIndex;
            SetLayout(Layout);
            FirstImageIndex = FirstImageIndex / CanvasColumns * CanvasColumns;
            if (index >= _firstImageIndexBeforeZoomed)
            {
                if (index < _firstImageIndexBeforeZoomed + ImagesOnCanvas)
                    FirstImageIndex = _firstImageIndexBeforeZoomed;
                else
                    Navigate(CanvasColumns - ImagesOnCanvas);
            }
            SelectedPositionOnCanvas = index - FirstImageIndex;
        }
    }

    /// <summary>
    /// Changes the auto-scaling (inside edges, outside edges, etc.), either in the zoomed or un-zoomed mode.
    /// If <paramref name="restore"/> is set, restores scaling properties from default settings in the app.config file.
    /// </summary>
    public void ToggleAutoScaling(bool restore = false)
    {
        static AutoScalingModes NextFitMode(AutoScalingModes fit) => (AutoScalingModes)((int)fit % 3 + 1);

        if (restore)
        {
            Fit = DefaultFit;
            FitWhenZoomed = DefaultFitWhenZoomed;
            ExtraZoom = DefaultExtraZoom;
        }
        else if (IsZoomed)
        {
            FitWhenZoomed = NextFitMode(FitWhenZoomed);
            ExtraZoom = (FitWhenZoomed == AutoScalingModes.OutsideEdges) ? DefaultExtraZoom : 1.0;
        }
        else
            Fit = NextFitMode(Fit);
    }

    /// <summary>
    /// Sets the auto-scaling mode.
    /// </summary>
    public void SetAutoScaling(AutoScalingModes mode, bool isZoomed)
    {
        if (isZoomed)
        {
            FitWhenZoomed = mode;
            ExtraZoom = (FitWhenZoomed == AutoScalingModes.OutsideEdges) ? DefaultExtraZoom : 1.0;
        }
        else
            Fit = mode;
    }

    /// <summary>
    /// Resets the extra zoom to disable extra zooming.
    /// </summary>
    public void ResetExtraZoom() => ExtraZoom = 1.0;

    /// <summary>
    /// Applies extra zoom (enlarging or shrinking) on the image when in the zoomed mode (<see cref="Reasons.GoIn"/>).
    /// Returns <see langword="false"/> if the scaling hasn't changed.
    /// </summary>
    public bool ApplyExtraZoom(bool direction)
    {
        if (IsZoomed && SimpleTimer.IsTimerRunning(ref _extraZoomResetTime, SimpleTimer.BlockMouseWheelTime) == false)
        {
            double factor = ImageBrowserConstants.GetZoomStepFactor(direction);
            double zoom = (ExtraZoom * factor).LimitScalingZoom();
            if (ExtraZoom != 1.0 && zoom * factor > 1 != ExtraZoom > 1)
            {
                zoom = 1.0;  // 100% was reached and going further should be delayed a bit
                SimpleTimer.StartTimer(ref _extraZoomResetTime);
            }
            bool hasChanged = ExtraZoom != zoom;
            ExtraZoom = zoom;
            return hasChanged;
        }
        return false;
    }
}