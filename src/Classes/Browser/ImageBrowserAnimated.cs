namespace Random_Image.Classes.Browser;

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;
using Random_Image.Classes.Wpf;

/// <summary>
/// Extends the basic functionality of the image browser with animations.
/// </summary>
public class ImageBrowserAnimated : ImageBrowser
{
    /// <summary>
    /// Indicates the class implements animations.
    /// </summary>
    public override bool EnableAnimations => true;

    /// <summary>
    /// Indicates that animations are in progress.
    /// </summary>
    public override bool IsAnimating => AnimationExtensionMethods.IsAnimating();

    /// <summary>
    /// Indicates that the navigation animation is in progress (the entire canvas gets shifted).
    /// </summary>
    protected override bool IsNavigating => ImageCanvas.IsAnimating() || IsAlignmentPostponed;

    /// <summary>
    /// Indicates that the navigation has just started or full-screen mode has just been enabled
    /// and any image alignments must be postponed.
    /// </summary>
    private bool IsAlignmentPostponed =>
        SimpleTimer.IsTimerRunning(ref _postponeAlignmentTime, 1000) && State.IsZoomed == false;

    /// <summary>
    /// Returns the maximum shift when navigating in either direction (i.e. either positive or negative shift).
    /// </summary>
    private Vector MaxShift => new(2 * ImageCanvas.ActualWidth, 2 * ImageCanvas.ActualHeight);

    /// <summary>
    /// The expected animation duration of most animations.
    /// </summary>
    private const int StepDuration = ImageBrowserConstants.AnimationStepDuration;

    /// <summary>
    /// No shift or displacement of the image.
    /// </summary>
    private static readonly Vector NoShift = new(0, 0);

    /// <summary>
    /// No change in the scale or zoom of the image.
    /// </summary>
    private static readonly Vector NoScale = new(1, 1);

    private bool _shakeIsRandom = false;
    private bool _shakeDirection = false;
    private int _navigationDelta = 0;
    private int _navigationDuration = 0;
    private long _postponeAlignmentTime = 0;
    private DispatcherTimer _presentTimer = null;
    private DispatcherTimer _alignmentTimer = null;
    private DispatcherTimer _cropTimer = null;
    private DispatcherTimer _shadowTimer = null;
    private DispatcherTimer _shakeTimer = null;

    public ImageBrowserAnimated(ImageBrowserStateDurable state, ImageBrowserCanvas canvas) : base(state, canvas)
    {
    }

    public ImageBrowserAnimated(ImageBrowser images) : base(images)
    {
    }

    /// <summary>
    /// Initializes the navigation animation (up, down, left or right) by the specified delta
    /// (number of images back or forward).
    /// </summary>
    protected override void SetNavigationDelta(int delta)
    {
        _navigationDelta = delta;
        PostponeAlignment();
    }

    /// <summary>
    /// Postpones any alignment and alignment animations.
    /// </summary>
    private void PostponeAlignment()
    {
        MainThread.StopInvoke(ref _presentTimer);
        MainThread.StopInvoke(ref _alignmentTimer);
        MainThread.StopInvoke(ref _shadowTimer);
        MainThread.StopInvoke(ref _shakeTimer);
        SimpleTimer.StartTimer(ref _postponeAlignmentTime);
    }

    /// <summary>
    /// Toggles between pausing and resuming all animations in the application.
    /// </summary>
    public override void TogglePauseOrResumeAnimations() =>
        AnimationExtensionMethods.TogglePauseOrResumeAnimations();

    /// <summary>
    /// Starts an animation shaking all images.
    /// </summary>
    public override void ShakeImages(bool postpone = false)
    {
        if (postpone)
        {
            MainThread.Invoke(ref _shakeTimer, StepDuration / 2, () => ShakeImages(false));
        }
        else
        {
            _shakeIsRandom = State.Rand.NextDouble() < 0.5;
            if (!_shakeIsRandom)
                _shakeDirection = !_shakeDirection;
            RefreshImages(Reasons.ShakeImages);
        }
    }

