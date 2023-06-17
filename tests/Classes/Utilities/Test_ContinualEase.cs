namespace Random_Image_Tests.Classes.Utilities;

using System.Windows.Media.Animation;

using MathNet.Numerics.Interpolation;

using Random_Image.Classes.Utilities;

public class Test_ContinualEase
{
    private const double HighPrecision = 1e-10;
    private const int Samples = 10_000;

    /// <summary>
    /// Samples of the transformation time.
    /// </summary>
    private static readonly double[] Time = new double[Samples];

    /// <summary>
    /// Samples of the transformation progress.
    /// </summary>
    private static readonly double[] Progress = new double[Samples];

    /// <summary>
    /// Samples of the transformation speed.
    /// </summary>
    private static readonly double[] Speed = new double[Samples];

    /// <summary>
    /// Samples of the transformation acceleration.
    /// </summary>
    private static readonly double[] Acceleration = new double[Samples];

    static Test_ContinualEase()
    {
        // Note: with EasingMode.EaseIn the ContinualEase.EaseInCore() function is equivalent to ContinualEase.Ease().
        ContinualEase ease = new() { EasingMode = EasingMode.EaseIn };

        for (int i = 0; i < Samples; i++)
        {
            // Note: time and progress have only values between 0 and 1, both inclusive.
            Time[i] = (double)i / (Samples - 1);
            Progress[i] = ease.Ease(Time[i]);
        }

        // Spline interpolation robust to discontinuities
        CubicSpline spline = CubicSpline.InterpolateAkimaSorted(Time, Progress);

        for (int i = 0; i < Samples; i++)
        {
            // Get the first and second derivative at each sample point.
            Speed[i] = spline.Differentiate(Time[i]);
            Acceleration[i] = spline.Differentiate2(Time[i]);
        }
    }

    [Theory]
    [InlineData(0, 0, EasingMode.EaseIn)]
    [InlineData(0, 0, EasingMode.EaseOut)]
    [InlineData(0, 0, EasingMode.EaseInOut)]
    [InlineData(1, 1, EasingMode.EaseIn)]
    [InlineData(1, 1, EasingMode.EaseOut)]
    [InlineData(1, 1, EasingMode.EaseInOut)]
    [InlineData(2.0 / 3.0, 0.5, EasingMode.EaseIn)]
    [InlineData(1.0 / 3.0, 0.5, EasingMode.EaseOut)]
    [InlineData(1.0 / 3.0, 0.25, EasingMode.EaseInOut)]
    [InlineData(2.0 / 3.0, 0.75, EasingMode.EaseInOut)]
    public void Test_ContinualEase_Boundaries(double time, double expected_result, EasingMode mode)
    {
        // arrange
        ContinualEase ease = new() { EasingMode = mode };

        // assert
        Assert.Equal(expected_result, ease.Ease(time));
    }

    /// <summary>
    /// Makes sure the easing function is continuous at the boundary points.
    /// </summary>
    [Theory]
    [InlineData(2.0 / 3.0, 0.5, EasingMode.EaseIn)]
    [InlineData(1.0 / 3.0, 0.5, EasingMode.EaseOut)]
    [InlineData(1.0 / 3.0, 0.25, EasingMode.EaseInOut)]
    [InlineData(2.0 / 3.0, 0.75, EasingMode.EaseInOut)]
    public void Test_ContinualEase_Continuity(double time, double expected_result, EasingMode mode)
    {
        // arrange
        ContinualEase ease = new() { EasingMode = mode };

        // assert
        Assert.True(Math.Abs(expected_result - ease.Ease(time - HighPrecision)) < HighPrecision * 2);
        Assert.True(Math.Abs(expected_result - ease.Ease(time + HighPrecision)) < HighPrecision * 2);
    }

    /// <summary>
    /// Makes sure the <see cref="EaseInCore"/>  method only accelerates in the first half of animation
    /// and changes smoothly.
    /// </summary>
    [Fact]
    public void Test_ContinualEase_EaseInCore_Acceleration()
    {
        // Average acceleration is from speed 0 to 1.5, but instantaneous acceleration starts faster and goes below 1.5.
        const double max_acceleration = 2.5;

        // assert
        for (int i = 0; i < Samples - 1; i++)
        {
            // Exclude the last progress point because the speed is discontinuous there and jumps from 1.36 to 1.5.
            if (Progress[i + 1] < 0.5)
                Assert.True(Acceleration[i] is > 1.0 and < max_acceleration);
        }
    }

    /// <summary>
    /// Makes sure the <see cref="EaseInCore"/> method has a constant speed of 1.5 in the second half of animation.
    /// </summary>
    [Fact]
    public void Test_ContinualEase_EaseInCore_ConstantSpeed()
    {
        const double speed = 1.5;

        // assert
        for (int i = 0; i < Samples; i++)
        {
            if (Progress[i] >= 0.5)
                Assert.True(Math.Abs(Speed[i] - speed) < HighPrecision);
        }
    }

    [Fact]
    public void Test_ContinualEase_DefaultEasingMode()
    {
        // assert
        Assert.Equal(EasingMode.EaseOut, new ContinualEase().EasingMode);
    }

    [Fact]
    public void Test_SineEase_DefaultEasingMode()
    {
        // assert
        Assert.Equal(EasingMode.EaseOut, new SineEase().EasingMode);
    }
}