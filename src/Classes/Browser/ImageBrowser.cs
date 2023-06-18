namespace Random_Image.Classes.Browser;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Random_Image.Classes.Cache;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;
using Random_Image.Resources;

/// <summary>
/// Provides the basic functionality of the image browser without animations.
/// </summary>
public class ImageBrowser
{
    /// <summary>
    /// Raised to show a message on the message panel at the bottom of the window and optionally slide the panel out.
    /// </summary>
    public delegate void ShowTextEventType(string text, bool expand);
    public event ShowTextEventType ShowTextEvent;

    /// <summary>
    /// Raised when scrolling is activated. Used to update navigation buttons.
    /// </summary>
    public delegate void NavigationEventType();
    public event NavigationEventType NavigationEvent;

    /// <summary>
    /// Raised when folders change and images need to be reloaded, or when animations are switched on and off.
    /// </summary>
    public delegate void ResetBrowserEventType(bool reload, bool toggleGrouping, bool toggleAnimations);
    public event ResetBrowserEventType ResetBrowserEvent;

    /// <summary>
    /// Indicates the class implements animations.
    /// </summary>
    public virtual bool EnableAnimations => false;  // The base class doesn't implement animations.

    /// <summary>
    /// Indicates that animations are in progress.
    /// </summary>
    public virtual bool IsAnimating => false;  // The base class doesn't implement animations.

    /// <summary>
    /// Indicates that the navigation animation is in progress.
    /// </summary>
    protected virtual bool IsNavigating => false;  // The base class doesn't implement animations.

    /// <summary>
    /// <see langword="true"/> when the browser has been initialized.
    /// </summary>
    public bool IsReady => Cache != null;

    /// <summary>
    /// The global state of the image browser.
    /// </summary>
    public ImageBrowserStateDurable State { get; }

    /// <summary>
    /// The image canvas where the browser paints all images.
    /// </summary>
    public ImageBrowserCanvas ImageCanvas { get; private set; }

    /// <summary>
    /// Image file paths found when scanning the file system, and cache and loader of image bitmaps.
    /// </summary>
    protected ImageCache Cache { get; private set; }

    private Thread _scanThread = null;
    private DispatcherTimer _preloadTimer = null;

    public ImageBrowser(ImageBrowserStateDurable state, ImageBrowserCanvas canvas)
    {
        State = state;
        ImageCanvas = canvas;
        Cache = null;
        if (File.Exists(state.SerializedCacheFile))
        {
            Cache = new(state.SerializedCacheFile);
            State.Initialize(Cache.Files.FileCount, keepImageIndex: true);
        }
    }

    public ImageBrowser(ImageBrowser images)
    {
        State = images.State;
        ImageCanvas = images.ImageCanvas;
        Cache = images.Cache;
        ShowTextEvent += images.ShowTextEvent;
        NavigationEvent += images.NavigationEvent;
        ResetBrowserEvent += images.ResetBrowserEvent;
        RefreshImages(Reasons.None, extraRows: 1);
        ShowText(EnableAnimations ? Text.AnimationsOn : Text.AnimationsOff);
    }

