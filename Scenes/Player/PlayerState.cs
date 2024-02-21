using System;
using Godot;
namespace MVM23.Scripts.AuxiliaryScripts;

public interface IPlayerState {
    public string Name { get; }

    /// Must be called exactly once per _PhysicsProcess,
    /// and nowhere else.
    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta);

    public static Vector2 GenericPositionUpdates(Player player, Player.InputInfo inputs, double delta) {
        var velocity = player.Velocity;

        if (inputs.InputDirection.X != 0)
            velocity.X = inputs.InputDirection.X < 0 ? -Player.RunSpeed : Player.RunSpeed;
        else
            velocity.X = Mathf.MoveToward(velocity.X, 0, Player.RunSpeed);

        if (player.IsOnFloor())
            return velocity;

        var grav = Math.Abs(velocity.Y) < Player.ApexGravityVelRange ? player.ApexGravity : player.Gravity;
        velocity.Y += grav * (float)delta;
        return velocity;
    }
}

// TODO: Consider "IGroundedState" and "IAerialState" ???

public class IdleState : IPlayerState {
    public string Name => "IdleState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("idle");
        player.Velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        if (inputs.IsPushingDash)
            return new DashState(player, inputs);

        if (inputs.IsPushingJump) {
            var jumpType = player.CanJump();
            if (jumpType != Player.JumpType.None) {
                return new JumpState(player, jumpType);
            }
        }

        if (!player.IsOnFloor())
            return new FallState();

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (inputs.InputDirection == Vector2.Zero) return null;

        return new RunState();
    }
}

public class JumpState : IPlayerState {
    public string Name => "JumpState";

    //TODO (#4): Jump buffering
    //TODO (#6): Jump corner protection

    public JumpState(Player player, Player.JumpType jumpType) {
        var jumpSpeed = player.JumpSpeed;

        // If this was a CT jump, add additional jump speed proportional to how long the player has spent in CT
        if (jumpType == Player.JumpType.CoyoteTime)
            jumpSpeed += (float)(player.JumpSpeed * player.CoyoteTimeElapsed);

        player.ResetJumpBuffers();
        player.CoyoteTimeExpired = true; // Set it to true so you can't infinitely jump up
        player.Velocity = new Vector2(player.Velocity.X, player.Velocity.Y - jumpSpeed);
    }

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("jump");

        var velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        player.Velocity = velocity;

        if (inputs.IsPushingDash)
            return new DashState(player, inputs);

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

        if (!player.CoyoteTimeExpired) {
            if (player.CoyoteTimeElapsed >= Player.CoyoteTimeBuffer) {
                player.CoyoteTimeElapsed = 0;
                player.CoyoteTimeExpired = true;
            }
            else {
                player.CoyoteTimeElapsed += delta;
            }
        }

        if (inputs.IsPushingDash)
            return new DashState(player, inputs);
        
        if (inputs.IsPushingJump) {
            var jumpType = player.CanJump();
            if (jumpType != Player.JumpType.None) {
                player.ResetJumpBuffers();
                return new JumpState(player, jumpType);
            }
        }

        if (!player.IsOnFloor()) return null;

        player.ResetJumpBuffers();
        return player.Velocity == Vector2.Zero ? new IdleState() : new RunState();
    }
}

public class RunState : IPlayerState {
    public string Name => "RunState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("run");

        player.Velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        if (inputs.IsPushingDash)
            return new DashState(player, inputs);

        if (inputs.IsPushingJump) {
            var jumpType = player.CanJump();
            if (jumpType != Player.JumpType.None) {
                return new JumpState(player, jumpType);
            }
        }

        if (!player.IsOnFloor())
            return new FallState();

        if (inputs.InputDirection == Vector2.Zero & player.Velocity == Vector2.Zero)
            return new IdleState();

        return null;
    }
}

public class DashState : IPlayerState {
    public string Name => "DashState";

    public DashState(Player player, Player.InputInfo inputs) {
        player.DashTimeElapsed = 0;
        player.DashStoredVelocity = player.Velocity;
        player.DashCurrentAngle = inputs.InputDirection;
    }

    private static Vector2 GetDashDirection(Player.InputInfo inputs) {
        var direction = inputs.InputDirection;
        var directions = new Vector2[]
        {
            Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right, new Vector2(1, 1).Normalized(), // Up-right
            new Vector2(-1, 1).Normalized(),                                                       // Up-left
            new Vector2(1, -1).Normalized(),                                                       // Down-right
            new Vector2(-1, -1).Normalized()                                                       // Down-left
        };

        var closestDirection = direction;
        var highestDot = -1.0f;
        foreach (var dir in directions) {
            var dot = direction.Dot(dir);
            if (dot < highestDot) continue;

            highestDot = dot;
            closestDirection = dir;
        }
        return closestDirection;
    }

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.DashTimeElapsed += delta;
        player.ChangeAnimation("jump");
        player.SetEmittingDashParticles(true);

        player.Velocity = player.DashCurrentAngle * (float)Player.DashSpeed;

        if (player.DashTimeElapsed < Player.DashDuration) return null;

        player.SetEmittingDashParticles(false);
        if (player.Velocity.Y != 0) {
            var tempVel = player.Velocity.Y < 0 ? -Player.MaxVerticalVelocity : Player.MaxVerticalVelocity;
            player.Velocity = new Vector2(player.Velocity.X, tempVel);
        }
        return player.IsOnFloor() ? new IdleState() : new FallState();
    }
}

public class FullCircleDashState : IPlayerState {
    private const float AdditionalMomentumExitSpeedMult = 1.5f;
    [Export] private const double NoInputExitSpeed = 0.5f;

    //TODO (#7): Dash corner protection

    private readonly float _angle;

    private double _timeElapsed;

    private readonly Vector2 _prevPlayerVelocity;

    public FullCircleDashState(Player player) {
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

        player.Velocity = Vector2.FromAngle(_angle) * (float)(Player.DashSpeed * delta);

        if (_timeElapsed < Player.DashDuration) return null;

        player.RestoreReticle();
        player.SetEmittingDashParticles(false);
        player.Velocity = GetExitVelocity(player, inputs);
        return player.IsOnFloor() ? new IdleState() : new JumpState(player, Player.JumpType.Normal);
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