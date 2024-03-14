using Godot;
using System;

using MVM23;

public partial class Checkpoint : Area2D
{
    private Player _player;
    private WorldStateManager _worldStateManager;
    private GodotObject _game;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("../../Player");
        _worldStateManager = GetNode<WorldStateManager>("/root/Game/WSM");
        _game = GetNode<GodotObject>("/root/Game");

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (!OverlapsBody(_player)) return;

        _worldStateManager.GetRoomName();
    }
}