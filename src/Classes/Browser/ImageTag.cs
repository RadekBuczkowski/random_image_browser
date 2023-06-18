namespace Random_Image.Classes.Browser;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Windows;

using Random_Image.Classes.Cache;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Resources;

/// <summary>
/// Provides parameters of a single image and methods transforming the image parameters to the current browser state.
/// </summary>
public class ImageTag : ImageCacheTag
{
    /// <summary>
    /// The horizontal and vertical scaling factor (e.g. 100%, 125%, 175%, 200%, etc.) from Display Settings
    /// in Windows. Must be set externally via the <see cref="SetWindowScaling"/> method when starting the program.
    /// </summary>
    private static Vector WindowScaling = new(1, 1);

    /// <summary>
    /// The file path to the image file. Supported image file extensions are specified in "ImageExtensions".
    /// in the app.config file.
    /// </summary>
    public string FilePath { get; } = string.Empty;

    /// <summary>
    /// <see langword="true"/> if the image is animated, i.e. it has more than one bitmap frame
    /// (only GIF images support this).
    /// </summary>
    public bool IsAnimated { get; private set; }

    /// <summary>
    /// <see langword="true"/> if the image has been successfully loaded from the image file.
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Contains a possible loading exception.
    /// </summary>
    public Exception LoadingException { get; private set; }

    /// <summary>
    /// <see langword="true"/> if the image has been successfully loaded from file, resized and positioned on canvas.
    /// </summary>
    public bool IsReady { get; private set; }

    /// <summary>
    /// The image is zoomed in (see <see cref="Reasons.GoIn"/>).
    /// </summary>
    public bool IsZoomed { get; private set; }

    /// <summary>
    /// A cloned copy of this <see cref="ImageTag"/> before resizing. Contains the previous image state.
    /// Used for the purpose of animating the image state transition.
    /// </summary>
    public ImageTag Previous { get; private set; }

    /// <summary>
    /// The position among visible images on canvas (0 = the top-most and left-most image on canvas).
    /// </summary>
    public int PositionOnCanvas { get; private set; }

    /// <summary>
    /// The image occupies the entire canvas or canvas slot leaving no space for shadow.
    /// </summary>
    public bool OccupiesWholeArea { get; private set; }

    /// <summary>
    /// The image rotation angle in degrees (only 0, 90, -90, or 180).
    /// </summary>
    public int Angle { get; private set; }

    /// <summary>
    /// The image is a mirror reflection.
    /// </summary>
    public bool IsMirror { get; private set; }

    /// <summary>
    /// The size of the image file in bytes, or -1 if the file size couldn't be retrieved.
    /// </summary>
    public long FileSize { get; private set; } = -1;

    /// <summary>
    /// The time (<see cref="DateTime.Ticks"/>) of the last write to the image file,
    /// or -1 if the time couldn't be retrieved.
    /// </summary>
    public long FileTime { get; private set; } = -1;

    /// <summary>
    /// The width and height of the image bitmap.
    /// </summary>
    public Size BitmapSize { get; private set; }

    /// <summary>
    /// The width and height of the image bitmap in pixels.
    /// Note: all dimensions used in this class and elsewhere in the application are device-scaled
    /// (i.e. device independent), unless they have "Pixel" in the name.
    /// </summary>
    public Size BitmapPixelSize { get; private set; }

    /// <summary>
    /// The image width and height in pixels as it is in the image file. Note: very large images are scaled down
    /// automatically when loaded to improve performance
    /// (see the <see cref="ImageBrowserState.MaxImagePixelDimension"/> setting defining the threshold).
    /// The <see cref="FilePixelSize"/> property can therefore be larger than
    /// the <see cref="BitmapPixelSize"/> property.
    /// 
    /// </summary>
    public Size FilePixelSize { get; private set; }

    /// <summary>
    /// The location on canvas and the current size of the scaled image as it appears on screen.
    /// </summary>
    public Rect ImageRectangle { get; private set; }

