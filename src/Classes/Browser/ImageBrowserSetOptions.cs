namespace Random_Image.Classes;

using System.Linq;
using System.Runtime.CompilerServices;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;
using Random_Image.Properties;
using Random_Image.Resources;

/// <summary>
/// Provides a class for setting browser options. The class supports WPF bindings to its internal properties.
/// </summary>
public partial class ImageBrowserSetOptions : BindingClass
{
    private const int ChangeDelay = 800;

    public string[] AvailableFolders { get; } = ImageBrowserConstants.SystemFolders;

    public string[] DefaultFileExtensions { get; } = new string[]
        { (string)Settings.Default.Properties["ImageExtensions"].DefaultValue };

    public string[] ValuesOfRestartEveryNImages { get; } = new string[]
        { Text.DisabledOption, "200", "300", "500", "1000", "2000", "5000" };

    public string[] ValuesOfMaxPixelDimension { get; } = new string[]
        { Text.DisabledOption, "720", "1280", "1920", "3840", "7680" };

    public string[] ValuesOfDesiredFrameRate { get; } = new string[] { "10", "30", "60", "120" };

    private readonly ImageBrowser _browser;
    private int _layout;
    private bool _fullScreen;
    private bool _blackBackground;
    private bool _enableAnimations;
    private bool _groupImages;
    private string _folder1;
    private string _folder2;
    private string _folder3;
    private string _fileExtensions;
    private long _changeTime = 0;

    public ImageBrowserSetOptions(ImageBrowser browser)
    {
        _browser = browser;
        _browser.LoadNextRowIfNotLoaded();  // make the row visible in case full load is checked
        EnableAnimations = _browser.EnableAnimations;
        GroupImages = _browser.State.GroupImages;
        Folder1 = GetFolder(1);
        Folder2 = GetFolder(2);
        Folder3 = GetFolder(3);
        FileExtensions = _browser.State.ImageExtensions.CombineItems();
        AvailableFolders = string.Empty.Yield()
                                 .Concat(Folders.Where(item => string.IsNullOrWhiteSpace(item) == false))
                                 .Concat(AvailableFolders).ToArray();
    }

    private static int _rememberSelectedTab;

    /// <summary>
    /// The index of the selected Tab in the Options dialog. The field underlying this property is static,
    /// so the selected tab will be remembered when the dialog is opened again.
    /// </summary>
    public int SelectedTab
    {
        get => _rememberSelectedTab;
        set
        {
            _rememberSelectedTab = value;
            if (value == 1 && _browser.State.IsZoomed)
                _browser.GoBack();
        }
    }

    /// <summary>
    /// Type of auto-scaling to fit images to their canvas slots. Used when not zoomed.
    /// Corresponds to <see cref="ImageBrowserState.Fit"/>.
    /// </summary>
    public AutoScalingModes Fit
    {
        get => _browser.State.Fit;
        set
        {
            if (value != _browser.State.Fit)
            {
                _browser.SetAutoScaling(value, false);
                Notify();
            }
        }
    }

    /// <summary>
    /// Type of auto-scaling to fit images to the entire canvas. Used when zoomed.
    /// Corresponds to <see cref="ImageBrowserState.FitWhenZoomed"/>.
    /// </summary>
    public AutoScalingModes FitWhenZoomed
    {
        get => _browser.State.FitWhenZoomed;
        set
        {
            if (value != _browser.State.FitWhenZoomed)
            {
                _browser.SetAutoScaling(value, true);
                Notify();
            }
        }
    }

    /// <summary>
    /// The arrangement and number of images visible on canvas.
    /// Corresponds to <see cref="ImageBrowserState.Layout"/>.
    /// </summary>
    public int Layout
    {
        get => IsInProgress() ? _layout : _browser.State.Layout;
        set
        {
            if (value != Layout)
            {
                _layout = value; ;
                _browser.SetLayout(value);
                NotifyTwice();
            }
        }
    }

    /// <summary>
    /// <see langword="true"/> for black window background, <see langword="false"/> for white.
    /// Corresponds to <see cref="ImageBrowserState.IsBlackBackground"/>.
    /// </summary>
    public bool IsBlackBackground
    {
        get => IsInProgress() ? _blackBackground : _browser.ImageCanvas.IsBlackBackground;
        set
        {
            if (value != IsBlackBackground)
            {
                _blackBackground = value;
                _browser.ToggleBackground();
                NotifyTwice();
            }
        }
    }

