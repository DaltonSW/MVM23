using System;
using System.Text.RegularExpressions;
using Godot;
namespace MVM23.Scripts.AuxiliaryScripts;

public interface IPlayerState {
    public string Name { get; }

    /// Must be called exactly once per _PhysicsProcess,
    /// and nowhere else.
    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta);

    public static Vector2 GenericPositionUpdates(Player player, Player.InputInfo inputs, double delta) {
        var velocity = player.Velocity;

        if (inputs.InputDirection != Vector2.Zero)
            velocity.X = inputs.InputDirection.X * Player.RunSpeed;
        else
            velocity.X = Mathf.MoveToward(velocity.X, 0, Player.RunSpeed);

        if (!player.IsOnFloor()) {
            // var grav = Math.Abs(velocity.Y) < Player.ApexGravityVelRange ? player.ApexGravity : player.Gravity;
            var grav = player.Gravity;
            velocity.Y += grav * (float)delta;
        }

        return velocity;
    }
}

// TODO: Consider "IGroundedState" and "IAerialState" ???

public class IdleState : IPlayerState {
    public string Name => "IdleState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("idle");
        var velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        if (inputs.IsPushingDash)
            return new DashState(player);

        if (player.CanJump(inputs)) {
            velocity.Y -= player.JumpSpeed;
            player.Velocity = velocity;
            return new JumpState();
        }

        player.Velocity = velocity;

        if (!player.IsOnFloor())
            return new FallState();

        if (inputs.InputDirection != Vector2.Zero)
            return new RunState();

        return null;
    }
}

public class JumpState : IPlayerState {
    public string Name => "JumpState";

    //TODO (#3): Coyote time 
    //TODO (#4): Jump buffering
    //TODO (#6): Jump corner protection

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("jump");

        var velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        player.Velocity = velocity;

        if (inputs.IsPushingDash)
            return new DashState(player);

        if (player.Velocity.Y > 0)
            return new FallState();

        if (player.IsOnFloor())
            return player.Velocity == Vector2.Zero ? new IdleState() : new RunState();

        return null;
    }
}

public class FallState : IPlayerState {
    public string Name => "FallState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("fall");

        player.Velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        if (player.CanJump(inputs))
            return new JumpState();

        if (player.IsOnFloor())
            return player.Velocity == Vector2.Zero ? new IdleState() : new RunState();


        return null;
    }
}

public class RunState : IPlayerState {
    public string Name => "RunState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("run");

        var velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        if (inputs.IsPushingJump && player.IsOnFloor()) {
            // Change sprite
            velocity.Y -= player.JumpSpeed;
            player.Velocity = velocity;
            return new JumpState();
        }

        player.Velocity = velocity;
        if (inputs.InputDirection == Vector2.Zero & player.Velocity == Vector2.Zero)
            return new IdleState();

        if (inputs.IsPushingDash)
            return new DashState(player);

        return null;
    }
}

public class DashState : IPlayerState {
    [Export] private const float DurationSeconds = 0.04f;
    [Export] private const double Speed = 100000;

    private const float AdditionalMomentumExitSpeedMult = 1.5f;
    [Export] private const double NoInputExitSpeed = 0.5f;

    //TODO (#7): Dash corner protection

    private readonly float _angle;

    private double _timeElapsed;

    private readonly Vector2 _prevPlayerVelocity;

    public DashState(Player player) {
        _angle = player.GetAngleToMouse();
        _timeElapsed = 0;
        _prevPlayerVelocity = player.Velocity;
    }

    public string Name => "DashState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        _timeElapsed += delta;
        player.ChangeAnimation("jump");
        player.SetEmittingDashParticles(true);
        player.FreezeReticle();

        player.Velocity = Vector2.FromAngle(_angle) * (float)(Speed * delta);

        if (_timeElapsed < DurationSeconds) return null;

        player.RestoreReticle();
        player.SetEmittingDashParticles(false);
        player.Velocity = GetExitVelocity(player, inputs);
        return player.IsOnFloor() ? new IdleState() : new JumpState();
    }

    private Vector2 GetExitVelocity(Player player, Player.InputInfo inputs) {
        return player.DashMode switch
        {
            Player.TestDashMode.NoExtraMomentum => _prevPlayerVelocity,
            Player.TestDashMode.MoveMaxMomentum => new Vector2((float)(Player.RunSpeed * Math.Cos(_angle)),
                (float)(player.JumpSpeed * Math.Sin(_angle))),
            Player.TestDashMode.MoreThanMoveMaxMomentum => new Vector2(
                (float)(Player.RunSpeed * Math.Cos(_angle) * AdditionalMomentumExitSpeedMult),
                (float)(player.JumpSpeed * Math.Sin(_angle) * AdditionalMomentumExitSpeedMult)),
            _ => Vector2.Zero
        };
    }
}