using Godot;
using System;
using System.Linq;

public partial class ConeWalker : CharacterBody2D
{
    [Export] public float WalkSpeed = 800;
    [Export] public float ChaseSpeed = 6000;

    private Area2D _lineOfSight;
    private AnimatedSprite2D _sprite;
    private RayCast2D _dropAheadRayCast;
    private RayCast2D _edgeAheadRayCast;

    private float _gravity;

    /// The node this character is locked onto, if any.
    /// Is null if there is none.
    private Node2D _target;

    /// The current direction this character is facing.
    /// After initialization, use the property instead of
    /// accessing this directly.
    // TODO: move to abstract base class to enforce this.
    private XDirection _direction;

    public XDirection Direction
    {
        get => _direction;
        set
        {
            if (value != _direction)
            {
                // Flip
                this.TransformScale(scale =>
                    scale.MapX(x => x *= -1));
            }
            _direction = value;
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _lineOfSight = GetNode<Area2D>("LineOfSight");
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _dropAheadRayCast = GetNode<RayCast2D>("DropAheadRayCast");
        _edgeAheadRayCast = GetNode<RayCast2D>("EdgeAheadRayCast");

        // This point should be positioned so that it
        // is in the direction the character is facing.
        // This should give a good indication regardless
        // of how the character is initially scaled.
        var pointInDirectionFacing = GetNode<Node2D>("PointInDirectionFacing");
        _direction = this.XDirectionTo(pointInDirectionFacing);

        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        ChangeAnimation(
            Velocity.IsZeroApprox()
                ? "idle"
                : "walk");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_target is null)
        {
            // Find target
            _target = _lineOfSight.GetOverlappingBodies()
                .FirstOrDefault(body => body.IsInGroup("enemy_target"), null);
        }
        if (_target is Node2D target)
        {
            // Face target
            Direction = this.XDirectionTo(target);
        }
        if (EdgeAhead() && !Chasing())
        {
            Direction = Direction.Opposite();
        }

        // Move in direction char is facing.
        Velocity = CalcVelocity(delta);
        MoveAndSlide();
    }

    private Vector2 CalcVelocity(double delta)
    {
        var velocity = Velocity;

        // fake friction
        velocity.X = 0;

        if (IsOnFloor())
        {
            // foot
            velocity += (float)delta * FootSpeed() * Direction.UnitVector();
        }

        // gravity
        velocity += (float)delta * _gravity * Vector2.Down;

        return velocity;
    }

    private bool Chasing() => _target is not null;
    private float FootSpeed() =>
        DropAhead()
            ? 0
            : Chasing()
                 ? ChaseSpeed
                 : WalkSpeed;

    private bool DropAhead() => !_dropAheadRayCast.IsColliding();
    private bool EdgeAhead() => !_edgeAheadRayCast.IsColliding();

    // TODO: reuse with Player's
    private void ChangeAnimation(string animation) {
        if (_sprite.Animation != animation)
            _sprite.Play(animation);
    }

}

