using System;
using Godot;
namespace MVM23;

public abstract class PlayerState {
    public abstract string Name { get; }
    public abstract PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta);

    private const float ApexGravityVelRange = 5F;

    protected static Vector2 GenericPositionUpdates(Player player, Player.InputInfo inputs, double delta) {
        var velocity = player.Velocity;

        GD.Print(player.KnockbackVelocity);
        velocity += player.KnockbackVelocity;

        if (inputs.InputDirection.X != 0) {
            // If you're in the air, keeps your velocity steady
            // Needs to be changed to allow better air control
            if (player.Velocity.X != 0 && !player.IsOnFloor())
                velocity.X = player.Velocity.X;
            else
                velocity.X = inputs.InputDirection.X < 0 ? -Player.RunSpeed : Player.RunSpeed;
            if (inputs.InputDirection.X < 0 && velocity.X < 0)
                player.FaceLeft();
            else
                player.FaceRight();
        }
        else {
            var drag = player.IsOnFloor() ? Player.GroundFriction : Player.AirFriction;
            velocity.X = Mathf.MoveToward(velocity.X, 0, drag * (float)delta);
        }

        if (player.IsOnFloor())
            return velocity;

        var grav = Math.Abs(velocity.Y) < ApexGravityVelRange ? player.ApexGravity : player.Gravity;
        velocity.Y += grav * (float)delta;
        return velocity;
    }
}

// Consider "GroundedState" and "AerialState" as intermediate classes?

public class IdleState : PlayerState {
    public override string Name => "Idle";

    public override PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
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
    public override string Name => "Charge";

    [Export] public double MinChargeTime = 1.00;

    private double _currentChargeTime;


    public override PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
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
    public override string Name => "Super Jump";

    [Export] public float SuperJumpVelocity = -750f;

    public override PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.SuperJumpCurrentBufferTime = 0;
        if (inputs.IsPushingCrouch) {
            player.CanSuperJump = false;
            return new IdleState();
        }
        
        if (inputs.IsPushingDash) {
            player.CanSuperJump = false;
            return new DashState(inputs, player);
        }
        
        player.Velocity = new Vector2(0, SuperJumpVelocity);
        return null;
    }
}

public class JumpState : PlayerState {
    public override string Name => "Jump";

    private Vector2 _nudgeEnterVel = Vector2.Inf;
    private const int NudgeAmount = 6;

    private const float BoostJumpVertMult = 0.75F;
    private const float BoostJumpHorzMult = 2.5F;

    public JumpState(Player player, Player.JumpType jumpType) {
        var jumpSpeed = player.JumpSpeed;
        var horizSpeed = player.Velocity.X;

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

    public override PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
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
    public override string Name => "Fall";

    [Export] public double CoyoteTimeBuffer = 0.1;

    public override PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
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
    public override string Name => "Run";

    public override PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
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
    public override string Name => "Dash";

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

    public override PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
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
        
        player.SetEmittingDashParticles(false);

        // ReSharper disable once InvertIf
        var tempY = 0F;
        var tempX = 0F;
        if (player.Velocity.Y != 0)
            tempY = player.Velocity.Y < 0 ? -ExitVelocity : ExitVelocity;

        if (player.Velocity.X != 0)
            tempX = player.Velocity.X < 0 ? -Player.RunSpeed : Player.RunSpeed;
        player.Velocity = new Vector2(tempX, tempY);
        
        if (player.IsOnFloor() && inputs.IsPushingCrouch &&
            player.SuperJumpCurrentBufferTime < player.SuperJumpInitBufferLimit)
            return null;

        return player.IsOnFloor() ? new IdleState() : new FallState();
    }
}

public class GrappleState : PlayerState {
    public override string Name => "Grapple";

    private float _curAngle;
    private float _angleVel;
    private float _angleAcc;

    private readonly float _length;
    private readonly float _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public GrappleState(Player player) {
        var entryVelocity = player.Velocity;
        player.Velocity = Vector2.Zero;
        
        var playerPos = player.GlobalPosition;

        _curAngle = (float)Math.PI / 2 - player.GrappledPoint.AngleToPoint(playerPos);
        _length = playerPos.DistanceTo(player.GrappledPoint);

        var dirToPlayer = (playerPos - player.GrappledPoint).Normalized();
        var tangentVec = new Vector2(dirToPlayer.Y, -dirToPlayer.X);
        _angleVel = entryVelocity.Dot(tangentVec) / _length;
    }

    public override PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        if (player.GrappledPoint == Vector2.Inf || !inputs.IsPushingGrapple) {
            // Calculate tangential exit velocity
            var exitVelocityDirection =
                new Vector2((float)Math.Cos(_curAngle), (float)Math.Sin(_curAngle)).Normalized();
            var tangentialExitVelocity = exitVelocityDirection * _angleVel * _length;

            if (_angleVel < 0 || (_angleVel > 0 && _angleAcc < 0))
                tangentialExitVelocity.Y *= -1;

            player.Velocity = tangentialExitVelocity;
            player.Reticle.Rotation = 0;

            if (player.IsOnFloor()) {
                return inputs.InputDirection.X != 0 ? new RunState() : new IdleState();
            }
            return new FallState();
        }

        if (player.IsOnFloor() || player.IsOnWall() || player.IsOnCeiling()) {
            player.GrappledPoint = Vector2.Inf;
            player.QueueRedraw();
            player.Reticle.Rotation = 0;
            return new IdleState();
        }

        // if (player.IsOnCeiling()) {
        //     _angleVel *= -0.85f;
        //     var collisions = player.MoveAndCollide(new Vector2(0, _gravity));
        //     while (collisions != null && collisions.GetNormal() == Vector2.Down)
        //         collisions = player.MoveAndCollide(new Vector2(0, _gravity));
        //     
        //     return null;
        // }

        player.Velocity = Vector2.Zero;

        _angleAcc = -_gravity / _length * (float)Math.Sin(_curAngle);

        _curAngle += _angleVel * (float)delta;
        _angleVel += _angleAcc * (float)delta;

        _angleVel *= 0.99F; // Dampening

        var newPos = new Vector2
        {
            X = player.GrappledPoint.X + _length * (float)Math.Sin(_curAngle),
            Y = player.GrappledPoint.Y + _length * (float)Math.Cos(_curAngle)
        };

        player.GlobalPosition = newPos;
        return null;
    }
}
