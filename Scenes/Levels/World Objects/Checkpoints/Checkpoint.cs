using Godot;
using System;

using MVM23;

public partial class Checkpoint : Area2D
{
    private Player _player;
    private WorldStateManager _worldStateManager;
    private GodotObject _game;
    private AnimatedSprite2D _sprite;

    private bool _active;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("../../Player");
        _worldStateManager = GetNode<WorldStateManager>("/root/Game/WSM");
        _game = GetNode<GodotObject>("/root/Game");
        _sprite = GetNode<AnimatedSprite2D>("Sprite");

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (!OverlapsBody(_player)) return;

        _sprite.Play("active");
        _worldStateManager.SaveCurrentRoom();
        _player.CurrentHealth = _player.MaxHealth;
    }
}