    /// <summary>
    /// Cropping rectangle when the image is larger than its corresponding slot on canvas and needs
    /// to be cropped to fit. Used with auto-scaling modes resulting in the image being larger than canvas slot.
    /// </summary>
    public Rect CropRectangle { get; private set; }

    /// <summary>
    /// The canvas slot where the image has to fit on screen. The slot is usually slightly larger than the image
    /// rectangle because images preserve the aspect ratio when scaling.
    /// </summary>
    public Rect CanvasRectangle { get; private set; }

    /// <summary>
    /// The current alignment of the image when it is larger than canvas or canvas slot.
    /// (0, 0) indicates the center of the canvas slot and (-0.5, -0.5) and (0.5, 0.5) are its two opposite corners.
    /// This property can be aligned with the mouse.
    /// </summary>
    public Vector Alignment { get; private set; }

    /// <summary>
    /// Radius in degrees (e.g. 45 degrees) of rounded corners in the cropping rectangle, or 0 if disabled.
    /// Used to show thumbnails with rounded corners.
    /// </summary>
    public double Radius { get; private set; }

    /// <summary>
    /// The reason for resizing the image.
    /// </summary>
    public Reasons Reason { get; private set; }

    /// <summary>
    /// Cropping rectangle with the same scaling as <see cref="ImageRectangle"/> but one that would not crop
    /// any part of the image.
    /// </summary>
    public Rect NoCropRectangle => new(ImageRectangle.Size);

    /// <summary>
    /// <see langword="true"/> if the image is taller than wider (portrait) or <see langword="false"/>
    /// if wider than taller (landscape).
    /// </summary>
    public bool IsPortrait => BitmapSize.Width <= BitmapSize.Height;

    /// <summary>
    /// <see langword="true"/> if the image is aligned to the center of the canvas slot.
    /// </summary>
    public bool IsCentered => Math.Abs(Alignment.X) < 0.0001 && Math.Abs(Alignment.Y) < 0.0001;

    /// <summary>
    /// <see langword="true"/> if the image is larger than the canvas or its corresponding slot on canvas
    /// and can be aligned with the mouse.
    /// </summary>
    public bool IsMovable => _movableHorizontally || _movableVertically;

    /// <summary>
    /// <see langword="true"/> if the image bitmap dimensions can be classified as a large image
    /// (e.g. larger than FHD).
    /// </summary>
    public bool IsLarge => BitmapPixelSize.Width >= ImageBrowserState.LargeImagePixelDimension ||
                           BitmapPixelSize.Height >= ImageBrowserState.LargeImagePixelDimension;

    /// <summary>
    /// <see langword="true"/> if the recent change in the <see cref="Alignment"/> property is too big (more than 25%)
    /// and the alignment transition should be animated.
    /// </summary>
    public bool IsAlignmentJump => Math.Abs((Alignment - Previous?.Alignment ?? ZeroVector).Length) > 0.25;

    /// <summary>
    /// <see langword="true"/> if the image format can potentially support animations - only GIF images supported.
    /// </summary>
    private bool IsPotentiallyAnimated => FilePath.ToLower().EndsWith(".gif");

    /// <summary>
    /// Vector of zero size.
    /// </summary>
    private static readonly Vector ZeroVector = new(0, 0);

    /// <summary>
    /// Total scaling factor of the image bitmap depending on the auto-scaling mode, zoomed mode, and the extra zoom.
    /// A scaling less than 1 means the image is shrunk, greater than 1 means enlarged, and 1 exactly means
    /// the unscaled bitmap size.
    /// </summary>
    private double _scaling = 1;

    /// <summary>
    /// The scaling factor of the loaded image bitmap with respect to the original image dimensions in the file.
    /// The value either equals 1 for regular images or is greater than 1 for very large images.
    /// Note: very large images can be scaled down when loaded to improve performance.
    /// </summary>
    private double _preScaling = 1;

