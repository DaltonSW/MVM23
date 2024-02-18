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

        if (!player.IsOnFloor())
            velocity.Y += player.Gravity * (float)delta;

        return velocity;
    }
}

public class IdleState : IPlayerState {
    public string Name => "IdleState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation("idle");
        var velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        if (inputs.IsPushingDash)
            return new DashState(player.GetAngleToMouse());

        if (inputs.IsPushingJump && player.IsOnFloor()) {
            // Change sprite
            velocity.Y -= player.JumpSpeed;
            player.Velocity = velocity;
            return new JumpState();
        }

        if (inputs.InputDirection != Vector2.Zero) {
            // Change sprite
            player.Velocity = velocity;
            return new RunState();
        }

        player.Velocity = velocity;
        return null;
    }
}

public class JumpState : IPlayerState {
    public string Name => "JumpState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        player.ChangeAnimation(player.Velocity.Y <= 0 ? "jump" : "fall");

        var velocity = IPlayerState.GenericPositionUpdates(player, inputs, delta);

        // Add the gravity.
        if (!player.IsOnFloor())
            velocity.Y += player.Gravity * (float)delta;

        player.Velocity = velocity;

        if (player.IsOnFloor())
            return player.Velocity == Vector2.Zero ? new IdleState() : new RunState();

        if (inputs.IsPushingDash)
            return new DashState(player.GetAngleToMouse());

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
            return new DashState(player.GetAngleToMouse());

        return null;
    }
}

public class DashState : IPlayerState {
    private const float DurationSeconds = 0.04f;
    private const double Speed = 100000;

    private float _angle;

    private double _timeElapsed;

    public DashState(float angle) {
        _angle = angle;
        _timeElapsed = 0;
    }

    public string Name => "DashState";

    public IPlayerState HandleInput(Player player, Player.InputInfo inputs, double delta) {
        _timeElapsed += delta;
        player.ChangeAnimation("jump");
        player.SetEmittingDashParticles(true);
        player.FreezeReticle();

        player.Velocity = Vector2.FromAngle(_angle) * (float)(Speed * delta);

        if (_timeElapsed >= DurationSeconds) {
            player.RestoreReticle();
            player.SetEmittingDashParticles(false);
            return new IdleState();
        }

        return null;
    }
}