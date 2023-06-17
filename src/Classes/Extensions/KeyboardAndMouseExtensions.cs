using Random_Image.Classes.Browser;

namespace Random_Image.Classes.Extensions;

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;
using Random_Image.Resources;

/// <summary>
/// Provides a class with extension methods for handling the keyboard and mouse in the image browser.
/// </summary>
public static class KeyboardAndMouseExtensions
{
    /// <summary>
    /// Handles keyboard. Some keyboard letters can be different in different language versions,
    /// e.g. the mirror images command has keyboard shortcut Key.M in the English version,
    /// but Key.S in the Danish version.
    /// </summary>
    public static bool HandleKeyboard(this ImageBrowser browser, KeyEventArgs e)
    {
        Dictionary<string, Action> commands = new()
        {
            [Text.KeyAnimations] = () => browser.ResetBrowser(toggleAnimations: true),
            [Text.KeyBackground] = browser.ToggleBackground,
            [Text.KeyGoToRandom] = browser.GoToRandomImage,
            [Text.KeyMirror] = browser.MirrorImages,
            [Text.KeyNormalImages] = browser.NormalImages,
            [Text.KeyPauseResume] = browser.TogglePauseOrResumeAnimations,
            [Text.KeyRotateLeft] = () => browser.RotateImages(false),
            [Text.KeyRotateRight] = () => browser.RotateImages(true),
            [Text.KeyUpsideDown] = browser.ToggleImagesUpsideDown,
            [Key.D2.ToString()] = () => browser.SetLayout(2),
            [Key.D3.ToString()] = () => browser.SetLayout(3),
            [Key.D3.ToString()] = () => browser.SetLayout(4),
            [Key.D5.ToString()] = () => browser.SetLayout(5),
            [Key.D6.ToString()] = () => browser.SetLayout(6),
            [Key.D7.ToString()] = () => browser.SetLayout(7),
            [Key.D8.ToString()] = () => browser.SetLayout(8),
            [Key.D9.ToString()] = () => browser.SetLayout(9),
            [Key.D0.ToString()] = () => browser.SetLayout(10),
            [Key.OemMinus.ToString()] = () => browser.SetLayout(11),
            [Key.OemPlus.ToString()] = () => browser.SetLayout(12),
            [Key.Add.ToString()] = () => browser.ChangeZoomFactorOrLayout(true),
            [Key.Subtract.ToString()] = () => browser.ChangeZoomFactorOrLayout(false),
            [Key.Tab.ToString()] = () => browser.ToggleAutoScaling(),
            [Key.Enter.ToString()] = browser.ToggleFullScreen,
            [Key.Escape.ToString()] = browser.GoBackOrUndo,
            [Key.Space.ToString()] = () => browser.ShakeImages(),
            [Key.F1.ToString()] = () => HelpWindow.Show(browser.ImageCanvas.Window),
            [Key.F2.ToString()] = () => browser.ResetBrowser(setImageFolder: true),
            [Key.F3.ToString()] = () => browser.ResetBrowser(toggleGrouping: true),
            [Key.F4.ToString()] = browser.RestartApplication,
            [Key.F8.ToString()] = browser.ShowDebugInfo,  // debug command
            [Key.F10.ToString()] = () => OptionsWindow.Show(browser),
            [Key.F11.ToString()] = ResourceHelper.OpenUserConfigFile
        };
        Key key = e.SystemKey == Key.F10 ? Key.F10 : e.Key;
        if (commands.TryGetValue(key.ToString(), out Action action))
        {
            action();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Handles the mouse down event for the entire window.
    /// </summary>
    public static void Window_MouseDown(this ImageBrowser browser, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
        {
            if (e.ClickCount == 1 && browser.State.IsZoomed)
            {
                browser.GoBack();
            }
            else if (e.ClickCount == 2 && browser.ImageCanvas.IsNearNavigationButtons() == false &&
                browser.IsAnimating == false)
            {
                browser.ToggleFullScreen();
            }
        }
        else if (e.ChangedButton == MouseButton.Middle && e.MiddleButton == MouseButtonState.Pressed)
            browser.ToggleAutoScaling();
        else if (e.ChangedButton == MouseButton.XButton1 && e.XButton1 == MouseButtonState.Pressed)
            browser.PresentImagesByAligning(false);
        else if (e.ChangedButton == MouseButton.XButton2 && e.XButton2 == MouseButtonState.Pressed)
            browser.PresentImagesByAligning(true);
        else if (e.ChangedButton == MouseButton.Right && e.RightButton == MouseButtonState.Pressed && e.ClickCount == 1)
            browser.RefreshImages(Reasons.ChangeBackground);
    }

    /// <summary>
    /// Handles the mouse down event when pressed on an image.
    /// </summary>
    public static void Image_MouseDown(this ImageBrowser browser, object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1)
            e.Handled = true;  // Prevent full-screen on double-clicking an image.
        else if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 1)
        {
            e.Handled = true;
            browser.GoIn(sender as Image);
        }
        else if (e.ChangedButton == MouseButton.Right && e.RightButton == MouseButtonState.Pressed && e.ClickCount == 1)
        {
            e.Handled = true;
            (sender as Image).Tag().StartIrfanView();
        }
    }

    /// <summary>
    /// Handles the mouse wheel event for the entire window.
    /// </summary>
    public static void Window_MouseWheel(this ImageBrowser browser, MouseWheelEventArgs e)
    {
        e.Handled = true;
        if (e.Delta != 0 && browser.IsReady)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                browser.ChangeZoomFactorOrLayout(e.Delta > 0);
            else if (browser.CanScroll())
            {
                if (browser.State.GetCompletedWheelScrolls(-e.Delta) != 0)
                    browser.LoadImages(browser.State.CanvasColumns * Math.Sign(-e.Delta));
            }
            else if (browser.ImageAtCursor() is Image image)
                browser.LoadImage(image, Reasons.ImageNeighbor, Math.Sign(-e.Delta));
        }
    }

    /// <summary>
    /// If this key activates the blue navigation buttons on screen, push or pull the respective button.
    /// </summary>
    public static void PushButton(this Key key, PushRepeatButton buttonUp, PushRepeatButton buttonDown, bool isPushed)
    {
        if (key.IsIn(Key.Up, Key.PageUp, Key.Left))
            buttonUp.Push(isPushed);
        else if (key.IsIn(Key.Down, Key.PageDown, Key.Right))
            buttonDown.Push(isPushed);
    }
}