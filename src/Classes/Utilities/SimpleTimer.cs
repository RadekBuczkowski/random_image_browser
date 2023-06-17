namespace Random_Image.Classes.Utilities;

using System;
using System.Collections.Generic;

using Random_Image.Classes.Extensions;

/// <summary>
/// Provides a class implementing a simple, fast and memory efficient timer with precision at 100 nanoseconds.
/// </summary>
public static class SimpleTimer
{
    /// <summary>
    /// The time in milliseconds to block the mouse wheel when an even value is reached (e.g. zoom = 100%).
    /// </summary>
    public const int BlockMouseWheelTime = 500;

    /// <summary>
    /// Starts counting the time.
    /// </summary>
    public static void StartTimer(ref long time)
    {
        time = DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// Stops counting the time.
    /// </summary>
    public static void StopTimer(ref long time)
    {
        time = 0;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the timer is started and the specified number
    /// of <paramref name="milliseconds"/> has not elapsed yet. If it has elapsed, the timer is stopped.
    /// </summary>
    public static bool IsTimerRunning(ref long time, int milliseconds)
    {
        if (time != 0)
        {
            if (Math.Abs(DateTime.UtcNow.Ticks - time) < milliseconds * TimeSpan.TicksPerMillisecond)
                return true;
            time = 0;
        }
        return false;
    }

    /// <summary>
    /// Returns <see langword="true"></see> if the timer is either stopped or the specified number
    /// of <paramref name="milliseconds"/> has elapsed since the timer was started. When either happens,
    /// the timer restarts and counts the cycle again.
    /// </summary>
    public static bool IsCycleCompleted(ref long time, int milliseconds)
    {
        if (time == 0 || Math.Abs(DateTime.UtcNow.Ticks - time) >= milliseconds * TimeSpan.TicksPerMillisecond)
        {
            time = DateTime.UtcNow.Ticks;
            return true;
        }
        return false;
    }

    /// <summary>
    /// If the timer is started and the specified number of <paramref name="milliseconds"/> has not elapsed yet,
    /// returns how much of the given time in milliseconds still remains. Returns 0 if the time has elapsed
    /// or the timer is stopped.
    /// </summary>
    public static int GetRemainingTime(long time, int milliseconds)
    {
        if (time == 0)
            return 0;
        long result = milliseconds - Math.Abs(DateTime.UtcNow.Ticks - time) / TimeSpan.TicksPerMillisecond;
        if (result < 0)
            return 0;
        return (int)result;
    }

    /// <summary>
    /// Returns a number between 1.0 and 0.0 representing the percentage of how much of the given time
    /// in <paramref name="milliseconds"/> still remains. 1.0 means the timer has just started and 0.0 means the time
    /// has elapsed or the timer is stopped.
    /// Note: Can be used as an alternative to the <see cref="ImageExtensionMethods.GetRemainingTimeFraction"/> method.
    /// </summary>
    public static double GetRemainingTimeFraction(long time, int milliseconds)
    {
        if (milliseconds <= 0 || time == 0)
            return 0;
        double result = 1.0 - (double)Math.Abs(DateTime.UtcNow.Ticks - time) / (milliseconds * TimeSpan.TicksPerMillisecond);
        if (result < 0)
            return 0;
        return result;  // note: the result will never be greater than 1
    }

    /// <summary>
    /// Counts the number of timers in this timer <paramref name="queue"/> that have not elapsed yet. In details:
    /// removes timers from the <paramref name="queue"/> that are more in the past from now than the given number
    /// of <paramref name="milliseconds"/>, adds a new timer with the current time to the queue, and returns
    /// the length of the queue. The method can be used to count mouse wheel activations in a given period.
    /// </summary>
    public static int CountRunningTimers(this Queue<long> queue, int milliseconds)
    {
        long currentTime = DateTime.UtcNow.Ticks;
        while (queue.TryPeek(out long time) && Math.Abs(currentTime - time) > milliseconds * TimeSpan.TicksPerMillisecond)
            queue.Dequeue();
        queue.Enqueue(currentTime);
        return queue.Count;
    }
}