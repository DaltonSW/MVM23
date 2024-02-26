using Godot;
using System;
using System.Collections.Generic;

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

public static class DoubleExtensions
{
    public static Sign Sign(this double n)
    {
        if (n > 0)
        {
            return global::Sign.POSITIVE;
        }
        if (n < 0)
        {
            return global::Sign.NEGATIVE;
        }
        return global::Sign.NONE;
    }

    public static Sign Sign(this float n)
        => ((double)n).Sign();
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

    public static XDirection Opposite(this XDirection direction) =>
        direction switch
        {
            XDirection.LEFT  => XDirection.RIGHT,
            XDirection.RIGHT => XDirection.LEFT,
            _                => throw new ArgumentOutOfRangeException(nameof(direction)),
        };

    public static Vector2 WithX(this Vector2 v, float x) =>
        new Vector2(x, v.Y);

    public static Vector2 MapX(this Vector2 v, Func<float, float> f) =>
        v.WithX(f(v.X));

    public static XDirection XDirectionTo(this Vector2 from, Vector2 to) =>
        from.X < to.X
            ? XDirection.RIGHT
            : XDirection.LEFT;

    public static XDirection XDirectionTo(this Node2D from, Node2D to) =>
        from.GlobalPosition.XDirectionTo(to.GlobalPosition);

    public static XDirection XDirectionFromOrigin(this Vector2 v) =>
        Vector2.Zero.XDirectionTo(v);

    public static float WithSign(this float n, Sign sign) => 
        Math.Abs(n) * sign.Unit();

    public static void TransformScale(this Node2D node, Func<Vector2, Vector2> f)
    {
        node.Scale = f(node.Scale);
    }

    public static void SetPoints(this Curve2D curve, List<CurvePoint> points)
    {
        curve.ClearPoints();
        foreach (var point in points)
        {
            curve.AddPoint(point.Position, point.InControlPoint, point.OutControlPoint);
        }
    }

    public static CurvePoint LastPoint(this Curve2D curve)
    {
        var lastI = curve.PointCount - 1;
        var pointPosition = curve.GetPointPosition(lastI);
        var pointIn = curve.GetPointIn(lastI);
        var pointOut = curve.GetPointOut(lastI);
        return new CurvePoint(pointPosition, pointIn, pointOut);
    }

    public static float RightAngleRadians(this RotDirection direction) =>
        direction switch
        {
            RotDirection.CLOCKWISE        =>  Constants.PI_OVER_2,
            RotDirection.COUNTERCLOCKWISE => -Constants.PI_OVER_2,
            _                             => throw new ArgumentOutOfRangeException(nameof(direction)),
        };

    public static Vector2 AtAngleWithRandomOffset(this Vector2 point,
            Range distanceBounds,
            float angleRadians) => 
        point + Vector2s.FromPolar(distanceBounds.RandF(), angleRadians);
    public static Vector2 WithRandomPolarOffsets(this Vector2 point,
            Range distanceBounds,
            float angleRadians, Range angleOffsetBounds) => 
        point.AtAngleWithRandomOffset(distanceBounds, angleRadians + angleOffsetBounds.RandF());

    public static Vector2 MidpointTo(this Vector2 from, Vector2 to) =>
        from + 0.5f * (to - from);
}

public class Range
{
    public double LowerInclusive { get; private set; }
    public double UpperInclusive { get; private set; }

    public Range(double lowerInclusive, double upperInclusive)
    {
        LowerInclusive = lowerInclusive;
        UpperInclusive = upperInclusive;
    }

    public static Range PlusOrMinus(double nInclusive)
        => new Range(-nInclusive, nInclusive);

    public float RandF() => (float) Rand();
    public double Rand() => GD.RandRange(LowerInclusive, UpperInclusive);
}

public class CurvePoint
{
    public Vector2 Position { get; private set; }
    public Vector2? InControlPoint { get; private set; }
    public Vector2? OutControlPoint { get; private set; }

    public CurvePoint(Vector2 position, Vector2? inControlPoint, Vector2? outControlPoint)
    {
        Position = position;
        InControlPoint = inControlPoint;
        OutControlPoint = outControlPoint;
    }

    public static CurvePoint WithSymmetricalControlPoints(Vector2 position, Vector2 outControlPoint) =>
        new CurvePoint(position, -outControlPoint, outControlPoint);
}

public static class SignExtensions
{
    public static RotDirection? RotDirection(this Sign sign) =>
        sign switch
        {
            Sign.POSITIVE => global::RotDirection.CLOCKWISE,
            Sign.NEGATIVE => global::RotDirection.COUNTERCLOCKWISE,
            Sign.NONE     => null,
            _             => throw new ArgumentOutOfRangeException(nameof(sign)),
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
}

public static class Vector2s
{
    public static Vector2 FromPolar(float distance, float angleRadians) =>
        distance * Vector2.FromAngle(angleRadians);
}

public static class Constants
{
    public const float PI_OVER_2 = (float)(Math.PI / 2);
    public static Range NEG_TO_POS_PI_OVER_2 = Range.PlusOrMinus(PI_OVER_2);
    public static Range NEG_TO_POS_PI = Range.PlusOrMinus(Math.PI);
}

public enum RotDirection
{
    CLOCKWISE,
    COUNTERCLOCKWISE
}
