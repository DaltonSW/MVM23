using Godot;
using System;

public partial class GrappleHook : Node2D {
    [Export] private const float Speed = 200f;
    
    [Signal]
    public delegate void GrappleHookStruckEventHandler();

    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        var position = Position;
        position += new Vector2(Speed * (float)delta * Mathf.Cos(Rotation), Speed * (float)delta * Mathf.Sin(Rotation));
        Position = position;
    }
}
