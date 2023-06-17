namespace Random_Image.Classes.Utilities;

using System;
using System.Windows;
using System.Windows.Media.Animation;

/// <summary>
/// Implements an easing function accelerating or decelerating animations and enabling fluid repeated scrolling.
/// </summary>
public class ContinualEase : EasingFunctionBase
{
    /// <summary>
    /// Implements an easing function accelerating or decelerating animations. It is a close alternative to
    /// <see cref="SineEase"/>. When decelerating animations have an increased and constant speed in the first half
    /// of the transformation, which corresponds to exactly one third of the animation time, and in the second half
    /// they smoothly slow down to 0. It allows for fluid repeated scrolling with a visible deceleration in the end.
    /// </summary>
    public ContinualEase() : base() { }

    /// <summary>
    /// Returns a number between 0 and 1 representing the animation progress. 0 is equivalent to the start value
    /// of the animated property and 1 to its end value. The <paramref name="timeFraction"/> is a number between 
    /// 0 and 1 specifying the animation time. 0 is equivalent to the animation start and 1 to the end, or vice versa,
    /// depending on the <see cref="EasingMode"/> value. This method is called from
    /// <see cref="EasingFunctionBase.Ease"/> using the following formula:
    /// <code>
    /// Acceleration (<see cref="EasingMode.EaseIn"/>):   currentProgress = EaseInCore(currentTime)
    /// Deceleration (<see cref="EasingMode.EaseOut"/>):  currentProgress = (1 - EaseInCore(1 - currentTime))
    /// Both (<see cref="EasingMode.EaseInOut"/>):        currentProgress = (currentTime &lt; 0.5) ?
    ///                         EaseInCore(2 * currentTime) / 2 : 1 - EaseInCore(2 * (1 - currentTime)) / 2
    /// </code>
    /// </summary>
    protected override double EaseInCore(double timeFraction)
    {
        // Note: The piecewise function is continuous and monotonically increasing in its domain but is not smooth at
        // argument 2/3 (is not differentiable there). Although both pieces of the function return exactly 0.5 at this
        // point, the derivatives of the pieces (y'=Sin(PI/2*x)*PI/2 and y'=1.5) differ slightly there: 1.35 and 1.5
        // respectively. Which means the change in speed is discontinuous at this point despite the entire
        // transformation progress is fluid. The discontinuity, however, is relatively small and not noticeable in
        // regular animations. The advantage is the constant speed for half of the transformation effect, allowing for
        // e.g. fluid repeated scrolling.

        if (timeFraction < 2.0 / 3.0)
            return 1.0 - Math.Cos(Math.PI * 0.5 * timeFraction);  // acceleration or deceleration
        else
            return 1.5 * timeFraction - 0.5;  // constant speed
    }

    protected override Freezable CreateInstanceCore() => new ContinualEase();
}