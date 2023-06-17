namespace Random_Image.Classes;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;

/// <summary>
/// Interaction logic for the main window of the application.
/// </summary>
public partial class MainWindow : Window
{
    private ImageBrowser Browser { get; set; }

    public MainWindow()
    {
        MainThread.SetGlobalLanguage();
        InitializeComponent();
        ImageBrowserStateDurable state = new(App.CommandLine);
        Browser = state.EnableAnimations ?
            new ImageBrowserAnimated(state, ImageCanvas) : new ImageBrowser(state, ImageCanvas);
        Browser.ShowTextEvent += ShowText;
        Browser.NavigationEvent += () => state.UpdateButtonTooltips(ButtonUp, ButtonDown);
        Browser.ResetBrowserEvent += ResetBrowser;
    }

    private void ShowText(string text, bool expand)
    {
        MessagePanelLabel.Text = text;
        if (expand)
            MessagePanel.Expand();  // show the message panel by sliding it out
    }

    private void ResetBrowser(bool reload = true, bool toggleGrouping = false, bool toggleAnimations = false)
    {
        if (toggleAnimations)
            Browser = Browser.EnableAnimations ? new ImageBrowser(Browser) : new ImageBrowserAnimated(Browser);
        if (reload)
            Initialize(toggleGrouping);
    }

    private void Initialize(bool toggleGrouping = false)
    {
        RotatingCircle.Visibility = FolderLog.Visibility = Visibility.Visible;
        MainMessageLabel.Visibility = Visibility.Collapsed;
        ButtonUp.IsEnabled = ButtonDown.IsEnabled = false;
        // Show a rotating circle animation for 1 second.
        Browser.Initialize(toggleGrouping, progressDelay: 1000, Progress, Finished);
    }

    private void Progress(string folders)
    {
        // After 1 second hide the rotating circle and instead show the folders being scanned.
        RotatingCircle.Visibility = Visibility.Collapsed;
        FolderLog.Text = folders;
        FolderLog.ScrollToVerticalOffset(double.MaxValue);
    }

    private void Finished()
    {
        RotatingCircle.Visibility = FolderLog.Visibility = Visibility.Collapsed;
        FolderLog.Text = string.Empty;
        MainMessageLabel.SetNoFilesFoundMessageLabel(Browser.State.FileCount);
        if (Browser.State.FileCount > 0)
        {
            ButtonMenuPanel.Expand(showDelay: 500, autoHideDelay: 500);
            ButtonUpPanel.Expand(showDelay: 500, autoHideDelay: 500);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ImageTag.SetWindowScaling(this.GetScalingFactor());
        Browser.State.RestoreWindow(this);
        this.CaptureTitleBarDoubleClick();
        this.BringToFront();
        SizeChanged += Window_SizeChanged;
        if (Browser.IsReady)
        {
            Finished();
            Browser.LoadImages(0, Reasons.Restart);
        }
        else
            Initialize();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Refresh with a delay to make time for canvas to be resized too.
        MainThread.Invoke(100, () => Browser.RefreshImages(Reasons.None, extraRows: 2));
        e.Handled = true;
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        Browser.ShakeImages(true);
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Environment.Exit(0);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Browser.HandleKeyboard(e) == false)
        {
            int delta = Browser.State.GetNavigationDelta(e.Key, ButtonDown.Delay);
            if (delta != 0 && Browser.IsReady)
            {
                Browser.LoadImages(delta);
                ButtonUpPanel.Expand();
                e.Key.PushButton(ButtonUp, ButtonDown, true);
            }
        }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        e.Key.PushButton(ButtonUp, ButtonDown, false);
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Browser.Window_MouseDown(e);
    }

    private void Window_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.Timestamp == int.MaxValue)  // Event raised in the window handler added by CaptureTitleBarDoubleClick().
        {
            Browser.ToggleFullScreen();
            e.Handled = true;
        }
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        Browser.Window_MouseWheel(e);
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        e.Handled = true;
        ImageCanvas.HideTooltip();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        MessagePanel.Expand(ImageCanvas.IsNearMessagePanel(MessagePanel.ActualWidth));
        if (Browser.IsReady == false)
            return;
        ButtonMenuPanel.Expand(ImageCanvas.IsNearMenuButtons());
        ButtonUpPanel.Expand(ImageCanvas.IsNearNavigationButtons());
        ImageCanvas.HideTooltip(onlyWhenMoved: true);
        Browser.SetMouseCursor();
        if (Browser.State.IsZoomed && Browser.ImageAtCursor() is Image image)
            Image_MouseMove(image, e);
    }

    private void Image_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Browser.Image_MouseDown(sender, e);
    }

    private void Image_MouseEnter(object sender, MouseEventArgs e)
    {
        ImageCanvas.ShowTooltip(sender as Image);
        Browser.AlignImage(sender as Image);
    }

    private void Image_MouseLeave(object sender, MouseEventArgs e)
    {
        ImageCanvas.HideTooltip(onlyWhenMoved: true);
        Browser.AlignImageToCenter(sender as Image);
    }

    private void Image_MouseMove(object sender, MouseEventArgs e)
    {
        Browser.SetMouseCursor(sender as Image);
        Browser.AlignImage(sender as Image);
    }

    private void ButtonHelp_Click(object sender, RoutedEventArgs e)
    {
        HelpWindow.Show(this);
    }

    private void ButtonOptions_Click(object sender, RoutedEventArgs e)
    {
        OptionsWindow.Show(Browser);
    }
    private void ButtonUp_Click(object sender, RoutedEventArgs e)
    {
        Browser.LoadImages(-Browser.State.CanvasColumns);
    }

    private void ButtonDown_Click(object sender, RoutedEventArgs e)
    {
        Browser.LoadImages(Browser.State.CanvasColumns);
    }

    private void Button_MouseEnter(object sender, MouseEventArgs e)
    {
        ImageCanvas.HideTooltip();
    }

    private void MessagePanel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            Browser.ShakeImages();
        if (e.ChangedButton.IsIn(MouseButton.Left, MouseButton.Right))
            e.Handled = true;
    }

    private void MessagePanel_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        Browser.LoadImages(Browser.State.CanvasColumns * Math.Sign(-e.Delta));
    }
}