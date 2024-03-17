using Godot;
using System;
using System.Collections.Generic;
using MVM23;

public partial class AbilityUnlock : Area2D
{
    [Export(PropertyHint.Enum, "Stick,Dash,SuperJump,Grapple,DoubleDash,DashOnKill,KeyToWorldTwo,WorldTwoBossKey,WorldThreeKeyOne,WorldThreeKeyTwo,TeleporterTutorial")] 
    private string _myUnlock;

    private List<string> _tutorial = new()
    {
        "Stick",
        "Dash",
        "SuperJump",
        "Grapple",
        "DoubleDash",
        "TeleporterTutorial",
        "KeyToWorldTwo",
        "WorldTwoBossKey",
        "WorldThreeKeyOne",
        "WorldThreeKeyTwo"
    };

    private Player _player;
    private PackedScene _textboxScene;
    private WorldStateManager _worldStateManager;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("/root/Game/Player");
        _worldStateManager = GetNode<WorldStateManager>("/root/Game/WSM");
        _textboxScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/textbox/textbox.tscn");
        
        if (_worldStateManager.IsObjectActivated(_myUnlock)) QueueFree();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!OverlapsBody(_player)) return;
        _player.UnlockAbility(_myUnlock);
        _worldStateManager.SetObjectAsActivated(_myUnlock);
        QueueFree();
        if (_tutorial.Contains(_myUnlock))
            SpawnTextbox();
    }
    
    private void SpawnTextbox() {
        var textbox = _textboxScene.Instantiate<Textbox>();
        textbox.DialogueID = _myUnlock;
        GetParent().AddChild(textbox);
        GetTree().Paused = true;
    }
}
