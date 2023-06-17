using System.Windows.Media.Imaging;

using Moq;

using Random_Image.Classes.Utilities;

namespace Random_Image_Tests;

public static class TestHelper
{
    /// <summary>
    /// Returns a mocked <see cref="BitmapSource"/> with the specified pixel size.
    /// </summary>
    public static BitmapSource MockBitmap(int width, int height)
    {
        var mock = new Mock<BitmapSource>();
        mock.SetupGet(x => x.PixelWidth).Returns(width);
        mock.SetupGet(x => x.PixelHeight).Returns(height);
        return mock.Object;
    }

    /// <summary>
    /// Suspends the the current thread for at least the specified number of <paramref name="milliseconds"/>.
    /// </summary>
    public static void Sleep(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// Suspends the the current thread for at least the specified number of <paramref name="milliseconds"/>
    /// and returns <see langword="true"/> if the sleep did not reach the maximum number of milliseconds yet
    /// specified in <paramref name="milliseconds_limit"/>. None: with 200 unit tests there is a high chance
    /// the sleep will suspend the thread for longer than expected because there will not be enough threads
    /// available to resume when the sleep is meant to be over.
    /// </summary>
    public static bool Sleep(int milliseconds, int milliseconds_limit)
    {
        long time = 0;
        SimpleTimer.StartTimer(ref time);
        Thread.Sleep(milliseconds);
        return SimpleTimer.IsTimerRunning(ref time, milliseconds_limit);
    }
}