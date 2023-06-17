namespace Random_Image.Classes.Utilities;

using System;
using System.Runtime.Serialization;

/// <summary>
/// Provides a class for casting exceptions from background threads to the UI thread,
/// where they can be caught by the global exception handler and shown to the user in Notepad.
/// </summary>
[Serializable]
public class WrapperException : Exception
{
    public WrapperException() : base()
    {
    }

    public WrapperException(string message, Exception exception) : base(message, exception)
    {
    }

    public WrapperException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}