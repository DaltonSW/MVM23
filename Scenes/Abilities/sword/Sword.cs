using Godot;
using System.Linq;

public partial class Sword : Node2D
{
    private Area2D _hitbox;
    
    public double Lifetime { get; private set; }

    public override void _Ready()
    {
        _hitbox = GetNode<Area2D>("Hitbox");
        Lifetime = 0;
    }

    public override void _PhysicsProcess(double delta)
    {
        Lifetime += delta;

        var hittables =
            from area in _hitbox.GetOverlappingAreas()
            where area.IsInGroup("hittable")
            select (IHittable) area;
        foreach (var hittable in hittables)
        {
            hittable.TakeHit();
        }
    }
}
