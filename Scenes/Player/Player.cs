using Godot;
using System;
using System.Diagnostics.CodeAnalysis;
using MVM23.Scripts.AuxiliaryScripts;

// Credits:
// Bruno Guedes - https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558

[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
[GlobalClass]
public partial class Player : CharacterBody2D {
    [Export] public float RunSpeed = 150.0f;

    [ExportGroup("Jump Properties")]
    [Export] public float ApexGravityVelRange = 5F;
    // Both of the below are in seconds
    [Export] public double CoyoteTimeBuffer = 0.1;
    [Export] public double EarlyJumpInputBuffer = 0.2;
    [Export] public float MaxVerticalVelocity = 150.0f;
    [Export] public float SuperJumpVelocity = 750f;

    [Export] public double SuperJumpMinChargeTime = 1.00;
    [Export] public double SuperJumpInitBufferLimit = 0.75;
    public double SuperJumpCurrentBufferTime;
    public double SuperJumpCurrentChargeTime;
    public bool CanSuperJump { get; set; }
    
    public double CoyoteTimeElapsed;
    public bool CoyoteTimeExpired;
    //public double EarlyJumpInputCounter;
    //public bool EarlyJumpTimeExpired;
    
    [ExportSubgroup("Constant Setters")]
    [Export] private float _jumpHeight = 70F; // I believe this is pixels
    [Export] private float _timeInAir = 0.17F; // No idea what this unit is. Definitely NOT seconds
    public float Gravity;
    public float JumpSpeed;
    public float ApexGravity;

    [ExportGroup("Dash Properties")]
    [Export] public float DashDuration = 0.08f;
    [Export] public double DashSpeed = 750.0f;
    public double DashTimeElapsed;
    public Vector2 DashStoredVelocity;
    public Vector2 DashCurrentAngle;

    private bool IsDashing { get; set; }
    private bool PlayerCanDash { get; set; }
    public bool IsFacingLeft { get; set; }

    private AnimatedSprite2D _sprite;
    public RayCast2D BonkCheck;
    public RayCast2D BonkBuffer;
    private CpuParticles2D _dashParticles;
    private Node2D _reticle;

    public IPlayerState CurrentState { get; private set; }
    private bool _reticleFrozen; // TODO: control with _currentState method
    private Vector2 _reticleFreezePos;

    public class InputInfo {
        public Vector2 InputDirection { get; init; }
        public bool IsPushingJump { get; init; }
        public bool IsPushingCrouch { get; init; }
        public bool IsPushingDash { get; init; }
        public bool IsPushingGrapple { get; init; }
    }

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
        BonkCheck = GetNode<RayCast2D>("BonkCheck");
        BonkBuffer = GetNode<RayCast2D>("BonkBuffer");
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
        if (SuperJumpCurrentBufferTime < SuperJumpInitBufferLimit)
            SuperJumpCurrentBufferTime += delta;
        
        var inputs = GetInputs();

        var newState = CurrentState.HandleInput(this, inputs, delta);
        if (newState != null) {
            ChangeState(newState);
        }

        // if (inputs.IsPushingGrapple) {
        //     var grappleHook = _grappleScene.Instantiate<GrappleHook>();
        //     grappleHook.Position = GlobalPosition;
        //     grappleHook.Rotation = GetAngleToMouse();
        //     GetParent().AddChild(grappleHook);
        // }
        
        MoveAndSlide();
    }

    private void ChangeState(IPlayerState newState) {
        GD.Print($"Changing from {CurrentState.Name} to {newState.Name}");
        CurrentState = newState;

        // TODO: Implement a "push down automaton"(?) pattern
        //  Basically just a stack that stores the previous states
        //  If you can "shoot" from idle or running or jumping, it shouldn't need to keep track of specific prev state
        //  It should be able to return something like PlayerState.Previous to go back to whatever the last one was
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
        SuperJump
    }


    public bool CanDash() {
        return PlayerCanDash;
    }
    
    public JumpType CanJump() {
        if (!IsOnFloor() && !CoyoteTimeExpired)
            return JumpType.CoyoteTime;
        if (IsOnFloor())
            return JumpType.Normal;
        return JumpType.None;
    }

    public bool CanStartCharge(InputInfo inputs) {
        return IsOnFloor() && inputs.IsPushingCrouch && SuperJumpCurrentBufferTime < SuperJumpInitBufferLimit;
    }

    public void ResetJumpBuffers() {
        CoyoteTimeExpired = false;
        CoyoteTimeElapsed = 0;
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

    public float GetAngleToMouse() => GetAngleTo(GetViewport().GetMousePosition());

    public void FreezeReticle() {
        _reticleFrozen = true;
        _reticleFreezePos = _reticle.GlobalPosition;
    }

    public void RestoreReticle() {
        _reticleFrozen = false;
    }

    public void FaceLeft() => SetFaceDirection(true);
    
    public void FaceRight() => SetFaceDirection(false);

    private void SetFaceDirection(bool faceLeft) {
        IsFacingLeft = faceLeft;
        _sprite.FlipH = !IsFacingLeft;
        var adjustment = IsFacingLeft ? 1 : -1;
        BonkBuffer.Position = new Vector2(Math.Abs(BonkBuffer.Position.X) * adjustment, BonkBuffer.Position.Y);
        BonkCheck.Position = new Vector2(Math.Abs(BonkCheck.Position.X) * adjustment, BonkCheck.Position.Y);
    }

    // public void OnGrappleStruck() {
    //     return;
    // }
}
