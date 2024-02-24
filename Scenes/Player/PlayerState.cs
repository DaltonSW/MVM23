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

        if (player.CanStartCharge(inputs))
            return new ChargeState();

        if (inputs.IsPushingDash && player.CanDash())
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
        if (inputs.InputDirection.X == 0) return null;

        return new RunState();
    }
}

public class ChargeState : IPlayerState {
    public string Name => "ChargeState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.Velocity = Vector2.Zero;
        if (!inputs.IsPushingCrouch) {
            player.ChangeColor(Colors.White);
            player.SuperJumpCurrentChargeTime = 0;
            return player.CanSuperJump ? new SuperJumpState() : new IdleState();
        }

        player.SuperJumpCurrentChargeTime += delta;
        if (player.SuperJumpCurrentChargeTime < Player.SuperJumpMinChargeTime) return null;
        
        player.CanSuperJump = true;
        player.ChangeColor(Colors.Red);

        return null;
    }
}

public class SuperJumpState : IPlayerState {
    public string Name => "SuperJumpState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        if (inputs.IsPushingCrouch) {
            player.CanSuperJump = false;
            return new IdleState();
        }
        player.Velocity = new Vector2(0, -Player.SuperJumpVelocity);
        return null;
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

        if (inputs.IsPushingDash && player.CanDash())
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

        if (inputs.IsPushingDash && player.CanDash())
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

        if (player.CanStartCharge(inputs))
            return new ChargeState();

        if (inputs.IsPushingDash && player.CanDash())
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
        player.SuperJumpCurrentBufferTime = 0;
        player.DashTimeElapsed += delta;
        player.ChangeAnimation("jump");
        player.SetEmittingDashParticles(true);

        player.Velocity = player.DashCurrentAngle * (float)Player.DashSpeed;

        if (player.CanStartCharge(inputs))
            return new ChargeState();

        if (player.DashTimeElapsed < Player.DashDuration)
            return null;

        player.SetEmittingDashParticles(false);

        // ReSharper disable once InvertIf
        if (player.Velocity.Y != 0) {
            var tempVel = player.Velocity.Y < 0 ? -Player.MaxVerticalVelocity : Player.MaxVerticalVelocity;
            player.Velocity = new Vector2(player.Velocity.X, tempVel);
        }

        return player.IsOnFloor() ? new IdleState() : new FallState();
    }
}