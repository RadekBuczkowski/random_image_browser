namespace Random_Image.Classes.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;

/// <summary>
/// Provides extension methods for basic operations on simple types, including
/// strings, double numbers, vectors, points, sizes, rectangles, and collections.
/// </summary>
public static class BasicExtensionMethods
{
    /// <summary>
    /// Returns a collection containing only this <paramref name="value"/>.
    /// </summary>
    public static IEnumerable<T> Yield<T>(this T value)
    {
        yield return value;
    }

    /// <summary>
    /// Returns <see langword="true"/> if this <paramref name="value"/> is included in the given parameters.
    /// </summary>
    public static bool IsIn<T>(this T value, params T[] values)
    {
        return values.Contains(value);
    }

    /// <summary>
    /// Removes all leading and trailing white-space characters from this string and converts null to an empty string.
    /// </summary>
    public static string TrimEmpty(this string text)
    {
        return text?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Extracts an array of strings from a comma or semicolon separated text.
    /// </summary>
    public static string[] ExtractItems(this string text)
    {
        return text.Replace(" ", string.Empty).ToLower()
            .Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Combines this collection of strings into one comma-separated text.
    /// </summary>
    public static string CombineItems(this IEnumerable<string> items)
    {
        return string.Join(", ", items);
    }

    /// <summary>
    /// Returns <see langword="true"/> if non-empty items contained in this comma-separated string
    /// and in the specified comma-separated string are identical, regardless of white spaces and empty items.
    /// </summary>
    public static bool AreNonEmptyItemsEqual(this string text1, string text2)
    {
        return text1.ExtractItems().SequenceEqual(text2.ExtractItems());
    }

    /// <summary>
    /// Compares this collection of string items with the specified collection disregarding empty or null items.
    /// </summary>
    public static bool AreNonEmptyItemsEqual(this IEnumerable<string> items1, IEnumerable<string> items2)
    {
        return items1.Where(item => string.IsNullOrWhiteSpace(item) == false)
            .SequenceEqual(items2.Where(item => string.IsNullOrWhiteSpace(item) == false));
    }

    /// <summary>
    /// Ensures this <see cref="value"/> is evenly divisible by the specified <see cref="divisor"/> parameter by 
    /// adding (if value is positive) or subtracting (if value is negative) the remainder.
    /// </summary>
    public static int MakeEvenlyDivisible(this int value, int divisor)
    {
        return (Math.Abs(value) + divisor - 1) / divisor * divisor * Math.Sign(value);
    }

    /// <summary>
    /// Returns this <paramref name="value"/> if it lies within the specified <paramref name="low"/>
    /// and <paramref name="high"/> limit or returns the limit that is exceeded.
    /// </summary>
    public static T Limit<T>(this T value, T low, T high) where T : IComparable
    {
        if (value.CompareTo(low) < 0)
            return low;
        if (value.CompareTo(high) > 0)
            return high;
        return value;
    }

    /// <summary>
    /// Limits both coordinates of this <paramref name="vector"/> in the positive and negative direction, i.e.
    /// vector.X will be between -limit.X and +limit.X, and vector.Y between -limit.Y and +limit.Y.
    /// When either limit is exceeded, the limit will be returned instead of the original vector coordinate.
    /// </summary>
    public static Vector Limit(this Vector vector, Vector limit)
    {
        if (Math.Abs(vector.X) > limit.X || Math.Abs(vector.Y) > limit.Y)
            return new(vector.X.Limit(-limit.X, limit.X), vector.Y.Limit(-limit.Y, limit.Y));
        return vector;
    }

    /// <summary>
    /// Changes either coordinate of this shifting <paramref name="vector"/> to 0
    /// if the coordinate is greater than -1 and less than 1 or if it isn't a number.
    /// </summary>
    public static Vector RoundToZero(this Vector vector)
    {
        static bool IsCloseToZero(double value) => double.IsNaN(value) || Math.Abs(value) < 1.0;
        return new Vector(IsCloseToZero(vector.X) ? 0 : vector.X,
                          IsCloseToZero(vector.Y) ? 0 : vector.Y);
    }

    /// <summary>
    /// Changes either coordinate of this scaling <paramref name="vector"/> to 1
    /// if the coordinate is very close to 1 or if it isn't a number.
    /// </summary>
    public static Vector RoundToOne(this Vector vector)
    {
        static bool IsCloseToOne(double value) => double.IsNaN(value) || Math.Abs(value - 1.0) < 0.001;
        return new Vector(IsCloseToOne(vector.X) ? 1 : vector.X,
                          IsCloseToOne(vector.Y) ? 1 : vector.Y);
    }

    /// <summary>
    /// Returns <see langword="true"/> if this <paramref name="vector1"/> has the same direction
    /// or <see langword="false"/> if the opposite direction as the specified <paramref name="vector2"/>.
    /// Both vectors must be horizontal or vertical.
    /// </summary>
    public static bool IsSameDirectionAs(this Vector vector1, Vector vector2)
    {
        return vector1.X >= 0 == vector2.X >= 0 && vector1.Y >= 0 == vector2.Y >= 0;
    }

    /// <summary>
    /// Returns the first value extracted from the given <paramref name="input"/> string with
    /// this <see cref="Regex"/> expression, or null if the expression was not found.
    /// </summary>
    public static string MatchFirst(this Regex regex, string input)
    {
        MatchCollection match = regex.Matches(input);
        return match.Count > 0 ? match[0].Groups[1].ToString() : null;
    }

    /// <summary>
    /// Returns the shift difference and scale difference between this rectangle and the specified rectangle.
    /// </summary>
    public static (Vector shift, Vector scale) RectangleDifference(this Rect rectangle1, Rect rectangle2)
    {
        double x = rectangle2.X - rectangle1.X + (rectangle2.Width - rectangle1.Width) / 2;
        double y = rectangle2.Y - rectangle1.Y + (rectangle2.Height - rectangle1.Height) / 2;
        double scaleX = rectangle2.Width / rectangle1.Width;
        double scaleY = rectangle2.Height / rectangle1.Height;
        return (new Vector(x, y), new Vector(scaleX, scaleY));
    }

    /// <summary>
    /// Returns the distance between this point and the specified point.
    /// </summary>
    public static double DistanceTo(this Point point1, Point point2)
    {
        return Point.Subtract(point1, point2).Length;
    }

    /// <summary>
    /// Returns the minimum distance between this point and all the specified points.
    /// </summary>
    public static double MinDistanceTo(this Point point1, params (double x, double y)[] points)
    {
        return points.Min(point => point1.DistanceTo(new Point(point.x, point.y)));
    }

    /// <summary>
    /// Swaps the width and height of this <paramref name="size"/> if <paramref name="angle"/> is 90 or -90 degrees.
    /// </summary>
    public static Size SetOrientation(this Size size, int angle)
    { 
        return (Math.Abs(angle) == 90) ? new Size(size.Height, size.Width) : size;
    }

    /// <summary>
    /// Undoes the scaling previously applied to this <paramref name="rectangle"/>
    /// and specified in <paramref name="scale"/>.
    /// </summary>
    public static Rect UndoScale(this Rect rectangle, Vector scale)
    {
        return new(rectangle.X / scale.X, rectangle.Y / scale.Y, rectangle.Width / scale.X, rectangle.Height / scale.Y);
    }

    /// <summary>
    /// Shuffles the order of items in this <paramref name="list"/>.
    /// </summary>
    public static void Shuffle<T>(this IList<T> list)
    {
        using var provider = RandomNumberGenerator.Create();
        byte[] data = new byte[4];
        int n = list.Count;
        while (n > 1)
        {
            provider.GetBytes(data);
            data[BitConverter.IsLittleEndian ? 3 : 0] &= 0x7f;
            int rand = BitConverter.ToInt32(data, 0);
            int k = rand % n;
            n--;
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}