    /// <summary>
    /// The image is horizontally larger than the canvas or its slot on canvas and can be aligned with the mouse.
    /// </summary>
    private bool _movableHorizontally = false;

    /// <summary>
    /// The image is vertically larger than the canvas or its slot on canvas and can be aligned with the mouse.
    /// </summary>
    private bool _movableVertically = false;

    /// <summary>
    /// Prevents opening IrfanView more than once at the same time.
    /// </summary>
    private long _startIrfanViewTime = 0;

    public ImageTag() : base()
    {
    }

    public ImageTag(ImageCacheTag tag, string filePath) : this()
    {
        CopyFrom(tag);
        FilePath = filePath;
    }

    /// <summary>
    /// Makes a clone copy of the entire <see cref="ImageTag"/> object and saves it in the <see cref="Previous"/>
    /// property. Note: members of this class are structs and primitive types, so this is in fact a deep clone.
    /// </summary>
    private void CloneToPrevious()
    {
        Previous = null;  // do not keep older clones
        Previous = (ImageTag)MemberwiseClone();
        Previous.LoadingException = null;
    }

    /// <summary>
    /// Sets the horizontal and vertical scaling factor (e.g. 100%, 125%, 175%, etc.) from Display Settings in Windows.
    /// </summary>
    public static void SetWindowScaling(Vector scaling)
    {
        WindowScaling = scaling;
    }

    /// <summary>
    /// Updates the image bitmap size and details of the image file after loading the image from cache.
    /// </summary>
    public void SetBitmapSize(ImageCacheBitmap result)
    {
        IsAnimated = result.IsAnimated;
        IsLoaded = result.IsLoaded;
        LoadingException = result.LoadingException;
        FileSize = result.FileSize;
        FileTime = result.FileTime;
        BitmapSize = new(result.Bitmap.PixelWidth / WindowScaling.X, result.Bitmap.PixelHeight / WindowScaling.Y);
        BitmapPixelSize = new(result.Bitmap.PixelWidth, result.Bitmap.PixelHeight);
        FilePixelSize = BitmapPixelSize;
        _preScaling = 1;
        if (result.FilePixelWidth > 0 && result.FilePixelHeight > 0)
        {
            FilePixelSize = new(result.FilePixelWidth, result.FilePixelHeight);
            _preScaling = FilePixelSize.Width / BitmapPixelSize.Width;
        }
    }

    /// <summary>
    /// Resizes the image and populates most of the properties in this class.
    /// </summary>
    /// <param name="state">The global state of the image browser</param>
    /// <param name="canvasSize">The width and height of the whole canvas where all images are painted</param>
    /// <param name="mouse">The position of the mouse cursor relative to the top-left corner of the whole canvas</param>
    /// <param name="offset">Any remaining alignment offset from a canceled animation that needs to be kept</param>
    /// <param name="reason">The reason for the resize</param>
    public void Resize(ImageBrowserState state, Size canvasSize, Point mouse, Vector offset, Reasons reason)
    {
        CloneToPrevious();
        // Note: properties in this class need to be calculated and set in the right order
        // because of their interdependencies. To make the dependencies easier to see, all methods
        // called from here are static and the dependent properties are specified explicitly!
        Reason = reason;
        PositionOnCanvas = Index - state.FirstImageIndex;
        CanvasRectangle = GetCanvasRectangleValue(state, canvasSize, PositionOnCanvas, state.SlotMargin, reason);
        bool previousIsMirror = IsMirror;
        int previousAngle = Angle;
        IsMirror = state.IsMirror ^ state.Angle == 180;
        Angle = (IsPortrait != state.IsPortrait || state.Angle == 180) ? state.Angle : 0;
        _scaling = GetScalingValue(state, CanvasRectangle, BitmapSize, _preScaling, Angle, IsLoaded);
        IsZoomed = state.IsZoomed;
        Size imageSize = (Size)((Vector)BitmapSize * _scaling);
        Vector slack = (Vector)imageSize.SetOrientation(Angle) - (Vector)CanvasRectangle.Size;
        Size slackSize = new((slack.X > 1) ? slack.X : 0, (slack.Y > 1) ? slack.Y : 0);
        OccupiesWholeArea = slack.X > -1 && slack.Y > -1;
        _movableHorizontally = slack.X > 1;
        _movableVertically = slack.Y > 1;
        bool isMovable = _movableHorizontally || _movableVertically;
        bool isTransformed = IsMirror != previousIsMirror || Angle != previousAngle;
        Alignment = GetRemainingAnimationAlignmentValue(Alignment, offset, slackSize);
        Alignment = GetMouseAlignmentValue(Alignment, state, mouse, CanvasRectangle, isMovable, isTransformed, reason);
        ImageRectangle = GetImageRectangleValue(CanvasRectangle, imageSize, Alignment, slackSize);
        CropRectangle = GetCropRectangleValue(CanvasRectangle.Size, Angle, IsMirror, Alignment, slackSize);
        Radius = state.HasRoundedCorners ? ImageBrowserConstants.RoundedCornerRadius : 0;
        IsReady = IsLoaded;
    }

