using Godot;
namespace MVM23.Scripts.AuxiliaryScripts;

public interface PlayerState
{
    public string Name { get; }

    public PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta);
    
    public static Vector2 GenericPositionUpdates(Player player, Player.InputInfo inputs, double delta)
    {
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

public class IdleState : PlayerState
{
    public string Name => "IdleState";

    public PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta)
    {
        var velocity = PlayerState.GenericPositionUpdates(player, inputs, delta);
        
        if (inputs.IsPushingJump && player.IsOnFloor())
        {
            // Change sprite
            velocity.Y -= player.JumpSpeed;
            player.Velocity = velocity;
            return new JumpState();
        }
        
        if (inputs.InputDirection != Vector2.Zero)
        {
            // Change sprite
            player.Velocity = velocity;
            return new RunState();
        }
        
        player.Velocity = velocity;
        return null; 
    }
}

public class JumpState : PlayerState
{
    public string Name => "JumpState";

    public PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta)
    {
        var velocity = PlayerState.GenericPositionUpdates(player, inputs, delta);
        
        // Add the gravity.
        if (!player.IsOnFloor())
            velocity.Y += player.Gravity * (float)delta;

        player.Velocity = velocity;
        if (player.IsOnFloor())
            return player.Velocity == Vector2.Zero ? new IdleState() : new RunState();
        
        return null;
    }
}

public class RunState : PlayerState
{       
    public string Name => "RunState";

    public PlayerState HandleInput(Player player, Player.InputInfo inputs, double delta)
    {
        var velocity = PlayerState.GenericPositionUpdates(player, inputs, delta);
        
        if (inputs.IsPushingJump && player.IsOnFloor())
        {
            // Change sprite
            velocity.Y += (float)(player.Gravity * delta);
            player.Velocity = velocity;
            return new JumpState();
        }

        player.Velocity = velocity;
        if (inputs.InputDirection == Vector2.Zero & player.Velocity == Vector2.Zero)
            return new IdleState();
        
        return null;
    }
}