namespace Random_Image_Tests.Classes.Utilities;

using System;
using System.Reflection;

using Random_Image.Classes.Utilities;

public class Test_ExceptionHelper
{
    [Fact]
    public void Test_Fix()
    {
        // arrange
        const string title = "Operation result:";
        const string message = "Failed to complete";
        const string parameters = "Extra parameters";
        string nl2x = Environment.NewLine + Environment.NewLine;
        string expected_result = $"{title}{nl2x}{message}{nl2x}{parameters}";

        // act
        Exception sut = new(message);
        sut.Fix(parameters);
        string actual_result = sut.ResolveMessage(title);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Theory]
    [InlineData(typeof(ArgumentException), true)]
    [InlineData(typeof(Exception), false)]
    public void Test_IsUserErrorException(Type type, bool expected_result)
    {
        // act
        Exception? sut = (Exception?)Activator.CreateInstance(type);
        bool actual_result = sut.IsUserErrorException();

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_ResolveMessage()
    {
        // arrange
        const string message = "Failed to complete";
        const string inner_message = "Sut";
        const string wrapper_message = "Wrapper";
        string long_message = string.Join(" ", Enumerable.Repeat("This is a very long message.", 10));
        string nl = Environment.NewLine;
        string expected_result = $"{message}{nl}{wrapper_message}{nl}{long_message}{nl}{inner_message}";

        // act
        ArgumentException nested = new(long_message, new Exception(inner_message));
        Exception sut = new(message, new WrapperException(wrapper_message, nested));
        string actual_result = sut.ResolveMessage(withStackTrace: false);

        // assert
        Assert.Equal(expected_result, actual_result);
    }

    [Fact]
    public void Test_ResolveMessage_WithDetails()
    {
        // arrange
        const string outer = "Failed to complete";
        const string inner = "Inner";
        const string wrapper = "Wrapper";
        const string comment = "Nothing to do.";
        string? module = Assembly.GetCallingAssembly().FullName;
        string long_message = string.Join(" ", Enumerable.Repeat("This is a very long message.", 10));
        string long_message_cut = long_message[..(ExceptionHelper.MaxStackTraceMessageLength - 3)] + "...";
        string nl = Environment.NewLine;
        string nl2x = Environment.NewLine + Environment.NewLine;
        string preamble = $"{outer}{nl}{wrapper}{nl}{long_message}{nl}{inner}{nl2x}{comment}{nl2x}Details:";
        string ex1 = $"{nl2x}{inner}{nl}{typeof(Exception).FullName}";
        string ex2 = $"{nl2x}{inner}...{nl}{typeof(WrapperException).FullName}";  // comment is replaced with "..."
        string ex3 = $"{nl2x}{long_message_cut}{nl}{typeof(Exception).FullName}";
        string ex4 = $"{nl2x}{wrapper}{nl}{typeof(WrapperException).FullName}";
        string ex5 = $"{nl2x}{outer}{nl}Module: {module}{nl}{typeof(Exception).FullName}";
        string expected_result = $"{preamble}{ex1}{ex2}{ex3}{ex4}{ex5}{nl}";

        // act
        Exception nested = new(long_message, new WrapperException(inner + nl + comment, new Exception(inner)));
        Exception sut = new(outer, new WrapperException(wrapper, nested)) { Source = module };
        string actual_result = sut.ResolveMessage(withStackTrace: true);

        // assert
        Assert.Equal(expected_result, actual_result);
    }
}