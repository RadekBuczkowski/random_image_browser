namespace Random_Image.Classes.Utilities;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Provides a class for parsing exceptions with any nested exceptions and converting them to a user-friendly text.
/// The text can be shown in Notepad together with the operating system information from the global exception handler.
/// </summary>
public static partial class ExceptionHelper
{
    /// <summary>
    /// When resolving exceptions with stack trace included and adding the same exception message again, messages
    /// with this length (minus 3 characters) or longer will be cut and three dots will be appended to them.
    /// </summary>
    public const int MaxStackTraceMessageLength = 250;

    /// <summary>
    /// Identifier of an extra message appended to the exception by the code that caught the exception.
    /// </summary>
    private const string ExtraMessageId = "Extra";

#if DEBUG
    private static readonly MethodInfo Preserve =
        typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);
#endif

    /// <summary>
    /// Attaches an extra message to the exception by the code that caught the exception. If in debug mode,
    /// the method also preserves the last stack trace line. In .NET executing <see langword="throw"/> deletes
    /// the last stack trace line (with the line number) if the exception was raised in the same method as the catch.
    /// This solves the problem, and the entire stack trace will be included. 
    /// </summary>
    public static Exception Fix(this Exception exception, string extraMessage = null)
    {
        if (string.IsNullOrWhiteSpace(extraMessage) == false)
        {
            exception.Data[ExtraMessageId] = extraMessage;
        }
#if DEBUG
        try
        {
            // The method is undocumented, although it has been available since .NET 2.0.
            Preserve.Invoke(exception, null);
        }
        catch 
        {
            // Do nothing.
        }
#endif
        return exception;
    }

    /// <summary>
    /// Extracts the extra message appended to this exception by the code that caught the exception.
    /// See the <see cref="Fix"/> method.
    /// </summary>
    private static string GetFix(this Exception exception)
    {
        return exception.ExtractExceptions()
                        .Select(ex => ex.Data[ExtraMessageId] as string)
                        .LastOrDefault(name => string.IsNullOrWhiteSpace(name) == false);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the exception is the result of a user error
    /// and the stack trace is irrelevant.
    /// </summary>
    public static bool IsUserErrorException(this Exception exception)
    {
        return exception is ArgumentException;
    }

    /// <summary>
    /// Extracts the exception message and inner exception messages from this <paramref name="exception"/>
    /// and returns a user-friendly text describing the entire object. Duplicate messages are removed.
    /// </summary>
    public static string ResolveMessage(this Exception exception, string header = null, bool withStackTrace = false)
    {
        try
        {
            List<string> messages = new();
            string previousMessage = string.Empty;
            string lastMessage = string.Empty;
            bool hideStackTrace = exception.ExtractExceptions().Any(ex => ex.IsUserErrorException());
            if (withStackTrace)
            {
                // If with stack trace, add the resolved message without stack trace first.
                messages.Add(ResolveMessage(exception, header, false));
                messages.Add(string.Empty);
                messages.Add("Details:");
                messages.Add(string.Empty);
            }
            else
            {
                // Add the optional header parameter if it is not already contained in the exception message.
                if (string.IsNullOrWhiteSpace(header) == false && exception?.Message?.Contains(header) != true)
                {
                    previousMessage = header.Trim();
                    messages.Add(previousMessage);
                    // Add a new line, if the header doesn't already contain the exception message.
                    if (string.IsNullOrWhiteSpace(exception?.Message) == false && header.Contains(exception.Message) == false)
                        messages.Add(string.Empty); 
                }
            }
            foreach (Exception ex in exception.ExtractExceptions(reverse: withStackTrace))
            {
                ProcessInnerException(ex, withStackTrace, hideStackTrace, messages, ref previousMessage, ref lastMessage);
            }
            string extraMessage = exception.GetFix();
            if (extraMessage != null && messages.Exists(message => message.Contains(extraMessage)) == false)
            {
                messages.Add(string.Empty);
                messages.Add(extraMessage.Trim());
            }
            return string.Join(Environment.NewLine, messages) + lastMessage;
        }
        catch (Exception ex) // The above code is correct, so this catch is most likely superfluous. :)
        {
            return (string.IsNullOrWhiteSpace(header) ?
                string.Empty : (header + Environment.NewLine + Environment.NewLine)) +
                "Failed parsing the exception! " + (ex?.Message ?? string.Empty) + (ex?.StackTrace ?? string.Empty);
        }
    }

    /// <summary>
    /// Processes one exception returned by <see cref="ExtractExceptions"/>.
    /// </summary>
    private static void ProcessInnerException(Exception exception, bool withStackTrace, bool hideStackTrace,
        List<string> messages, ref string previousMessage, ref string lastMessage)
    {
        if (withStackTrace)
        {
            string stackTrace = exception.StackTrace;
            messages.Add(StringWithMaxLength(exception.Message, MaxStackTraceMessageLength));
            if (string.IsNullOrWhiteSpace(exception.Source) == false && hideStackTrace == false)
            {
                messages.Add("Module: " + exception.Source);
            }
            messages.Add(exception.GetType().ToString());
            if (string.IsNullOrWhiteSpace(stackTrace) == false && hideStackTrace == false)
            {
                messages.Add(stackTrace);
            }
            messages.Add(string.Empty);  // Add a new line.
        }
        else if (string.IsNullOrWhiteSpace(exception.Message) == false)
        {
            string newMessage = exception.Message.Trim();
            if (messages.Exists(message => message.Contains(newMessage)) == false)
            {
                // Add the new message only if it is not contained in any of the previous exception messages.
                if (exception is AggregateException)
                {
                    if (messages.Count > 0)
                        messages.Add(string.Empty);  // Add a new line.
                }
                previousMessage = newMessage;
                messages.Add(previousMessage);
            }
            else if (lastMessage == string.Empty)
            {
                // If the preceding message is contained in the new message (inner-most exceptions come first!)
                // and the new message has a concatenated text at its end, e.g.:
                //    throw new Exception(ex.Message + Environment.NewLine + "Nothing to do!", ex)
                // and only if the concatenated text contains new lines, move the text to the end of the entire
                // exception message list.
                int index = previousMessage.LastIndexOf(newMessage);
                if (index >= 0 && (index + newMessage.Length) < previousMessage.Length)
                {
                    index += newMessage.Length;
                    newMessage = previousMessage[..index];
                    lastMessage = previousMessage[index..];
                    if (lastMessage.Contains('\n') || lastMessage.Contains('\r'))
                    {
                        messages[messages.LastIndexOf(previousMessage)] = newMessage;
                        previousMessage = newMessage;
                        lastMessage = Environment.NewLine + Environment.NewLine + lastMessage.Trim();
                    }
                    else
                        lastMessage = string.Empty;
                }
            }
        }
    }

    /// <summary>
    /// Returns a collection consisting of this <paramref name="exception"/> and all its inner exceptions extracted
    /// recursively down to the specified <paramref name="depth"/>. By default exceptions are ordered
    /// from outer-most to inner-most. If the <paramref name="reverse"/> parameter is <see langword="true"/>,
    /// the order is reversed.
    /// </summary>
    private static IEnumerable<Exception> ExtractExceptions(this Exception exception,
        bool reverse = false, int depth = 7)
    {
        IEnumerable<Exception> result = exception.ExtractExceptions(depth).Distinct();
        return reverse ? result.Reverse() : result;
    }

    /// <summary>
    /// Returns a collection consisting of this <paramref name="exception"/> and all its inner exceptions extracted
    /// recursively down to the specified <paramref name="depth"/>.
    /// </summary>
    private static IEnumerable<Exception> ExtractExceptions(this Exception exception, int depth)
    {
        if (exception != null)
        {
            yield return exception;
            if (depth-- > 0)
            {
                if (exception is AggregateException agEx)
                {
                    foreach (Exception inEx in agEx.InnerExceptions)
                    {
                        if (inEx is System.Threading.Tasks.TaskCanceledException)
                            continue;
                        foreach (Exception ex in inEx.ExtractExceptions(depth))
                            yield return ex;
                    }
                }
                else
                {
                    foreach (Exception ex in exception.InnerException.ExtractExceptions(depth))
                        yield return ex;
                }
                if (exception is ReflectionTypeLoadException rtlEx && rtlEx.LoaderExceptions != null)
                {
                    foreach (Exception lex in rtlEx.LoaderExceptions)
                        foreach (Exception ex in lex.ExtractExceptions(depth))
                            yield return ex;
                }
            }
        }
    }

    /// <summary>
    /// Cuts the <paramref name="text"/> string to the specified <paramref name="length"/>. If the string was cut,
    /// it will get three dots at the end. If the string has new line characters, the result will only contain
    /// the first line. If the string has multiple lines, the result will always end with three dots.
    /// </summary>
    private static string StringWithMaxLength(string text, int length)
    {
        if (text != null)
        {
            text = text.Trim();
            bool multi = false;
            int pos = text.IndexOf("\r");
            if (pos >= 0)
            {
                text = text[..pos];
                multi = true;
            }
            pos = text.IndexOf("\n");
            if (pos >= 0)
            {
                text = text[..pos];
                multi = true;
            }
            if (text.Length > length && length > 3)
                text = text[..(length - 3)] + "...";
            else if (multi)
                text = text.Trim('.') + "...";
        }
        return text;
    }

    /// <summary>
    /// Returns details about the system environment.
    /// </summary>
    private static string GetEnvironmentDetails()
    {
        StringBuilder info = new();
        info.AppendLine("Time: " + DateTime.Now.ToString());
        info.AppendLine("UTC: " + DateTime.UtcNow.ToString());
        info.AppendLine("OS version: " + Environment.OSVersion);
        info.AppendLine("Machine name: " + Environment.MachineName);
        info.AppendLine("CPU count: " + Environment.ProcessorCount);
        info.AppendLine("Build: " + (Environment.Is64BitProcess ? "64 bit" : "32 bit"));
        return info.ToString();
    }

    /// <summary>
    /// Shows details of this exception in Notepad together with environment details.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Starts Notepad")]
    public static void OpenInNotepad(this Exception ex, string header, string title)
    {
        try
        {
            string message = ex.ResolveMessage(header, true) + Environment.NewLine;
            message += GetEnvironmentDetails();
            OpenInNotepad(message, title);
        }
        catch
        {
            // Do nothing.
        }
    }

    /// <summary>
    /// Opens Windows Notepad.
    /// </summary>
    public static void OpenInNotepad(string message, string title)
    {
        Process notepad = Process.Start(new ProcessStartInfo("notepad.exe"));
        if (notepad != null)
        {
            notepad.WaitForInputIdle();

            if (string.IsNullOrEmpty(title) == false)
                _ = SetWindowText(notepad.MainWindowHandle, title);

            if (string.IsNullOrEmpty(message) == false)
            {
                IntPtr child = FindWindowEx(notepad.MainWindowHandle, new IntPtr(0), "Edit", null);
                _ = SendMessage(child, 0x000C, 0, message);
            }
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "SetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SetWindowText(IntPtr hWnd, string text);

    [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpSzClass, string lpSzWindow);

    [LibraryImport("User32.dll", EntryPoint = "SendMessageW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);
}