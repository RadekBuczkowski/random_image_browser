namespace Random_Image.Classes.Utilities;

using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

using Random_Image.Classes.Extensions;
using Random_Image.Properties;

/// <summary>
/// Provides a class for loading assembly resource files and for loading image files from the file system.
/// </summary>
public static class ResourceHelper
{
    public const string HelpHtml = "help.{0}.html";
    public const string ErrorPng = "error.png";
    public const string LoadingPng = "loading.png";

    private const string ResourcePathPrefix = "Random_Image.Resources.";

    /// <summary>
    /// Returns the path to the user.config file.
    /// </summary>
    private static string GetUserConfigFilePath()
    {
        try
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
        }
        catch { return null; }
    }

    /// <summary>
    /// Opens the user.config file in Notepad.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Starts Notepad")]
    public static void OpenUserConfigFile()
    {
        string path = GetUserConfigFilePath();
        if (File.Exists(path) == false)
        {
            Settings.Default.RefreshProperties();
            Settings.Default.Save();
        }
        if (PathHelper.CheckIfFileExists(path))
        {
            System.Diagnostics.Process proc = new();
            proc.StartInfo.FileName = "notepad.exe";
            proc.StartInfo.Arguments = path;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = false;
            proc.Start();
        }
    }

    /// <summary>
    /// Loads the specified text file from embedded resources using the current culture (default),
    /// or the specified culture. If the resource with the current culture could not be loaded,
    /// the file is loaded in the "en-US" culture.
    /// </summary>
    public static string LoadTextResource(string fileName, string culture = null)
    {
        string resourceSuffix = (culture ?? Thread.CurrentThread.CurrentUICulture.Name).Replace("-", "_");
        string resourceName = ResourcePathPrefix + string.Format(fileName.Replace('/', '.'), resourceSuffix);
        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
        return (culture != null) ? null : LoadTextResource(fileName, "en-US");
    }

    /// <summary>
    /// Loads the specified image file from embedded resources.
    /// </summary>
    public static BitmapSource LoadBitmapResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(ResourcePathPrefix + fileName.Replace('/', '.'));
        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// Loads the specified image file from the file system. The bitmap image can optionally be scaled down
    /// to the specified dimensions in <paramref name="desiredWidth"/> and <paramref name="desiredHeight"/>.
    /// </summary>
    public static BitmapSource LoadBitmap(string filePath,
        int desiredWidth = 0, int desiredHeight = 0, bool troubleShooting = false)
    {
        BitmapImage bitmap = new();
        bitmap.BeginInit();
        // if the previous attempt failed, improve compatibility at the cost of some performance degradation
        if (troubleShooting)
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(filePath);
        if (desiredWidth > 0 && desiredHeight > 0)
        {
            bitmap.DecodePixelWidth = desiredWidth;
            bitmap.DecodePixelHeight = desiredHeight;
        }
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// Loads the specified image file from the file system. If width or height of the bitmap exceed the given limit,
    /// the bitmap will be scaled down to the limit with the original aspect ratio of the image unchanged.
    /// </summary>
    /// <returns>The image bitmap and original width and height of the image if the bitmap was scaled down
    /// to the specified limit, or the unscaled bitmap and (0, 0) otherwise.</returns>
    public static (BitmapSource bitmap, int width, int height) LoadBitmapWithLimit(string filePath, int pixelDimensionLimit)
    {
        using Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
        Size original = new(decoder.Frames[0].PixelWidth, decoder.Frames[0].PixelHeight);
        Size scaled = original;
        if (pixelDimensionLimit > 0 && pixelDimensionLimit < scaled.Width)
            scaled = new(pixelDimensionLimit, Math.Round(scaled.Height * pixelDimensionLimit / scaled.Width));
        if (pixelDimensionLimit > 0 && pixelDimensionLimit < scaled.Height)
            scaled = new(Math.Round(scaled.Width * pixelDimensionLimit / scaled.Height), pixelDimensionLimit);
        if (scaled == original)
            return (LoadBitmap(filePath), 0, 0);
        else
            return (LoadBitmap(filePath, (int)scaled.Width, (int)scaled.Height),
                (int)original.Width, (int)original.Height);
    }

    /// <summary>
    /// Checks if the specified GIF image file is animated, i.e. it has more than one frame.
    /// </summary>
    public static bool IsAnimatedGif(string filePath)
    {
        if (filePath.ToLower().EndsWith(".gif"))
        {
            using Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            GifBitmapDecoder decoder = new(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
            return decoder.Frames.Count > 1;
        }
        return false;
    }
}