    /// <summary>
    /// Calculates the value of the <see cref="CanvasRectangle"/> property containing the canvas slot
    /// where the image has to fit on screen.
    /// </summary>
    private static Rect GetCanvasRectangleValue(ImageBrowserState state, Size canvasSize, int positionOnCanvas,
        double margin, Reasons reason)
    {
        int position = positionOnCanvas;
        // Note: the position number can be negative but the column number below is always non-negative!
        double column = (position % state.CanvasColumns + state.CanvasColumns) % state.CanvasColumns;
        // Note: the row number below can be negative for images outside of the screen.
        double row = (position < 0) ? -1 + (position + 1) / state.CanvasColumns : position / state.CanvasColumns;
        if (reason == Reasons.GoIn)
        {
            (row, column) = SpreadImageLocations(row, column, state, canvasSize, positionOnCanvas);
        }
        Vector maxShift = new(2 * canvasSize.Width, 3 * canvasSize.Height);
        Size size = new((canvasSize.Width + margin) / state.CanvasColumns, (canvasSize.Height + margin) / state.CanvasRows);
        Point location = (Point)new Vector(size.Width * column, size.Height * row).Limit(maxShift);
        size.Width -= margin;
        size.Height -= margin;
        return new Rect(location, size);
    }

    /// <summary>
    /// When selecting an image and zooming in (<see cref="Reasons.GoIn"/>), animate the images spreading
    /// in all directions instead of positioning them in one vertical line as they become after zooming in.
    /// </summary>
    private static (double row, double column) SpreadImageLocations(double row, double column,
        ImageBrowserState state, Size canvasSize, int positionOnCanvas)
    {
        int columns = state.CanvasColumnsBeforeZoomed;
        int selectedPosition = state.SelectedPositionOnCanvas;
        int position = positionOnCanvas + selectedPosition;
        if (position >= 0 && position < state.ImagesOnCanvasBeforeZoomed)
        {
            column = position % columns - selectedPosition % columns;
            row = position / columns - selectedPosition / columns;
            double aspectRatio = canvasSize.Width / canvasSize.Height;
            if (aspectRatio < 1)
                column /= aspectRatio;
            else
                row *= aspectRatio;
        }
        return (row, column);
    }

