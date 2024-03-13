using Godot;
using System;

namespace MVM23;

public partial class Lever : Area2D {
    [Export] public string ObjectID;

    [Export] public Godot.Collections.Array<string> SameSceneObjects;
    [Export] public Godot.Collections.Array<string> OtherSceneObjects;

    private Player _player;
    private WorldStateManager _worldStateManager;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("../../Player");
        _worldStateManager = GetNode<WorldStateManager>("/root/Game/WSM");

        if (_worldStateManager.IsObjectActivated(ObjectID)) {
            // Do whatever needs to be done to indicate "flipped"
            QueueFree();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (!OverlapsBody(_player)) return;
        if (!Input.IsActionJustPressed("interact")) return;

        Flip();

    }

    private void Flip() {
        // Get the nodes in the same scene array and free them
        foreach (var objectID in SameSceneObjects) {
            var node = GetNodeOrNull<Node2D>($"../{objectID}");
            node?.QueueFree();
            _worldStateManager.SetObjectAsActivated(objectID);
        }

        _worldStateManager.SetObjectAsActivated(ObjectID);

        foreach (var objectID in OtherSceneObjects) {
            _worldStateManager.SetObjectAsActivated(objectID);
        }
    }
}