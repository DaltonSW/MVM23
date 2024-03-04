using System;
using Godot;
namespace MVM23;

public interface IPlayerState {
    public string Name => "InterfaceName";

    /// Must be called EXACTLY once per _PhysicsProcess, and nowhere else.
    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta);
}

public abstract class PlayerState : IPlayerState {
    public string Name => "PlayerState";
    public abstract IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta);

    private const float ApexGravityVelRange = 5F;

    protected static Vector2 GenericPositionUpdates(Player player, Player.InputInfo inputs, double delta) {
        var velocity = player.Velocity;

        if (inputs.InputDirection.X != 0) {
            if (player.Velocity.X != 0 && !player.IsOnFloor())
                velocity.X = player.Velocity.X;
            else
                velocity.X = inputs.InputDirection.X < 0 ? -player.RunSpeed : player.RunSpeed;
            if (inputs.InputDirection.X < 0 && velocity.X < 0)
                player.FaceLeft();
            else
                player.FaceRight();
        }
        else
            velocity.X = Mathf.MoveToward(velocity.X, 0, player.RunSpeed);

        if (player.IsOnFloor())
            return velocity;

        var grav = Math.Abs(velocity.Y) < ApexGravityVelRange ? player.ApexGravity : player.Gravity;
        velocity.Y += grav * (float)delta;
        return velocity;
    }
}

// Consider "GroundedState" and "AerialState" as intermediate classes?

public class IdleState : PlayerState {
    public new string Name => "IdleState";

    public override IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("idle");
        player.Velocity = GenericPositionUpdates(player, inputs, delta);

        if (player.CanStartCharge(inputs))
            return new ChargeState();

        if (inputs.IsPushingDash && player.CanDash())
            return new DashState(inputs, player);

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

public class ChargeState : PlayerState {
    public new string Name => "ChargeState";

    [Export] public double MinChargeTime = 1.00;

    private double _currentChargeTime;


    public override IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
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

public class SuperJumpState : PlayerState {
    public new string Name => "SuperJumpState";

    [Export] public float SuperJumpVelocity = -750f;

    public override IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.SuperJumpCurrentBufferTime = 0;
        if (inputs.IsPushingCrouch) {
            player.CanSuperJump = false;
            return new IdleState();
        }
        player.Velocity = new Vector2(0, SuperJumpVelocity);
        return null;
    }
}

public class JumpState : PlayerState {
    public new string Name => "JumpState";

    private Vector2 _nudgeEnterVel = Vector2.Inf;
    private const int NudgeAmount = 6;

    private const float BoostJumpVertMult = 0.75F;
    private const float BoostJumpHorzMult = 2.5F;

    public JumpState(Player player, Player.JumpType jumpType) {
        var jumpSpeed = player.JumpSpeed;
        var horizSpeed = player.RunSpeed;
        if (player.Velocity.X < 0)
            horizSpeed *= -1;

        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (jumpType == Player.JumpType.BoostJump) {
            jumpSpeed *= BoostJumpVertMult;
            horizSpeed *= BoostJumpHorzMult;
            player.ResetJumpBuffers();
            player.CoyoteTimeExpired = true; // Set it to true so you can't infinitely jump up
            player.Velocity = new Vector2(horizSpeed, -jumpSpeed);
            return;
        }

        // If this was a CT jump, add additional jump speed proportional to how long the player has spent in CT
        if (jumpType == Player.JumpType.CoyoteTime)
            jumpSpeed += (float)(player.JumpSpeed * player.CoyoteTimeElapsed);

        player.ResetJumpBuffers();
        player.CoyoteTimeExpired = true; // Set it to true so you can't infinitely jump up
        player.Velocity = new Vector2(horizSpeed, -jumpSpeed);
    }

    public override IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("jump");

        var velocity = GenericPositionUpdates(player, inputs, delta);

        player.Velocity = velocity;

        if (inputs.IsPushingDash && player.CanDash())
            return new DashState(inputs, player);


        if (player.IsOnCeiling()) {
            if (player.ShouldNudgeNegative()) {
                player.NudgePlayer(-NudgeAmount, _nudgeEnterVel);
                return null;
            }

            if (player.ShouldNudgePositive()) {
                player.NudgePlayer(NudgeAmount, _nudgeEnterVel);
                return null;
            }
        }