    /// <summary>
    /// Starts an animation moving the visible part of each image left or right and then back to center.
    /// </summary>
    public override void PresentImagesByAligning(bool direction, bool postpone = false)
    {
        if (MainThread.IsInvoked(_presentTimer) || State.CanAlignMany == false)
            return;
        int delay2 = GetEffectDuration() + 50;
        int delay3 = GetEffectDuration(Reasons.AlignTopLeft, Reasons.AlignBottomRight) + 50;
        int delay1 = IsAnimating || postpone ? delay2 : 0;

        ChainAlignment(delay1, direction ? Reasons.AlignBottomRight : Reasons.AlignTopLeft, () =>
            ChainAlignment(delay2, direction ? Reasons.AlignTopLeft : Reasons.AlignBottomRight, () =>
                ChainAlignment(delay3, Reasons.AlignCenter, () =>
                    ChainAlignment(delay2))));  // Keep the timer active during the last animation and do nothing.
    }

    /// <summary>
    /// Allows chaining alignments in a sequence.
    /// </summary>
    private void ChainAlignment(int delay, Reasons reason = Reasons.None, Action next = null)
    {
        MainThread.Invoke(ref _presentTimer, delay, () =>
        {
            if (IsAnimating || State.CanAlignMany == false || next == null)
                return;
            next();
            RefreshImages(reason);
        });
    }

    /// <summary>
    /// Positions the visible part of the image left or right with the mouse.
    /// </summary>
    public override void AlignImage(Image image)
    {
        if (MainThread.IsInvoked(_presentTimer, _cropTimer) && State.IsZoomed == false)
            return;
        if (image.IsAnimating() && State.IsZoomed == false || IsNavigating)
            return;
        base.AlignImage(image);
    }

    /// <summary>
    /// Starts an animation moving the visible part of the image to the center.
    /// </summary>
    public override void AlignImageToCenter(Image image)
    {
        if (GetVisibleImages().All(image => image.Tag().IsCentered))
            return;
        if (MainThread.IsInvoked(_presentTimer, _cropTimer) || image.IsAnimating() || IsNavigating)
        {
            MainThread.Invoke(ref _alignmentTimer, 1000, () =>
            {
                if (MainThread.IsInvoked(_presentTimer, _cropTimer) || IsNavigating)
                    return;
                foreach (Image image in GetVisibleImages().Where(image => image.IsAnimating() == false))
                    base.AlignImageToCenter(image);
            });
        }
        else
            base.AlignImageToCenter(image);
    }

    /// <summary>
    /// Gets any remaining alignment animation offset.
    /// </summary>
    protected override Vector GetRemainingAlignmentOffset(Image image)
    {
        if (image.Tag().Reason.IsIn(Reasons.Align, Reasons.AlignCenter, Reasons.AlignTopLeft, Reasons.AlignBottomRight))
        {
            return image.GetRemainingShift();
        }
        return NoShift;
    }

    /// <summary>
    /// Crops the image to fit to the canvas slot and adds rounded corners if needed.
    /// </summary>
    protected override void CropImage(Image image, Reasons reason)
    {
        if (reason.IsIn(Reasons.ChangeLayout, Reasons.ChangeOrientation, Reasons.ChangeScaling) && State.IsZoomed ||
            reason == Reasons.GoIn ||
            reason == Reasons.GoBack && image.Tag().PositionOnCanvas == State.SelectedPositionOnCanvas)
        {
            image.ResetClip();
            MainThread.Invoke(ref _cropTimer, StepDuration * 1.1, () =>
            {
                foreach (Image image in GetVisibleImages())
                    base.CropImage(image, reason);
            });
        }
        else if (reason != Reasons.Align || MainThread.IsInvoked(_cropTimer) == false)
            base.CropImage(image, reason);
    }

