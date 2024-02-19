using Godot;
using System;

using MVM23.Scripts;

public partial class DebugLabel : Label {
    private Player _player;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("../Player");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        Text = $"X Vel: {_player.Velocity.X}\nY Vel: {_player.Velocity.Y}";
    }
}
