namespace Random_Image.Classes.Extensions;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using Random_Image.Classes.Utilities;
using Random_Image.Resources;

/// <summary>
/// Provides extension methods for Window and FrameworkElement manipulations.
/// </summary>
public static class WindowExtensionMethods
{
    /// <summary>
    /// Returns the location and dimensions of this <paramref name="window"/> as a serialized string.
    /// </summary>
    public static string SerializeWindowRectangle(this Window window)
    {
        if (window.WindowStyle == WindowStyle.None)
            return string.Empty;
        if (window.WindowState == WindowState.Maximized)
            return "max";
        double width = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
        double height = window.ActualHeight > 0 ? window.ActualHeight : window.Height;
        return $"{window.Left},{window.Top},{width},{height}";
    }

    /// <summary>
    /// Applies the location and dimensions from a serialized string to this <paramref name="window"/>.
    /// </summary>
    public static void ApplyWindowRectangle(this Window window, string rectangle)
    {
        if (string.IsNullOrEmpty(rectangle))
            return;
        if (rectangle == "max")
        {
            window.WindowState = WindowState.Maximized;
            return;
        }
        double[] values = rectangle.Split(',').Select(text => double.TryParse(text, out double value) ? value : 0).ToArray();
        if (values.Length != 4)
            return;
        window.Left = values[0];
        window.Top = values[1];
        if (values[2] > 0)
            window.Width = values[2];
        if (values[3] > 0)
            window.Height = values[3];
    }

    /// <summary>
    /// Returns <see langword="true"/> if this <paramref name="window"/> is in the full screen mode.
    /// </summary>
    public static bool IsFullScreen(this Window window) => window.WindowStyle == WindowStyle.None;

    /// <summary>
    /// Changes between full screen mode and normal screen mode for this <paramref name="window"/>,
    /// and returns <see langword="true"/> if the mode has changed.
    /// </summary>
    /// <param name="state"><see langword="null"/>: toggle between full screen and normal screen,
    /// <see langword="true"/>: set full screen, <see langword="false"/>: set normal screen</param>
    public static bool ToggleFullScreen(this Window window, bool? state = null)
    {
        if (window.WindowStyle == WindowStyle.None)
        {
            if (state != true)
            {
                // Switch to normal window mode.
                window.WindowState = WindowState.Normal;
                window.WindowStyle = WindowStyle.ThreeDBorderWindow;
                return true;
            }
        }
        else if (state != false)
        {
            // Check if the window is maximized but is not in the full-screen mode.
            if (window.WindowState == WindowState.Maximized)
            {
                // We need to switch to normal window first - it's a known bug in .NET
                window.WindowState = WindowState.Normal;
                MainThread.Invoke(500, () =>
                {
                    // Switch to full-screen window mode.
                    window.WindowStyle = WindowStyle.None;
                    window.WindowState = WindowState.Maximized;
                });
            }
            else
            {
                // Switch to full-screen window mode.
                window.WindowStyle = WindowStyle.None;
                window.WindowState = WindowState.Maximized;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the Windows scaling factor (e.g. 100%, 125%, 175%, 200%, etc.) for this <paramref name="window"/>.
    /// </summary>
    public static Vector GetScalingFactor(this Window window)
    {
        PresentationSource source = PresentationSource.FromVisual(window);
        if (source != null)
            return new Vector(source.CompositionTarget.TransformToDevice.M11, source.CompositionTarget.TransformToDevice.M22);
        return new Vector(1, 1);
    }

    /// <summary>
    /// Brings this <paramref name="window"/> to the front of all windows on desktop.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Needs a real window to work")]
    public static void BringToFront(this Window window)
    {
        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();
    }

    /// <summary>
    /// Captures mouse double-clicks on this window's title-bar and prevents maximizing the window.
    /// Instead, the <see cref="Control.PreviewMouseDoubleClickEvent"/> will be raised, which can start
    /// full-screen mode if needed.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Needs a real window to work")]
    public static void CaptureTitleBarDoubleClick(this Window window)
    {
        static IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            if (msg == WM_NCLBUTTONDBLCLK)
            {
                handled = true;
                Application.Current.MainWindow.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,
                    int.MaxValue, MouseButton.Left)
                { RoutedEvent = Control.PreviewMouseDoubleClickEvent });
            }
            return IntPtr.Zero;
        }
        HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
        source.AddHook(new HwndSourceHook(WndProc));
    }

