using Godot;
using System;

public static class Util
{
}

public enum XDirection
{
    LEFT,
    RIGHT,
}

public enum Sign
{
    POSITIVE,
    NONE,
    NEGATIVE
}

public static class Signs
{
    public static Sign Of(double n)
    {
        if (n > 0)
        {
            return Sign.POSITIVE;
        }
        if (n < 0)
        {
            return Sign.NEGATIVE;
        }
        return Sign.NONE;
    }
}

public static class Extensions
{
    public static Vector2 UnitVector(this XDirection direction) =>
        direction switch
        {
            XDirection.LEFT  => Vector2.Left,
            XDirection.RIGHT => Vector2.Right,
            _                => throw new ArgumentOutOfRangeException(nameof(direction)),
        };

    public static Sign Opposite(this Sign sign) =>
        sign switch 
        {
            Sign.POSITIVE => Sign.NEGATIVE,
            Sign.NEGATIVE => Sign.POSITIVE,
            Sign.NONE     => Sign.NONE,
            _             => throw new ArgumentOutOfRangeException(nameof(sign)),
        };

    public static int Unit(this Sign sign) =>
        sign switch 
        {
            Sign.POSITIVE => 1,
            Sign.NEGATIVE => -1,
            Sign.NONE     => 0,
            _             => throw new ArgumentOutOfRangeException(nameof(sign)),
        };

    public static Vector2 WithX(this Vector2 v, float x) =>
        new Vector2(x, v.Y);

    public static Vector2 MapX(this Vector2 v, Func<float, float> f) =>
        v.WithX(f(v.X));

    public static float WithSign(this float n, Sign sign) => 
        Math.Abs(n) * sign.Unit();

    public static void TransformScale(this Node2D node, Func<Vector2, Vector2> f)
    {
        node.Scale = f(node.Scale);
    }
}