    /// <summary>
    /// Applies all image effects and animations (i.e. navigating, moving, scaling, rotating, shaking,
    /// animating shadow, and animating crop) according to the newest canvas state.
    /// </summary>
    protected override void ApplyImageEffects(Image image, Reasons reason)
    {
        ImageTag tag = image.Tag();
        if (reason != Reasons.ShakeImages)
        {
            MainThread.StopInvoke(ref _shakeTimer);
        }
        if (reason != Reasons.Align || tag.IsAlignmentJump && State.IsZoomed == false)
        {
            int duration = GetEffectDuration(reason, tag.Previous.Reason);
            int scalingDelay = tag.IsMirror != tag.Previous.IsMirror && tag.Angle != tag.Previous.Angle ? duration : 0;
            IEasingFunction easing = reason.IsIn(Reasons.Align, Reasons.ChangeScaling) ?
                new SineEase() : new SineEase() { EasingMode = EasingMode.EaseInOut };
            ApplyCanvasNavigationAnimation(tag, reason, duration);
            // Note: the new shape has already been applied to the image but with the following variables we can
            // restore the previous shape (to make the image appear visually unchanged) and then animate the transition.
            (Vector shift, Vector scale, int angle, bool isMirror) = GetImageChange(image, reason);
            int totalDuration = reason == Reasons.Navigate ? _navigationDuration : duration + scalingDelay;
            TransformGroup group = new();
            // Note: the order has to be correct in WPF, i.e. shift after rotate and scale, and shake before rotate.
            ApplyImageShakeAnimation(group, tag, reason);
            ApplyImageRotateAnimation(group, tag, angle, duration, easing);
            ApplyImageScaleAnimation(group, tag, scale, isMirror, duration, scalingDelay, easing);
            ApplyImageShiftAnimation(group, tag, shift, duration, easing);
            ApplyImageShadowAnimation(group, tag, reason, image, totalDuration);
            image.ResetTransformations(group);
            ApplyImageCropAnimation(image, tag, reason, shift, scale, duration, easing);
        }
        else if (tag.Previous.Reason.IsIn(Reasons.AlignCenter, Reasons.AlignTopLeft, Reasons.AlignBottomRight))
            image.SetDefaultTransformations();
    }

    /// <summary>
    /// Returns the duration of the animation depending on the reason.
    /// </summary>
    private static int GetEffectDuration(Reasons reason = Reasons.None, Reasons previousReason = Reasons.None)
    {
        if (reason == Reasons.Align)
            return 250;
        if (reason == Reasons.GoIn)
            return StepDuration * 5 / 4;
        if (reason.IsIn(Reasons.AlignTopLeft, Reasons.AlignBottomRight) &&
            previousReason.IsIn(Reasons.AlignTopLeft, Reasons.AlignBottomRight))
            return StepDuration * 3 / 2;
        return StepDuration;
    }

    /// <summary>
    /// Returns the movement, zoom, rotation and mirror state representing how to get from the current shape
    /// of the image as it is visible on canvas to the new requested shape specified in <paramref name="image.Tag"/>.
    /// </summary>
    private (Vector shift, Vector scale, int angle, bool isMirror) GetImageChange(Image image, Reasons reason)
    {
        ImageTag tag = image.Tag();
        (Vector shift, Vector scale) = tag.ImageRectangle.RectangleDifference(tag.Previous.ImageRectangle);
        int angle = tag.Previous.Angle;
        bool isMirror = tag.Previous.IsMirror;
        image.PauseAnimations();  // Pause shifting, scaling, and rotating.
        (shift, scale, angle) = image.ApplyRemainingTransformations(shift, scale, angle);
        (shift, scale) = (shift.RoundToZero().Limit(MaxShift), scale.RoundToOne());
        if (tag.IsReady && tag.Previous.IsReady == false ||
            reason.IsIn(Reasons.Navigate, Reasons.ImageNeighbor, Reasons.Restart))
        {
            (shift, scale, angle, isMirror) = (NoShift, NoScale, tag.Angle, tag.IsMirror);  // Cancel animated movement.
            if (reason.IsIn(Reasons.ChangeLayout, Reasons.ChangeOrientation, Reasons.GoBack)) // New images start outside.
                shift = new Vector(0, ImageCanvas.ActualHeight * ((tag.PositionOnCanvas < State.CanvasColumns) ? -1 : 1));
        }
        return (shift, scale, angle, isMirror);
    }

