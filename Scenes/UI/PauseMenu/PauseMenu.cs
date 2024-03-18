using Godot;

public partial class PauseMenu : Sprite2D {
    private Button _resumeButton;
    private Button _loadSaveButton;
    private Button _mainMenuButton;
    private Button _quitButton;

    private GodotObject _game;

    public void SetGame(GodotObject game) {
        _game = game;
    }

    public override void _Ready() {
        GetTree().Paused = true;
        
        _resumeButton = GetNode<Button>("VBoxContainer/ResumeButton");
        _loadSaveButton = GetNode<Button>("VBoxContainer/LoadSaveButton");
        _mainMenuButton = GetNode<Button>("VBoxContainer/MainMenuButton");
        _quitButton = GetNode<Button>("VBoxContainer/QuitButton");
    }

    public override void _Process(double delta) {
        _resumeButton.Disabled = false;
        _loadSaveButton.Disabled = false;
        _mainMenuButton.Disabled = false;
        _quitButton.Disabled = false;

        if (Input.IsActionJustPressed("pause")) {
            GetTree().Paused = false;
            QueueFree();
        }
    }

    private void _on_ResumeButton_pressed() {
        GetTree().Paused = false;
        _game.Call("play_world_music");
        QueueFree();
    }
    
    private void _on_LoadSaveButton_pressed() {
        
    }
    
    private void _on_MainMenuButton_pressed() {
        GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu/MainMenu.tscn");
    }

    private void _on_QuitButton_pressed() {
        GetTree().Quit();
    }
}
