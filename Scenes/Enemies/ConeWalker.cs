using Godot;
using System;

public partial class ConeWalker : CharacterBody2D, IHittable
{
    [Export] public int StartHitPoints { get; set; } = 3;
    [Export] float KnockbackMagnitude { get; set; } = 200f;

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

        _hitManager = new HitManager(this, StartHitPoints, _sprite);
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
        bool collided = MoveAndSlide();
        if (collided)
        {
            EnemyUtils.HitCollideeIfApplicable(this, GetLastSlideCollision(), KnockbackMagnitude);
        }
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

    public void QueueDeath()
    {
        QueueFree();
    }

    public bool DeathQueued() => IsQueuedForDeletion();
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
    private const float KNOCKBACK_BOUNCE_MAGNITUDE = 30f;

    private bool _invulnerabilityOverride;
    private IHittable _hitee;
    private CanvasItem _bodySprite; 
    public int HitPoints { get; set; }

    private double _postHitInvulnerabilityTimeElapsed;
    private double _flickerTimeElapsed;

    public Vector2 KnockbackVelocity { get; private set; } = Vector2.Zero;

    public HitManager(IHittable hitee, int hitPoints, CanvasItem bodySprite)
    {
        _hitee = hitee;
        HitPoints = hitPoints;
        _postHitInvulnerabilityTimeElapsed = INVULNERABILITY_TIME;
        _flickerTimeElapsed = 0;
        _invulnerabilityOverride = false;
        _bodySprite = bodySprite;
    }

    public void _PhysicsProcess(double delta)
    {
        if (Dead())
        {
            return;
        }
        if (Invulnerable)
        {
            _flickerTimeElapsed += delta;
            if (_flickerTimeElapsed > FLICKER_TIME)
            {
                _bodySprite.ChangeModulate(m => m.MapA(a =>
                            a == 1 ? 0.5f : 1));
                _flickerTimeElapsed = 0;
            }
        }
        if (PostHitInvulnerable())
        {
            _postHitInvulnerabilityTimeElapsed += delta;
            if (!PostHitInvulnerable())
            {
                _hitee.Unstun();
            }
        }
        if (_bodySprite.Modulate.A != 1 && !Invulnerable)
            _bodySprite.ChangeModulate(m => m.WithA(1));

        KnockbackVelocity = KnockbackVelocity.MoveToward(
                Vector2.Zero,
                (float) delta * KNOCKBACK_DRAG);
    }


    public void TakeHit(Vector2 _knockbackVelocity)
    {
        if (Dead() || Invulnerable)
        {
            return;
        }

        // adjust knockback to include upward vertical if on floor
        if (_hitee.MustKnockOffFloorToCreateDistance())
        {
            _knockbackVelocity.Y = -KNOCKBACK_BOUNCE_MAGNITUDE;
        }

        // apply knockback
        KnockbackVelocity += _knockbackVelocity;

        // start invulnerability
        _postHitInvulnerabilityTimeElapsed = 0;
        _flickerTimeElapsed = 0;

        // stun
        _hitee.Stun();

        // apply damage
        HitPoints -= 1;
        if (Dead() && !_hitee.DeathQueued())
        {
            _hitee.QueueDeath();
        }
    }

    public void TakeDamage(int amount = 1)
    {
        HitPoints -= amount;
    }

    public bool Invulnerable {
        private get => _invulnerabilityOverride || PostHitInvulnerable();
        set => _invulnerabilityOverride = value;
    }
    private bool PostHitInvulnerable() => _postHitInvulnerabilityTimeElapsed < INVULNERABILITY_TIME;
    private bool Dead() => HitPoints <= 0;
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
