using Godot;
using System;

using MVM23;

public partial class Intro : Node2D
{
    private PackedScene _textboxScene;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _textboxScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/textbox/textbox.tscn");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    
    private void SpawnTextbox(string dialogue) {
        var textbox = _textboxScene.Instantiate<Textbox>();
        textbox.DialogueID = dialogue;
        GetParent().AddChild(textbox);
        GetTree().Paused = true;
    }
}
