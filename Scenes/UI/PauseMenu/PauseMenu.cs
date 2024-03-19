using Godot;

public partial class PauseMenu : Sprite2D {
    private Button _resumeButton;
    private Button _mainMenuButton;

    private GodotObject _game;

    public void SetGame(GodotObject game) {
        _game = game;
    }

    public override void _Ready() {
        GetTree().Paused = true;
        
        _resumeButton = GetNode<Button>("ResumeButton");
        _mainMenuButton = GetNode<Button>("MainMenuButton");
    }

    public override void _Process(double delta) {
        _resumeButton.Disabled = false;
        _mainMenuButton.Disabled = false;

        if (Input.IsActionJustPressed("pause")) {
            GetTree().Paused = false;
            _game.Call("play_world_music");
            QueueFree();
        }
    }

    private void _on_ResumeButton_pressed() {
        GetTree().Paused = false;
        _game.Call("play_world_music");
        QueueFree();
    }
    
    private void _on_MainMenuButton_pressed() {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu/MainMenu.tscn");
    }
}