    /// <summary>
    /// Adds a rotation animation.
    /// </summary>
    private void ApplyImageRotateAnimation(TransformGroup group, ImageTag tag, int angle, int duration, IEasingFunction easing)
    {
        if (tag.Angle != angle && State.IsImageVisible(tag.Index, extraRows: 1))
        {
            RotateTransform transform = new(angle);
            transform.AddAnimation(RotateTransform.AngleProperty, tag.Angle, duration, 0, easing);
            group.Children.Add(transform);
        }
        else if (tag.Angle != 0)
            group.Children.AddFrozen(new RotateTransform(tag.Angle));
    }

    /// <summary>
    /// Adds a scaling animation (zooming in or out) and mirror animation.
    /// </summary>
    private void ApplyImageScaleAnimation(TransformGroup group, ImageTag tag, Vector scale, bool isMirror,
        int duration, int delay, IEasingFunction easing)
    {
        if ((tag.IsMirror != isMirror || scale != NoScale) && State.IsImageVisibleBeforeZoomed(tag.Index, extraRows: 1))
        {
            ScaleTransform transform = new(isMirror ? -scale.X : scale.X, scale.Y);
            transform.AddAnimation(ScaleTransform.ScaleXProperty, tag.IsMirror ? -1 : 1, duration, delay, easing);
            transform.AddAnimation(ScaleTransform.ScaleYProperty, 1, duration, delay, easing);
            group.Children.Add(transform);
        }
        else if (tag.IsMirror)
            group.Children.AddFrozen(new ScaleTransform(-1, 1));
    }

    /// <summary>
    /// Adds a movement animation (e.g. because of layout changes, alignment, etc., but excluding navigation).
    /// </summary>
    private void ApplyImageShiftAnimation(TransformGroup group, ImageTag tag, Vector shift, int duration, IEasingFunction easing)
    {
        if (shift != NoShift && State.IsImageVisibleBeforeZoomed(tag.Index, extraRows: 1))
        {
            TranslateTransform transform = new(shift.X, shift.Y);
            transform.AddAnimation(TranslateTransform.XProperty, 0, duration, 0, easing);
            transform.AddAnimation(TranslateTransform.YProperty, 0, duration, 0, easing);
            group.Children.Add(transform);
        }
    }

    /// <summary>
    /// Adds an animation shaking all visible images.
    /// </summary>
    private void ApplyImageShakeAnimation(TransformGroup group, ImageTag tag, Reasons reason)
    {
        if (reason != Reasons.ShakeImages || State.IsImageVisible(tag.Index) == false)
            return;
        (double angle, int delay) = GetShakeAnimationParameters(tag);
        int duration = StepDuration;
        // marginal skew of the image
        SkewTransform skew = new(0, 0, tag.ImageRectangle.Width / 2, tag.ImageRectangle.Height / 2);
        skew.AddAnimation(SkewTransform.AngleXProperty, angle, duration / 4, delay, compose: true);
        skew.AddAnimation(SkewTransform.AngleYProperty, angle, duration / 4, delay, compose: true);
        skew.AddAnimation(SkewTransform.AngleXProperty, -angle, duration / 2, delay + duration / 4, compose: true);
        skew.AddAnimation(SkewTransform.AngleYProperty, -angle, duration / 2, delay + duration / 4, compose: true);
        skew.AddAnimation(SkewTransform.AngleXProperty, 0, duration / 4, delay + duration * 3 / 4, compose: true);
        skew.AddAnimation(SkewTransform.AngleYProperty, 0, duration / 4, delay + duration * 3 / 4, compose: true);
        group.Children.Add(skew);
        // marginal rotation of the image
        RotateTransform rotation = new(0, tag.ImageRectangle.Width / 2, tag.ImageRectangle.Height / 2);
        rotation.AddAnimation(RotateTransform.AngleProperty, -angle / 2, duration / 4, delay, compose: true);
        rotation.AddAnimation(RotateTransform.AngleProperty, angle / 2, duration / 2, delay + duration / 4, compose: true);
        rotation.AddAnimation(RotateTransform.AngleProperty, 0, duration / 4, delay + 3 * duration / 4, compose: true);
        group.Children.Add(rotation);
    }