    /// <summary>
    /// Calculates the value of the <see cref="Scaling"/> property, containing the image scaling factor
    /// of the bitmap size.
    /// </summary>
    private static double GetScalingValue(ImageBrowserState state, Rect canvasRec, Size bitmapSize,
        double preScaling, int angle, bool isLoaded)
    {
        bitmapSize = bitmapSize.SetOrientation(angle);
        double scaling = canvasRec.Width / bitmapSize.Width;
        double scaling2 = canvasRec.Height / bitmapSize.Height;
        if (scaling > scaling2 ^ state.FitInsideEdges)
            scaling = scaling2;
        scaling2 = (state.IsZoomed || state.FitInsideEdges) ?
            ImageBrowserConstants.MaxScalingWhenZoomed : ImageBrowserConstants.MaxScaling;
        if (scaling > scaling2)
            scaling = scaling2;
        if (state.UseOriginalSize)
            scaling = preScaling;
        if (state.IsZoomed && Math.Abs(state.ExtraZoom - 1.0) > 0.001)
            scaling *= state.ExtraZoom;
        if (isLoaded == false)
        {
            scaling *= 0.5;
            if (scaling > 1.0 || state.IsZoomed)
                scaling = 1.0;
        }
        return scaling;
    }

    /// <summary>
    /// Calculates the value of the <see cref="Alignment"/> property preserving the offset
    /// of an interrupted alignment animation.
    /// </summary>
    private static Vector GetRemainingAnimationAlignmentValue(Vector alignment, Vector offset, Size slackSize)
    {
        if (offset == ZeroVector)
            return alignment;
        alignment -= new Vector((slackSize.Width > 0) ? offset.X / slackSize.Width : 0,
                                (slackSize.Height > 0) ? offset.Y / slackSize.Height : 0);
        return alignment.Limit(new(0.5, 0.5));
    }

    /// <summary>
    /// Calculates the value of the <see cref="Alignment"/> property aligning the image with the mouse
    /// if it is larger than canvas or its canvas slot.
    /// </summary>
    private static Vector GetMouseAlignmentValue(Vector alignment, ImageBrowserState state, Point mouse,
        Rect canvasRec, bool isMovable, bool isTransformed, Reasons reason)
    {
        if (isMovable && (state.IsZoomed || state.CanAlignMany && reason.IsIn
            (Reasons.GoBack, Reasons.Align, Reasons.AlignCenter, Reasons.AlignTopLeft, Reasons.AlignBottomRight)))
        {
            if (reason == Reasons.AlignCenter)
                alignment = ZeroVector;
            else if (reason == Reasons.AlignTopLeft)
                alignment = new Vector(-0.5, -0.5);
            else if (reason == Reasons.AlignBottomRight)
                alignment = new Vector(0.5, 0.5);
            else if (canvasRec.Contains(mouse))
                alignment = new Vector(GetMouseAlignmentCoordinate(mouse.X - canvasRec.Location.X, canvasRec.Width),
                                       GetMouseAlignmentCoordinate(mouse.Y - canvasRec.Location.Y, canvasRec.Height));
            else if (isTransformed)
                alignment = ZeroVector;
        }
        else if (reason.IsIn(Reasons.Navigate, Reasons.ShakeImages, Reasons.None) == false)
            alignment = ZeroVector;
        return alignment;
    }

    /// <summary>
    /// Calculates either the horizontal or vertical alignment of the image when the image is larger
    /// than the canvas slot (e.g. "inside edges" or "original size" auto-scaling or when extra zoom was applied).
    /// </summary>
    /// <param name="mouseLocation">The mouse position over the canvas slot either in the horizontal (width)
    /// or vertical plane (height). The top and left slot edge is at 0, and the bottom and right edge is at
    /// <paramref name="slotDimension"/></param>
    /// <param name="slotDimension">The width or the height of the canvas slot</param>
    /// <returns>Returns a value between -0.5 (max shift to the left or top), 0.0 (image is centered),
    /// and +0.5 (max shift to the right or bottom). </returns>
    private static double GetMouseAlignmentCoordinate(double mouseLocation, double slotDimension)
    {
        if (double.IsNaN(mouseLocation) || double.IsNaN(slotDimension))
            return 0;
        // the outer percentage of the image where the mouse cursor is ignored when shifting (20%)
        const double excluded = 0.2;
        double result = -0.5 * (mouseLocation / slotDimension - 0.5) / (excluded - 0.5);
        return result.Limit(-0.5, 0.5);
    }

