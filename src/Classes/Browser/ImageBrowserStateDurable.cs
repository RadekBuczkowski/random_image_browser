namespace Random_Image.Classes.Browser;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Random_Image.Classes.Extensions;
using Random_Image.Resources;

/// <summary>
/// Implements the global state of the image browser with text tooltips and the possibility of persisting
/// and restoring the state from the command line. It allows for keeping the state after the application restart.
/// </summary>
public class ImageBrowserStateDurable : ImageBrowserState
{
    /// <summary>
    /// Creates a new instance of the image browser state.
    /// </summary>
    public ImageBrowserStateDurable() { }

    /// <summary>
    /// Creates a new instance of the image browser state and initializes it from the command line.
    /// </summary>
    public ImageBrowserStateDurable(string[] parameters) => ParseCommandLine(parameters);

    /// <summary>
    /// Makes a clone copy of the entire ImageBrowserState object.
    /// Note: members of this class are structs and primitive types, so this is in fact a deep clone.
    /// </summary>
    public ImageBrowserStateDurable Clone()
    {
        return (ImageBrowserStateDurable)MemberwiseClone();
    }

    /// <summary>
    /// Returns the default message to be shown on the message panel.
    /// </summary>
    public string GetDefaultDescription() => IsZoomed ? GetScalingDescription() : GetFileDescription();

    /// <summary>
    /// Returns a description of how many files were found.
    /// </summary>
    private string GetFileDescription() =>
        string.Format((FileCount == 0) ? Text.NoImagesFound : Text.ImageFileCount, FileCount);

    /// <summary>
    /// Returns the description of the layout.
    /// </summary>
    public string GetLayoutDescription() => string.Format(Text.LayoutDimensions, CanvasColumns, CanvasRows);

    /// <summary>
    /// Returns the description of the image file order.
    /// </summary>
    public string GetOrderDescription() => GroupImages ? Text.RandomGroupOrder : Text.RandomOrder;

    /// <summary>
    /// Returns the description of the auto-scaling and the extra zoom.
    /// </summary>
    public string GetScalingDescription()
    {
        static string FitText(AutoScalingModes fit) =>
            (fit == AutoScalingModes.InsideEdges) ? Text.FitInsideEdges : Text.FitOutsideEdges;

        string text;
        if (IsZoomed)
        {
            if (FitWhenZoomed == AutoScalingModes.OriginalSize)
                text = Text.OriginalSize;
            else
                text = FitText(FitWhenZoomed);
            if (Math.Abs(ExtraZoom - 1.0) > 0.005)
                text += $" * {Math.Round(ExtraZoom * 100)}%";
        }
        else if (Fit == AutoScalingModes.Thumbnails)
            text = Text.Thumbnails;
        else
            text = FitText(Fit);
        return text;
    }

    /// <summary>
    /// Returns the description of the rotation and mirror transformations.
    /// </summary>
    public string GetOrientationDescription()
    {
        StringBuilder text = new();
        if (IsMirror)
            text.Append(Text.MirrorToolTip);
        if (Math.Abs(Angle) == 90)
        {
            if (text.Length > 0)
                text.AppendLine();
            text.Append((Angle == 90) ? Text.RotateRight : Text.RotateLeft);
            if (IsZoomed == false)
                text.Append(' ').Append(IsPortrait ? Text.AllPortraitSuffix : Text.AllLandscapeSuffix);
        }
        else if (Angle == 180)
        {
            if (text.Length > 0)
                text.AppendLine();
            text.Append(Text.UpsideDownImage);
        }
        if (text.Length == 0)
            text.Append((ImagesOnCanvas == 1) ? Text.NormalImage : Text.NormalImages);
        return text.ToString();
    }

    /// <summary>
    /// Sets the tooltips and the enabled state of the image navigation up and down buttons.
    /// </summary>
    public void UpdateButtonTooltips(FrameworkElement buttonUp, FrameworkElement buttonDown)
    {
        string text = (CanvasColumns == 1) ? Text.GoBackToImageToolTip : Text.GoBackToImagesToolTip;
        int index = (FirstImageIndex - CanvasColumns + 1).Limit(1, int.MaxValue);
        buttonUp.SetTooltip(string.Format(text, index, index + CanvasColumns - 1));
        buttonUp.IsEnabled = FirstImageIndex > 0;

        text = (CanvasColumns == 1) ? Text.GoToImageToolTip : Text.GoToImagesToolTip;
        index = FirstImageIndex + ImagesOnCanvas + 1;
        buttonDown.SetTooltip(string.Format(text, index, index + CanvasColumns - 1));
        buttonDown.IsEnabled = FirstImageIndex < MaximumFirstImageIndex;
    }

