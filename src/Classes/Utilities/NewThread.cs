namespace Random_Image.Classes.Utilities;

using System;
using System.Collections.Concurrent;
using System.Threading;

/// <summary>
/// Provides a class for executing code in new threads. The class ensures that some processors will remain unused,
/// if possible, to make the GUI more responsive. Pending actions are either queued (FIFO) or stacked (LIFO),
/// and they get executed when busy processors becomes idle again. The class is thread-safe.
/// </summary>
public static class NewThread
{
    /// <summary>
    /// Number of processors that should remain unused if possible.
    /// </summary>
    private const int ProcessorsReserved = 2;

    /// <summary>
    /// Starts the specified <see cref="action"/> in a new thread without thread limitation. Makes sure this
    /// <paramref name="thread"/> is not already started. The thread parameter can later be used with
    /// the <see cref="IsInvoked"/> method to check if it is completed.
    /// Note: The default thread priority is <see cref="ThreadPriority.BelowNormal"/> to make the GUI more responsive.
    /// </summary>
    public static void Invoke(ref Thread thread, Action action, ThreadPriority priority = ThreadPriority.BelowNormal)
    {
        if (MainThread.Disable && priority < ThreadPriority.Normal)
        {
            action();
            return;
        }
        if (IsInvoked(thread) == false)
        {
            thread = new Thread((ThreadStart)delegate
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    // Re-throw the exception raised in a new thread back to the UI thread.
                    MainThread.Invoke(() => throw new WrapperException(ex.Message, ex));
                }
            })
            {
                Priority = priority,
                IsBackground = true,
                CurrentCulture = Thread.CurrentThread.CurrentCulture,
                CurrentUICulture = Thread.CurrentThread.CurrentUICulture,
                Name = "Invoke ref"
            };
            thread.Start();
        }
    }

    /// <summary>
    /// Returns true if the thread is still running.
    /// </summary>
    public static bool IsInvoked(this Thread thread) => thread?.IsAlive == true;

    /// <summary>
    /// Starts the given <see cref="action"/> in a new thread with thread limitation. If all available threads
    /// are busy, actions are queued in the first-in-first-out order (default) or last-in-last-out order
    /// (LIFO parameter = true).
    /// Note: The default thread priority is <see cref="ThreadPriority.BelowNormal"/> to make the GUI more responsive.
    /// </summary>
    public static void Invoke(Action action, bool LIFO = false, ThreadPriority priority = ThreadPriority.BelowNormal)
    {
        if (MainThread.Disable && priority < ThreadPriority.Normal)
        {
            action();
            return;
        }
        if (LIFO)
            ActionStack.Push(action);
        else
            ActionQueue.Enqueue(action);
        if (TryAllocateProcessor())
        {
            Thread thread = new((ThreadStart)delegate
            {
                try
                {
                    while (ActionStack.TryPop(out Action action))
                        action();
                    while (ActionQueue.TryDequeue(out Action action))
                        action();
                }
                catch (Exception ex)
                {
                    // Re-throw the exception raised in the new thread back to the UI thread.
                    MainThread.Invoke(() => throw new WrapperException(ex.Message, ex));
                }
                finally { DeallocateProcessor(); }
            })
            {
                Priority = priority,
                IsBackground = true,
                CurrentCulture = Thread.CurrentThread.CurrentCulture,
                CurrentUICulture = Thread.CurrentThread.CurrentUICulture,
                Name = "Invoke"
            };
            thread.Start();
        }
    }

    private static readonly ConcurrentStack<Action> ActionStack = new();

    private static readonly ConcurrentQueue<Action> ActionQueue = new();

    private static readonly int ProcessorCount = Environment.ProcessorCount;

    private static int _processorsUsed = 0;

    /// <summary>
    /// Tries to allocate a processor and returns true if succeeded.
    /// </summary>
    private static bool TryAllocateProcessor()
    {
        int used = Interlocked.Increment(ref _processorsUsed);
        if (used == 1 || used <= ProcessorCount - ProcessorsReserved)
            return true;
        Interlocked.Decrement(ref _processorsUsed);
        return false;
    }

    /// <summary>
    /// Frees the currently used processor.
    /// </summary>
    private static void DeallocateProcessor()
    {
        Interlocked.Decrement(ref _processorsUsed);
    }
}