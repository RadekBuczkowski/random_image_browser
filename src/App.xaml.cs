namespace Random_Image;

using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Random_Image.Classes.Utilities;

/// <summary>
/// Interaction logic of the entire application.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// The application title and name of the application specified in the Properties/AssemblyInfo.cs file.
    /// </summary>
    public static string Title => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title;

    /// <summary>
    /// Command line arguments when starting the application.
    /// </summary>
    public static string[] CommandLine { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Prevents opening a window by too many exceptions
    /// </summary>
    private static int _counter = 0;

    /// <summary>
    /// Saves the command line arguments.
    /// </summary>
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        Dispatcher.UnhandledException += OnUnhandledException;
        if (e.Args != null)
            CommandLine = e.Args;
    }

    /// <summary>
    /// Implements the global exception handler opening the exception details in Notepad.
    /// </summary>
    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (Interlocked.Increment(ref _counter) > 3)
            return;

        // Note: we only support English as the language of exceptions.
        const string UnhandledExceptionOccurred = "An unhandled exception occurred and the program is about to close.";
        const string InformationCanHelpFixing = "The following information can help finding the problem.";

        string message = e.Exception.ResolveMessage(UnhandledExceptionOccurred, false);
        MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
        e.Exception.OpenInNotepad(InformationCanHelpFixing, Title);
        e.Handled = false;
    }
}