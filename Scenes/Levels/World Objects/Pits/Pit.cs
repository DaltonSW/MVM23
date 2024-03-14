using Godot;
using System;

using MVM23;

public partial class Pit : Area2D
{
    [Export] public string AssociatedRespawn;

    private Player _player;
    private Node2D _respawnPoint;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("../../Player");
        _respawnPoint = GetNode<Node2D>($"../Respawns/{AssociatedRespawn}");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (!OverlapsBody(_player)) return;
        
        _player.TakeDamage();
        _player.GlobalPosition = _respawnPoint.GlobalPosition;
        _player = GetNode<Player>("../../Player");
    }

}