    /// <summary>
    /// Returns parameters for the shake animation: angle in degrees, duration and delay in milliseconds.
    /// </summary>
    private (double angle, int delay) GetShakeAnimationParameters(ImageTag tag)
    {
        const int maxAngle = 5;
        const int shortDelay = 300;
        const int randomDelay = 1000;
        const int sequenceDelay = 3000;
        int delay = State.Rand.Next(shortDelay);
        // Angle is a random value between -maxAngle and +maxAngle excluding values close to 0 (i.e. -0.5 .. +0.5).
        double angle = State.Rand.NextDouble() * 2.0 * maxAngle - maxAngle;
        angle += (Math.Abs(angle) > 0.5) ? 0 : (angle < 0.0) ? -0.5 : 0.5;
        if (State.IsZoomed == false)
        {
            if (_shakeIsRandom == false)
            {
                // Images are shaken one after another from top-left to bottom-right or vice versa.
                // The shake intensity increases with every image.
                int index = _shakeDirection ? tag.PositionOnCanvas : (State.ImagesOnCanvas - tag.PositionOnCanvas - 1);
                delay = index * sequenceDelay / State.ImagesOnCanvas;
                angle = maxAngle * ((double)delay / sequenceDelay + 0.1) * (_shakeDirection ? 1 : -1);
            }
            else
                delay = State.Rand.Next(randomDelay);  // Images are shaken at random.
        }
        return (angle, delay);
    }

    /// <summary>
    /// Adds an animation showing an image shadow.
    /// </summary>
    private void ApplyImageShadowAnimation(TransformGroup group, ImageTag tag, Reasons reason, Image image, int delay)
    {
        bool toWhite = ImageCanvas.IsBlackBackground && reason == Reasons.ChangeBackground;
        if (group.Children.Count > 0 || reason.IsIn(Reasons.None, Reasons.Navigate, Reasons.Restart) || toWhite)
        {
            // Hide the shadow effect because performance degradation is too high when animating objects with a shadow.
            image.ResetEffect();
            if (tag.IsLoaded && tag.OccupiesWholeArea == false && State.HasShadow && State.IsImageVisible(tag.Index)
                && (ImageCanvas.IsBlackBackground == false || toWhite))
            {
                double radius = ImageCanvas.ActualHeight * 0.014;
                int duration = StepDuration * 3 / 2;
                IEasingFunction easing = new SineEase();
                DropShadowEffect shadowTiny = ImageCanvas.Window.FindResource("shadowTiny") as DropShadowEffect;
                // Marginal resizing of the image to make shadow reappearing on a white background look more realistic.
                ScaleTransform transform = new(0.998, 0.998);
                transform.AddAnimation(ScaleTransform.ScaleXProperty, 1, duration, delay, easing);
                transform.AddAnimation(ScaleTransform.ScaleYProperty, 1, duration, delay, easing);
                group.Children.Add(transform);
                // Reenable the shadow under images when all other animations are finished. Note: we use a timer
                // instead of setting the "delay" parameter in AddAnimation to avoid performance degradations.
                MainThread.Invoke(ref _shadowTimer, delay, () =>
                {
                    foreach (Image item in GetVisibleImages())
                        if (item.Effect == null && item.Tag().IsLoaded && item.Tag().OccupiesWholeArea == false)
                        {
                            DropShadowEffect shadow = shadowTiny.CloneEffect();
                            shadow.AddAnimation(DropShadowEffect.BlurRadiusProperty, radius, duration, 0, easing);
                            item.ResetEffect(shadow);
                        }
                });
            }
        }
    }

