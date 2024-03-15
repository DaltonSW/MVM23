using Godot;
using System;
using MVM23;

public partial class Door : Node2D {
    [Export] public string ObjectID;

    private WorldStateManager _worldStateManager;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _worldStateManager = GetNode<WorldStateManager>("/root/Game/WSM");

        if (_worldStateManager.IsObjectActivated(ObjectID)) {
            QueueFree();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
