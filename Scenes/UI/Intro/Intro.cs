using Godot;
using System;

using MVM23;

public partial class Intro : Node2D
{
    private PackedScene _textboxScene;
    private GodotObject _fontHelper;

    [Export] public string IntroID = "Intro";
    private bool _textboxSpawned;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _textboxScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/textbox/textbox.tscn");
        _fontHelper = GetNode<GodotObject>("FontHelper");
        
        SpawnTextbox(IntroID);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        GetTree().ChangeSceneToFile("res://Scenes/Levels/Game.tscn");
    }
    
    private void SpawnTextbox(string dialogue) {
        var textbox = _textboxScene.Instantiate<Textbox>();
        textbox.DialogueID = dialogue;
        textbox.SetFontHelper(_fontHelper);
        GetParent().CallDeferred("add_child", textbox);
        GetTree().Paused = true;
    }
}
