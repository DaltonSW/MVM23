using Godot;
using System;

using MVM23;

public partial class Teleporter : Area2D
{
    [Export] 
    private string _sceneToLoad;

    [Export] private Vector2 _teleportPosition;

    private Player _player;
    private WorldStateManager _worldStateManager;
    private GodotObject _game;
    private AnimatedSprite2D _indicator;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("../Player");
        _worldStateManager = GetNode<WorldStateManager>("/root/Game/WSM");
        //_game = GetNode<GodotObject>("/root/Game");
        _indicator = GetNode<AnimatedSprite2D>("Indicator");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!OverlapsBody(_player)) {
            _indicator.Frame = 0;
            _indicator.Stop();
            return;
        }
        
        if (_indicator.Frame == 0 && !_indicator.IsPlaying())
            _indicator.Play("fade-in");
        
        if (!Input.IsActionJustPressed("interact")) return;

        _indicator.Frame = 0;
        _indicator.Stop();
        //_game.Call("teleport_player", _sceneToLoad, _teleportPosition);
    }
}
