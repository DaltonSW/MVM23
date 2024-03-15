using Godot;

public partial class MainMenu : Control {
    private AudioStreamPlayer _audioPlayer;
    private AudioStreamWav _menuSong;
    private Sprite2D _creditsSprite;

    private Button _newGameButton;
    private Button _loadGameButton;
    private Button _creditsButton;
    private Button _quitButton;

    public override void _Ready() {
        // _menuSong = GD.Load<AudioStreamWav>("res://Assets/Sounds/MainMenu.wav");
        // _audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
        _creditsSprite = GetNode<Sprite2D>("Credits");
        _newGameButton = GetNode<Button>("HBoxContainer/NewGameButton");
        _loadGameButton = GetNode<Button>("HBoxContainer/LoadGameButton");
        _creditsButton = GetNode<Button>("HBoxContainer/CreditsButton");
        _quitButton = GetNode<Button>("HBoxContainer/QuitButton");

        // _audioPlayer.Autoplay = true;
        // _audioPlayer.Stream = _menuSong;
        // _audioPlayer.VolumeDb = -5f;
        // _audioPlayer.Play();
    }

    public override void _Process(double delta) {
        if (!_creditsSprite.Visible ||
            (!Input.IsActionJustPressed("pause") && !Input.IsActionJustPressed("close_menu"))) return;
        _creditsSprite.Visible = false;
        _newGameButton.Disabled = false;
        _loadGameButton.Disabled = false;
        _creditsButton.Disabled = false;
        _quitButton.Disabled = false;
    }

    private void _on_NewGameButton_pressed() {
        GetTree().ChangeSceneToFile("res://Scenes/Levels/Game.tscn");
    }

    private void _on_QuitButton_pressed() {
        GetTree().Quit();
    }

    private void _on_CreditsButton_pressed() {
        _creditsSprite.Visible = true;
        _newGameButton.Disabled = true;
        _loadGameButton.Disabled = true;
        _creditsButton.Disabled = true;
        _quitButton.Disabled = true;
    }
}