using Godot;
using System;
using System.Collections.Generic;

public static class Util
{
}

public enum Direction8
{
    RIGHT,
    DOWN_RIGHT,
    DOWN,
    DOWN_LEFT,
    LEFT,
    UP_LEFT,
    UP,
    UP_RIGHT,
}

public enum YDirection
{
    UP,
    DOWN,
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
    public static Angle GetAngleObjectTo(this Node2D n, Vector2 pos) =>
        Angle.FromRadians(n.GetAngleTo(pos));

    public static Angle GetAngleObjectToPoint(this Node2D n, Vector2 other) =>
        Angle.FromRadians(n.GetAngleToPoint(other));

    public static float GetAngleToPoint(this Node2D n, Vector2 other) =>
        n.GlobalPosition.AngleToPoint(other);
    
    public static float GetAngleToNode(this Node2D n, Node2D other) =>
        n.GetAngleToPoint(other.GlobalPosition);

    public static Angle AngleObject(this Vector2 v) =>
        Angle.FromRadians(v.Angle());

    public static Direction8 NearestDirection8(this Vector2 v) =>
        v.AngleObject().NearestDirection8();

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

public static class Direction8Extensions
{
    public static Angle Angle(this Direction8 d) =>
        global::Angle.FromRadians(d.Radians());

    public static float Radians(this Direction8 d) =>
        d switch
        {
            Direction8.UP_LEFT    => -3 * Constants.PI_OVER_4,
            Direction8.UP         => -Constants.PI_OVER_2,
            Direction8.UP_RIGHT   => -Constants.PI_OVER_4,
            Direction8.RIGHT      => 0,
            Direction8.DOWN_RIGHT => Constants.PI_OVER_4,
            Direction8.DOWN       => Constants.PI_OVER_2,
            Direction8.DOWN_LEFT  => 3 * Constants.PI_OVER_4,
            Direction8.LEFT       => (float)Math.PI,
            _ => throw new ArgumentOutOfRangeException(nameof(d)),
        };
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
    public const float PI_OVER_4 = (float)(Math.PI / 4);
    public const float PI_OVER_8 = (float)(Math.PI / 8);
    public static Range NEG_TO_POS_PI_OVER_2 = Range.PlusOrMinus(PI_OVER_2);
    public static Range NEG_TO_POS_PI = Range.PlusOrMinus(Math.PI);
}

public enum RotDirection
{
    CLOCKWISE,
    COUNTERCLOCKWISE
}

public class Angle
{
    public float Radians { get; private init; }

    private Angle(float radians)
    {
        Radians = radians;
    }

    /// https://stackoverflow.com/questions/1878907/how-can-i-find-the-smallest-difference-between-two-angles-around-a-point
    public Angle SmallestAngleTo(Angle other)
    {
        var d = other.Radians - Radians;
        if (d > 180)
            d -= 360;
        if (d < -180)
            d += 360;
        return Angle.FromRadians(d);
    }

    public Angle ReflectedOverY() =>
        Angle.FromRadians(Mathf.Pi - Radians);

    public Angle Lerp(Angle other, float weight) =>
        Angle.FromRadians(Mathf.LerpAngle(this.Radians, other.Radians, weight));

    public Direction8 NearestDirection8() =>
        Radians switch
        {
             < -7 * Constants.PI_OVER_8
                => Direction8.LEFT,
            >= -7 * Constants.PI_OVER_8 and < -5 * Constants.PI_OVER_8
                => Direction8.UP_LEFT,
            >= -5 * Constants.PI_OVER_8 and < -3 * Constants.PI_OVER_8
                => Direction8.UP,
            >= -3 * Constants.PI_OVER_8 and < -1 * Constants.PI_OVER_8
                => Direction8.UP_RIGHT,
            >= -1 * Constants.PI_OVER_8 and < +1 * Constants.PI_OVER_8
                => Direction8.RIGHT,
            >= +1 * Constants.PI_OVER_8 and < +3 * Constants.PI_OVER_8
                => Direction8.DOWN_RIGHT,
            >= +3 * Constants.PI_OVER_8 and < +5 * Constants.PI_OVER_8
                => Direction8.DOWN,
            >= +5 * Constants.PI_OVER_8 and < +7 * Constants.PI_OVER_8
                => Direction8.DOWN_LEFT,
            >= +7 * Constants.PI_OVER_8
                => Direction8.LEFT,
            _ => throw new ArgumentOutOfRangeException(nameof(Radians)),
        };

    public static Angle FromRadians(float radians) =>
        new Angle(Normalize(radians));

    private static float Normalize(float radians) =>
        radians % (float)Math.PI;
}

