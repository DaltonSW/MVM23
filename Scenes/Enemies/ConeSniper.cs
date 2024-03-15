using Godot;
using System;
using System.Linq;

public partial class ConeSniper : CharacterBody2D, IHittable
{
    [Export] public int StartHitPoints { get; set; } = 2;
    [Export] float KnockbackMagnitude { get; set; } = 100f;

    private AnimatedSprite2D _sprite;
    private Polygon2D _laserSight;
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
        var beamArea = GetNode<Area2D>("BeamArea");
        var beam = GetNode<Polygon2D>("Beam");
        var laserSight = GetNode<Polygon2D>("LaserSight");
        _xDirMan = GetNode<XDirectionManager>("XDirectionManager");

        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

        _ai = new AvoidDrops(dropAheadRayCast,
                new NoticeTarget(lineOfSight,
                    new Patrol(edgeAheadRayCast),
                    target => new FireAtWill(gun, laserSight, beam, beamArea, this, target, _xDirMan)));

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


public class FireAtWill : IAi
{
    private const float KNOCKBACK_MAGNITUDE = 100f;
    private const float READY_THRESHOLD = (float) Math.PI / 128;
    private const float READY_SPEED = 3f;
    private const double AIM_DURATION = 0.5f;
    private const double FIRE_DURATION = 0.1f;

    private Node2D _gun;
    private Node2D _aimingIndicator;
    private Node2D _damageIndicator;
    private Area2D _hitbox;

    private Node2D _self;
    private Node2D _target;
    private XDirectionManager _xDirMan;

    private Angle _aim;
    private double _aimTimeElapsed;
    private double _fireTimeElapsed;

    private ShootingState _state;

    public FireAtWill(
            Node2D gun,
            Node2D aimingIndicator,
            Node2D damageIndicator,
            Area2D damageZone,
            Node2D self,
            Node2D target,
            XDirectionManager xDirMan)
    {
        _gun = gun;
        _aimingIndicator = aimingIndicator;
        _damageIndicator = damageIndicator;
        _hitbox = damageZone;
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
                var targetAngle = _self.GetAngleObjectToNode(_target);
                _aim = _aim.Lerp(targetAngle, (float)(delta * READY_SPEED));

                GD.Print("aim: " + _aim.Radians);
                GD.Print(_xDirMan.SpriteRotationFor(_aim));
                // TODO: combine into one gun node
                var gunRotation = _xDirMan.SpriteRotationFor(_aim); 
                _gun.Rotation = gunRotation;
                _aimingIndicator.Rotation = gunRotation;
                _damageIndicator.Rotation = gunRotation;
                _hitbox.Rotation = gunRotation;

                _aimingIndicator.Visible = true;

                var aimError = Math.Abs(_aim.SmallestAngleTo(targetAngle).Radians);
                if (aimError <= READY_THRESHOLD)
                {            
                    nextState = ShootingState.AIMING;
                    _aimingIndicator.Visible = false;
                    _aimTimeElapsed = 0;
                }
                break;

            case ShootingState.AIMING:
                _aimTimeElapsed += delta;
                _aimingIndicator.Visible = true;
                if (_aimTimeElapsed >= AIM_DURATION)
                {
                    nextState = ShootingState.FIRING;
                    _aimingIndicator.Visible = false;
                    _fireTimeElapsed = 0;
                }
                break;

            case ShootingState.FIRING:
                _fireTimeElapsed += delta;
                _damageIndicator.Visible = true;
                
                var hittables =
                    from body in _hitbox.GetOverlappingBodies()
                    where body.IsInGroup("enemy_hitboxes_hurt")
                    select body;
                foreach (var hittable in hittables)
                {
                    ((IHittable) hittable).TakeHit(Vector2s.FromPolar(KNOCKBACK_MAGNITUDE, _gun.GetAngleToNode(hittable)));
                }
                if (_fireTimeElapsed >= AIM_DURATION)
                {
                    nextState = ShootingState.READYING;
                    _damageIndicator.Visible = false;
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
