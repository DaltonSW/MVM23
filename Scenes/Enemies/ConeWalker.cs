using Godot;
using System;

public partial class ConeWalker : CharacterBody2D, IHittable
{
    [Export] public int StartHitPoints { get; set; } = 3;

    private HitManager _hitManager;

    private AnimatedSprite2D _sprite;
    private XDirectionManager _xDirMan;

    private float _gravity;

    private IAi _ai;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var lineOfSight = GetNode<Area2D>("Shapes/LineOfSight");
        _sprite = GetNode<AnimatedSprite2D>("Shapes/AnimatedSprite2D");
        var dropAheadRayCast = GetNode<RayCast2D>("Shapes/DropAheadRayCast");
        var edgeAheadRayCast = GetNode<RayCast2D>("Shapes/EdgeAheadRayCast");
        _xDirMan = GetNode<XDirectionManager>("XDirectionManager");

        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

        _ai = new AvoidDrops(dropAheadRayCast,
                new NoticeTarget(lineOfSight,
                    new Patrol(edgeAheadRayCast),
                    target => new Chase(this, target)));

        _hitManager = new HitManager(this, this, StartHitPoints, _sprite);
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
        _hitManager._PhysicsProcess(delta);
        _ai._PhysicsProcess(delta);
        _xDirMan.Direction = _ai.NextXDirection(_xDirMan.Direction);
        Velocity = CalcVelocity(delta);
        MoveAndSlide();
    }

    private Vector2 CalcVelocity(double delta)
    {
        var velocity = Velocity;

        // fake friction
        velocity.X = 0;

        // knockback
        velocity += _hitManager.KnockbackVelocity;

        if (IsOnFloor() && !_stunned)
        {
            // foot
            velocity += (float)delta * _ai.FootSpeed() * _xDirMan.Direction.UnitVector();
        }

        // gravity
        velocity += (float)delta * _gravity * Vector2.Down;

        return velocity;
    }

    // TODO: reuse with Player's
    private void ChangeAnimation(string animation) {
        if (_sprite.Animation != animation)
            _sprite.Play(animation);
    }

    public void TakeHit(Vector2 initialKnockbackVelocity)
    {
        _hitManager.TakeHit(initialKnockbackVelocity);
    }

    private bool _stunned = false;

    public void Stun()
    {
        _stunned = true;
    }

    public void Unstun()
    {
        _stunned = false;
    }
}

/// Moves quickly, always switching directions toward a given target.
public class Chase : IAi
{
    private Node2D _self;
    private Node2D _target;

    public Chase(Node2D self, Node2D target)
    {
        _self = self;
        _target = target;
    }
    
    public XDirection NextXDirection(XDirection current) =>
        _self.XDirectionTo(_target);

    public float FootSpeed() => 6000;
}


/// Manages hit-related logic for a character,
/// including hit points and knockback.
public class HitManager
{
    private const float KNOCKBACK_DRAG = 500f;
    private const double INVULNERABILITY_TIME = 0.5f;
    private const double FLICKER_TIME = 0.15f;

    private Node2D _self;
    private IHittable _stunee;
    private CanvasItem _bodySprite; 
    private int _hitPoints;

    private double _invulnerabilityTimeElapsed;
    private double _flickerTimeElapsed;

    public Vector2 KnockbackVelocity { get; private set; } = Vector2.Zero;

    public HitManager(Node2D self, IHittable stunee, int hitPoints, CanvasItem bodySprite)
    {
        _self = self;
        _stunee = stunee;
        _hitPoints = hitPoints;
        _invulnerabilityTimeElapsed = INVULNERABILITY_TIME;
        _flickerTimeElapsed = 0;
        _bodySprite = bodySprite;
    }

    public void _PhysicsProcess(double delta)
    {
        if (Dead())
        {
            return;
        }
        if (Invulnerable())
        {
            _invulnerabilityTimeElapsed += delta;
            _flickerTimeElapsed += delta;
            if (_flickerTimeElapsed > FLICKER_TIME)
            {
                _bodySprite.ChangeModulate(m => m.MapA(a =>
                            a == 1 ? 0.5f : 1));
                _flickerTimeElapsed = 0;
            }
            if (!Invulnerable())
            {
                _stunee.Unstun();
                _bodySprite.ChangeModulate(m => m.WithA(1));
            }
        }

        KnockbackVelocity = KnockbackVelocity.MoveToward(
                Vector2.Zero,
                (float) delta * KNOCKBACK_DRAG);
    }


    public void TakeHit(Vector2 _knockbackVelocity)
    {
        if (Dead() || Invulnerable())
        {
            return;
        }

        // apply knockback
        KnockbackVelocity += _knockbackVelocity;

        // start invulnerability
        _invulnerabilityTimeElapsed = 0;
        _flickerTimeElapsed = 0;

        // stun
        _stunee.Stun();

        // apply damage
        _hitPoints -= 1;
        if (Dead() && !_self.IsQueuedForDeletion())
        {
            _self.QueueFree();
        }
    }

    private bool Invulnerable() => _invulnerabilityTimeElapsed < INVULNERABILITY_TIME;
    private bool Dead() => _hitPoints <= 0;
}

public static class CanvasItemExtensions
{
    public static void ChangeModulate(this CanvasItem c, Func<Color, Color> f)
    {
        c.Modulate = f(c.Modulate);
    }
}

public static class ColorExtensions
{
    public static Color MapA(this Color c, Func<float, float> f) =>
        c.WithA(f(c.A));

    public static Color WithA(this Color c, float a)
    {
        c.A = a;
        return c;
    }
}
