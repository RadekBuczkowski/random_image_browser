namespace Random_Image.Classes.Extensions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

using Random_Image.Classes.Browser;

/// <summary>
/// Provides extension methods for handling image transformation animations.
/// </summary>
public static class AnimationExtensionMethods
{
    /// <summary>
    /// Global, application-wide dictionary of animation clocks that can be stopped when the corresponding
    /// animatable object is removed. It controls animation clocks in the entire application,
    /// and all animations can be paused and resumed simultaneously.
    /// </summary>
    private static readonly ConcurrentDictionary<Animatable, List<AnimationClock>> ClockDictionary = new();

    /// <summary>
    /// Adds a <see langword="double"/>-type number animation to the specified <paramref name="property"/> in this
    /// <paramref name="animatable"/> element.
    /// </summary>
    /// <param name="animatable">This animatable element</param>
    /// <param name="property">The property to animate</param>
    /// <param name="to">The value of the property when finished (the start is the original property value)</param>
    /// <param name="duration">How long is the animation going last (in milliseconds)</param>
    /// <param name="delay">Postpone the start of the animation (in milliseconds)</param>
    /// <param name="easing">Enable acceleration or deceleration or none of them (null)</param>
    /// <param name="compose"><see langword="true"/> if more than one animation will be added to the same property</param>
    public static void AddAnimation(this Animatable animatable, DependencyProperty property,
        double toValue, int duration, int delay, IEasingFunction easing = null, bool compose = false)
    {
        if (toValue == (double)animatable.GetValue(property) && compose == false)
            return;
        DoubleAnimation timeline = new(toValue, GetDuration(duration)) { EasingFunction = easing };
        AddAnimation(animatable, property, timeline, delay, compose);
    }

    /// <summary>
    /// Adds a <see cref="Rect"/>-type animation to the specified <paramref name="property"/> in this
    /// <paramref name="animatable"/> element.
    /// </summary>
    /// <param name="animatable">This animatable element</param>
    /// <param name="property">The property to animate</param>
    /// <param name="to">The value of the property when finished (the start is the original property value)</param>
    /// <param name="duration">How long is the animation going last (in milliseconds)</param>
    /// <param name="delay">Postpone the start of the animation (in milliseconds)</param>
    /// <param name="easing">Enable acceleration or deceleration or none of them (null)</param>
    public static void AddAnimation(this Animatable animatable, DependencyProperty property,
        Rect toValue, int duration, int delay, IEasingFunction easing = null)
    {
        if (toValue == (Rect)animatable.GetValue(property))
            return;
        RectAnimation timeline = new(toValue, GetDuration(duration)) { EasingFunction = easing };
        AddAnimation(animatable, property, timeline, delay, false);
    }

    /// <summary>
    /// Adds an animation to the specified <paramref name="property"/> in this <paramref name="animatable"/> element,
    /// and adds the newly created animation clock to the global, application-wide clock dictionary.
    /// </summary>
    private static void AddAnimation(Animatable animatable, DependencyProperty property,
        AnimationTimeline timeline, int delay, bool compose)
    {
        timeline.SetCurrentValue(Timeline.BeginTimeProperty, TimeSpan.FromMilliseconds(delay));
        timeline.SetCurrentValue(Timeline.DesiredFrameRateProperty, ImageBrowserState.DesiredFrameRate);
        timeline.Freeze();
        AnimationClock clock = timeline.CreateClock();
        HandoffBehavior behavior = compose ? HandoffBehavior.Compose : HandoffBehavior.SnapshotAndReplace;
        animatable.ApplyAnimationClock(property, clock, behavior);
        ClockDictionary.GetOrAdd(animatable, (key) => new List<AnimationClock>()).Add(clock);
    }

    /// <summary>
    /// Removes animations from the specified properties in this <paramref name="animatable"/> element,
    /// and deletes all animation clocks applied to the element.
    /// </summary>
    private static void RemoveAnimations(this Animatable animatable, params DependencyProperty[] properties)
    {
        if (animatable.GetClocks().Any())
        {
            foreach (DependencyProperty property in properties)
            {
                animatable.ApplyAnimationClock(property, null);  // Removes animations from the property.
            }
            foreach (AnimationClock clock in animatable.GetClocks())
            {
                clock.Controller.Stop();
                clock.Controller.Remove();
            }
            ClockDictionary.TryRemove(animatable, out _);
            animatable.Freeze();
        }
    }

