namespace Random_Image.Classes.Utilities;

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

using Microsoft.Win32;

using Ookii.Dialogs.Wpf;

using Random_Image.Classes.Extensions;
using Random_Image.Resources;

/// <summary>
/// Provides a class for grouping, ordering image file paths, selecting folders, handling system folders
/// that may contain image files, and getting the IrfanView executable path.
/// </summary>
public static partial class PathHelper
{
    /// <summary>
    /// Compares two file paths and checks if they belong to the same file group,
    /// e.g. image01.jpeg, image02.jpeg, etc. File extensions are ignored.
    /// </summary>
    public static bool IsSameFileGroup(string path1, string path2, int minCharsIdentical = 6)
    {
        if (Path.GetDirectoryName(path1).ToLower() != Path.GetDirectoryName(path2).ToLower())
            return false;
        string name1 = Path.GetFileNameWithoutExtension(path1).ToLower();
        string name2 = Path.GetFileNameWithoutExtension(path2).ToLower();
        Regex[] patterns = new[] { ExtractUUID(), ExtractLeadingInteger(), ExtractTrailingInteger() };
        foreach (Regex pattern in patterns)
            if (pattern.Matches(name1).Count > 0 || pattern.Matches(name2).Count > 0)
            {
                name1 = pattern.Replace(name1, string.Empty);
                name2 = pattern.Replace(name2, string.Empty);
                if (name1 == string.Empty && name2 == string.Empty && pattern == ExtractUUID())
                    return false;  // UUIDs alone don't make a common group
                break;
            }
        return name1.FirstCharacters(minCharsIdentical) == name2.FirstCharacters(minCharsIdentical);
    }

    /// <summary>
    /// Returns this string truncated to the specified number of characters or until a special character occurs.
    /// </summary>
    private static string FirstCharacters(this string text, int length)
    {
        if (text == null)
            return null;
        text = text.Replace('_', '-');
        string prefix = text.Split(new char[] { '-', ';', ',', '.', '#' })[0];
        if (prefix.Length < 4)
            prefix = text;
        return prefix[0..Math.Min(prefix.Length, length)];
    }

    /// <summary>
    /// Splits a file path to its base name and the number before the extension. E.g. "c:/users/icon15.png" is split
    /// to "c:/users/icon" and integer 15. File extensions are ignored. It allows for better sorting of files,
    /// e.g. "icon9.png" will be before "icon10.png", even though alphabetically it is the opposite. 
    /// </summary>
    public static (string, long) GetFileOrder(string path)
    {
        string folder = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        name = ExtractUUID().Replace(name, string.Empty);
        long index = 0;
        Regex[] patterns = new[] { ExtractLeadingInteger(), ExtractTrailingInteger() };
        foreach (Regex pattern in patterns)
            if (long.TryParse(pattern.MatchFirst(name), out index))
            {
                name = pattern.Replace(name, string.Empty);
                break;
            }
        return (Path.Combine(folder, name).ToLower(), index);
    }

    /// <summary>
    /// Ensures the folder path does not exceed the given number of characters.
    /// If it does, the string is cut and "..." is appended.
    /// </summary>
    public static string LimitPathLength(string path, int limit = 80)
    {
        StringBuilder result = new();
        foreach (string part in path.Split(new char[] { '/', '\\' }))
        {
            if (result.Length + part.Length + 1 > limit)
            {
                result.Append("/...");
                break;
            }
            if (result.Length > 0)
                result.Append('/');
            result.Append(part);
        }
        return result.ToString();
    }

    /// <summary>
    /// Replaces the "system:" prefix with the appropriate system folder, e.g. "system:MyPictures"
    /// for the current user's "My Pictures" folder.
    /// </summary>
    public static string ResolveSystemFolder(string folder)
    {
        const string prefix = "system:";
        if (folder != null && folder.StartsWith(prefix) && folder.Length > 8)
        {
            string[] parts = folder.Split(new char[] { '\\', '/' }, StringSplitOptions.None);
            if (Enum.TryParse(parts[0][prefix.Length..].Trim(), out Environment.SpecialFolder special))
            {
                parts[0] = Environment.GetFolderPath(special);
                return Path.Combine(parts);
            }
            else
                return string.Empty;
        }
        return folder;
    }

    /// <summary>
    /// Shows a folder selection dialog. If a folder was selected, returns <see langword="true"/>
    /// and calls the <paramref name="setFolders"/> delegate.
    /// </summary>
    public static bool ShowFolderDialog(Action<string[],string[]> setFolders, string defaultFolder = null)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(7))
        {
            string folder = Path.GetFullPath(ResolveSystemFolder(defaultFolder ?? "system:MyPictures") + '\\');
            VistaFolderBrowserDialog dialog = new() { SelectedPath = folder };
            if (dialog.ShowDialog() == true)
            {
                setFolders(new[] { dialog.SelectedPath }, null);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the specified file exists and shows an error message if it doesn't.
    /// </summary>
    public static bool CheckIfFileExists(string path, string errorMessage = null)
    {
        if (File.Exists(path))
            return true;
        Window window = Application.Current.MainWindow;
        MessageBox.Show(window, errorMessage ?? $"{Text.TheFileDoesNotExist}\n{path}", window.Title,
            MessageBoxButton.OK, MessageBoxImage.Exclamation);
        return false;
    }

    /// <summary>
    /// Returns the file size in bytes and the time (<see cref="DateTime.Ticks"/>) of the last write
    /// to the specified file. If the file couldn't be accessed, returns (-1, -1).
    /// </summary>
    public static (long size, long time) GetFileSizeAndTime(string path)
    {
        try
        {
            FileInfo info = new(path);
            return (info.Length, info.LastWriteTime.Ticks);
        }
        catch
        {
            // Do nothing.
        }
        return (-1, -1);
    }

    /// <summary>
    /// Returns the IrfanView path from Windows registry.
    /// </summary>
    public static string GetIrfanViewPath()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(7))
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"IrfanView\shell\open\command"))
            {
                string command = key?.GetValue(null)?.ToString() ?? string.Empty;
                string path = ExtractPathFromCommand().MatchFirst(command);
                if (path != null)
                    return path;
            }
        return @"C:\Program Files\IrfanView\i_view64.exe";
    }

    [GeneratedRegex("\"(.*?)\"")]
    private static partial Regex ExtractPathFromCommand();


    [GeneratedRegex(@"(^[0-9]{1,18})(?=[^0-9A-Za-z]+)")]
    private static partial Regex ExtractLeadingInteger();


    [GeneratedRegex(@"(?<![0-9])([0-9]{1,18}[a-zA-Z]?$)")]
    private static partial Regex ExtractTrailingInteger();


    [GeneratedRegex(@"(?<![0-9a-fA-F])([0-9a-f]{32}|[0-9A-F]{32})(?![0-9a-fA-F])")]
    private static partial Regex ExtractUUID();
}