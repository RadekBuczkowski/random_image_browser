namespace Random_Image.Classes.Wpf;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;

/// <summary>
/// Provides a class implementing additional functionality of the image browser canvas.
/// </summary>
public class ImageBrowserCanvas : Canvas
{
    private Window _window = null;
    private DispatcherTimer _tooltipTimer = null;
    private DispatcherTimer _syncTimer = null;
    private long _lastSyncTime = 0;

    /// <summary>
    /// The window containing the image canvas.
    /// </summary>
    public Window Window
    {
        get
        {
            _window ??= this.FindVisualParent<Window>();
            return _window;
        }
    }

    /// <summary>
    /// <see langword="true"/> if the canvas has a black background or <see langword="false"/> if white background.
    /// </summary>
    public bool IsBlackBackground => Window.Background is not SolidColorBrush ||
        (Window.Background as SolidColorBrush).Color == Colors.Black;

    /// <summary>
    /// Toggles between white and black background color.
    /// </summary>
    public void ToggleBackground()
    {
        Window.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
            { RoutedEvent = Mouse.MouseDownEvent });
    }

    /// <summary>
    /// Returns all images on canvas including expired images.
    /// </summary>
    public IEnumerable<Image> AllImages => Children.OfType<Image>().Where(image => image.Tag() != null);

    /// <summary>
    /// Returns only active images on canvas, both visible and invisible.
    /// </summary>
    public IEnumerable<Image> Images => AllImages.Where(image => image.Tag().Index > int.MinValue);

    /// <summary>
    /// Returns <see langword="true"/> if the canvas has been refreshed recently and a new refresh request
    /// should be delayed, or <see langword="false"/> if the canvas is ok to be refreshed now.
    /// </summary>
    public bool SyncNeeded(Action action) => MainThread.SyncInvokeNeeded(ref _syncTimer, ref _lastSyncTime, 100, action);

    /// <summary>
    /// Adds a new image to the canvas with the specified <paramref name="tag"/>.
    /// </summary>
    public Image AddNewImage(ImageTag tag)
    {
        Image image = new() { Margin = new Thickness(0) };
        image.SetResourceReference(StyleProperty, "styleImage");
        image.SnapsToDevicePixels = true;
        image.SetImageRectangle(new(0, 0, 0, 0));
        image.SetVisibility(false);
        image.SetZIndex(tag.Index);
        image.SetTag(tag);
        Children.Add(image);
        return image;
    }

    /// <summary>
    /// Adds new images to the canvas with the specified <paramref name="tags"/> if they don't already exist.
    /// </summary>
    public void AddMissingImages(IEnumerable<ImageTag> tags)
    {
        foreach (ImageTag tag in tags.Except(Images.Select(image => image.Tag())))
            AddNewImage(tag);
    }

    /// <summary>
    /// Moves the image with specified index to the foreground (on top of any other images).
    /// </summary>
    public void PromoteImage(int index)
    {
        foreach (Image image in Images)
            image.SetZIndex((image.Tag().Index == index) ? int.MaxValue : image.Tag().Index);
    }

    /// <summary>
    /// If an image was returned after reloading and is expired, find the matching existing image.
    /// Otherwise return the same image. <see langword="null"/> is returned if the image does not exist anymore.
    /// </summary>
    public Image MatchExpiredImage(Image imageToFind)
    {
        if (Images.Contains(imageToFind))
            return imageToFind;
        return Images.FirstOrDefault(image => image.Tag().FilePath == imageToFind.Tag().FilePath);
    }

    /// <summary>
    /// Returns the size of this canvas object.
    /// </summary>
    public Size GetSize() => new(ActualWidth, ActualHeight);

    /// <summary>
    /// Returns the position of the mouse relative to the parent of this canvas. Note: the canvas gets shifted
    /// while animating, but the <see cref="Grid"/> that is the immediate parent container of the canvas is stable.
    /// </summary>
    private Point GetParentMouse() => Mouse.GetPosition(this.FindVisualParent<Grid>());

    /// <summary>
    /// Enables or disables cache on this <paramref name="canvas"/> improving animation smoothness, provided images
    /// don't change. Used only when navigating. If the cache already has the desired state, nothing changes.
    /// </summary>
    public void ResetCache(bool enable = false)
    {
        if (enable)
            CacheMode ??= new BitmapCache();
        else if (CacheMode != null)
            CacheMode = null;
    }

    /// <summary>
    /// Starts a timer showing the tooltip of the specified image provided the mouse doesn't move too much.
    /// </summary>
    public void ShowTooltip(Image image)
    {
        if (image.IsTooltipShown())
            return;
        HideTooltip();  // Possibly hide the tooltip of another image.
        int jumps = 0;
        Point lastPoint;
        DelayedShow(50);

        void DelayedShow(int milliseconds) => MainThread.Invoke(ref _tooltipTimer, milliseconds, Show);
        void Show()
        {
            Point point = Mouse.GetPosition(this);
            if (jumps > 0 && image.Tag()?.CanvasRectangle.Contains(point) == true && lastPoint.DistanceTo(point) < 3)
            {
                image.ShowTooltip();
            }
            else if (++jumps <= 3)
            {
                lastPoint = point;
                DelayedShow(200);  // Makes sure the mouse cursor doesn't move too much in the given time.
            }
        }
    }

    /// <summary>
    /// Hides the tooltips of all images. Optionally, do it only when the mouse has moved too much
    /// since the tooltip was shown.
    /// </summary>
    public void HideTooltip(bool onlyWhenMoved = false)
    {
        if (onlyWhenMoved == false)
            MainThread.StopInvoke(ref _tooltipTimer);
        foreach (Image image in Children.OfType<Image>())
            image.HideTooltip(onlyWhenMoved ? ImageBrowserConstants.HiteTooltipWhenMouseMovedBy : 0);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the mouse cursor is in the corners of the window. Used to enable scrolling
    /// of all images with the mouse wheel instead of scrolling only one image. The <paramref name="ratio"/> parameter
    /// is the divider of <see cref="ActualWidth"/> to make calculations independent from the window size.
    /// </summary>
    public bool CanScroll(bool isZoomed, int ratio = 4)
    {
        Point point = GetParentMouse();
        Size size = GetSize();
        double boundary = Math.Min(size.Width / ratio, size.Height / ratio);
        if (isZoomed)
            return point.X < boundary || point.X > size.Width - boundary;
        else
            return point.MinDistanceTo((0, 0), (size.Width, 0), (0, size.Height), (size.Width, size.Height)) < boundary;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the mouse cursor is close to the menu buttons. Used to slide
    /// the buttons out and make them visible. The <paramref name="ratio"/> parameter is the divider of
    /// <see cref="ActualHeight"/> to make calculations independent from the window size.
    /// </summary>
    public bool IsNearMenuButtons(int ratio = 16)
    {
        Rect rectangle = new(0, 0, ActualWidth / 5, ActualHeight / ratio);
        return rectangle.Contains(GetParentMouse());
    }

    /// <summary>
    /// Returns <see langword="true"/> if the mouse cursor is close to the navigation buttons. Used to slide
    /// the buttons out and make them visible. The <paramref name="ratio"/> parameter is the divider of
    /// <see cref="ActualHeight"/> to make calculations independent from the window size.
    /// </summary>
    public bool IsNearNavigationButtons(int ratio = 8)
    {
        Point point = GetParentMouse();
        Size size = GetSize();
        return point.MinDistanceTo((size.Width, 0), (size.Width, size.Height)) < size.Height / ratio;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the mouse cursor is close to the message panel. Used to slide
    /// the message panel out and make it visible. The <paramref name="ratio"/> parameter is the divider
    /// of <see cref="ActualHeight"/> to make calculations independent from the window size.
    /// </summary>
    public bool IsNearMessagePanel(double messagePanelWidth, int ratio = 10)
    {
        Size size = GetSize();
        Rect rectangle = new((size.Width - messagePanelWidth) / 2, size.Height - size.Height / ratio,
                            messagePanelWidth, size.Height / ratio);
        return rectangle.Contains(GetParentMouse());
    }

    /// <summary>
    /// Recreates the canvas.
    /// </summary>
    public Canvas CloneAndResetCanvas()
    {
        Grid grid = this.FindVisualParent<Grid>();
        ImageBrowserCanvas canvas = this;
        if (grid.Children.Contains(this))
        {
            foreach (Image image in Children.OfType<Image>().Where(image => image.Tag() != null).ToList())
                Children.Remove(image.RecycleImage());
            Children.Clear();
            this.ResetTransformations();
            string name = Name;
            grid.Children.Remove(this);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            canvas = new ImageBrowserCanvas() { Name = name, Background = Brushes.Transparent };
            grid.Children.Add(canvas);
            canvas.UpdateLayout();
        }
        return canvas;
    }
}