    /// <summary>
    /// Restarts the application and keeps the application state as it is, so that there is no visible change
    /// after the restart. It solves the problem of stuttering animations. It will preserve
    /// the entire application state, scanned files and file order, and window size and location.
    /// </summary>
    public void RestartApplication()
    {
        if (IsReady && (Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            GoBack();
            State.AppendBrowserState(ImageCanvas, EnableAnimations, Cache.Files.GetSerializedTempFile());
            MainThread.RestartApplication(State.GetCommandLine());
        }
    }

    /// <summary>
    /// Resets the browser and cache forcing the initialization process and loading image files from the file system.
    /// </summary>
    public void ResetBrowser(bool reload = false, bool toggleGrouping = false, bool toggleAnimations = false,
        bool setImageFolder = false)
    {
        if (IsReady && IsInitializing() == false)
        {
            reload = reload || toggleGrouping || setImageFolder;
            if (setImageFolder == false || PathHelper.ShowFolderDialog(State.SetImageFolders))
                ResetBrowserEvent?.Invoke(reload, toggleGrouping, toggleAnimations);
        }
    }

    /// <summary>
    /// Scans folders for image files and initializes the canvas with the first page of loaded images.
    /// </summary>
    public void Initialize(bool toggleGrouping, int progressDelay, Action<string> progress, Action finish)
    {
        if (IsInitializing())
            return;
        if (toggleGrouping)
            State.ToggleGroupingImages();
        ClearCanvas();
        _scanThread = ImageCacheFiles.Scan(State.ImageFolders, State.ImageExtensions, progressDelay, progress, Finish);

        // A delegate called asynchronously from a background thread when scanning is finished.
        void Finish(string[] files)
        {
            State.Initialize(files.Length);
            Cache = new ImageCache(files, State.GroupImages);
            SetLayout(State.Layout, Reasons.Navigate);
            ShowText(toggleGrouping ? State.GetOrderDescription() : State.GetDefaultDescription());
            finish?.Invoke();
        }
    }

    /// <summary>
    /// Shows a loading message and returns <see langword="true"/> if the <see cref="Initialize"/> method is in progress.
    /// </summary>
    private bool IsInitializing()
    {
        bool loading = _scanThread.IsInvoked();
        if (loading)
            ShowText(Text.LoadingInProgress);
        return loading;
    }

    /// <summary>
    /// Clears the canvas by expiring all images.
    /// </summary>
    private void ClearCanvas()
    {
        ShowText(Text.LoadingImageFiles);
        ImageCanvas.HideTooltip();
        State.Initialize(0);
        Cache = null;
        foreach (Image image in ImageCanvas.Images)
        {
            image.Tag().Expire();
            image.SetVisibility(false);
        }
    }

    /// <summary>
    /// Shows the specified text in the auto-expanding message panel.
    /// </summary>
    private void ShowText(string text, bool expand = true) => ShowTextEvent?.Invoke(text, expand);

    /// <summary>
    /// Returns only images on canvas that are currently visible.
    /// </summary>
    protected IEnumerable<Image> GetVisibleImages(int extraRows = 0) =>
        ImageCanvas.Images.Where(image => State.IsImageVisible(image.Tag().Index, extraRows));

    /// <summary>
    /// Sets the image layout specifying how many images are visible on canvas at the same time.
    /// </summary>
    public void SetLayout(int layout)
    {
        if (layout != State.Layout)
            SetLayout(layout, Reasons.ChangeLayout);
        ShowText(State.GetLayoutDescription());
    }

    /// <summary>
    /// Sets the image layout specifying how many images are visible on canvas at the same time.
    /// </summary>
    private void SetLayout(int layout, Reasons reason)
    {
        if (State.IsZoomed)
            GoBack();
        else if (State.SetLayout(layout))
            LoadImages(0, reason);
    }

    /// <summary>
    /// Cancels any rotation and mirror.
    /// </summary>
    public void NormalImages() => RotateImages(0, true, false);

    /// <summary>
    /// Toggles between normal and mirrored image.
    /// </summary>
    public void MirrorImages() => RotateImages(State.Angle, State.IsPortrait, State.IsMirror == false);

    /// <summary>
    /// Rotates images by 0, 90, -90, or 180 degrees. If <paramref name="angle"/> is 90 and -90 degrees, makes
    /// all images either portrait (<paramref name="isPortrait"/> == <see langword="true") or landscape oriented
    /// (i.e. vertically or horizontally oriented).
    /// </summary>
    private void RotateImages(int angle, bool isPortrait) => RotateImages(angle, isPortrait, State.IsMirror);

    /// <summary>
    /// Rotates images by 0, 90, -90, or 180 degrees and possibly makes a mirror reflection.
    /// If <paramref name="angle"/> is 90 and -90 degrees, makes all images either portrait
    /// (<paramref name="isPortrait"/> == <see langword="true") or landscape oriented
    /// (i.e. vertically or horizontally oriented).
    /// </summary>
    private void RotateImages(int angle, bool isPortrait, bool isMirror)
    {
        LoadNextRowIfNotLoaded();
        int layout = State.Rotate(angle, isPortrait, isMirror);
        if (layout != State.Layout)
            SetLayout(layout, Reasons.ChangeOrientation);
        else
            RefreshImages(Reasons.ChangeOrientation);
        ShowText(State.GetOrientationDescription());
    }

    /// <summary>
    /// Makes all images either portrait or landscape oriented (i.e. vertically or horizontally oriented).
    /// Rotates relevant images to the right by 90 degrees if <paramref name="direction"/> is true, or to the left
    /// by -90 degrees otherwise. 
    /// </summary>
    public void RotateImages(bool direction)
    {
        int angle = direction ? 90 : -90;
        if (State.Angle == 0 && AnyImageWithOrientation(false))
            RotateImages(angle, true);
        else if ((State.Angle == 0 || State.Angle == angle && State.IsPortrait) && AnyImageWithOrientation(true))
            RotateImages(angle, false);
        else
            RotateImages(0, true);
    }

    /// <summary>
    /// Toggles between upside-down images and normal images.
    /// </summary>
    public void ToggleImagesUpsideDown()
    {
        if (State.Angle == 0 || Math.Abs(State.Angle) == 90 && AnyImageWithOrientation(!State.IsPortrait) == false)
            RotateImages(180, true);
        else
            RotateImages(0, true);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the canvas has visible images with the specified orientation
    /// (portrait or landscape).
    /// </summary>
    private bool AnyImageWithOrientation(bool portrait) =>
        GetVisibleImages().Any(image => image.Tag().IsPortrait == portrait);

    /// <summary>
    /// Toggles between pausing and resuming all animations in the application.
    /// </summary>
    public virtual void TogglePauseOrResumeAnimations()
    {
        // Animations are implemented in the derived class.
    }

    /// <summary>
    /// Toggles between white and black background color.
    /// </summary>
    public void ToggleBackground()
    {
        ImageCanvas.ToggleBackground();
        RefreshImages(Reasons.ChangeBackground);
    }

    /// <summary>
    /// Toggles the window mode between full screen or normal screen.
    /// </summary>
    public void ToggleFullScreen()
    {
        LoadImages(0);  // Updates invisible and not yet loaded images that may become visible.
        MainThread.Invoke(() => ImageCanvas.Window.ToggleFullScreen());
    }

    /// <summary>
    /// Toggles auto-scaling between outside edges, inside edges, and thumbnails (not zoomed) / original size (zoomed).
    /// </summary>
    public void ToggleAutoScaling(bool restore = false)
    {
        State.ToggleAutoScaling(restore);
        ScalingHasChanged();
    }

    /// <summary>
    /// Sets the auto-scaling mode.
    /// </summary>
    public void SetAutoScaling(AutoScalingModes mode, bool isZoomed)
    {
        State.SetAutoScaling(mode, isZoomed);
        ScalingHasChanged();
        if (isZoomed)
        {
            Image image = GetVisibleImages().FirstOrDefault();
            if (State.IsZoomed == false && image != null)
                GoIn(image);
        }
        else
            GoBack();
    }

    /// <summary>
    /// Changes zoom factor when zoomed or the layout when not zoomed.
    /// </summary>
    public void ChangeZoomFactorOrLayout(bool direction)
    {
        if (State.IsZoomed == false)
            SetLayout(State.Layout + (direction ? 1 : -1));
        else if (State.ApplyExtraZoom(direction))
            ScalingHasChanged();
    }

    /// <summary>
    /// Update the browser after changing scaling.
    /// </summary>
    public void ScalingHasChanged()
    {
        LoadNextRowIfNotLoaded();
        RefreshImages(Reasons.ChangeScaling, extraRows: 1);
        ShowText(State.GetScalingDescription());
    }

    /// <summary>
    /// Zooms in the selected image (see <see cref="Reasons.GoIn"/>).
    /// </summary>
    public void GoIn(Image image)
    {
        ImageTag tag = image.Tag();
        if (State.IsZoomed)
        {
            GoBack();
        }
        else if (tag.IsLoaded)
        {
            State.GoIn(tag.Index);
            SetMouseCursor();
            ImageCanvas.PromoteImage(tag.Index);
            LoadImages(State.SelectedPositionOnCanvas, Reasons.GoIn);
        }
    }

    /// <summary>
    /// Goes back to normal view (see <see cref="Reasons.GoBack"/>).
    /// </summary>
    public void GoBack()
    {
        if (State.IsZoomed)
        {
            ImageCanvas.PromoteImage(State.FirstImageIndex);
            State.GoBack();
            SetMouseCursor();
            LoadImages(0, Reasons.GoBack);
        }
    }

    /// <summary>
    /// Goes back to the normal view and undoes changes to orientation, auto-scaling, full screen, layout,
    /// and toggling animations.
    /// </summary>
    public void GoBackOrUndo()
    {
        ImageCanvas.HideTooltip();
        ShowText(State.GetDefaultDescription(), false);
        if (State.IsZoomed)
        {
            GoBack();
        }
        else if (State.IsNonDefault || EnableAnimations != ImageBrowserState.DefaultEnableAnimations)
        {
            if (State.IsNonDefaultAutoScaling)
                ToggleAutoScaling(restore: true);
            if (State.IsNonDefaultLayout)
                SetLayout(ImageBrowserState.DefaultLayout);
            if (State.IsNonDefaultOrientation)
                NormalImages();
            if (EnableAnimations != ImageBrowserState.DefaultEnableAnimations)
                ResetBrowser(toggleAnimations: true);
        }
        else if (ImageCanvas.Window.ToggleFullScreen(false) == false)
            RefreshImages(Reasons.None);
    }

    /// <summary>
    /// Returns the image at the mouse cursor, either directly over it or on the empty space in the image canvas slot.
    /// </summary>
    public Image ImageAtCursor()
    {
        Point point = Mouse.GetPosition(ImageCanvas);
        return GetVisibleImages().FirstOrDefault(image => image.Tag().CanvasRectangle.Contains(point));
    }

    /// <summary>
    /// Adds new images to canvas when navigating by the specified <paramref name="delta"/> parameter.
    /// </summary>
    private void AddNewImages(int delta)
    {
        int loadExtraRows = (delta < 0) ? -1 : 1;
        int preloadExtraPages = loadExtraRows * ImageBrowserConstants.ExpireAfterPage;
        List<ImageTag> requested = new(), preloaded = new();
        foreach (int index in State.GetAvailableImageIndexes(preloadExtraPages))
        {
            bool visible = State.IsImageVisible(index, loadExtraRows);
            (visible ? requested : preloaded).Add(Cache.Files.GetImageTag(index));
        }
        ImageCanvas.AddMissingImages(requested);
        MainThread.Invoke(ref _preloadTimer, 1000, () => PreloadImages(preloaded));
    }

    /// <summary>
    /// Removes the excess of expired images from the canvas.
    /// </summary>
    private void RemoveExpiredImages()
    {
        int count = ImageCanvas.AllImages.Count() - State.MaximumImagesLoaded;
        if (count >= State.CanvasColumns)
        {
            List<Image> recycled = ImageCanvas.AllImages
                .Where(image => State.IsImageExpired(image.Tag().Index)).Take(count).ToList();
            foreach (Image image in recycled)
                ImageCanvas.Children.Remove(image.RecycleImage());
        }
    }

    /// <summary>
    /// Preloads the specified image files to the cache in different background threads.
    /// The call is non-blocking, i.e. the current thread is not suspended.
    /// </summary>
    private void PreloadImages(IEnumerable<ImageTag> preloaded)
    {
        foreach (string filePath in preloaded.Select(tag => tag.FilePath).Reverse())
        {
            void Receive(ImageCacheBitmap result)
            {
                // If the image becomes visible in the meantime, refresh it.
                Image image = ImageCanvas.Images.FirstOrDefault(image => image.Tag().FilePath == filePath);
                if (image != null && IsImageVisible(image))
                    ReceiveImage(result, image, Reasons.None);
            }
            Cache.LoadImage(filePath, Receive);
        }
    }

    /// <summary>
    /// Prepares an animation moving up, down, left or right by the specified delta (number of images back or forward).
    /// </summary>
    protected virtual void SetNavigationDelta(int delta)
    {
        // Animations are implemented in the derived class.
    }

    /// <summary>
    /// Starts an animation shaking all images.
    /// </summary>
    public virtual void ShakeImages(bool postpone = false)
    {
        // Animations are implemented in the derived class.
    }

    /// <summary>
    /// Starts an animation moving the visible part of each image left or right and then back to center
    /// (when images can be positioned with the mouse).
    /// </summary>
    public virtual void PresentImagesByAligning(bool direction, bool postpone = false)
    {
        // Animations are implemented in the derived class.
    }

    /// <summary>
    /// Positions the visible part of the image left or right with the mouse.
    /// </summary>
    public virtual void AlignImage(Image image)
    {
        if (image.Tag().IsMovable && (State.CanAlignMany || State.IsZoomed))
            RefreshImage(image, Reasons.Align);
    }

    /// <summary>
    /// Moves the visible part of the image to the center (when images can be positioned with the mouse).
    /// </summary>
    public virtual void AlignImageToCenter(Image image)
    {
        if (image.Tag().IsMovable && image.Tag().IsCentered == false && State.CanAlignMany)
            RefreshImage(image, Reasons.AlignCenter);
    }

    /// <summary>
    /// If images are grouped, goes to a random page. If all images are in random order, do nothing.
    /// </summary>
    public void GoToRandomImage()
    {
        if (State.GroupImages)
            LoadImages(State.DeltaRandom);
    }

    /// <summary>
    /// If not already loaded, load the next image row following the visible images.
    /// </summary>
    public void LoadNextRowIfNotLoaded()
    {
        if (GetVisibleImages(extraRows: 1).Any(image => image.Tag().IsLoaded == false))
            LoadImages(0);
    }

    /// <summary>
    /// Navigates by the specified delta (positive or negative) and loads new images onto the canvas.
    /// </summary>
    public void LoadImages(int delta, Reasons reason = Reasons.Navigate)
    {
        if (State.FileCount == 0)
        {
            ShowText(Text.NoImagesFound);
        }
        else if (IsReady && State.HasEffect(delta) && ImageCanvas.SyncNeeded(() => LoadImages(delta, reason)) == false)
        {
            ImageCanvas.HideTooltip();
            delta = State.Navigate(delta);
            SetNavigationDelta(delta);
            AddNewImages(delta);
            RemoveExpiredImages();
            NavigationEvent?.Invoke();
            foreach (Image image in ImageCanvas.Images)
                LoadImage(image, reason);
            if (reason != Reasons.ChangeOrientation)
                ShowText(State.GetDefaultDescription(), false);
            if (Cache.NeedsRestart(State.AutoRestartEveryNImages, State.MaximumImagesLoaded))
                RestartApplication();
        }
    }

    /// <summary>
    /// Loads the specified image on the canvas. If images are loaded because of navigating,
    /// <paramref name="neighbor"/> == 0. If an image neighbor is loaded when activating the mouse wheel,
    /// <paramref name="neighbor"/> != 0.
    /// </summary>
    public void LoadImage(Image image, Reasons reason, int neighbor = 0)
    {
        if (IsReady == false)
            return;
        ImageTag tag = image.Tag();
        if (neighbor != 0)
        {
            tag = Cache.Files.GetImageTag(tag.Index, neighbor);
            image.RecycleImage(tag);
        }
        if (tag.IsLoaded == false && tag.LoadingException == null)
        {
            void Receive(ImageCacheBitmap result) => ReceiveImage(result, image, reason, neighbor);
            bool found = Cache.LoadImage(tag.FilePath, Receive);
            // If the image wasn't in cache, show the loading icon. The proper image will be received asynchronously.
            if (found == false)
            {
                if (image.Source == null && State.IsZoomed == false && reason != Reasons.Restart && neighbor == 0)
                    Receive(ImageCacheBitmap.GetLoadingIcon(isAsynchronous: false));
                else
                    RefreshImage(image, reason);
            }
        }
        else
            RefreshImage(image, reason);
    }

    /// <summary>
    /// Called when the UI thread retrieved the bitmap from the cache (synchronous callback)
    /// or when another thread loaded the bitmap from disk storage (asynchronous callback).
    /// </summary>
    private void ReceiveImage(ImageCacheBitmap result, Image image, Reasons reason, int neighbor = 0)
    {
        image = ImageCanvas.MatchExpiredImage(image);
        if (image != null && image.Tag().IsLoaded == false &&
            (result.IsAsynchronous == false || IsImageVisible(image) || IsAnimating == false))
        {
            if (neighbor == 0)
                Cache.AcknowledgeReceived();
            image.AssignBitmap(result);
            RefreshImage(image, reason);
            if (image.Tag().IsLoaded && neighbor != 0 && State.IsImageVisible(image.Tag().Index))
                image.ShowTooltip();
        }
    }

    /// <summary>
    /// Updates visible images on canvas and resizes them to the latest changes in the canvas state.
    /// </summary>
    public void RefreshImages(Reasons reason, int extraRows = 0)
    {
        if (ImageCanvas.SyncNeeded(() => RefreshImages(reason, extraRows)))
            return;
        ImageCanvas.HideTooltip();
        foreach (Image image in GetVisibleImages(extraRows))
            RefreshImage(image, reason);
    }

    /// <summary>
    /// Resizes the image according to the current state of the browser.
    /// </summary>
    private void RefreshImage(Image image, Reasons reason)
    {
        ImageTag tag = image.Tag();
        Size size = ImageCanvas.GetSize();
        tag.Resize(State, size, Mouse.GetPosition(ImageCanvas), GetRemainingAlignmentOffset(image), reason);
        image.SetImageRectangle(tag.ImageRectangle);
        CropImage(image, reason);
        ApplyImageEffects(image, reason);
        SetImageVisibility(image, reason);
        image.SetTooltip(tag.GetImageDescription());
    }

    /// <summary>
    /// Returns the remaining offset of an unfinished animated image alignment, so that the image can stay unchanged.
    /// </summary>
    protected virtual Vector GetRemainingAlignmentOffset(Image image) => new(0, 0);

    /// <summary>
    /// Returns <see langword="true"/> if the image is visible, also when the navigation is in progress.
    /// </summary>
    protected virtual bool IsImageVisible(Image image) => GetVisibleImages().Contains(image);

    /// <summary>
    /// Shows or hides the specified image according to the latest changes in the canvas state.
    /// </summary>
    private void SetImageVisibility(Image image, Reasons reason)
    {
        ImageTag tag = image.Tag();
        if (reason != Reasons.Align)
        {
            if (reason == Reasons.GoIn)
            {
                // State.IsZoomed is already true when zooming in, but we need to animate spreading the images around.
                if (State.IsImageVisibleBeforeZoomed(tag.Index) == false)
                    image.SetVisibility(false);
            }
            else if (reason.IsIn(Reasons.Navigate, Reasons.Restart))
            {
                if (State.IsImageVisible(tag.Index, extraRows: 1))
                    image.SetVisibility(true);
            }
            else if (State.IsZoomed)
                image.SetVisibility(tag.PositionOnCanvas == 0);
            else
                image.SetVisibility(State.IsImageVisible(tag.Index, extraRows: (reason == Reasons.GoBack) ? 0 : 1));
        }
    }

    /// <summary>
    /// Crops the image to fit to the canvas slot and adds rounded corners if needed.
    /// </summary>
    protected virtual void CropImage(Image image, Reasons reason)
    {
        ImageTag tag = image.Tag();
        if (tag.IsMovable || State.HasRoundedCorners)
            image.ResetClip(new RectangleGeometry(tag.CropRectangle, tag.Radius, tag.Radius));
        else
            image.ResetClip();
    }

    /// <summary>
    /// Applies all image effects (e.g. navigating, moving, scaling, etc.) according to the newest canvas state.
    /// </summary>
    protected virtual void ApplyImageEffects(Image image, Reasons reason)
    {
        image.SetDefaultTransformations();
        image.ResetEffect();
    }

    /// <summary>
    /// Sets the mouse cursor for the entire canvas or for the specified image.
    /// </summary>
    public void SetMouseCursor(Image image = null)
    {
        Cursor cursor = Cursors.Arrow;
        if (CanScroll() && State.GroupImages == false)
            cursor = Cursors.ScrollNS;
        else if (image != null || State.IsZoomed)
            cursor = Cursors.Hand;
        FrameworkElement element = (image != null) ? image : ImageCanvas;
        element.SetCursor(cursor);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the mouse cursor is in the corners of the window. Used to enable scrolling
    /// of all images with the mouse wheel instead of scrolling only one image. The <paramref name="ratio"/> parameter
    /// is the divider of <see cref="ImageCanvas.ActualWidth"/> to make calculations independent from the window size.
    /// </summary>
    public bool CanScroll(int ratio = 4)
    {
        if (State.GroupImages || IsNavigating)
            return true;
        return ImageCanvas.CanScroll(State.IsZoomed, ratio);
    }
 
    /// <summary>
    /// Debugging method showing animation clocks.
    /// </summary>
    public void ShowDebugInfo() =>
        ShowText($"{Text.ImagesLabel} {ImageCanvas.AllImages.Count()}\r\n" + AnimationExtensionMethods.GetClockInfo());
}