    /// <summary>
    /// Sets the tooltip <paramref name="text"/> and a common tooltip color on this <paramref name="element"/>.
    /// </summary>
    public static void SetTooltip(this FrameworkElement element, string text)
    {
        TextBlock block = new();
        block.Inlines.Add(text);
        if (element.ToolTip is not ToolTip tip)
            element.ToolTip = tip = new();
        tip.Content = block;
        tip.Background = new BrushConverter().ConvertFromString("#D0FFFFE1") as SolidColorBrush;
        tip.HasDropShadow = false;
    }

    /// <summary>
    /// Returns the tooltip text currently set on this <paramref name="element"/>.
    /// </summary>
    public static string GetTooltipText(this FrameworkElement element)
    {
        return element.ToolTip is ToolTip tooltip && tooltip.Content is TextBlock block ? block.Text : null;
    }

    /// <summary>
    /// Returns <see langword="true"/> if this <paramref name="element"/> has a tooltip shown.
    /// </summary>
    public static bool IsTooltipShown(this FrameworkElement element)
    {
        return element?.ToolTip is ToolTip tooltip && tooltip.IsOpen;
    }

    /// <summary>
    /// Shows the tooltip of this <paramref name="element"/> and remembers the mouse location.
    /// </summary>
    public static void ShowTooltip(this FrameworkElement element)
    {
        if (element?.ToolTip is ToolTip tooltip && tooltip.IsOpen == false)
        {
            tooltip.IsOpen = true;
            tooltip.Tag = Mouse.GetPosition(element.FindVisualParent<Window>());
        }
    }

    /// <summary>
    /// Hides the tooltip of this <paramref name="element"/>. Optionally, hides the tooltip only if the mouse cursor
    /// has moved more than the specified <paramref name="distance"/> parameter (in device-independent points)
    /// since the tooltip was shown.
    /// </summary>
    public static void HideTooltip(this FrameworkElement element, int distance = 0)
    {
        if (element?.ToolTip is ToolTip tooltip && tooltip.IsOpen)
            if (distance <= 0 || tooltip.Tag is Point point &&
                Mouse.GetPosition(element.FindVisualParent<Window>()).DistanceTo(point) > distance)
                tooltip.IsOpen = false;
    }

    /// <summary>
    /// Sets the mouse cursor of this <paramref name="element"/>.
    /// </summary>
    public static void SetCursor(this FrameworkElement element, Cursor cursor)
    {
        if (element.Cursor != cursor)
            element.Cursor = cursor;
    }

    /// <summary>
    /// Sets this image visibility if the <paramref name="visible"/> is not <see langword="null"/>.
    /// </summary>
    public static void SetVisibility(this FrameworkElement element, bool visible)
    {
        Visibility visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (element.Visibility != visibility)
            element.Visibility = visibility;
    }

    /// <summary>
    /// Sets the z-index specifying the order on the z-plane (top or bottom) in which this image appears.
    /// </summary>
    public static void SetZIndex(this FrameworkElement element, int index)
    {
        if (Panel.GetZIndex(element) != index)
            Panel.SetZIndex(element, index);
    }

    /// <summary>
    /// If there are no image files found, shows a message about how to select an image folder.
    /// </summary>
    public static void SetNoFilesFoundMessageLabel(this TextBlock label, int fileCount)
    {
        label.Text = string.Empty;
        label.SetVisibility(fileCount == 0);
        if (fileCount == 0)
        {
            string[] text = Text.SelectAFolderOrUseOptions.Split("{0}");
            label.Inlines.Add(new Run(text[0]));
            label.Inlines.Add(new Run(Text.OptionsTitle) { FontWeight = FontWeights.Bold });
            label.Inlines.Add(new Run(text[1]));
        }
    }
}