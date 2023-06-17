namespace Random_Image.Classes.Extensions;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Cache;
using Random_Image.Resources;

using XamlAnimatedGif;

/// <summary>
/// Provides extension methods for handling images.
/// </summary>
public static class ImageExtensionMethods
{
    /// <summary>
    /// Returns the <see cref="ImageTag"/> object associated with this image.
    /// </summary>
    public static ImageTag Tag(this Image image)
    {
        return image.Tag as ImageTag;
    }

    /// <summary>
    /// Sets the <see cref="ImageTag"/> object associated with this <paramref name="image"/>.
    /// </summary>
    public static void SetTag(this Image image, ImageTag tag)
    {
        image.Tag = tag;
    }

    /// <summary>
    /// Returns the position and dimensions (left and top, width and height) of this <paramref name="image"/>.
    /// Note: the same values exist in the <see cref="ImageTag"/> object.
    /// </summary>
    public static Rect GetImageRectangle(this Image image)
    {
        return new Rect((double)image.GetValue(Canvas.LeftProperty),
                        (double)image.GetValue(Canvas.TopProperty), image.Width, image.Height);
    }

    /// <summary>
    /// Sets the position and dimensions (left and top, width and height) of this <paramref name="image"/>.
    /// </summary>
    public static void SetImageRectangle(this Image image, Rect rectangle)
    {
        image.Width = rectangle.Width;
        image.Height = rectangle.Height;
        image.SetValue(Canvas.LeftProperty, rectangle.X);
        image.SetValue(Canvas.TopProperty, rectangle.Y);
    }

    /// <summary>
    /// Assigns a bitmap loaded from the image cache to this <paramref name="image"/>.
    /// </summary>
    public static void AssignBitmap(this Image image, ImageCacheBitmap result)
    {
        ImageTag tag = image.Tag();
        try
        {
            image.Source = result.Bitmap;
            if (result.IsAnimated)
                image.StartAnimatedImage(tag.FilePath);  // handle animated GIFs
        }
        catch (Exception ex)
        {
            result = ImageCacheBitmap.GetErrorIcon(ex, Text.FailedToDisplayImage);
            image.Source = result.Bitmap;
        }
        tag.SetBitmapSize(result);
        RenderOptions.SetBitmapScalingMode(image, tag.IsLarge ?
            BitmapScalingMode.LowQuality : BitmapScalingMode.HighQuality);
        RenderOptions.SetCachingHint(image, CachingHint.Unspecified);
        RenderOptions.SetEdgeMode(image, EdgeMode.Unspecified);
    }

    /// <summary>
    /// Makes this <paramref name="image"/> ready to be reused or deleted.
    /// </summary>
    public static Image RecycleImage(this Image image, ImageTag tag = null)
    {
        if (image.Tag().IsAnimated)
            image.StopAnimatedImage();
        image.ResetTransformations();
        image.SetTag(tag);
        if (tag == null)
        {
            image.ResetClip();
            image.ResetEffect();
            image.HideTooltip();
            image.ToolTip = null;
            image.Source = null;
        }
        return image;
    }

    /// <summary>
    /// Loads an animated image file and starts animating it (only animated GIFs are supported).
    /// </summary>
    private static void StartAnimatedImage(this Image image, string fileName)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(7))
        {
            AnimationBehavior.SetSourceUri(image, new Uri(fileName));
            AnimationBehavior.SetRepeatBehavior(image, RepeatBehavior.Forever);
        }
    }

    /// <summary>
    /// Stops an animated image file (animated GIF).
    /// </summary>
    private static void StopAnimatedImage(this Image image)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(7))
            AnimationBehavior.SetSourceUri(image, null);
    }
}