    /// <summary>
    /// <see langword="true"/> if the application window is in full-screen mode, or <see langword="false"/>
    /// if in normal window mode.
    /// Corresponds to <see cref="ImageBrowserState.IsFullScreen"/>.
    /// </summary>
    public bool IsFullScreen
    {
        get => IsInProgress() ? _fullScreen : _browser.ImageCanvas.Window.IsFullScreen();
        set
        {
            if (value != IsFullScreen)
            {
                _fullScreen = value;
                _browser.ImageCanvas.Window.ToggleFullScreen();
                NotifyTwice();
            }
        }
    }

    /// <summary>
    /// The browser should enable animations. Corresponds to <see cref="ImageBrowser.EnableAnimations"/>.
    /// </summary>
    public bool EnableAnimations
    {
        get => _enableAnimations;
        set
        {
            _enableAnimations = value;
            Notify();
        }
    }

    /// <summary>
    /// The browser should enable animations. Corresponds to <see cref="ImageBrowserState.GroupImages"/>.
    /// </summary>
    public bool GroupImages
    {
        get => _groupImages;
        set
        {
            _groupImages = value;
            Notify();
        }
    }

    /// <summary>
    /// Image folder #1. Corresponds to <see cref="ImageBrowserState.ImageFolders[0]"/>.
    /// </summary>
    public string Folder1
    {
        get => _folder1;
        set
        {
            _folder1 = value.TrimEmpty();
            Notify();
        }
    }

    /// <summary>
    /// Image folder #2. Corresponds to <see cref="ImageBrowserState.ImageFolders[1]"/>.
    /// </summary>
    public string Folder2
    {
        get => _folder2;
        set
        {
            _folder2 = value.TrimEmpty();
            Notify();
        }
    }

    /// <summary>
    /// Image folder #3. Corresponds to <see cref="ImageBrowserState.ImageFolders[2]"/>.
    /// </summary>
    public string Folder3
    {
        get => _folder3;
        set
        {
            _folder3 = value.TrimEmpty();
            Notify();
        }
    }

    /// <summary>
    /// Image folders. Corresponds to <see cref="ImageBrowserState.ImageFolders"/>.
    /// </summary>
    private string[] Folders => new string[] { Folder1, Folder2, Folder3 };

    /// <summary>
    ///  Supported image file extensions. Corresponds to <see cref="ImageBrowserState.ImageExtensions"/>.
    /// </summary>
    public string FileExtensions
    {
        get => _fileExtensions;
        set
        {
            _fileExtensions = value;
            Notify();
        }
    }

    /// <summary>
    /// Restarts the application after the specified number of images.
    /// Corresponds to <see cref="ImageBrowserState.AutoRestartEveryNImages"/>.
    /// </summary>
    public int RestartEveryNImages
    {
        get => _browser.State.AutoRestartEveryNImages;
        set
        {
            if (value != RestartEveryNImages)
            {
                _browser.State.AutoRestartEveryNImages = value;
                Notify();
            }
        }
    }

    /// <summary>
    /// Images with either width or height larger than this value will be scaled down when loading.
    /// Corresponds to <see cref="ImageBrowserState.MaxImagePixelDimension"/>.
    /// </summary>
    public int MaxPixelDimension
    {
        get => ImageBrowserState.MaxImagePixelDimension;
        set
        {
            if (value != MaxPixelDimension)
            {
                ImageBrowserState.MaxImagePixelDimension = value;
                Notify();
            }
        }
    }

    /// <summary>
    /// The desired frame rate for all animations.
    /// Corresponds to <see cref="ImageBrowserState.DesiredFrameRate"/>.
    /// </summary>
    public int DesiredFrameRate
    {
        get => ImageBrowserState.DesiredFrameRate;
        set
        {
            if (value != DesiredFrameRate)
            {
                ImageBrowserState.DesiredFrameRate = value;
                Notify();
            }
        }
    }

    /// <summary>
    /// <see langword="true"/> if current settings are different than defaults in user.config.
    /// </summary>
    public bool Modified
    {
        get => Settings.Default.AutoScaling != (byte)_browser.State.Fit ||
            Settings.Default.AutoScalingWhenZoomed != (byte)_browser.State.FitWhenZoomed ||
            Settings.Default.Layout != (byte)_browser.State.Layout ||
            Settings.Default.IsBlackBackground != _browser.ImageCanvas.IsBlackBackground ||
            Settings.Default.IsFullScreen != _browser.ImageCanvas.Window.IsFullScreen() ||
            Settings.Default.EnableAnimations != EnableAnimations ||
            Settings.Default.GroupImages != GroupImages ||
            Settings.Default.ImageFolder1 != Folder1 ||
            Settings.Default.ImageFolder2 != Folder2 ||
            Settings.Default.ImageFolder3 != Folder3 ||
            Settings.Default.ImageExtensions.AreNonEmptyItemsEqual(FileExtensions) == false ||
            Settings.Default.AutoRestartEveryNImages != RestartEveryNImages ||
            Settings.Default.MaxImagePixelDimension != MaxPixelDimension ||
            Settings.Default.DesiredFrameRate != DesiredFrameRate;
        set => Notify();
    }

