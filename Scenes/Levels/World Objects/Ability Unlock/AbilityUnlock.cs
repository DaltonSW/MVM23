using Godot;
using System;
using MVM23;

public partial class AbilityUnlock : Area2D
{
    [Export(PropertyHint.Enum, "Stick,Dash,SuperJump,Grapple,DoubleDash,DashOnKill,KeyToWorldTwo,WorldThreeKeyOne,WorldThreeKeyTwo")] 
    private string _myUnlock;

    private Player _player;
    private WorldStateManager _worldStateManager;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("/root/Game/Player");
        _worldStateManager = GetNode<WorldStateManager>("/root/Game/WSM");
        
        if (_worldStateManager.IsObjectActivated(_myUnlock)) QueueFree();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!OverlapsBody(_player)) return;
        _player.UnlockAbility(_myUnlock);
        _worldStateManager.SetObjectAsActivated(_myUnlock);
        QueueFree();
    }
}
