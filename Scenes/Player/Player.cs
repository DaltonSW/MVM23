using Godot;
using System;
using System.Diagnostics.CodeAnalysis;
using MVM23;

// Credits:
// Bruno Guedes - https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558

[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
[GlobalClass]
public partial class Player : CharacterBody2D {
    [Export] public float RunSpeed = 150.0f;
    [Export] public double EarlyJumpMaxBufferTime = 0.1;
    [Export] public double SuperJumpInitBufferLimit = 0.2; // Waits to start charging to give time to boost jump

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

    public bool CanSuperJump { get; set; }
    private bool IsDashing { get; set; }
    public bool PlayerCanDash { get; set; }
    private IPlayerState CurrentState { get; set; }

    private AnimatedSprite2D _sprite;
    public bool IsFacingLeft { get; private set; }
    public RayCast2D PosBonkCheck;
    public RayCast2D PosBonkBuffer;
    public RayCast2D NegBonkCheck;
    public RayCast2D NegBonkBuffer;
    private CpuParticles2D _dashParticles;
    private Node2D _reticle;
    private bool _reticleFrozen;
    private Vector2 _reticleFreezePos;

    private PackedScene _grappleScene;


    public override void _Ready() {
        Gravity = (float)(_jumpHeight / (2 * Math.Pow(_timeInAir, 2)));
        ApexGravity = Gravity / 2;
        JumpSpeed = (float)Math.Sqrt(2 * _jumpHeight * Gravity);

        // Set project gravity so it syncs to other nodes
        ProjectSettings.SetSetting("physics/2d/default_gravity", Gravity);

        CurrentState = new IdleState();
        _reticleFrozen = false;
        PlayerCanDash = true;
        _reticleFreezePos = Vector2.Zero;

        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        PosBonkCheck = GetNode<RayCast2D>("Sprite/PosBonkCheck");
        PosBonkBuffer = GetNode<RayCast2D>("Sprite/PosBonkBuffer");
        NegBonkCheck = GetNode<RayCast2D>("Sprite/NegBonkCheck");
        NegBonkBuffer = GetNode<RayCast2D>("Sprite/NegBonkBuffer");
        _dashParticles = GetNode<CpuParticles2D>("DashParticles");
        _reticle = GetNode<Node2D>("Reticle");
        _reticle.Visible = false;

        _grappleScene = ResourceLoader.Load<PackedScene>("res://Scenes/Abilities/grapple_hook/grapple_hook.tscn");
    }

    public override void _Process(double delta) {
        if (_reticleFrozen) {
            _reticle.GlobalPosition = _reticleFreezePos;
        }
        else {
            var mousePosition = GetViewport().GetMousePosition();
            _reticle.LookAt(mousePosition);
            _reticle.Position = Vector2.Zero;
        }

        if (CurrentState.GetType() != typeof(DashState))
            SetEmittingDashParticles(false);
    }

    public override void _PhysicsProcess(double delta) {
        var inputs = GetInputs();

        // if (!inputs.IsPushingJump || (IsOnFloor() && inputs.InputDirection.X != 0)) ???
        if (!inputs.IsPushingJump)
            _timeSinceStartHoldingJump = 0;
        else // Jump is being pushed 
            _timeSinceStartHoldingJump += delta;

        if (IsOnFloor()) {
            _timeSinceLeftGround = 0;
            PlayerCanDash = true;
        }
        else
            _timeSinceLeftGround += delta;


        var newState = CurrentState.HandleInput(this, inputs, delta);
        if (newState != null) {
            ChangeState(newState);
        }

        if (inputs.IsPushingGrapple) {
            GD.Print("Grapple!");
            // var grappleHook = _grappleScene.Instantiate<GrappleHook>();
            // grappleHook.Position = GlobalPosition;
            // grappleHook.Rotation = GetAngleToMouse();
            // GetParent().AddChild(grappleHook);
        }

        MoveAndSlide();
    }

    private void ChangeState(IPlayerState newState) {
        GD.Print($"Changing from {CurrentState.Name} to {newState.Name}");
        CurrentState = newState;

        // TODO: Implement a "push down automaton"(?) pattern (and consider if it's even needed)
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
    }

    private static InputInfo GetInputs() {
        var inputInfo = new InputInfo
        {
            InputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down"),
            IsPushingJump = Input.IsActionPressed("jump"),
            IsPushingCrouch = Input.IsActionPressed("move_down"),
            IsPushingDash = Input.IsActionPressed("dash"),
            IsPushingGrapple = Input.IsActionPressed("grapple")
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

    private float GetAngleToMouse() => GetAngleTo(GetViewport().GetMousePosition());

    private void FreezeReticle() {
        _reticleFrozen = true;
        _reticleFreezePos = _reticle.GlobalPosition;
    }

    private void RestoreReticle() {
        _reticleFrozen = false;
    }

    public void FaceLeft() => SetFaceDirection(true);

    public void FaceRight() => SetFaceDirection(false);

    private void SetFaceDirection(bool faceLeft) {
        IsFacingLeft = faceLeft;
        _sprite.FlipH = !IsFacingLeft;
    }

    public bool ShouldNudgePositive() {
        return NegBonkCheck.IsColliding() && !NegBonkBuffer.IsColliding();
    }


    public bool ShouldNudgeNegative() {
        return PosBonkCheck.IsColliding() && !PosBonkBuffer.IsColliding();
    }


    public void NudgePlayer(int nudgeAmount, Vector2 nudgeEnterVelocity) {
        var playerPos = GlobalPosition;
        Velocity = nudgeEnterVelocity;
        GlobalPosition = new Vector2(playerPos.X + nudgeAmount, playerPos.Y);
    }

    // public void OnGrappleStruck() {
    //     return;
    // }
}