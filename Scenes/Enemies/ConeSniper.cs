using Godot;
using System;

public partial class ConeSniper : CharacterBody2D, IHittable
{
    [Export] public int StartHitPoints { get; set; } = 2;

    private AnimatedSprite2D _sprite;
    private XDirectionManager _xDirMan;

    private float _gravity;

    private IAi _ai;

    private HitManager _hitManager;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var lineOfSight = GetNode<Area2D>("Shapes/LineOfSight");
        _sprite = GetNode<AnimatedSprite2D>("Shapes/AnimatedSprite2D");
        var dropAheadRayCast = GetNode<RayCast2D>("Shapes/DropAheadRayCast");
        var edgeAheadRayCast = GetNode<RayCast2D>("Shapes/EdgeAheadRayCast");
        var gun = GetNode<Sprite2D>("Gun");
        _xDirMan = GetNode<XDirectionManager>("XDirectionManager");

        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

        _ai = new AvoidDrops(dropAheadRayCast,
                new NoticeTarget(lineOfSight,
                    new Patrol(edgeAheadRayCast),
                    target => new FireAtWill(gun, this, target, _xDirMan)));

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


public class FireAtWill : IAi
{
    private const float READY_THRESHOLD = (float) Math.PI / 128;
    private const float READY_SPEED = 2f;
    private const double AIM_DURATION = 0.5f;
    private const double FIRE_DURATION = 0.1f;

    private Node2D _gun;
    private Node2D _self;
    private Node2D _target;
    private XDirectionManager _xDirMan;

    private Angle _aim;
    private double _aimTimeElapsed;
    private double _fireTimeElapsed;

    private ShootingState _state;

    public FireAtWill(Node2D gun, Node2D self, Node2D target, XDirectionManager xDirMan)
    {
        _gun = gun;
        _self = self;
        _target = target;
        _xDirMan = xDirMan;
        _aim = Angle.FromRadians(gun.Rotation);
        _aimTimeElapsed = 0;
    }

    public void _PhysicsProcess(double delta)
    {
        var nextState = _state;
        switch (_state)
        {
            case ShootingState.READYING:
                var targetAngle = _self.GetAngleObjectTo(_target.GlobalPosition);
                _aim = _aim.Lerp(targetAngle, (float)(delta * READY_SPEED));
                _gun.Rotation = _xDirMan.SpriteRotationFor(_aim);

                var aimError = Math.Abs(_aim.SmallestAngleTo(targetAngle).Radians);
                if (aimError <= READY_THRESHOLD)
                {            
                    nextState = ShootingState.AIMING;
                    _aimTimeElapsed = 0;
                }
                break;

            case ShootingState.AIMING:
                _aimTimeElapsed += delta;
                if (_aimTimeElapsed >= AIM_DURATION)
                {
                    nextState = ShootingState.FIRING;
                    _fireTimeElapsed = 0;
                }
                break;

            case ShootingState.FIRING:
                _fireTimeElapsed += delta;
                _gun.Scale = _gun.Scale.MapX(x => x*5);
                if (_fireTimeElapsed >= AIM_DURATION)
                {
                    nextState = ShootingState.READYING;
                    _gun.Scale = _gun.Scale.WithX(1);
                }
                break;
        }
        _state = nextState;
    }

    public XDirection NextXDirection(XDirection current) =>
        _self.XDirectionTo(_target);
    public float FootSpeed() => 0;
}

public enum ShootingState
{
    READYING,
    AIMING,
    FIRING,
}
