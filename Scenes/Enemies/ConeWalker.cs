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

    /// The direction this character's sprite is
    /// initially facing, when loaded into its parent scene.
    private XDirection _baseSpriteDirection;
    private XDirection _baseLineOfSightDirection;
    private Sign _baseLineOfSightXScaleSign;

    /// The direction this character is facing.
    private XDirection _direction; 

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _lineOfSight = GetNode<Area2D>("LineOfSight");
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        _baseSpriteDirection = Scale.X > 0
            ? XDirection.RIGHT
            : XDirection.LEFT;
        _baseLineOfSightDirection = _baseSpriteDirection;
        _baseLineOfSightXScaleSign = Signs.Of(_lineOfSight.Scale.X);

        _direction = _baseSpriteDirection;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Flip the sprite when the character is not facing the
        // same direction as its sprite was set to face.
        _sprite.FlipH = _direction != _baseSpriteDirection;
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
            _direction = GlobalPosition.X < target.GlobalPosition.X
                ? XDirection.RIGHT
                : XDirection.LEFT;
        }

        // Point LoS in direction char is facing.
        _lineOfSight.TransformScale(scale =>
            scale.MapX(x =>
                x.WithSign(
                    _baseLineOfSightDirection != _direction
                        ? _baseLineOfSightXScaleSign.Opposite()
                        : _baseLineOfSightXScaleSign)));
        
        // Move in direction char is facing.
        Velocity = WalkSpeed * _direction.UnitVector();
        MoveAndSlide();
    }

    // TODO: reuse with Player's
    public void ChangeAnimation(string animation) {
        if (_sprite.Animation != animation)
            _sprite.Play(animation);
    }
}

