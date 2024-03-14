using Godot;
using System;
using System.Collections.Generic;

public partial class RandomFlyer : Node2D, IHittable
{
    [Export] bool Debug = false;
    [Export] int StartHitPoints { get; set; } = 1;
    [Export] float KnockbackMagnitude { get; set; } = 100f;

    private const float MAX_FLOAT_SPEED = 30;
    private const float MIN_FLOAT_SPEED = 10;
    private const float FLOAT_ACCEL = 10;
    private const int NUM_CURVE_POINTS = 3;

    private static Range CURVE_POINT_DISTANCE_RANGE = new Range(20, 50);
    private static Range CURVE_CONTROL_POINT_DISTANCE_RANGE = new Range(20, 50);

    private Curve2D _floatPath;
    private float _floatPathDistance; 
    private float _floatSpeed;
    private Sign _accelDirection;

    private HitManager _hitManager;

    private RandomFlyerBody _body;

    public override void _Draw()
    {
        base._Draw();
        if (Debug)
        {
            for (int i = 0; i < _floatPath.PointCount; i++)
            {
                var pos = _floatPath.GetPointPosition(i);
                var inPos = _floatPath.GetPointIn(i);
                var outPos = _floatPath.GetPointOut(i);
                DrawCircle(pos, 3, Godot.Colors.Gold);
                if (!inPos.IsZeroApprox())
                    DrawCircle(pos + inPos, 3, Godot.Colors.Red);
                if (!outPos.IsZeroApprox())
                    DrawCircle(pos + outPos, 3, Godot.Colors.Blue);
            }
            for (float n = 0; n <= 1; n += 0.01f)
            {
                DrawCircle(_floatPath.SampleBaked(n * _floatPath.GetBakedLength(), cubic: true), 0.5f, Godot.Colors.Cyan);
            }
        }
    }
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _body = GetNode<RandomFlyerBody>("Body");

        _floatPath = new Curve2D();
        _floatPath.SetPoints(GenerateEssCurveInRandomDir(_body.Position));
        _floatPathDistance = 0;
        _floatSpeed = MIN_FLOAT_SPEED;

        _hitManager = new HitManager(this, StartHitPoints, GetNode<AnimatedSprite2D>("Body/AnimatedSprite2D"));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public override void _PhysicsProcess(double delta)
    {
        // Oscillate speed between slowest and fastest
        if (_floatSpeed >= MAX_FLOAT_SPEED)
        {
            _accelDirection = Sign.NEGATIVE;
        }
        if (_floatSpeed <= MIN_FLOAT_SPEED)
        {
            _accelDirection = Sign.POSITIVE;
        }
        _floatSpeed += (float)delta * FLOAT_ACCEL.WithSign(_accelDirection);

        // Move along path
        _floatPathDistance += (float)delta * _floatSpeed;

        // If at the end of the float path, generate a new one
        if (_floatPathDistance >= _floatPath.GetBakedLength())
        {
            _floatPath.SetPoints(GenerateEssCurveInRandomDir(_floatPath.LastPoint().Position));
            _floatPathDistance = 0;
        }

        // Apply movement
        var pos = _floatPath.SampleBaked(_floatPathDistance, cubic: true);
        var movement = pos - _body.Position;
        _body.XDirMan.Direction = movement.XDirectionFromOrigin();
        var maybeCollision = _body.MoveAndCollide(movement);

        // If it hits something, generate a new
        // path going the opposite direction.
        if (maybeCollision is KinematicCollision2D collision)
        {
            EnemyUtils.HitCollideeIfApplicable(_body, collision, KnockbackMagnitude);
            _floatPath.SetPoints(GenerateEssCurveOfRandomLength(_body.Position,
                        collision.GetAngle() + (float)-Constants.PI_OVER_2));
            _floatPathDistance = 0;
        }

        if (Debug)
            QueueRedraw();
    }

    public void TakeHit(Vector2 initialKnockbackVelocity)
    {
        _hitManager.TakeHit(initialKnockbackVelocity);
    }

    public void QueueDeath()
    {
        QueueFree();
    }

    public bool DeathQueued() => IsQueuedForDeletion();

    private static List<CurvePoint> GenerateEssCurveInRandomDir(Vector2 start) =>
        GenerateEssCurveOfRandomLength(start, Constants.NEG_TO_POS_PI.RandF());

    private static List<CurvePoint> GenerateEssCurveOfRandomLength(Vector2 start, float angleRadians) =>
        GenerateEssCurve(start, 
            start.AtAngleWithRandomOffset(CURVE_POINT_DISTANCE_RANGE, angleRadians));

    private static List<CurvePoint> GenerateEssCurve(Vector2 start, Vector2 end)
    {
        var list = new List<CurvePoint>();
        list.Add(new CurvePoint(start, null, Vector2.Zero));

        var midpointPos = start.MidpointTo(end);
        var angle = start.AngleTo(end);
        var outPosition = Vector2.Zero.WithRandomPolarOffsets(
                CURVE_CONTROL_POINT_DISTANCE_RANGE,
                angle, Range.PlusOrMinus(Math.PI * 3 / 4));
        var midpoint = CurvePoint.WithSymmetricalControlPoints(midpointPos, outPosition);
        list.Add(midpoint);

        list.Add(new CurvePoint(end, Vector2.Zero, null));
        return list;
    }

}

public class EnemyUtils
{
    public static void HitCollideeIfApplicable(Node2D self, KinematicCollision2D collision, float knockbackMagnitude)
    {
        if (collision.GetCollider() is Node2D colliderNode
                && colliderNode.IsInGroup("enemy_hurt_on_collide"))
        {
            var knockback = Vector2s.FromPolar(knockbackMagnitude,
                    self.GetAngleToNode(colliderNode));
            ((IHittable) colliderNode).TakeHit(knockback);
        }
    }
}
