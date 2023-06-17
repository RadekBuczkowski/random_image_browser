namespace Random_Image_Tests.Classes.Extensions;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

using Random_Image.Classes.Browser;
using Random_Image.Classes.Extensions;
using Random_Image.Classes.Utilities;

public class Test_AnimationExtensionMethods
{
    [Fact]
    public async Task Test_AddAnimation()
    {
        // arrange
        BitmapSource bitmap = ResourceHelper.LoadBitmapResource(ResourceHelper.ErrorPng);
        ImageTag tag = new();
        List<bool> actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new();
            sut.SetTag(tag);
            sut.AssignBitmap(new(bitmap));
            actual_result.Add(sut.IsAnimating() == false);
            TranslateTransform transform = new(2, 2);
            transform.AddAnimation(TranslateTransform.XProperty, 0, 200, 0);
            transform.AddAnimation(TranslateTransform.YProperty, 0, 200, 0);
            TransformGroup group = new();
            group.Children.Add(transform);
            sut.ResetTransformations(group);
            actual_result.Add(sut.IsAnimating());
            actual_result.Add(AnimationExtensionMethods.IsAnimating());
            sut.PauseAnimations();
            sut.ResumeAnimations();
            sut.RecycleImage();
            actual_result.Add(sut.IsAnimating() == false);
            actual_result.Add(AnimationExtensionMethods.IsAnimating() == false);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public async Task Test_AddAnimation_TestShift()
    {
        // arrange
        BitmapSource bitmap = ResourceHelper.LoadBitmapResource(ResourceHelper.LoadingPng);
        ImageTag tag = new();
        Vector expected_shift = new(2, 2);
        Vector actual_shift1 = new();
        Vector actual_shift2 = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new();
            sut.SetTag(tag);
            sut.AssignBitmap(new(bitmap));
            TranslateTransform transform = new(expected_shift.X, expected_shift.Y);
            transform.AddAnimation(TranslateTransform.XProperty, 0, 200, 0);
            transform.AddAnimation(TranslateTransform.YProperty, 0, 200, 0);
            TransformGroup group = new();
            group.Children.Add(transform);
            sut.ResetTransformations(group);
            sut.PauseAnimations();
            actual_shift1 = sut.GetRemainingShift();
            (actual_shift2, _, _) = sut.ApplyRemainingTransformations(new(0, 0), new(), 0);
            sut.ResumeAnimations();
            sut.RecycleImage();
        });

        // assert
        Assert.Equal(expected_shift, actual_shift1);
        Assert.Equal(expected_shift, actual_shift2);
    }

    [Fact]
    public async Task Test_AddAnimation_Rectangle()
    {
        // arrange
        BitmapSource bitmap = ResourceHelper.LoadBitmapResource(ResourceHelper.LoadingPng);
        ImageTag tag = new();
        List<bool> actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new();
            sut.SetTag(tag);
            sut.AssignBitmap(new(bitmap));
            actual_result.Add(sut.Clip == null);
            RectangleGeometry clip = new(new Rect(0, 0, 100, 50), radiusX: 45, radiusY: 45);
            sut.ResetClip(clip);
            actual_result.Add(sut.Clip == clip);
            RectangleGeometry clip2 = new(new Rect(0, 0, 100, 50), radiusX: 45, radiusY: 45);
            sut.ResetClip(clip2);  // no animations and same transformations, so no change
            actual_result.Add(sut.Clip == clip);
            RectangleGeometry clip3 = new(new Rect(0, 0, 100, 50), radiusX: 45, radiusY: 45);
            clip3.AddAnimation(RectangleGeometry.RectProperty, new Rect(1, 1, 100, 50), 200, 0);
            sut.ResetClip(clip3);  // has animations, so changed
            actual_result.Add(sut.Clip == clip3);
            AnimationExtensionMethods.TogglePauseOrResumeAnimations();
            sut.RecycleImage();
            actual_result.Add(sut.Clip == null);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public async Task Test_SetDefaultTransformations()
    {
        // arrange
        BitmapSource bitmap = ResourceHelper.LoadBitmapResource(ResourceHelper.LoadingPng);
        ImageBrowserState state = new();
        state.Initialize(20);
        state.SetLayout(7);
        state.Rotate(180, true, false);  // 180 degrees
        ImageTag tag = new();
        List<bool> actual_result = new();

        // act
        await MainThread.InvokeSTA(() =>
        {
            Image sut = new();
            sut.SetTag(tag);
            sut.AssignBitmap(new(bitmap));
            tag.Resize(state, new(1920, 1080), new(0, 0), new(0, 0), Reasons.Navigate);
            actual_result.Add(sut.RenderTransform == Transform.Identity);
            sut.SetDefaultTransformations();  // change
            actual_result.Add(sut.RenderTransform != Transform.Identity);
            actual_result.Add(sut.RenderTransform != null);
            Transform? transform = sut.RenderTransform;
            sut.SetDefaultTransformations();  // no animations and same transformations, so no change
            actual_result.Add(sut.RenderTransform == transform);
            sut.RecycleImage();
            actual_result.Add(sut.RenderTransform == Transform.Identity);
        });

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public async Task Test_CloneEffect()
    {
        // arrange
        bool actual_result = false;

        // act
        await MainThread.InvokeSTA(() =>
        {
            DropShadowEffect sut = new() { BlurRadius = 5 };
            actual_result = sut.CloneEffect().BlurRadius == 5;
        });

        // assert
        Assert.True(actual_result);
    }
}