    /// <summary>
    /// Creates the animation duration for the specified number of <paramref name="milliseconds"/>.
    /// </summary>
    private static Duration GetDuration(int milliseconds)
    {
        return new(TimeSpan.FromMilliseconds(milliseconds > 0 ? milliseconds : 1));
    }

    /// <summary>
    /// Returns all animation clocks in the application.
    /// </summary>
    private static IEnumerable<AnimationClock> GetClocks() => ClockDictionary.Values.SelectMany(clocks => clocks);

    /// <summary>
    /// Returns all animation clocks attached to this <paramref name="animatable"/> element
    /// (e.g. transformation, effect, clip, etc.).
    /// </summary>
    private static IEnumerable<AnimationClock> GetClocks(this Animatable animatable)
    {
        if (animatable != null && ClockDictionary.TryGetValue(animatable, out List<AnimationClock> clocks))
            return clocks;
        return Enumerable.Empty<AnimationClock>();
    }

    /// <summary>
    /// Returns all animation clocks attached to this UI <paramref name="element"/>.
    /// </summary>
    private static IEnumerable<AnimationClock> GetClocks(this UIElement element)
    {
        return element.GetAnimatableElements().SelectMany(clock => clock.GetClocks());
    }

    /// <summary>
    /// Returns all animatable objects in the application having animations attached.
    /// </summary>
    private static IEnumerable<Animatable> GetAnimatableElements()
    {
        return ClockDictionary.Keys.Where(animatable => (animatable as DispatcherObject)?.CheckAccess() != false);
    }

    /// <summary>
    /// Returns all animatable objects applied to this UI <paramref name="element"/>.
    /// </summary>
    private static IEnumerable<Animatable> GetAnimatableElements(this UIElement element)
    {
        if (element.Clip != null)
            yield return element.Clip;
        if (element.Effect != null)
            yield return element.Effect;
        foreach (Transform transform in element.GetTransformations())
            yield return transform;
    }

    /// <summary>
    /// Returns all transformations applied to this UI <paramref name="element"/>.
    /// </summary>
    private static IEnumerable<Transform> GetTransformations(this UIElement element)
    {
        if (element.RenderTransform is TransformGroup group)
            return group.Children;
        if (element.RenderTransform is Transform transform)
            return transform.Yield();
        return Enumerable.Empty<Transform>();
    }

    /// <summary>
    /// <see langword="true"/> if there are any animations in the application that are in progress.
    /// </summary>
    public static bool IsAnimating() => IsAnimating(GetClocks(), GetAnimatableElements());

    /// <summary>
    /// <see langword="true"/> if this UI <paramref name="element"/> has any attached animations that are in progress.
    /// </summary>
    public static bool IsAnimating(this UIElement element) => IsAnimating(element.GetClocks(), element.GetTransformations());

    /// <summary>
    /// <see langword="true"/> if any attached animations are in progress.
    /// </summary>
    private static bool IsAnimating(IEnumerable<AnimationClock> clocks, IEnumerable<Animatable> animatable)
    {
        /// Note: CurrentProgress is null when the animation is stopped or when it hasn't started yet!
        return clocks.Any(clock => (clock.CurrentProgress ?? 1.0) < 1.0) ||
            animatable.OfType<TranslateTransform>().Any(translate => translate.X != 0 || translate.Y != 0);
    }

    /// <summary>
    /// Toggles between pausing and resuming all animation clocks in the application.
    /// </summary>
    public static void TogglePauseOrResumeAnimations()
    {
        bool? paused = null;
        foreach (AnimationClock clock in GetClocks())
            if (clock.CurrentState == ClockState.Active)
            {
                // When toggled, the first active clock specifies if all clocks should be paused or resumed.
                paused ??= clock.IsPaused;
                if (paused.Value && clock.IsPaused)
                    clock.Controller.Resume();
                else if (paused.Value == false && clock.IsPaused == false)
                    clock.Controller.Pause();
            }
    }

