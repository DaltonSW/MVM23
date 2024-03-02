using Godot;
using System;

public partial class TextboxTrigger : Area2D {
    [Export] public string TextboxID;

    private Player _player;
    private PackedScene _textboxScene;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("../Player");
        _textboxScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/textbox/textbox.tscn");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("interact") && OverlapsBody(_player)) {
            SpawnTextbox();
        }
    }

    private void SpawnTextbox() {
        var textbox = _textboxScene.Instantiate<Textbox>();
        textbox.DialogueID = TextboxID;
        GetParent().AddChild(textbox);
        GetTree().Paused = true;
    }
}