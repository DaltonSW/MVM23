using Godot;
using System;
using System.Diagnostics.CodeAnalysis;
using MVM23;

// Credits:
// Bruno Guedes - https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558

[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
[GlobalClass]
public partial class Player : CharacterBody2D {
    public const float RunSpeed = 150.0f;
    [Export] public double EarlyJumpMaxBufferTime = 0.1;
    [Export] public double SuperJumpInitBufferLimit = 0.1; // Waits to start charging to give time to boost jump

    [Export] private float _jumpHeight = 70F;  // I believe this is pixels
    [Export] private float _timeInAir = 0.17F; // No idea what this unit is. Definitely NOT seconds
    public float Gravity;
    public float JumpSpeed;
    public float ApexGravity;

    private double _timeSinceStartHoldingJump;
    private double _timeSinceLeftGround;
    public double SuperJumpCurrentBufferTime;
    public double CoyoteTimeElapsed;
    public bool CoyoteTimeExpired;

    public int MaxHealth = 15;
    public float CurrentHealth { get; set; }

    private double _timeSinceMelee;
    [Export] public float MeleeDuration { get; set; } = 0.2F;

    public bool CanSuperJump { get; set; }
    private bool IsDashing { get; set; }
    public bool PlayerCanDash { get; set; }
    private PlayerState CurrentState { get; set; }

    private bool CanThrowGrapple { get; set; }
    public RayCast2D GrappleCheck { get; set; }
    public Vector2 GrappledPoint { get; set; }

    public const float GroundFriction = RunSpeed * 20f;
    public const float AirFriction = GroundFriction * 0.8f;

    private Sword Sword { get; set; }

    private AnimatedSprite2D _sprite;
    public bool IsFacingLeft { get; private set; }
    private RayCast2D _posBonkCheck;
    private RayCast2D _posBonkBuffer;
    private RayCast2D _negBonkCheck;
    private RayCast2D _negBonkBuffer;
    private CpuParticles2D _dashParticles;
    public Node2D Reticle { get; set; }
    private bool _reticleFrozen;
    private Vector2 _reticleFreezePos;

    private PackedScene _grappleScene;
    private PackedScene _swordScene;


    public override void _Ready() {
        Gravity = (float)(_jumpHeight / (2 * Math.Pow(_timeInAir, 2)));
        ApexGravity = Gravity / 2;
        JumpSpeed = (float)Math.Sqrt(2 * _jumpHeight * Gravity);

        // Set project gravity so it syncs to other nodes
        ProjectSettings.SetSetting("physics/2d/default_gravity", Gravity);

        GrappleCheck = GetNode<RayCast2D>("Reticle/GrappleCheck");

        CurrentState = new IdleState();
        _reticleFrozen = false;
        PlayerCanDash = true;
        CanThrowGrapple = true;
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

        CurrentHealth = MaxHealth;
    }

    public override void _Process(double delta) {
        if (CurrentState.Name != "Grapple") {
            var mousePosition = GetGlobalMousePosition();
            Reticle.LookAt(mousePosition);
            Reticle.Position = Vector2.Zero;    
        }

        if (CurrentState.GetType() != typeof(DashState))
            SetEmittingDashParticles(false);

        if (GrappledPoint != Vector2.Inf)
            QueueRedraw();
    }

    public override void _PhysicsProcess(double delta) {
        var inputs = GetInputs();

        if (!inputs.IsPushingJump)
            _timeSinceStartHoldingJump = 0;
        else
            _timeSinceStartHoldingJump += delta;

        if (!inputs.IsPushingGrapple) {
            OnGrappleFree();
        }

        if (IsOnFloor()) {
            _timeSinceLeftGround = 0;
            PlayerCanDash = true;
        }
        else
            _timeSinceLeftGround += delta;

        if (Sword is null && inputs.IsPushingMelee) {
            GD.Print("creating sword");
            Sword = _swordScene.Instantiate<Sword>();
            AddChild(Sword);
            Sword.Rotation = GetAngleToMouse().NearestDirection8().Radians();
        }

        if (Sword is not null && Sword.Lifetime >= MeleeDuration) {
            GD.Print("clearing sword");
            Sword.QueueFree();
            Sword = null;
        }

        var newState = CurrentState.HandleInput(this, inputs, delta);

        if (newState != null) {
            ChangeState(newState);
        }

        if (inputs.IsPushingGrapple && CanThrowGrapple) {
            ThrowGrapple();
        }

        MoveAndSlide();
    }

    public override void _Draw() {
        if (GrappledPoint == Vector2.Inf) return;

        var from = Vector2.Zero;
        var to = GrappledPoint - GlobalPosition;
        var color = Colors.Aqua;
        DrawLine(from, to, color, 2f);
    }

    private void ChangeState(PlayerState newState) {
        GD.Print($"Changing from {CurrentState.Name} to {newState.Name}");
        CurrentState = newState;

        //  Consider if a "push down automaton"(?) pattern is useful
        //  Basically just a stack that stores the previous states
        //  If you can "shoot" from idle or running or jumping, it shouldn't need to keep track of specific prev state
        //  It should be able to return something like PlayerState.Previous to go back to whatever the last one was
    }

    public class InputInfo {
        public Vector2 InputDirection { get; init; }
        public bool IsPushingJump { get; init; }
        public bool IsPushingCrouch { get; init; }
        public bool IsPushingDash { get; init; }
        public bool IsPushingGrapple { get; init; }
        public bool IsPushingMelee { get; init; }
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

    public enum JumpType {
        None,
        Normal,
        CoyoteTime,
        BoostJump
    }

    // This function exists because I assume the logic is going to expand in the future
    // If it really is only this property, we can swap it out elsewhere maybe
    public bool CanDash() {
        return PlayerCanDash;
    }

    public JumpType CanJump() {
        if (!IsOnFloor() && !CoyoteTimeExpired && _timeSinceStartHoldingJump < _timeSinceLeftGround)
            return JumpType.CoyoteTime;

        if (IsOnFloor() && _timeSinceStartHoldingJump < EarlyJumpMaxBufferTime)
            return CurrentState.GetType() == typeof(DashState) ? JumpType.BoostJump : JumpType.Normal;
        return JumpType.None;
    }

    public bool CanStartCharge(InputInfo inputs) {
        return IsOnFloor() && inputs.IsPushingCrouch && SuperJumpCurrentBufferTime >= SuperJumpInitBufferLimit;
    }

    public void ResetJumpBuffers() {
        CoyoteTimeExpired = false;
        CoyoteTimeElapsed = 0;
        SuperJumpCurrentBufferTime = 0;
    }

    public void ChangeColor(Color color) {
        _sprite.Modulate = color;
    }

    public void ChangeAnimation(string animation) {
        if (_sprite.Animation != animation)
            _sprite.Play(animation);
    }

    public void SetEmittingDashParticles(bool emit) {
        _dashParticles.Emitting = emit;
    }

    private Angle GetAngleToMouse() => Angle.FromRadians(GetAngleTo(GetGlobalMousePosition()));

    public void FaceLeft() => SetFaceDirection(true);

    public void FaceRight() => SetFaceDirection(false);

    private void SetFaceDirection(bool faceLeft) {
        IsFacingLeft = faceLeft;
        _sprite.FlipH = !IsFacingLeft;
    }

    public bool ShouldNudgePositive() {
        return _negBonkCheck.IsColliding() && !_negBonkBuffer.IsColliding();
    }


    public bool ShouldNudgeNegative() {
        return _posBonkCheck.IsColliding() && !_posBonkBuffer.IsColliding();
    }


    public void NudgePlayer(int nudgeAmount, Vector2 nudgeEnterVelocity) {
        var playerPos = GlobalPosition;
        Velocity = nudgeEnterVelocity;
        GlobalPosition = new Vector2(playerPos.X + nudgeAmount, playerPos.Y);
    }

    private void ThrowGrapple() {
        if (!GrappleCheck.IsColliding()) return;

        GrappledPoint = GrappleCheck.GetCollisionPoint();
        ChangeState(new GrappleState(this));
        CanThrowGrapple = false;
    }

    public void OnGrappleFree() {
        GrappledPoint = Vector2.Inf;
        QueueRedraw();
        CanThrowGrapple = true;
    }
}