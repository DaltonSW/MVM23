using Godot;

public interface IHittable
{
    public void TakeHit(Vector2 initialKnockbackVelocity);
    public void Stun() { }
    public void Unstun() { }
}
