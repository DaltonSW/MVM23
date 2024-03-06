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
    private XDirectionManager _xDirMan;

    private float _gravity;

    /// The node this character is locked onto, if any.
    /// Is null if there is none.
    private Node2D _target;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _lineOfSight = GetNode<Area2D>("LineOfSight");
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _dropAheadRayCast = GetNode<RayCast2D>("DropAheadRayCast");
        _edgeAheadRayCast = GetNode<RayCast2D>("EdgeAheadRayCast");
        _xDirMan = GetNode<XDirectionManager>("XDirectionManager");

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
            _xDirMan.Direction = this.XDirectionTo(target);
        }
        if (EdgeAhead() && !Chasing())
        {
            _xDirMan.Direction = _xDirMan.Direction.Opposite();
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
            velocity += (float)delta * FootSpeed() * _xDirMan.Direction.UnitVector();
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

    public void _Hurt()
    {
        if (!IsQueuedForDeletion())
            QueueFree();
    }

}