    /// <summary>
    /// Pauses all animation clocks applied to this UI <paramref name="element"/>.
    /// </summary>
    public static void PauseAnimations(this UIElement element)
    {
        foreach (AnimationClock clock in element.GetClocks())
            if (clock.CurrentState == ClockState.Active && clock.IsPaused == false)
                clock.Controller.Pause();
    }

    /// <summary>
    /// Resumes all paused animation clocks applied to this UI <paramref name="element"/>.
    /// </summary>
    public static void ResumeAnimations(this UIElement element)
    {
        foreach (AnimationClock clock in element.GetClocks())
            if (clock.CurrentState == ClockState.Active && clock.IsPaused)
                clock.Controller.Resume();
    }

    /// <summary>
    /// Gets a number between 1.0 and 0.0 representing the remaining time percentage
    /// of the animated transformation T attached to this UI <paramref name="element"/>. 0.0 means no time left.
    /// Note: This method will return 0.0 also when the animation hasn't started yet.
    /// </summary>
    public static double GetRemainingTimeFraction<T>(this UIElement element) where T : Transform
    {
        /// Note: CurrentProgress is null when the animation is stopped or when it hasn't started yet!
        foreach (AnimationClock clock in element.GetTransformations().OfType<T>().FirstOrDefault().GetClocks())
            if (clock.CurrentProgress.HasValue)
                return 1.0 - clock.CurrentProgress.Value;
        return 0.0;
    }

