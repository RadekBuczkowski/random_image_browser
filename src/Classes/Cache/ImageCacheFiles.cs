namespace Random_Image.Classes.Cache;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;

/// <summary>
/// Provides a class scanning the file system for image files, grouping images, and randomizing the order of images.
/// The scanning is performed in a background thread. The class instance can be serialized to a temporary JSON file.
/// Restoring from the file preserves the order of images after restarting the application and prevents the
/// file system from being scanned again.
/// </summary>
[Serializable]
public class ImageCacheFiles
{
    /// <summary>
    /// Number of image files found when scanning the file system.
    /// </summary>
    public int FileCount => FileOrder.Length;

    /// <summary>
    /// File paths of images found when scanning the file system, grouped by similar file names.
    /// </summary>
    [JsonInclude]
    public List<List<string>> FileGroups { get; private set; } = new List<List<string>>();

    /// <summary>
    /// The randomized order of image file paths in <see cref="FileGroups"/>.
    /// </summary>
    [JsonInclude]
    public ImageCacheTag[] FileOrder { get; private set; } = Array.Empty<ImageCacheTag>();

    /// <summary>
    /// Initializes the object for de-serializing.
    /// </summary>
    public ImageCacheFiles() {}

    /// <summary>
    /// Initializes the object with the specified collection of image file paths.
    /// Use the <see cref="Scan"/> method to scan the file system and prepare the collection.
    /// </summary>
    public ImageCacheFiles(IEnumerable<string> files, bool groupImages)
    {
        string previous = string.Empty;
        List<string> group = null;
        foreach (string file in files.OrderBy(PathHelper.GetFileOrder))
        {
            if (group == null || PathHelper.IsSameFileGroup(previous, file) == false)
            {
                group = new List<string>();
                FileGroups.Add(group);
            }
            group.Add(file);
            previous = file;
        }
        ShuffleFileGroups(groupImages);
    }

    /// <summary>
    /// Randomizes image files.
    /// </summary>
    private void ShuffleFileGroups(bool groupImages)
    {
        if (groupImages)
            FileGroups.Shuffle();
        List<ImageCacheTag> items = new();
        for (int i = 0; i < FileGroups.Count; i++)
            for (int j = 0; j < FileGroups[i].Count; j++)
            {
                items.Add(new ImageCacheTag(i, j, FileGroups[i].Count));
            }
        if (groupImages == false)
            items.Shuffle();
        for (int i = 0; i < items.Count; i++)
        {
            items[i].SetIndex(i);
        }
        FileOrder = items.ToArray();
    }

    /// <summary>
    /// Returns the <see cref="ImageTag"/> object with file path at the specified image <paramref name="index"/>.
    /// If the <paramref name="neighbor"/> argument is non-zero (i.e. the mouse wheel was activated when hovering
    /// over an image), the alphabetic neighbor of the image file in the same folder will be returned. The neighbor
    /// is distant from the original file by the specified <paramref name="neighbor"/> parameter (+ or -).
    /// </summary>
    public ImageTag GetImageTag(int index, int neighbor = 0)
    {
        ImageCacheTag tag = FileOrder[index];
        if (neighbor != 0)
            tag.GoToNeighbor(neighbor, FileGroups);
        string filePath = FileGroups[tag.GroupIndex][tag.GroupItemIndex];
        return new ImageTag(tag, filePath);
    }

    /// <summary>
    /// Serializes the object to a temporary JSON file and returns the file name.
    /// </summary>
    public string GetSerializedTempFile()
    {
        string name = Path.GetTempFileName();
        using FileStream stream = File.Create(name);
        JsonSerializer.Serialize(stream, this);
        stream.Dispose();
        return name;
    }

    /// <summary>
    /// De-serializes the object from the given temporary JSON file and deletes the file.
    /// </summary>
    public static ImageCacheFiles DeSerializeFromTempFile(string name)
    {
        using FileStream stream = File.OpenRead(name);
        ImageCacheFiles result = JsonSerializer.Deserialize<ImageCacheFiles>(stream);
        stream.Dispose();
        File.Delete(name);
        return result;
    }

    /// <summary>
    /// Scans the specified folders for image files in a background thread. Returns the background thread.
    /// </summary>
    /// <param name="fileFolders">List of folders to scan</param>
    /// <param name="fileExtensions">File extensions to scan for</param>
    /// <param name="progressDelay">A delay in milliseconds of the first call to the <paramref name="progress"/>
    /// delegate, so that a rotating circle animation can be shown first</param>
    /// <param name="progress">A delegate to show scanned folders on screen during the scan
    /// (can be <see langword="null"/>)</param>
    /// <param name="finish">A delegate called when the scan is finished receiving paths of files found</param>
    public static Thread Scan(string[] fileFolders, string[] fileExtensions, int progressDelay,
        Action<string> progress, Action<string[]> finish)
    {
        Thread result = null;
        NewThread.Invoke(ref result, () =>  // Run in a background thread.
        {
            List<string> files = new(), folderLog = new();
            DateTime progressAfter = DateTime.Now.AddMilliseconds(progressDelay);
            foreach (string folder in fileFolders)
                if (string.IsNullOrWhiteSpace(folder) == false)
                    ScanRecursively(folder, fileExtensions, progressAfter, progress, files, folderLog);
            MainThread.Invoke(() => finish(files.ToArray()));  // Run in the UI thread.
        });
        return result;
    }

    /// <summary>
    /// Scans the specified folder recursively for files.
    /// </summary>
    private static void ScanRecursively(string folder, string[] fileExtensions, DateTime progressAfter,
        Action<string> progress, List<string> files, List<string> folderLog, int level = 0)
    {
        folder = PathHelper.ResolveSystemFolder(folder);
        if (string.IsNullOrWhiteSpace(folder) || level > 500)
            return;
        try
        {
            int count = files.Count;
            foreach (string filename in Directory.GetFiles(folder))
            {
                string extension = Path.GetExtension(filename).ToLower().Trim('.');
                if (fileExtensions.Contains(extension))
                    files.Add(Path.GetFullPath(filename));
            }
            if (count < files.Count && progress != null)
                ReportProgress(folder, progressAfter, progress, folderLog);
            foreach (string subfolder in Directory.GetDirectories(folder))
                ScanRecursively(subfolder, fileExtensions, progressAfter, progress, files, folderLog, level + 1);
        }
        catch (Exception)
        {
            // Do nothing.
        }
    }

    /// <summary>
    /// Calls the progress delegate with the list of scanned folders that contained image files.
    /// </summary>
    private static void ReportProgress(string folder, DateTime progressAfter, Action<string> progress,
        List<string> folderLog = null)
    {
        string text = PathHelper.LimitPathLength(folder);
        if (folderLog.Contains(text) == false)
        {
            folderLog.Add(text);
            if (folderLog.Count > 100)
                folderLog.RemoveAt(0);
            if (DateTime.Now >= progressAfter && progress != null)
                MainThread.Invoke(() => progress(string.Join(Environment.NewLine, folderLog)));  // Run in UI thread.
        }
    }
}