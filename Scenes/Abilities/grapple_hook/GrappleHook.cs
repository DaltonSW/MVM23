using Godot;

public partial class GrappleHook : Node2D {
    [Export] public float Speed = 200f;
    [Export] public double Lifespan = 1;
    private double _timeAlive;

    [Signal]
    public delegate void GrappleHookStruckEventHandler();

    [Signal]
    public delegate void FreeingEventHandler();


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    private void OnAreaEntered(Area2D area) {
        GD.Print($"Grappling hook has intersected with an area: {area}");
        // EmitSignal(nameof(GrappleHookStruck));
    }

    private void OnBodyEntered(Node2D body) {
        GD.Print($"Grappling hook has intersected with a body: {body}");
        EmitSignal(nameof(GrappleHookStruck));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        var position = Position;
        position += new Vector2(Speed * (float)delta * Mathf.Cos(Rotation), Speed * (float)delta * Mathf.Sin(Rotation));
        Position = position;
        _timeAlive += delta;

        if (_timeAlive < Lifespan) return;

        EmitSignal(nameof(Freeing));
        QueueFree();
    }
}