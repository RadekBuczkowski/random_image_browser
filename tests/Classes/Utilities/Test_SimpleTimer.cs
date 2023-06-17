namespace Random_Image_Tests.Classes.Utilities;

using System.Collections.Generic;

using Random_Image.Classes.Utilities;

public class Test_SimpleTimer
{
    [Fact]
    public void Test_IsTimerRunning()
    {
        // arrange
        List<bool> actual_result = new();
        long time = 0;  // long, timer stopped

        // act
        actual_result.Add(SimpleTimer.IsTimerRunning(ref time, 80) == false);  // not running, timer is stopped
        SimpleTimer.StartTimer(ref time);
        actual_result.Add(SimpleTimer.IsTimerRunning(ref time, 80));  // running, timer has just started
        if (TestHelper.Sleep(50, 80))
            actual_result.Add(SimpleTimer.IsTimerRunning(ref time, 80));  // running, 80 ms not elapsed yet
        TestHelper.Sleep(50);
        actual_result.Add(SimpleTimer.IsTimerRunning(ref time, 80) == false);  // not running, 80 ms passed and timer gets stopped
        actual_result.Add(time == 0);  // timer is stopped

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_IsCycleCompleted()
    {
        // arrange
        List<bool> actual_result = new();
        long time = 0;  // long, timer stopped

        // act
        actual_result.Add(SimpleTimer.IsCycleCompleted(ref time, 80));  // completed, timer was stopped and gets started
        if (TestHelper.Sleep(50, 80))
            actual_result.Add(SimpleTimer.IsCycleCompleted(ref time, 80) == false);  // not completed, 80 ms not elapsed yet
        TestHelper.Sleep(50);
        actual_result.Add(SimpleTimer.IsCycleCompleted(ref time, 80));  // completed, 80 ms passed and timer gets restarted
        if (TestHelper.Sleep(50, 80))
            actual_result.Add(SimpleTimer.IsCycleCompleted(ref time, 80) == false);  // not completed, 80 ms not elapsed yet
        SimpleTimer.StopTimer(ref time);
        actual_result.Add(time == 0);  // timer is stopped

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_GetRemainingTime()
    {
        // arrange
        List<bool> actual_result = new();
        long time = 0;  // long, timer stopped

        // act
        actual_result.Add(SimpleTimer.GetRemainingTime(time, 80) == 0);  // 0 ms left, timer is stopped
        SimpleTimer.StartTimer(ref time);
        actual_result.Add(SimpleTimer.GetRemainingTime(time, 80) >= 80 - 1);  // nearly 80 ms left, timer just started
        TestHelper.Sleep(50);
        actual_result.Add(SimpleTimer.GetRemainingTime(time, 80) <= 30);  // max 30 ms left
        TestHelper.Sleep(50);
        actual_result.Add(SimpleTimer.GetRemainingTime(time, 80) == 0);  // 0 ms left, 80 ms passed
        SimpleTimer.StopTimer(ref time);
        actual_result.Add(time == 0);  // timer is stopped

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_GetRemainingTimeFraction()
    {
        // arrange
        List<bool> actual_result = new();
        long time = 0;  // long, timer stopped

        // act
        actual_result.Add(SimpleTimer.GetRemainingTimeFraction(time, 80) == 0);  // 0% left, timer is stopped
        SimpleTimer.StartTimer(ref time);
        actual_result.Add(SimpleTimer.GetRemainingTimeFraction(time, 80).RoughlyEquals(1));  // 100% left, timer just started
        TestHelper.Sleep(50);
        actual_result.Add(SimpleTimer.GetRemainingTimeFraction(time, 80) <= 0.375);  // max 37% left
        TestHelper.Sleep(50);
        actual_result.Add(SimpleTimer.GetRemainingTimeFraction(time, 80) == 0);  // 0% left, 80 ms passed
        SimpleTimer.StopTimer(ref time);
        actual_result.Add(time == 0);  // timer is stopped

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_CountRunningTimers()
    {
        // arrange
        List<bool> actual_result = new();
        Queue<long> timers = new();

        // act
        actual_result.Add(timers.CountRunningTimers(80) == 1);  // 1 timer added
        if (TestHelper.Sleep(50, 80))
        {
            actual_result.Add(timers.CountRunningTimers(80) == 2);  // 1 timer added
            actual_result.Add(timers.CountRunningTimers(80) == 3);  // 1 timer added
            if (TestHelper.Sleep(50, 80))
                actual_result.Add(timers.CountRunningTimers(80) == 3);  // 1 timer added, 1 timer removed
        }
        else
            actual_result.Add(timers.CountRunningTimers(80) == 1);  // 1 timer added, 1 timer removed

        // assert
        Assert.DoesNotContain(false, actual_result);
    }
}