using Godot;

public partial class Hurtbox : Area2D, IHittable
{
    [Export] public Node Hurtee { get; set; }
    [Signal] public delegate void HurtEventHandler();

    public void TakeHit(Vector2 initialKnockbackVelocity)
    {
        ((IHittable) Hurtee).TakeHit(initialKnockbackVelocity);
    }

    // TODO: break apart the interfaces so this doesn't need to happen
    public bool DeathQueued() => true;
    public void QueueDeath() { }
}

