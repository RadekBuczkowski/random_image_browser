namespace Random_Image_Tests.Classes.Utilities;

using Random_Image.Classes.Utilities;

public class Test_NewThread
{
    [Fact]
    public void Test_Invoke()
    {
        // arrange
        List<bool> actual_result = new();
        Thread? thread = null;
        Thread? thread1 = Thread.CurrentThread;
        Thread? thread2 = thread1;

        // act
        NewThread.Invoke(ref thread, () =>
        {
            thread2 = Thread.CurrentThread;
            Thread.Sleep(10);
        }, ThreadPriority.Normal);
        actual_result.Add(NewThread.IsInvoked(thread));
        Thread.Sleep(100);
        actual_result.Add(NewThread.IsInvoked(thread) == false);
        actual_result.Add(thread == thread2);
        actual_result.Add(thread1 != thread2);

        // assert
        Assert.DoesNotContain(false, actual_result);
    }

    [Fact]
    public void Test_Invoke_FIFO()
    {
        // arrange
        Thread? thread1 = Thread.CurrentThread;
        Thread? thread2 = thread1;

        // act
        NewThread.Invoke(() =>
        {
            thread2 = Thread.CurrentThread;
        }, false, ThreadPriority.Normal);
        Thread.Sleep(100);

        // assert
        Assert.NotEqual(thread1, thread2);
    }

    [Fact]
    public void Test_Invoke_LIFO()
    {
        // arrange
        Thread? thread1 = Thread.CurrentThread;
        Thread? thread2 = thread1;

        // act
        NewThread.Invoke(() =>
        {
            thread2 = Thread.CurrentThread;
        }, true, ThreadPriority.Normal);
        Thread.Sleep(100);

        // assert
        Assert.NotEqual(thread1, thread2);
    }
}