    /// <summary>
    /// Adds image crop animation. Used when images are larger than the space on canvas available for the image.
    /// </summary>
    private void ApplyImageCropAnimation(Image image, ImageTag tag, Reasons reason,
        Vector shift, Vector scale, int duration, IEasingFunction easing)
    {
        if ((shift != NoShift || scale != NoScale) &&
            tag.Previous.IsReady && (tag.IsMovable || State.HasRoundedCorners) &&
            State.IsZoomed == false && State.IsImageVisible(tag.Index, extraRows: 1))
        {
            double radius = reason == Reasons.GoBack ? tag.Previous.Radius : tag.Radius;
            Rect rectangle = tag.Previous.IsZoomed ? tag.Previous.NoCropRectangle : tag.Previous.CropRectangle;
            rectangle = rectangle.UndoScale(scale);
            if (reason == Reasons.ChangeScaling && image.Clip is RectangleGeometry previousClip)
            {
                rectangle = tag.GetShiftedCrop(previousClip.Rect, image.GetRemainingShift());
            }
            RectangleGeometry clip = new(rectangle, radiusX: radius / scale.X, radiusY: radius / scale.Y);
            clip.AddAnimation(RectangleGeometry.RectProperty, tag.CropRectangle, duration, 0, easing);
            clip.AddAnimation(RectangleGeometry.RadiusXProperty, tag.Radius, duration, 0, easing);
            clip.AddAnimation(RectangleGeometry.RadiusYProperty, tag.Radius, duration, 0, easing);
            image.ResetClip(clip);
        }
    }

    /// <summary>
    /// Starts a fluid navigation animation moving all images up, down, left or right. Note: the animation
    /// affects all images but only the first image triggers the animation. The navigation animation is applied
    /// to the entire canvas, unlike all other animations that are applied directly to the image.
    /// </summary>
    private void ApplyCanvasNavigationAnimation(ImageTag tag, Reasons reason, int duration)
    {
        if (reason == Reasons.Navigate && _navigationDelta != 0 && State.IsImageVisible(tag.Index))
        {
            ImageCanvas.PauseAnimations();
            Vector shift = tag.GetNavigationShift(State, _navigationDelta);
            Vector remainingShift = ImageCanvas.GetRemainingShift();
            bool sameDirection = remainingShift.IsSameDirectionAs(shift);
            double remainingTime = ImageCanvas.GetRemainingTimeFraction<TranslateTransform>();
            if (remainingTime == 0)
                duration += (int)(duration * (shift.Y >= ImageCanvas.ActualHeight ? 0.75 : 0.5));
            else if (sameDirection)
                duration += (int)(_navigationDuration * remainingTime).Limit(0, duration);
            shift = (shift + remainingShift).RoundToZero().Limit(MaxShift);
            TranslateTransform transform = new(shift.X, shift.Y);
            IEasingFunction easing = (remainingTime > 0.1 && sameDirection) ?
                new ContinualEase() : new SineEase() { EasingMode = EasingMode.EaseInOut };
            transform.AddAnimation(TranslateTransform.XProperty, 0, duration, 0, easing);
            transform.AddAnimation(TranslateTransform.YProperty, 0, duration, 0, easing);
            _navigationDuration = duration;
            _navigationDelta = 0;  // Only one image starts the navigation animation for the entire canvas.
            ImageCanvas.ResetCache(State.IsZoomed == false && tag.Previous.Reason == Reasons.Navigate);
            ImageCanvas.ResetTransformations(transform);  // Resume animations.
        }
        else if (reason != Reasons.Navigate)
        {
            ImageCanvas.ResetCache();
            ImageCanvas.ResumeAnimations();  // Quick check if canvas animations were paused (e.g. by using keyboard).
        }
    }
}