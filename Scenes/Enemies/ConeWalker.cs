using Godot;
using System;
using System.Linq;

public partial class ConeWalker : CharacterBody2D
{
    [Export] public float WalkSpeed = 50.0f;
    private Area2D _lineOfSight;
    private AnimatedSprite2D _sprite;

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

        // This point should be positioned so that it
        // is in the direction the character is facing.
        // This should give a good indication regardless
        // of how the character is initially scaled.
        var pointInDirectionFacing = GetNode<Node2D>("PointInDirectionFacing");
        _direction = this.XDirectionTo(pointInDirectionFacing);
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

        // Move in direction char is facing.
        Velocity = WalkSpeed * Direction.UnitVector();
        MoveAndSlide();
    }

    // TODO: reuse with Player's
    public void ChangeAnimation(string animation) {
        if (_sprite.Animation != animation)
            _sprite.Play(animation);
    }

}

