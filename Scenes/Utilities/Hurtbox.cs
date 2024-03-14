using Godot;

public partial class Hurtbox : Area2D, IHittable
{
    [Export] public Node Hurtee { get; set; }
    [Signal] public delegate void HurtEventHandler();

    public void TakeHit(Vector2 initialKnockbackVelocity)
    {
        ((IHittable) Hurtee).TakeHit(initialKnockbackVelocity);
    }
}

