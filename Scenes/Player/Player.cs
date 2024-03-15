using Godot;
using System;
using System.Diagnostics.CodeAnalysis;
using Godot.Collections;
using MVM23;

// Credits:
// Bruno Guedes - https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558

[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
[GlobalClass]
public partial class Player : CharacterBody2D, IHittable {
    #region Properties

    public bool CanSuperJump { get; set; }
    public Vector2 GrappledPoint { get; set; }
    public Node2D Reticle { get; private set; }
    public float Gravity { get; private set; }
    public float JumpSpeed { get; private set; }
    public float ApexGravity { get; private set; }

    public double SuperJumpCurrentBufferTime { get; set; }
    public double CoyoteTimeElapsed { get; set; }
    public bool CoyoteTimeExpired { get; set; }
    public int MaxHealth { get; private set; } = 15;

    public float CurrentHealth {
        get => _hitManager.HitPoints;
    }

    public Vector2 KnockbackVelocity {
        get => _hitManager.KnockbackVelocity;
    }
    
    [Export] public int MaxDashes { get; set; }
    [Export] public int DashesAvailable { get; set; }

    #endregion

    #region Fields

    private WorldStateManager _worldStateManager;
    private RayCast2D _grappleCheck;
    private PlayerState _currentState;
    private Sword _sword;
    private bool _canThrowGrapple;
    private double _timeSinceStartHoldingJump;
    private double _timeSinceLeftGround;
    private double _timeSinceMelee;
    private AnimatedSprite2D _sprite;
    private bool _isFacingLeft;
    private RayCast2D _posBonkCheck;
    private RayCast2D _posBonkBuffer;
    private RayCast2D _negBonkCheck;
    private RayCast2D _negBonkBuffer;
    private CpuParticles2D _dashParticles;
    private bool _reticleFrozen;
    private Vector2 _reticleFreezePos;

    private PackedScene _grappleScene;
    private PackedScene _swordScene;

    private HitManager _hitManager;

    #endregion

    #region Constants

    public const float RunSpeed = 150.0f;
    public const float GroundFriction = RunSpeed * 20f;
    public const float AirFriction = GroundFriction * 0.8f;
    private const float KnockbackOnHittingEnemy = 100f;

    #endregion

    #region Exports

    [Export] public double EarlyJumpMaxBufferTime = 0.1;
    [Export] public double SuperJumpInitBufferLimit = 0.1; // Waits to start charging to give time to boost jump

    [Export] private float _jumpHeight = 70F;  // I believe this is pixels
    [Export] private float _timeInAir = 0.17F; // No idea what this unit is. Definitely NOT seconds
    [Export] public float MeleeDuration { get; set; } = 0.2F;

    // ReSharper disable once RedundantNameQualifier
    [Export] public Godot.Collections.Dictionary<string, bool> Abilities = new()
    {
        { "Stick", false },
        { "Dash", false },
        { "SuperJump", false },
        { "Grapple", false },
        { "DoubleDash", false },
        { "DashOnKill", false },
        { "KeyToWorldTwo", false },
        { "WorldThreeKeyOne", false },
        { "WorldThreeKeyTwo", false }
    };

    #endregion

    #region Classes/Enums

    public class InputInfo {
        public Vector2 InputDirection { get; init; }
        public bool IsPushingJump { get; init; }
        public bool IsPushingCrouch { get; init; }
        public bool IsPushingDash { get; init; }
        public bool IsPushingGrapple { get; init; }
        public bool IsPushingMelee { get; init; }
    }

    public enum JumpType {
        None,
        Normal,
        CoyoteTime,
        BoostJump
    }

    #endregion

    #region Override Methods

    public override void _Ready() {
        _worldStateManager = GetNode<WorldStateManager>("/root/Game/WSM");

        Gravity = (float)(_jumpHeight / (2 * Math.Pow(_timeInAir, 2)));
        ApexGravity = Gravity / 2;
        JumpSpeed = (float)Math.Sqrt(2 * _jumpHeight * Gravity);

        // Set project gravity so it syncs to other nodes
        ProjectSettings.SetSetting("physics/2d/default_gravity", Gravity);

        _grappleCheck = GetNode<RayCast2D>("Reticle/GrappleCheck");

        _reticleFrozen = false;
        _canThrowGrapple = true;
        _reticleFreezePos = Vector2.Zero;

        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        _posBonkCheck = GetNode<RayCast2D>("Sprite/PosBonkCheck");
        _posBonkBuffer = GetNode<RayCast2D>("Sprite/PosBonkBuffer");
        _negBonkCheck = GetNode<RayCast2D>("Sprite/NegBonkCheck");
        _negBonkBuffer = GetNode<RayCast2D>("Sprite/NegBonkBuffer");
        _dashParticles = GetNode<CpuParticles2D>("DashParticles");
        Reticle = GetNode<Node2D>("Reticle");

        _grappleScene = ResourceLoader.Load<PackedScene>("res://Scenes/Abilities/grapple_hook/grapple_hook.tscn");
        _swordScene = ResourceLoader.Load<PackedScene>("res://Scenes/Abilities/sword/Sword.tscn");

        _hitManager = new HitManager(this, MaxHealth, _sprite);
        _currentState = new IdleState(this);
    }

    public override void _Process(double delta) {
        if (_currentState.Name != "Grapple") {
            var mousePosition = GetGlobalMousePosition();
            Reticle.LookAt(mousePosition);
            Reticle.Position = Vector2.Zero;
        }

        if (_currentState.GetType() != typeof(DashState))
            SetEmittingDashParticles(false);

        if (GrappledPoint != Vector2.Inf)
            QueueRedraw();
    }

    public override void _PhysicsProcess(double delta) {
        var inputs = GetInputs();

        _hitManager._PhysicsProcess(delta);

        if (!inputs.IsPushingJump)
            _timeSinceStartHoldingJump = 0;
        else
            _timeSinceStartHoldingJump += delta;

        if (!inputs.IsPushingGrapple) {
            OnGrappleFree();
        }

        if (IsOnFloor()) {
            _timeSinceLeftGround = 0;
            DashesAvailable = MaxDashes;
        }
        else
            _timeSinceLeftGround += delta;

        if (_sword is null && inputs.IsPushingMelee && Abilities["Stick"]) {
            GD.Print("creating sword");
            _sword = _swordScene.Instantiate<Sword>();
            AddChild(_sword);
            _sword.Rotation = GetAngleToMouse().NearestDirection8().Radians();
        }

        if (_sword is not null && _sword.Lifetime >= MeleeDuration) {
            GD.Print("clearing sword");
            _sword.QueueFree();
            _sword = null;
        }

        var newState = _currentState.HandleInput(this, inputs, delta);

        if (newState != null) {
            ChangeState(newState);
        }

        if (inputs.IsPushingGrapple && _canThrowGrapple) {
            ThrowGrapple();
        }

        bool collided = MoveAndSlide();
        if (collided && GetLastSlideCollision()
                             .GetCollider() is Node2D
                         colliderNode // TODO: go through all collisions. Might collide with enemy, then floor, which would ignore enemy?
                     && colliderNode.IsInGroup("hurt_player_on_collide")) {
            if (!"Dash".Equals(_currentState
                    .Name)) // TODO: smells like type-checking. use a method on PlayerState for that sweet polymorphism?
            {
                var knockback = Vector2s.FromPolar(KnockbackOnHittingEnemy, colliderNode.GetAngleToNode(this));
                TakeHit(knockback);
            }
        }
    }

    public override void _Draw() {
        if (GrappledPoint == Vector2.Inf) return;

        var from = Vector2.Zero;
        var to = GrappledPoint - GlobalPosition;
        var color = Colors.Aqua;
        DrawLine(from, to, color, 2f);
    }

    #endregion

    #region Public Methods

    public void UnlockAbility(string unlock) {
        Abilities[unlock] = true;
        if (unlock is "Dash" or "DoubleDash")
            MaxDashes += 1;
        
        _worldStateManager.Save();
    }

    public bool CanDash() {
        if (!Abilities["Dash"]) return false;
        return DashesAvailable > 0;
    }

    public JumpType CanJump() {
        if (!IsOnFloor() && !CoyoteTimeExpired && _timeSinceStartHoldingJump < _timeSinceLeftGround)
            return JumpType.CoyoteTime;

        if (IsOnFloor() && _timeSinceStartHoldingJump < EarlyJumpMaxBufferTime)
            return _currentState.GetType() == typeof(DashState) ? JumpType.BoostJump : JumpType.Normal;
        return JumpType.None;
    }

    public bool CanStartCharge(InputInfo inputs) {
        if (!Abilities["SuperJump"]) return false;

        return IsOnFloor() && inputs.IsPushingCrouch && SuperJumpCurrentBufferTime >= SuperJumpInitBufferLimit;
    }

    public void ResetJumpBuffers() {
        CoyoteTimeExpired = false;
        CoyoteTimeElapsed = 0;
        SuperJumpCurrentBufferTime = 0;
    }

    public void NudgePlayer(int nudgeAmount, Vector2 nudgeEnterVelocity) {
        var playerPos = GlobalPosition;
        Velocity = nudgeEnterVelocity;
        GlobalPosition = new Vector2(playerPos.X + nudgeAmount, playerPos.Y);
    }


    public void ChangeAnimation(string animation) {
        if (_sprite.Animation != animation)
            _sprite.Play(animation);
    }

    public void QueueDeath() {
        // TODO: load saved game                
        GetTree().Paused = true;
    }

    public void SetEmittingDashParticles(bool emit) => _dashParticles.Emitting = emit;

    public void TakeDamage(int amount = 1) => _hitManager.TakeDamage(amount);

    public void TakeHit(Vector2 initialKnockbackVelocity) => _hitManager.TakeHit(initialKnockbackVelocity);

    public bool DeathQueued() => false; // Necessary to implement HitManager

    public void RestoreHitPoints() => _hitManager.HitPoints = MaxHealth;

    public bool MustKnockOffFloorToCreateDistance() => IsOnFloor();

    public void FaceLeft() => SetFaceDirection(true);

    public void FaceRight() => SetFaceDirection(false);

    public bool ShouldNudgePositive() => _negBonkCheck.IsColliding() && !_negBonkBuffer.IsColliding();

    public bool ShouldNudgeNegative() => _posBonkCheck.IsColliding() && !_posBonkBuffer.IsColliding();
    
    public void ChangeColor(Color color) => _sprite.Modulate = color;

    #endregion

    #region Private Methods

    private void ChangeState(PlayerState newState) {
        GD.Print($"Changing from {_currentState.Name} to {newState.Name}");
        _currentState = newState;
    }

    private static InputInfo GetInputs() {
        var inputInfo = new InputInfo
        {
            InputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down"),
            IsPushingJump = Input.IsActionPressed("jump"),
            IsPushingCrouch = Input.IsActionPressed("move_down"),
            IsPushingDash = Input.IsActionPressed("dash"),
            IsPushingGrapple = Input.IsActionPressed("grapple"),
            IsPushingMelee = Input.IsActionPressed("melee"),
        };

        return inputInfo;
    }

    private void SetFaceDirection(bool faceLeft) {
        _isFacingLeft = faceLeft;
        _sprite.FlipH = !_isFacingLeft;
    }

    private void ThrowGrapple() {
        if (!Abilities["Grapple"]) return;

        if (!_grappleCheck.IsColliding()) return;

        GrappledPoint = _grappleCheck.GetCollisionPoint();
        ChangeState(new GrappleState(this));
        _canThrowGrapple = false;
    }

    private void OnGrappleFree() {
        GrappledPoint = Vector2.Inf;
        QueueRedraw();
        _canThrowGrapple = true;
    }
    
    private Angle GetAngleToMouse() => Angle.FromRadians(GetAngleTo(GetGlobalMousePosition()));

    #endregion
}