    /// <summary>
    /// Initializes the canvas state from command line.
    /// (The logic is to some degree tolerant to misplacements of parameters.)
    /// Example:
    ///     -ImageFolder1 "c:\My Pictures" -Layout 7 -IsFullScreen True
    /// </summary>
    private void ParseCommandLine(string[] parameters)
    {
        static bool IsSwitch(string text) => text?.StartsWith('-') == true && double.TryParse(text, out double _) == false;

        List<string> folders = new();
        string key, value = null;
        IEnumerator<string> enumerator = parameters.ToList().GetEnumerator();
        while (enumerator.MoveNext())
        {
            key = value;
            value = enumerator.Current?.Trim();
            if (IsSwitch(key) == true && IsSwitch(value) == false && string.IsNullOrEmpty(value) == false)
            {
                // internal use only - quotes are removed automatically when the command line is handled by the OS
                value = value.Trim('"');
                _ = int.TryParse(value, out int integer);
                _ = bool.TryParse(value, out bool enabled);
                _ = double.TryParse(value, out double doubleNumber);
                switch (key)
                {
                    case "-ImageFolder1":
                    case "-ImageFolder2":
                    case "-ImageFolder3": folders.Add(value); break;
                    case "-Layout": SetLayout(integer); break;
                    case "-PreviousLayout": _previousLayout = integer.LimitLayout(); break;
                    case "-AutoScaling": Fit = (AutoScalingModes)integer.Limit(1, 3); break;
                    case "-AutoScalingWhenZoomed": FitWhenZoomed = (AutoScalingModes)integer.Limit(1, 3); break;
                    case "-ExtraZoom": ExtraZoom = doubleNumber.LimitScalingZoom(); break;
                    case "-IsBlackBackground": IsBlackBackground = enabled; break;
                    case "-IsFullScreen": IsFullScreen = enabled; break;
                    case "-EnableAnimations": EnableAnimations = enabled; break;
                    case "-GroupImages": GroupImages = enabled; break;
                    case "-RotationAngle": Angle = integer.IsIn(0, 90, -90, 180) ? integer : 0; break;
                    case "-Mirror": IsMirror = enabled; break;
                    case "-Portrait": IsPortrait = enabled; break;
                    case "-SerializedWindowRectangle": SerializedWindowRectangle = value; break;
                    case "-SerializedCacheFile": SerializedCacheFile = value; break;
                    case "-FirstImageIndex": FirstImageIndex = integer; break;
                    case "-FirstUnseenImageIndex": _firstUnseenImageIndex = integer; break;
                    case "-AutoRestartEveryNImages": AutoRestartEveryNImages = integer; break;
                    case "-MaxImagePixelDimension": MaxImagePixelDimension = integer; break;
                    case "-DesiredFrameRate": DesiredFrameRate = integer; break;
                }
            }
        }
        if (folders.Count > 0)
            SetImageFolders(folders.ToArray());
    }

    /// <summary>
    /// Returns command line parameters to start the application in the same state as now.
    /// </summary>
    public string GetCommandLine()
    {
        static string BooleanText(bool value) => value ? "True" : "False";

        StringBuilder result = new();
        for (int i = 0; i < 3; i++)
            if (ImageFolders.Length > i)
                result.Append($" -ImageFolder{i + 1} \"{ImageFolders[i]}\"");
        result.Append(" -Layout ").Append(Layout);
        result.Append(" -PreviousLayout ").Append(_previousLayout);
        result.Append(" -AutoScaling ").Append((int)Fit);
        result.Append(" -AutoScalingWhenZoomed ").Append((int)FitWhenZoomed);
        result.Append(" -ExtraZoom ").Append(ExtraZoom);
        result.Append(" -IsBlackBackground ").Append(BooleanText(IsBlackBackground));
        result.Append(" -IsFullScreen ").Append(BooleanText(IsFullScreen));
        result.Append(" -EnableAnimations ").Append(BooleanText(EnableAnimations));
        result.Append(" -GroupImages ").Append(BooleanText(GroupImages));
        result.Append(" -RotationAngle ").Append(Angle);
        result.Append(" -Mirror ").Append(BooleanText(IsMirror));
        result.Append(" -Portrait ").Append(BooleanText(IsPortrait));
        result.Append($" -SerializedWindowRectangle \"{SerializedWindowRectangle}\"");
        result.Append($" -SerializedCacheFile \"{SerializedCacheFile}\"");
        result.Append($" -FirstImageIndex {FirstImageIndex}");
        result.Append($" -FirstUnseenImageIndex {_firstUnseenImageIndex}");
        result.Append($" -MaxImagePixelDimension {MaxImagePixelDimension}");
        result.Append($" -DesiredFrameRate {DesiredFrameRate}");
        return result.ToString();
    }
}