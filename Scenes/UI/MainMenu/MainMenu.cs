using Godot;

public partial class MainMenu : Control {
    private Sprite2D _creditsSprite;

    private Button _newGameButton;
    private Button _loadGameButton;
    private Button _creditsButton;
    private Button _quitButton;

    private GodotObject _fileManager;
    
    public override void _Ready() {
        _creditsSprite = GetNode<Sprite2D>("Credits");

        _fileManager = GetNode<GodotObject>("FileManager");
        
        _newGameButton = GetNode<Button>("HBoxContainer/NewGameButton");
        _loadGameButton = GetNode<Button>("HBoxContainer/LoadGameButton");
        _creditsButton = GetNode<Button>("HBoxContainer/CreditsButton");
        _quitButton = GetNode<Button>("HBoxContainer/QuitButton");

        if (_fileManager.Call("does_save_file_exist").As<bool>())
            _loadGameButton.Icon = _fileManager.Call("get_save_available_texture").As<Texture2D>();
        else {
            GD.Print("No save file found!");
        }
    }

    public override void _Process(double delta) {
        if (!_creditsSprite.Visible ||
            (!Input.IsActionJustPressed("pause") && !Input.IsActionJustPressed("close_menu"))) return;
    }

    private void _on_NewGameButton_pressed() {
        if (_fileManager.Call("does_save_file_exist").As<bool>())
            _fileManager.Call("delete_save_file");
        GetTree().ChangeSceneToFile("res://Scenes/Levels/Game.tscn");
    }
    
    private void _on_LoadGameButton_pressed() {
        if (_fileManager.Call("does_save_file_exist").As<bool>())
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
