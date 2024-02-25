using System;
using Godot;
namespace MVM23.Scripts.AuxiliaryScripts;

public interface IPlayerState {
    public string Name { get; }

    public static float ApexGravityVelRange => 5F;

    /// Must be called exactly once per _PhysicsProcess,
    /// and nowhere else.
    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta);

    public static Vector2 GenericPositionUpdates(Player player, Player.InputInfo inputs, double delta) {
        var velocity = player.Velocity;

        if (inputs.InputDirection.X != 0)
            velocity.X = inputs.InputDirection.X < 0 ? -player.RunSpeed : player.RunSpeed;
        else
            velocity.X = Mathf.MoveToward(velocity.X, 0, player.RunSpeed);

        if (player.IsOnFloor())
            return velocity;

        var grav = Math.Abs(velocity.Y) < ApexGravityVelRange ? player.ApexGravity : player.Gravity;
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
            return new DashState(inputs);

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
    
    [Export] public double MinChargeTime = 1.00;

    private double _currentChargeTime;


    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.Velocity = Vector2.Zero;
        if (!inputs.IsPushingCrouch) {
            player.ChangeColor(Colors.White);
            _currentChargeTime = 0;
            return player.CanSuperJump ? new SuperJumpState() : new IdleState();
        }

        _currentChargeTime += delta;
        if (_currentChargeTime < MinChargeTime) return null;
        
        player.CanSuperJump = true;
        player.ChangeColor(Colors.Red);

        return null;
    }
}

public class SuperJumpState : IPlayerState {
    public string Name => "SuperJumpState";
    
    [Export] public float SuperJumpVelocity = -750f;

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        if (inputs.IsPushingCrouch) {
            player.CanSuperJump = false;
            return new IdleState();
        }
        player.Velocity = new Vector2(0, SuperJumpVelocity);
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
            return new DashState(inputs);

        if (player.Velocity.Y > 0)
            return new FallState();

        if (player.IsOnFloor())
            return player.Velocity == Vector2.Zero ? new IdleState() : new RunState();

        return null;
    }
}

public class FallState : IPlayerState {
    public string Name => "FallState";
    
    [Export] public double CoyoteTimeBuffer = 0.1;

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("fall");

        player.Velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        if (!player.CoyoteTimeExpired) {
            if (player.CoyoteTimeElapsed >= CoyoteTimeBuffer) {
                player.CoyoteTimeElapsed = 0;
                player.CoyoteTimeExpired = true;
            }
            else {
                player.CoyoteTimeElapsed += delta;
            }
        }

        if (inputs.IsPushingDash && player.CanDash())
            return new DashState(inputs);

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
            return new DashState(inputs);

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
    
    [Export] public float ExitVelocity = 150.0f;
    [Export] public float DashDuration = 0.08f;
    [Export] public double DashSpeed = 750.0f;
    
    private double _dashTimeElapsed;
    private readonly Vector2 _dashCurrentAngle;

    public DashState(Player.InputInfo inputs) {
        _dashTimeElapsed = 0;
        _dashCurrentAngle = inputs.InputDirection;
    }

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.SuperJumpCurrentBufferTime = 0;
        _dashTimeElapsed += delta;
        player.ChangeAnimation("jump");
        player.SetEmittingDashParticles(true);

        player.Velocity = _dashCurrentAngle * (float)DashSpeed;

        if (player.CanStartCharge(inputs))
            return new ChargeState();

        if (_dashTimeElapsed < DashDuration)
            return null;

        player.SetEmittingDashParticles(false);

        // ReSharper disable once InvertIf
        if (player.Velocity.Y != 0) {
            var tempVel = player.Velocity.Y < 0 ? -ExitVelocity : ExitVelocity;
            player.Velocity = new Vector2(player.Velocity.X, tempVel);
        }

        return player.IsOnFloor() ? new IdleState() : new FallState();
    }
    
    
    // private static Vector2 GetDashDirection(Player.InputInfo inputs) {
    //     var direction = inputs.InputDirection;
    //     var directions = new Vector2[]
    //     {
    //         Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right, new Vector2(1, 1).Normalized(), // Up-right
    //         new Vector2(-1, 1).Normalized(),                                                       // Up-left
    //         new Vector2(1, -1).Normalized(),                                                       // Down-right
    //         new Vector2(-1, -1).Normalized()                                                       // Down-left
    //     };
    //
    //     var closestDirection = direction;
    //     var highestDot = -1.0f;
    //     foreach (var dir in directions) {
    //         var dot = direction.Dot(dir);
    //         if (dot < highestDot) continue;
    //
    //         highestDot = dot;
    //         closestDirection = dir;
    //     }
    //     return closestDirection;
    // }
}
