using Godot;

public partial class MainMenu : Control {
    private Sprite2D _creditsSprite;

    private Button _newGameButton;
    private Button _loadGameButton;
    private Button _creditsButton;
    private Button _quitButton;
    private Button _closeCreditsButton;

    private GodotObject _fileManager;
    
    public override void _Ready() {
        _creditsSprite = GetNode<Sprite2D>("Credits");

        _fileManager = GetNode<GodotObject>("FileManager");
        
        _newGameButton = GetNode<Button>("HBoxContainer/NewGameButton");
        _loadGameButton = GetNode<Button>("HBoxContainer/LoadGameButton");
        _creditsButton = GetNode<Button>("HBoxContainer/CreditsButton");
        _quitButton = GetNode<Button>("HBoxContainer/QuitButton");
        _closeCreditsButton = GetNode<Button>("CloseCreditsButton");

        _loadGameButton.Disabled = !_fileManager.Call("does_save_file_exist").As<bool>();
    }

    public override void _Process(double _delta) {

    }

    private void _on_NewGameButton_pressed() {
        if (_fileManager.Call("does_save_file_exist").As<bool>())
            _fileManager.Call("delete_save_file");
        GetTree().ChangeSceneToFile("res://Scenes/UI/Intro/Intro.tscn");
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
        _closeCreditsButton.Disabled = false;
        _closeCreditsButton.Visible = true;
    }
    
    private void _on_close_credits_button_pressed()
    {
        _creditsSprite.Visible = false;
        _newGameButton.Disabled = false;
        _loadGameButton.Disabled = !_fileManager.Call("does_save_file_exist").As<bool>();
        _creditsButton.Disabled = false;
        _quitButton.Disabled = false;
        _closeCreditsButton.Disabled = true;
        _closeCreditsButton.Visible = false;
    }
}



