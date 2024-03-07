using Godot;

public partial class Hurtbox : Area2D, IHittable
{
    [Signal] public delegate void HurtEventHandler();

    public void TakeHit()
    {
        EmitSignal(SignalName.Hurt);
    }
}
