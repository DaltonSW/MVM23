using Godot;

public partial class TextboxTrigger : Area2D {
    [Export] public string TextboxID;

    private Player _player;
    private PackedScene _textboxScene;

    private AnimatedSprite2D _indicator;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _player = GetNode<Player>("../Player");
        _textboxScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/textbox/textbox.tscn");
        
        _indicator = GetNode<AnimatedSprite2D>("Indicator");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
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
        SpawnTextbox();
    }

    private void SpawnTextbox() {
        var textbox = _textboxScene.Instantiate<Textbox>();
        textbox.DialogueID = TextboxID;
        GetParent().AddChild(textbox);
        GetTree().Paused = true;
    }
}