    /// <summary>
    /// Updates the default settings in App.config with the current values.
    /// </summary>
    public void SetAsDefault()
    {
        Settings.Default.AutoScaling = (byte)_browser.State.Fit;
        Settings.Default.AutoScalingWhenZoomed = (byte)_browser.State.FitWhenZoomed;
        Settings.Default.ExtraZoom = Settings.Default.ExtraZoom;
        Settings.Default.Layout = (byte)_browser.State.Layout;
        Settings.Default.IsBlackBackground = _browser.ImageCanvas.IsBlackBackground;
        Settings.Default.IsFullScreen = _browser.ImageCanvas.Window.IsFullScreen();
        Settings.Default.EnableAnimations = EnableAnimations;
        Settings.Default.GroupImages = GroupImages;
        Settings.Default.ImageFolder1 = Folder1;
        Settings.Default.ImageFolder2 = Folder2;
        Settings.Default.ImageFolder3 = Folder3;
        Settings.Default.ImageExtensions = FileExtensions;
        Settings.Default.AutoRestartEveryNImages = RestartEveryNImages;
        Settings.Default.LargeImagePixelDimension = Settings.Default.LargeImagePixelDimension;
        Settings.Default.MaxImagePixelDimension = MaxPixelDimension;
        Settings.Default.DesiredFrameRate = DesiredFrameRate;
        Settings.Default.Language = Settings.Default.Language;
        Settings.Default.Save();
        Notify(nameof(Modified));
    }

    /// <summary>
    /// Opens the folder selection dialog and updates the specified property with the selected folder.
    /// The property name is specified in the "Tag" property of the button.
    /// </summary>
    public void ShowFolderDialog(string propertyName)
    {
        PathHelper.ShowFolderDialog(SetFolders, this.GetPropertyValue<string>(propertyName));
        void SetFolders(string[] folders, string[] _) => this.SetPropertyValue(propertyName, folders[0]);
    }

    /// <summary>
    /// Verify if some changes need images to be reloaded and reload the images.
    /// </summary>
    public void Apply()
    {
        bool reload = _browser.State.ImageFolders.AreNonEmptyItemsEqual(Folders) == false ||
            _browser.State.ImageExtensions.AreNonEmptyItemsEqual(FileExtensions.ExtractItems()) == false;
        bool toggleAnimations = EnableAnimations != _browser.EnableAnimations;
        bool toggleGrouping = GroupImages != _browser.State.GroupImages;
        if (reload || toggleAnimations || toggleGrouping)
        {
            _browser.State.SetImageFolders(Folders, FileExtensions.ExtractItems());
            _browser.ResetBrowser(reload, toggleGrouping, toggleAnimations);
        }
    }

    /// <summary>
    /// Returns one of the three folders specified in the Options dialog and the App.config file.
    /// </summary>
    private string GetFolder(int index)
    {
        if (index > 0 && index <= _browser.State.ImageFolders.Length)
            return _browser.State.ImageFolders[index - 1];
        return string.Empty;
    }

    /// <summary>
    /// Returns true if the change is in progress. Note: When the background is changing, it is not possible
    /// to determine if the background is white or black. Similar with full-screen changing. 
    /// </summary>
    private bool IsInProgress() => SimpleTimer.IsTimerRunning(ref _changeTime, ChangeDelay - 100);

    /// <summary>
    /// Notifies the UI that the specified property or the caller property has changed value.
    /// The notification is sent a second time after a delay.
    /// </summary>
    private void NotifyTwice([CallerMemberName] string name = null)
    {
        SimpleTimer.StartTimer(ref _changeTime);
        Notify(name);
        MainThread.Invoke(ChangeDelay, () => Notify(name));
    }

    /// <summary>
    /// Notifies the UI that the specified property or the caller property has changed value.
    /// </summary>
    protected override void Notify([CallerMemberName] string name = null)
    {
        base.Notify(name);
        base.Notify(nameof(Modified));
    }
}