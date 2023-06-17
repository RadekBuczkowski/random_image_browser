namespace Random_Image.Classes.Utilities;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Random_Image.Classes.Browser;

/// <summary>
/// Provides a class for executing code in the UI thread asynchronously and possibly delaying the code execution.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires Dispatcher")]
public static class MainThread
{
    /// <summary>
    /// Makes this class and the <see cref="NewThread"/> class execute actions in the caller thread. Invokes with
    /// thread priority <see cref="ThreadPriority.Normal"/> or above, however, will still create new threads.
    /// Used in unit tests.
    /// </summary>
    public static bool Disable { get; set; } = false;

    private static Dispatcher _dispatcher = null;

    /// <summary>
    /// Executes the specified <paramref name="action"/> in the UI thread asynchronously.
    /// </summary>
    public static void Invoke(Action action, DispatcherPriority priority = DispatcherPriority.Background)
    {
        if (Disable)
        {
            action();
            return;
        }
        _dispatcher ??= Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _dispatcher.BeginInvoke(priority, (ThreadStart)delegate { action(); });
    }

    /// <summary>
    /// Executes the specified <paramref name="action"/> in the UI thread asynchronously after a delay
    /// in <paramref name="milliseconds"/>.
    /// </summary>
    public static void Invoke(double milliseconds, Action action,
        DispatcherPriority priority = DispatcherPriority.Background)
    {
        if (Disable)
        {
            action();
            return;
        }
        DispatcherTimer dispatcherTimer = null;
        Invoke(ref dispatcherTimer, milliseconds, action, priority);
    }

    /// <summary>
    /// Executes the specified <paramref name="action"/> in the UI thread asynchronously after a delay
    /// in <paramref name="milliseconds"/>. Any previous action invoked with this
    /// <paramref name="dispatcherTimer"/> that hasn't been executed yet will be canceled.
    /// </summary>
    public static void Invoke(ref DispatcherTimer dispatcherTimer, double milliseconds, Action action,
        DispatcherPriority priority = DispatcherPriority.Background)
    {
        if (Disable)
        {
            action();
            return;
        }
        dispatcherTimer?.Stop();
        if (milliseconds > 1)
        {
            dispatcherTimer = new(priority) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            dispatcherTimer.Tick += delegate (object sender, EventArgs e)
            {
                (sender as DispatcherTimer).Stop();
                Invoke(action, priority);
            };
            dispatcherTimer.Start();
        }
        else
            Invoke(action, priority);
    }

    /// <summary>
    /// If the previous call to this method with the same parameters was less than the given number
    /// of <paramref name="milliseconds"/> ago, returns <see langword="true"/> and executes the given
    /// <paramref name="action"/> asynchronously after waiting the remainder of this time. Otherwise, does nothing
    /// and returns <see langword="false"/>. Can be used to postpone executing a method and make it execute
    /// at regular time intervals.
    /// </summary>
    /// <param name="dispatcherTimer">The dispatcher timer used to invoke the action</param>
    /// <param name="time">The last time (<see cref="DateTime.Ticks"/>) the method returned <see langword="false"/></param>
    /// <param name="milliseconds">Minimum number of milliseconds between two method calls</param>
    /// <param name="action">The action to be executed after a delay when the method returns <see langword="true"/></param>
    /// <param name="priority">The dispatcher priority</param>
    public static bool SyncInvokeNeeded(ref DispatcherTimer dispatcherTimer, ref long time, int milliseconds,
        Action action, DispatcherPriority priority = DispatcherPriority.Background)
    {
        if (Disable)
            return false;
        StopInvoke(ref dispatcherTimer);
        int remaining = SimpleTimer.GetRemainingTime(time, milliseconds);
        if (remaining > 1)
        {
            Invoke(ref dispatcherTimer, remaining, action, priority);
            return true;
        }
        SimpleTimer.StartTimer(ref time);
        return false;
    }

    /// <summary>
    /// Stops waiting and cancels the delayed action if it hasn't been executed yet.
    /// </summary>
    public static void StopInvoke(ref DispatcherTimer dispatcherTimer)
    {
        dispatcherTimer?.Stop();
        dispatcherTimer = null;
    }

    /// <summary>
    /// Returns <see langword="true"/> if any delayed action hasn't been executed yet
    /// for any of the specified timers.
    /// </summary>
    public static bool IsInvoked(params DispatcherTimer[] dispatcherTimers)
    {
        foreach (var timer in dispatcherTimers)
            if (timer?.IsEnabled == true)
                return true;
        return false;
    }

    /// <summary>
    /// Executes the specified <paramref name="action"/> in a STA thread (Single-Threaded Apartment) and allows
    /// awaiting completion. Can be used to implement unit tests of WPF components that require the UI thread.
    /// </summary>
    public static Task InvokeSTA(Action action)
    {
        TaskCompletionSource<object> source = new();
        Thread thread = new(() =>
        {
            try
            {
                action();
                source.SetResult(new object());
            }
            catch (Exception e) { source.SetException(e); }
        });
        if (OperatingSystem.IsWindowsVersionAtLeast(7))
            thread.SetApartmentState(ApartmentState.STA);
        else
            throw new NotSupportedException("Requires Windows 7 or later");
        thread.Start();
        return source.Task;
    }

    /// <summary>
    /// Restarts the application with the specified command line <paramref name="arguments"/>
    /// and a shutdown delay in <paramref name="milliseconds"/>.
    /// </summary>
    public static void RestartApplication(string arguments, double milliseconds = 1000)
    {
        Process.Start(Environment.ProcessPath, arguments);
        Invoke(milliseconds, Application.Current.Shutdown);
    }

    /// <summary>
    /// Sets the language of the application, either from the App.config file
    /// or from the operating system if not specified in App.config.
    /// </summary>
    public static void SetGlobalLanguage()
    {
        if (string.IsNullOrWhiteSpace(ImageBrowserState.Language))
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;
        else
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(ImageBrowserState.Language);
    }
}