    /// <summary>
    /// Stops all transformation animations previously applied to this UI <paramref name="element"/>
    /// and assigns new transformation(s).
    /// </summary>
    public static void ResetTransformations(this UIElement element, Transform value = null)
    {
        if (value == null || value is TransformGroup group && group.Children.Count == 0)
            value = Transform.Identity;
        if (element.RenderTransform != value && element.HasIdenticalTransformations(value) == false)
        {
            List<Transform> transformations = element.GetTransformations().ToList();
            element.RenderTransform = value;
            foreach (Transform transform in transformations)
            {
                if (transform is TranslateTransform)
                    transform.RemoveAnimations(TranslateTransform.XProperty, TranslateTransform.YProperty);
                else if (transform is ScaleTransform)
                    transform.RemoveAnimations(ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty);
                else if (transform is RotateTransform)
                    transform.RemoveAnimations(RotateTransform.AngleProperty);
                else if (transform is SkewTransform)
                    transform.RemoveAnimations(SkewTransform.AngleXProperty, SkewTransform.AngleYProperty);
                else
                    transform?.RemoveAnimations();
            }
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if this UI <paramref name="element"/> already has the same transformations
    /// as the specified <paramref name="value"/>. The transformations cannot have any animations attached
    /// and should be frozen (unless the UI element unfreezes it).
    /// Used to optimize images with the same frozen rotation and mirror transformations applied over and over.
    /// </summary>
    private static bool HasIdenticalTransformations(this UIElement element, Transform value = null)
    {
        if (element.RenderTransform is TransformGroup group1 && value is TransformGroup group2 &&
            group1.Children.Count == group2.Children.Count)
        {
            for (int i = 0; i < group1.Children.Count; i++)
            {
                if (group1.Children[i] is RotateTransform rotate1 &&
                    group2.Children[i] is RotateTransform rotate2 &&
                    rotate1.GetClocks().Any() == false && rotate2.IsFrozen &&
                    rotate1.Angle == rotate2.Angle)
                    continue;
                if (group1.Children[i] is ScaleTransform scale1 &&
                    group2.Children[i] is ScaleTransform scale2 &&
                    scale1.GetClocks().Any() == false && scale2.IsFrozen &&
                    scale1.ScaleX == scale2.ScaleX && scale1.ScaleY == scale2.ScaleY)
                    continue;
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Stops any previously applied shadow effect animation and assigns a new shadow effect
    /// to this UI <paramref name="element"/>.
    /// </summary>
    public static void ResetEffect(this UIElement element, Effect value = null)
    {
        if (element.Effect != value)
        {
            Effect effect = element.Effect;
            element.Effect = value;
            if (effect is DropShadowEffect)
                effect.RemoveAnimations(DropShadowEffect.BlurRadiusProperty);
            else
                effect?.RemoveAnimations();
        }
    }

    /// <summary>
    /// Stops any previously applied crop rectangle animation and assigns a new crop rectangle
    /// to this UI <paramref name="element"/>.
    /// </summary>
    public static void ResetClip(this UIElement element, Geometry value = null)
    {
        if (element.Clip != value)
        {
            Geometry geometry = element.Clip;
            if (value != null && value.GetClocks().Any() == false)
            {
                value.Freeze();
                if (geometry != null && geometry.GetClocks().Any() == false &&
                    value is RectangleGeometry rec1 && geometry is RectangleGeometry rec2 &&
                    rec1.Rect == rec2.Rect && rec1.RadiusX == rec2.RadiusX && rec1.RadiusY == rec2.RadiusY)
                    return;
            }
            element.Clip = value;
            if (geometry is RectangleGeometry)
                geometry.RemoveAnimations(RectangleGeometry.RectProperty, RectangleGeometry.RadiusXProperty, RectangleGeometry.RadiusYProperty);
            else
                geometry?.RemoveAnimations();
        }
    }

    /// <summary>
    /// Gets the remaining shift vector of an animation in this <paramref name="element"/> to allow continuing
    /// the animation remainder (e.g. when scrolling all images repeatedly).
    /// </summary>
    public static Vector GetRemainingShift(this UIElement element)
    {
        TranslateTransform translate = element.GetTransformations().OfType<TranslateTransform>().FirstOrDefault();
        return translate != null ? new Vector(translate.X, translate.Y) : new Vector(0, 0);
    }

    /// <summary>
    /// Update the specified <paramref name="shift"/>, <paramref name="scale"/>, and <paramref name="angle"/>
    /// parameters with the parameters of unfinished animated transformations in this UI
    /// <paramref name="element"/> to allow for a smooth continuation of the animations.
    /// </summary>
    /// <returns>The updated location vector, the updated scale vector, and the updated rotation</returns>
    public static (Vector shift, Vector scale, int angle) ApplyRemainingTransformations(this UIElement element,
        Vector shift, Vector scale, int angle)
    {
        foreach (Transform transform in element.GetTransformations())
        {
            if (transform is TranslateTransform translateTransform)
                shift = new Vector(shift.X + translateTransform.X, shift.Y + translateTransform.Y);
            else if (transform is ScaleTransform scaleTransform)
                scale = new Vector(scale.X * Math.Abs(scaleTransform.ScaleX), scale.Y * Math.Abs(scaleTransform.ScaleY));
            else if (transform is RotateTransform rotateTransform)
                angle = (int)rotateTransform.Angle;
        }
        return (shift, scale, angle);
    }

    /// <summary>
    /// Applies the default rotation and scale to this <paramref name="image"/> if necessary (with no animations!).
    /// </summary>
    public static void SetDefaultTransformations(this Image image)
    {
        ImageTag tag = image.Tag();
        TransformGroup group = new();
        if (tag.Angle != 0)
            group.Children.AddFrozen(new RotateTransform(tag.Angle));
        if (tag.IsMirror)
            group.Children.AddFrozen(new ScaleTransform(-1, 1));
        image.ResetTransformations(group);
    }

    /// <summary>
    /// Adds a frozen transformation to this <paramref name="collection"/>.
    /// </summary>
    public static void AddFrozen(this TransformCollection collection, Transform transform)
    {
        transform.Freeze();
        collection.Add(transform);
    }

    /// <summary>
    /// Clones this <paramref name="shadow"/> effect object.
    /// </summary>
    public static DropShadowEffect CloneEffect(this DropShadowEffect shadow)
    {
        return new DropShadowEffect()
        {
            Color = shadow.Color,
            ShadowDepth = shadow.ShadowDepth,
            Direction = shadow.Direction,
            Opacity = shadow.Opacity,
            BlurRadius = shadow.BlurRadius
        };
    }

    /// <summary>
    /// Debugging method.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Debug method")]
    public static string GetClockInfo() => $"Clocks: {GetClocks().Count()}\r\n{GetClockTypes()}";

    /// <summary>
    /// Debugging method.
    /// </summary>
    private static string GetClockTypes() => string.Join(", ", GetAnimatableElements()
        .Select(item => item.GetType().ToString().Split('.')[^1])
        .GroupBy(name => name)
        .Select(group => $"{group.Key} ({group.Count()})")
        .OrderBy(name => name));
}