    /// <summary>
    /// Calculates the value of the <see cref="ImageRectangle"/> property containing the image rectangle
    /// as it appears on screen.
    /// </summary>
    private static Rect GetImageRectangleValue(Rect canvasRec, Size imageSize, Vector alignment, Size slackSize)
    {
        Point location = canvasRec.Location;
        location -= ((Vector)imageSize - (Vector)canvasRec.Size) * 0.5;
        location -= new Vector(slackSize.Width * alignment.X, slackSize.Height * alignment.Y);
        return new(location, imageSize);
    }

    /// <summary>
    /// Calculates the value of the <see cref="CropRectangle"/> property which contains the image cropping rectangle
    /// when the image is larger than its canvas slot.
    /// </summary>
    private static Rect GetCropRectangleValue(Size canvasSize, int angle, bool isMirror, Vector alignment, Size slack)
    {
        double x = slack.Width * alignment.X;
        double y = slack.Height * alignment.Y;
        if (angle.IsIn(90, 180) ^ isMirror)
            x = -x;
        if (angle.IsIn(-90, 180))
            y = -y;
        x += slack.Width * 0.5;
        y += slack.Height * 0.5;
        if (Math.Abs(angle) == 90)
            return new(y, x, canvasSize.Height, canvasSize.Width);  // Swap width and height if orientation is changed.
        return new(x, y, canvasSize.Width, canvasSize.Height);
    }

    /// <summary>
    /// Shifts the specified cropping rectangle (like the one in the <see cref="CropRectangle"/> property)
    /// by a <paramref name="shift"/> vector, taking every rotation and mirror configuration into consideration.
    /// Allows resuming image crop animations. We assume that <see cref="crop.Size"/> is already transformed
    /// with the rotation and mirror, so we only focus on the <paramref name="crop.Location"/> property.
    /// </summary>
    public Rect GetShiftedCrop(Rect crop, Vector shift)
    {
        double x = shift.X;
        double y = shift.Y;
        if (Angle.IsIn(90, 180) ^ IsMirror)
            x = -x;
        if (Angle.IsIn(-90, 180))
            y = -y;
        if (Math.Abs(Angle) == 90)
            (x, y) = (y, x);
        return new Rect(crop.Left - x, crop.Top - y, crop.Width, crop.Height);  // Keep width and height unaffected.
    }

    /// <summary>
    /// Returns the shift when navigating by the specified number of images forward or backwards (positive or negative).
    /// Used to animate all images with an identical movement up, down, left or right.
    /// </summary>
    public Vector GetNavigationShift(ImageBrowserState state, int delta)
    {
        if (Math.Abs(delta) < state.CanvasColumns && state.IsZoomed == false)
            return new Vector((CanvasRectangle.Width + state.SlotMargin) * delta, 0);
        else
            return new Vector(0, (CanvasRectangle.Height + state.SlotMargin) *
                (delta.MakeEvenlyDivisible(state.CanvasColumns) / state.CanvasColumns));
    }

    /// <summary>
    /// Returns the navigation delta (number of images scrolled) corresponding to the given remaining
    /// navigation shift. It is the opposite operation to <see cref="GetNavigationShift"/>.
    /// Used to identify what images are visible during very fast scrolling animations.
    /// </summary>
    public static int GetNavigationDelta(ImageBrowserState state, Size canvasSize, Vector remainingShift)
    {
        Rect canvasRectangle = GetCanvasRectangleValue(state, canvasSize, 0, state.SlotMargin, Reasons.None);
        return (int)(remainingShift.X / (canvasRectangle.Width + state.SlotMargin) +
                    remainingShift.Y * state.CanvasColumns / (canvasRectangle.Height + state.SlotMargin));
    }

