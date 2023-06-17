namespace Random_Image.Classes.Cache;

using System;
using System.Windows.Media.Imaging;

using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;

/// <summary>
/// Provides a class for returning image bitmaps from the cache or from the file system to the UI thread.
/// </summary>
public class ImageCacheBitmap
{
    /// <summary>
    /// Represents the image bitmap.
    /// </summary>
    public BitmapSource Bitmap { get; } = null;

    /// <summary>
    /// Contains a possible exception if loading from the file failed.
    /// </summary>
    public Exception LoadingException { get; } = null;

    /// <summary>
    /// Original width of the bitmap in pixels. Can be different from <see cref="Bitmap.PixelWidth"/>
    /// if the bitmap was scaled down when loading. 0 means the same as <see cref="bitmap.PixelWidth"/>.
    /// </summary>
    public int FilePixelWidth { get; init; } = 0;

    /// <summary>
    /// Original height of the bitmap in pixels. Can be different from <see cref="Bitmap.PixelHeight"/>
    /// if the bitmap was scaled down when loading. 0 means the same as <see cref="bitmap.PixelHeight"/>.
    /// </summary>
    public int FilePixelHeight { get; init; } = 0;

    /// <summary>
    /// The size of the image file in bytes, or -1 if the size couldn't be retrieved.
    /// </summary>
    public long FileSize { get; init; } = -1;

    /// <summary>
    /// The time of the last write to the image file, or -1 if the time couldn't be retrieved.
    /// </summary>
    public long FileTime { get; init; } = -1;

    /// <summary>
    /// <see langword="true"/> if the image is animated, i.e. it has more than one bitmap frame
    /// (only GIF images support this).
    /// </summary>
    public bool IsAnimated { get; init; } = false;

    /// <summary>
    /// <see langword="true"/> if the image was asynchronously loaded from the file system, or <see langword="false"/>
    /// if synchronously from the cache. Synchronous calls are blocking and use the same thread, asynchronous calls
    /// are non-blocking and start a new thread.
    /// </summary>
    public bool IsAsynchronous { get; init; } = false;

    /// <summary>
    /// <see langword="true"/> if this object has a valid image bitmap.
    /// </summary>
    public bool IsLoaded => Bitmap?.IsIn(LoadingIcon, ErrorIcon) == false && LoadingException == null;

    public ImageCacheBitmap(BitmapSource bitmap = null, Exception exception = null)
    {
        Bitmap = bitmap ?? ((exception == null) ? null : ErrorIcon);
        LoadingException = exception;
    }

    private static readonly BitmapSource LoadingIcon = ResourceHelper.LoadBitmapResource(ResourceHelper.LoadingPng);

    private static readonly BitmapSource ErrorIcon = ResourceHelper.LoadBitmapResource(ResourceHelper.ErrorPng);

    /// <summary>
    /// Gets the loading icon (the icon to be shown when loading of another image is in progress).
    /// </summary>
    public static ImageCacheBitmap GetLoadingIcon(bool isAsynchronous)
    {
        return new(LoadingIcon) { IsAsynchronous = isAsynchronous };
    }

    /// <summary>
    /// Gets the error icon (the icon to be shown when loading of another images fails).
    /// </summary>
    public static ImageCacheBitmap GetErrorIcon(Exception ex, string message) => new(ErrorIcon, ex.Fix(message));
}