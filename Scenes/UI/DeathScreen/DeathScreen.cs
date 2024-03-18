using Godot;

public partial class DeathScreen : Control {

    private Button _loadSaveButton;
    private Button _mainMenuButton;
    
    public override void _Ready() {
        
        _loadSaveButton = GetNode<Button>("HBoxContainer/LoadGameButton");
        _mainMenuButton = GetNode<Button>("HBoxContainer/QuitButton");
    }
    
    private void _on_LoadSaveButton_pressed() {
        GetTree().ChangeSceneToFile("res://Scenes/Levels/Game.tscn");
    }

    private void _on_MainMenuButton_pressed() {
        GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu/MainMenu.tscn");
    }
}



