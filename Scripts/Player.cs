using Godot;
using System;
using MVM23.Scripts.AuxiliaryScripts;

// Credits:
// Bruno Guedes - https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558

public partial class Player : CharacterBody2D
{
    [Export] public const float RunSpeed = 300.0f;
    
    [Export] private const float JumpHeight = 50F; // I believe this is pixels
    [Export] private const float TimeInAir = 0.2F; // No idea what this unit is. Definitely NOT seconds
    public float Gravity;
    public float JumpSpeed;

    private PlayerState _currentState;

    public class InputInfo
    {
        public Vector2 InputDirection { get; set; }
        public bool IsPushingJump { get; set; }
        public bool IsPushingCrouch { get; set; }
    }

    public override void _Ready()
    {
        Gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
        JumpSpeed = (float)Math.Sqrt(2 * JumpHeight * Gravity);
        
        // Set project gravity so it syncs to other nodes
        ProjectSettings.SetSetting("physics/2d/default_gravity", Gravity);
        
        _currentState = IsOnFloor() ? new IdleState() : new JumpState();
    }

    public override void _PhysicsProcess(double delta)
    {
        var inputs = GetInputs();
        
        var newState = _currentState.HandleInput(this, inputs, delta);
        if (newState != null)
        {
            ChangeState(newState);
        }
        MoveAndSlide();
    }

    private void ChangeState(PlayerState newState)
    {
        GD.Print($"Changing from {_currentState.Name} to {newState.Name}");
        _currentState = newState;
        
        // TODO: Implement a "push down automaton"(?) pattern
        //  Basically just a stack that stores the previous states
        //  If you can "shoot" from idle or running or jumping, it shouldn't need to keep track of specific prev state
        //  It should be able to return something like PlayerState.Previous to go back to whatever the last one was
    }

    private static InputInfo GetInputs()
    {
        var inputInfo = new InputInfo
        {
            InputDirection = Input.GetVector("move_left", "move_right", "ui_up", "ui_down"),
            IsPushingJump = Input.IsActionJustPressed("jump")
        };
        
        return inputInfo;
    }
}