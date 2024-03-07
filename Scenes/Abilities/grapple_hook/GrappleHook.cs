using Godot;

public partial class GrappleHook : Node2D {
    [Export] public float Speed = 400f;
    [Export] public double Lifespan = 1;
    private double _timeAlive;
    private bool IsStuck { get; set; }

    [Signal]
    public delegate void GrappleHookStruckEventHandler();

    [Signal]
    public delegate void FreeingEventHandler();

    private Sprite2D _sprite;

    private enum State {
        Flying,
        Stuck
    }

    private State _state;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _state = State.Flying;
    }

    private void OnAreaEntered(Area2D area) {
        GD.Print($"Grappling hook has intersected with an area: {area}");
        // EmitSignal(nameof(GrappleHookStruck));
    }

    private void OnBodyEntered(Node2D body) {
        GD.Print($"Grappling hook has intersected with a body: {body}");
        if (body == null) return;
        if (body.GetType() == typeof(Player)) return;
        StruckSomething();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        switch (_state) {
            case State.Flying:
                var position = Position;
                position += new Vector2(Speed * (float)delta * Mathf.Cos(Rotation),
                    Speed * (float)delta * Mathf.Sin(Rotation));
                Position = position;
                _timeAlive += delta;

                if (_timeAlive < Lifespan) return;

                EmitSignal(nameof(Freeing));
                QueueFree();
                break;
            case State.Stuck:
                break;
        }

    }

    private void StruckSomething() {
        _state = State.Stuck;
        EmitSignal(nameof(GrappleHookStruck));

    }
}