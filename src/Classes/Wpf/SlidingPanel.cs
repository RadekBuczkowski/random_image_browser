namespace Random_Image.Classes.Wpf;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using Random_Image.Classes.Utilities;

/// <summary>
/// Provides an animated WPF component implementing a sliding panel that can be expanded or closed programmatically.
/// </summary>
public class SlidingPanel : Border
{
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.RegisterAttached("IsExpanded", typeof(bool), typeof(SlidingPanel), new PropertyMetadata(false));

    public static readonly DependencyProperty IsVerticalProperty =
        DependencyProperty.RegisterAttached("IsVertical", typeof(bool), typeof(SlidingPanel), new PropertyMetadata(false));

    public static readonly DependencyProperty TwinProperty =
        DependencyProperty.RegisterAttached("Twin", typeof(SlidingPanel), typeof(SlidingPanel), new PropertyMetadata(null));

    /// <summary>
    /// Specifies if the panel is currently in the expanded state (<see langword="true"/>)
    /// or hidden state (<see langword="false"/>).
    /// </summary>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Specifies if the panel slides vertically (<see langword="true"/>) or horizontally (<see langword="false"/>).
    /// </summary>
    public bool IsVertical
    {
        get => (bool)GetValue(IsVerticalProperty);
        set => SetValue(IsVerticalProperty, value);
    }

    /// <summary>
    /// The sliding panel has a twin that must slide in and out at the same time.
    /// </summary>
    public SlidingPanel Twin
    {
        get => (SlidingPanel)GetValue(TwinProperty);
        set => SetValue(TwinProperty, value);
    }

    /// <summary>
    /// Specifies if the panel is fully out and can slide in and hide.
    /// </summary>
    public bool IsFullyExpanded => RenderTransform is TranslateTransform transform &&
        (transform.X == 0 && IsVertical == false || transform.Y == 0 && IsVertical);

    private DispatcherTimer _expandTimer = null;

    /// <summary>
    /// Forces the sliding panel to slide out and show (<paramref name="isExpanded"/> == <see langword="true"/>)
    /// or slide in and hide (<paramref name="isExpanded"/> == <see langword="false"/>). If the panel is not fully out
    /// yet or the mouse cursor is over it, it will keep showing. The optional <paramref name="showDelay"/> parameter
    /// specifies the time in milliseconds to delay sliding out and showing.
    /// If <paramref name="isExpanded"/> == <see langword="true"/>, the optional <paramref name="autoHideDelay"/>
    /// parameter specifies the time in milliseconds for the panel to automatically slide in and hide.
    /// </summary>
    public void Expand(bool isExpanded = true, int showDelay = 0, int autoHideDelay = 1200)
    {
        if (isExpanded && showDelay > 0)
        {
            MainThread.Invoke(ref _expandTimer, showDelay, () => Expand(isExpanded, 0, autoHideDelay));
            return;
        }
        if (MainThread.IsInvoked(_expandTimer))
            return;
        bool isMouseOver = IsMouseOver == true || Twin?.IsMouseOver == true;
        SetExpanded(isExpanded, isMouseOver);
        Twin?.SetExpanded(isExpanded, isMouseOver);
        if (isExpanded && autoHideDelay > 0)
            MainThread.Invoke(ref _expandTimer, autoHideDelay, () => Expand(false));
    }

    /// <summary>
    /// Note: in XAML we use the dependency property <see cref="IsExpanded"/> to trigger the sliding animation.
    /// </summary>
    private void SetExpanded(bool isExpanded, bool isMouseOver)
    {
        if (IsExpanded != isExpanded && (isExpanded || IsFullyExpanded && isMouseOver == false))
            IsExpanded = isExpanded;
    }
}