    /// <summary>
    /// Returns a multi-line tooltip-text describing the image.
    /// </summary>
    public string GetImageDescription()
    {
        StringBuilder text = new(FilePath);
        Size original = FilePixelSize.SetOrientation(Angle);
        Size bitmap = BitmapPixelSize.SetOrientation(Angle);
        Size current = ImageRectangle.Size;
        if (IsZoomed == false)
            current = new(Math.Min(current.Width, CropRectangle.Width), Math.Min(current.Height, CropRectangle.Height));
        current = current.SetOrientation(Angle);
        current = new(Math.Round(current.Width * WindowScaling.X), Math.Round(current.Height * WindowScaling.Y));

        if (IsLoaded)
        {
            if (ItemCountWithinGroup > 1)
                text.Append($" ({GroupItemIndex + 1}/{ItemCountWithinGroup})");
            if (IsAnimated)
                text.Append($"\r\n{Text.AnimatedYesToolTip}");
            else if (IsPotentiallyAnimated)
                text.Append($"\r\n{Text.AnimatedNoToolTip}");
            if (FileTime != -1)
                text.Append($"\r\n{Text.FileDateToolTip}: {new DateTime(FileTime).ToString("d")}");
            if (FileSize > 0)
                text.Append($"\r\n{Text.FileSizeToolTip}: {Math.Round((double)FileSize / 1024, 2)} KB");
            text.Append($"\r\n\r\n{Text.OriginalSizeToolTip}: {original.Width} x {original.Height}");
            if (bitmap != original)
                text.Append($"\r\n{Text.LoadedSizeToolTip}: {bitmap.Width} x {bitmap.Height}");
            text.Append($"\r\n{Text.CurrentSizeToolTip}: {current.Width} x {current.Height}");
            text.Append($"\r\n{Text.ZoomToolTip}: {Math.Round(100 * _scaling / _preScaling)}%");
            if (IsMirror ^ Angle == 180)
                text.Append($"\r\n{Text.MirrorToolTip}");
            if (Angle == -90)
                text.Append($"\r\n{Text.RotateLeftToolTip}");
            else if (Angle == 90)
                text.Append($"\r\n{Text.RotateRightToolTip}");
            else if (Angle == 180)
                text.Append($"\r\n{Text.UpsideDownToolTip}");
        }
        else
        {
            if (LoadingException != null)
                text.Append("\r\n\r\n" + LoadingException.ResolveMessage());
        }
        text.Append($"\r\n\r\n{Text.IndexToolTip}: {Index + 1}");
        if (NeighborDelta != 0)
            text.Append($", {Text.NeighborToolTip}: {NeighborDelta.ToString("+#;-#")}");
        return text.ToString();
    }

    /// <summary>
    /// Starts IrfanView with this image.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Starts IrfanView")]
    public void StartIrfanView()
    {
        string exe = PathHelper.GetIrfanViewPath();
        if (SimpleTimer.IsCycleCompleted(ref _startIrfanViewTime, 1000)
            && PathHelper.CheckIfFileExists(exe, string.Format(Text.ApplicationNotInstalled, "IrfanView"))
            && PathHelper.CheckIfFileExists(FilePath))
        {
            NewThread.Invoke(() =>
            {
                Thread.Sleep(200);
                StringBuilder arguments = new(FilePath);
                arguments.Append(" /sharpen=2 /contrast=1.1 /bright=1.1 /gamma=1.1");
                if (Angle == 90)
                    arguments.Append(" /rotate_r");
                if (Angle == -90)
                    arguments.Append(" /rotate_l");
                if (IsMirror ^ Angle == 180)
                    arguments.Append(" /h").Append("flip");
                if (Angle == 180)
                    arguments.Append(" /v").Append("flip");
                System.Diagnostics.Process proc = new();
                proc.StartInfo.FileName = exe;
                proc.StartInfo.Arguments = arguments.ToString();
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = false;
                proc.Start();
            });
        }
    }
}