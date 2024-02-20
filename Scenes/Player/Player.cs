using Godot;
using System;
using MVM23.Scripts.AuxiliaryScripts;

// Credits:
// Bruno Guedes - https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558

public partial class Player : CharacterBody2D {
    [Export] public const float RunSpeed = 300.0f;

    [Export] private const float JumpHeight = 50F; // I believe this is pixels
    [Export] private const float TimeInAir = 0.2F; // No idea what this unit is. Definitely NOT seconds
    public float Gravity;
    public float JumpSpeed;

    private AnimatedSprite2D _sprite;
    private CpuParticles2D _dashParticles;
    private Node2D _reticle;

    private IPlayerState _currentState;
    private bool _reticleFrozen; // TODO: control with _currentState method
    private Vector2 _reticleFreezePos;

    public class InputInfo {
        public Vector2 InputDirection { get; init; }
        public bool IsPushingJump { get; init; }
        public bool IsPushingCrouch { get; init; }
        public bool IsPushingDash { get; init; }
    }

    public TestDashMode DashMode = TestDashMode.NoExtraMomentum;

    public enum TestDashMode {
        NoExtraMomentum,
        MoreThanMoveMaxMomentum,
        MoveMaxMomentum
    }

    public override void _Ready() {
        Gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
        JumpSpeed = (float)Math.Sqrt(2 * JumpHeight * Gravity);

        // Set project gravity so it syncs to other nodes
        ProjectSettings.SetSetting("physics/2d/default_gravity", Gravity);

        _currentState = IsOnFloor() ? new IdleState() : new JumpState();
        _reticleFrozen = false;
        _reticleFreezePos = Vector2.Zero;

        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        _dashParticles = GetNode<CpuParticles2D>("DashParticles");
        _reticle = GetNode<Node2D>("Reticle");
    }

    public override void _Process(double delta) {

        if (Input.IsKeyPressed(Key.Key1)) {
            DashMode = TestDashMode.NoExtraMomentum;
        }
        else if (Input.IsKeyPressed(Key.Key2)) {
            DashMode = TestDashMode.MoreThanMoveMaxMomentum;
        }
        else if (Input.IsKeyPressed(Key.Key3)) {
            DashMode = TestDashMode.MoveMaxMomentum;
        }

        if (_reticleFrozen) {
            _reticle.GlobalPosition = _reticleFreezePos;
        }
        else {
            var mousePosition = GetViewport().GetMousePosition();
            _reticle.LookAt(mousePosition);
            _reticle.Position = Vector2.Zero;
        }
    }

    public override void _PhysicsProcess(double delta) {
        var inputs = GetInputs();

        var newState = _currentState.HandleInput(this, inputs, delta);
        if (newState != null) {
            ChangeState(newState);
        }
        MoveAndSlide();
    }

    private void ChangeState(IPlayerState newState) {
        GD.Print($"Changing from {_currentState.Name} to {newState.Name}");
        _currentState = newState;

        // TODO: Implement a "push down automaton"(?) pattern
        //  Basically just a stack that stores the previous states
        //  If you can "shoot" from idle or running or jumping, it shouldn't need to keep track of specific prev state
        //  It should be able to return something like PlayerState.Previous to go back to whatever the last one was
    }

    private static InputInfo GetInputs() {
        var inputInfo = new InputInfo
        {
            InputDirection = Input.GetVector("move_left", "move_right", "ui_up", "ui_down"),
            IsPushingJump = Input.IsActionJustPressed("jump"),
            IsPushingCrouch = Input.IsActionJustPressed("crouch"),
            IsPushingDash = Input.IsActionJustPressed("dash"),
        };

        return inputInfo;
    }

    public void ChangeAnimation(string animation) {
        _sprite.FlipH = Velocity.X >= 0;

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
}
