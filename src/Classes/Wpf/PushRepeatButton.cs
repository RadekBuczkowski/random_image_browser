namespace Random_Image.Classes.Wpf;

using System.Windows;
using System.Windows.Controls.Primitives;

using Random_Image.Classes.Browser;

/// <summary>
/// Provides a WPF component implementing a repeat button that can be pushed programmatically.
/// </summary>
public class PushRepeatButton : RepeatButton
{
    public static readonly DependencyProperty IsPushedProperty =
        DependencyProperty.RegisterAttached("IsPushed", typeof(bool), typeof(PushRepeatButton), new PropertyMetadata(false));

    public static readonly DependencyProperty AngleProperty =
        DependencyProperty.RegisterAttached("Angle", typeof(double), typeof(PushRepeatButton), new PropertyMetadata(0.0));

    public PushRepeatButton() : base()
    {
        // Note: the speed at the beginning of the navigation animation is close to linear
        // which enables fluid repeated scrolling.
        int delay = ImageBrowserConstants.AnimationStepDuration / 2;
        Delay = delay;
        Interval = delay;
    }

    /// <summary>
    /// <see langword="true"/> if the button is pushed down, <see langword="false"/> if pulled up.
    /// </summary>
    public bool IsPushed
    {
        get => (bool)GetValue(IsPushedProperty);
        set
        {
            SetValue(IsPushedProperty, value);
            IsPressed = value;  // Note: the style reacts to changes in the IsPressed property.
        }
    }

    /// <summary>
    /// Allows rotating round buttons to get lights from a different angle.
    /// </summary>
    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    /// <summary>
    /// Forces the button to push down (<see langword="true"/>) or pull up (<see langword="false"/>).
    /// </summary>
    public void Push(bool isPushed = true)
    {
        if (IsPushed != isPushed)
            IsPushed = isPushed;  // Note: in XAML we use the dependency property "IsPushed" to trigger a style change.
    }
}