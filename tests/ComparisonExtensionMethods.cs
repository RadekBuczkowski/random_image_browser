namespace Random_Image_Tests;

using System.Windows;

/// <summary>
/// Provides extension methods for comparing double numbers, vectors, points, sizes and rectangles.
/// </summary>
public static class ComparisonExtensionMethods
{
    /// <summary>
    /// The tolerance for comparing double values.
    /// </summary>
    private const double Precision = 1e-3;

    /// <summary>
    /// Returns <see langword="true"/> if this double value equals the specified double value.
    /// </summary>
    public static bool RoughlyEquals(this double value1, double value2)
    {
        return Math.Abs(value1 - value2) < Precision;
    }

    /// <summary>
    /// Returns <see langword="true"/> if this vector equals the specified vector.
    /// </summary>
    public static bool RoughlyEquals(this Vector vector1, Vector vector2)
    {
        return RoughlyEquals(vector1.X, vector2.X) && RoughlyEquals(vector1.Y, vector2.Y);
    }

    /// <summary>
    /// Returns <see langword="true"/> if this point equals the specified point.
    /// </summary>
    public static bool RoughlyEquals(this Point point1, Point point2)
    {
        return RoughlyEquals(point1.X, point2.X) && RoughlyEquals(point1.Y, point2.Y);
    }

    /// <summary>
    /// Returns <see langword="true"/> if this size equals the specified size.
    /// </summary>
    public static bool RoughlyEquals(this Size size1, Size size2)
    {
        return RoughlyEquals(size1.Width, size2.Width) && RoughlyEquals(size1.Height, size2.Height);
    }

    /// <summary>
    /// Returns <see langword="true"/> if this rectangle equals the specified rectangle.
    /// </summary>
    public static bool RoughlyEquals(this Rect rectangle1, Rect rectangle2)
    {
        return RoughlyEquals(rectangle1.Location, rectangle2.Location) &&
               RoughlyEquals(rectangle1.Size, rectangle2.Size);
    }
}