        _nudgeEnterVel = player.Velocity;

        if (player.Velocity.Y > 0)
            return new FallState();

        if (player.IsOnFloor())
            return player.Velocity == Vector2.Zero ? new IdleState() : new RunState();

        return null;
    }
}

public class FallState : PlayerState {
    public new string Name => "FallState";

    [Export] public double CoyoteTimeBuffer = 0.1;

    public override IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("fall");

        player.Velocity = GenericPositionUpdates(player, inputs, delta);

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
            return new DashState(inputs, player);

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

public class RunState : PlayerState {
    public new string Name => "RunState";

    public override IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("run");

        player.Velocity = GenericPositionUpdates(player, inputs, delta);

        if (player.CanStartCharge(inputs))
            return new ChargeState();

        if (inputs.IsPushingDash && player.CanDash())
            return new DashState(inputs, player);

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

public class DashState : PlayerState {
    public new string Name => "DashState";

    [Export] public static float ExitVelocity { get; } = 150.0f;
    [Export] public float DashDuration = 0.12f;
    [Export] public double DashSpeed = 750.0f;

    private const int NudgeAmount = 6;

    private double _dashTimeElapsed;
    private readonly Vector2 _dashCurrentAngle;

    public DashState(Player.InputInfo inputs, Player player) {
        player.SuperJumpCurrentBufferTime = 0;
        _dashTimeElapsed = 0;
        _dashCurrentAngle = inputs.InputDirection;
        player.PlayerCanDash = false;
    }

    public override IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        if (player.IsOnFloor())
            player.SuperJumpCurrentBufferTime += delta;

        _dashTimeElapsed += delta;
        player.ChangeAnimation("jump");
        player.SetEmittingDashParticles(true);

        player.Velocity = _dashCurrentAngle * (float)DashSpeed;

        if (player.IsOnCeiling()) {
            if (player.ShouldNudgeNegative()) {
                player.NudgePlayer(-NudgeAmount, player.Velocity);
                return null;
            }

            if (player.ShouldNudgePositive()) {
                player.NudgePlayer(NudgeAmount, player.Velocity);
                return null;
            }
        }

        if (player.CanStartCharge(inputs)) {
            player.SuperJumpCurrentBufferTime = 0;
            return new ChargeState();
        }

        if (inputs.IsPushingJump) {
            if (player.CanJump() == Player.JumpType.BoostJump) {
                player.SuperJumpCurrentBufferTime = 0;
                return new JumpState(player, Player.JumpType.BoostJump);
            }
        }

        if (_dashTimeElapsed < DashDuration)
            return null;

        player.SuperJumpCurrentBufferTime = 0;

        player.SetEmittingDashParticles(false);

        // ReSharper disable once InvertIf
        var tempY = 0F;
        var tempX = 0F;
        if (player.Velocity.Y != 0)
            tempY = player.Velocity.Y < 0 ? -ExitVelocity : ExitVelocity;

        if (player.Velocity.X != 0)
            tempX = player.Velocity.X < 0 ? -player.RunSpeed : player.RunSpeed;
        player.Velocity = new Vector2(tempX, tempY);


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

public class GrappleState : PlayerState {
    private GrappleHook GrappleHook { get; set; }
    private float _playerDistanceToHook;

    [Export] private float GrappleForce = 600f;
    [Export] private double GrappleGravDiv = 1.3;
    private const float MaxSpeed = 150f;

    public GrappleState(GrappleHook grappleHook, float playerDistanceToHook) {
        GrappleHook = grappleHook;
        _playerDistanceToHook = playerDistanceToHook;
    }

    public override IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        var currentAngle = (GrappleHook.GlobalPosition - player.GlobalPosition).Normalized();

        var velocity = player.Velocity;

        velocity += currentAngle * GrappleForce * (float)delta;

        velocity.Y += (float)(player.ApexGravity * delta / GrappleGravDiv);

        player.Velocity = velocity;

        if (inputs.IsPushingGrapple) return null;

        if (player.IsOnFloor()) {
            return inputs.InputDirection.X != 0 ? new RunState() : new IdleState();
        }
        return new FallState();
    }
}
