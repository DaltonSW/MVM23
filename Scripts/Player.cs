using Godot;
using System;

public partial class Player : CharacterBody2D
{
    private enum PlayerState
    {
        Idle,
        Running,
        Jumping
    }

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public const float Speed = 300.0f;
    public const float JumpVelocity = -400.0f;
    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    private PlayerState _currentState;

    public override void _Ready()
    {
        _currentState = PlayerState.Idle;
    }

    public override void _PhysicsProcess(double delta)
    {
        // GetInputs();
        var direction = Input.GetVector("move_left", "move_right", "ui_up", "ui_down");
        var isJumping = Input.IsActionJustPressed("jump");
        
        switch (_currentState)
        {
            case PlayerState.Idle:
                break;
            case PlayerState.Running:
                break;
            case PlayerState.Jumping:
                break;
            default:
                throw new InvalidOperationException();
        }
        var velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = JumpVelocity;

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        if (direction != Vector2.Zero)
        {
            velocity.X = direction.X * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
