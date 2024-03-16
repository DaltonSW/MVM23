using Godot;
// ReSharper disable MemberCanBePrivate.Global

namespace MVM23;

public partial class WorldStateManager : Node {

    [Export] public Godot.Collections.Dictionary<string, bool> WorldObjects { get; set; }
    
    public string CurrentCheckpointID;
    public Vector2 GlobalRespawnLocation;
    private string _currentRoom;
    
    private GodotObject _game;
    private Player _player;

    public override void _Ready() {
        _game = GetNode<GodotObject>("/root/Game");
        _player = GetNode<Player>("/root/Game/Player");
        
        // New stateful things should be added in format "<World>/<Object>/<Specifier>"
        // The strings are entirely arbitrary, but that'll prevent overlap
        WorldObjects = new Godot.Collections.Dictionary<string, bool>
        {
            // Doors
            { "World1/Door/CrossroadsRight", false },
            { "World1/Door/BigRoomBottomLeft", false },
            { "World1/Door/BigRoomTopRight", false },

            // Levers
            { "World1/Lever/Crossroads", false },
            
            // Unlock Items
            { "Stick", false },
            { "Dash", false },
            { "SuperJump", false },
            { "Grapple", false },
            { "DoubleDash", false },
            { "DashOnKill", false },
            { "KeyToWorldTwo", false },
            { "WorldTwoBossKey", false},
            { "WorldThreeKeyOne", false },
            { "WorldThreeKeyTwo", false },

            // Testing
            { "TestLever", false },
            { "TestDoor1", false },
            { "TestDoor2", false }
        };
    }

    public void SetObjectAsActivated(string objectID) {
        WorldObjects[objectID] = true;
    }

    public bool IsObjectActivated(string objectID) {
        return WorldObjects[objectID];
    }

    public void SetCurrentCheckpoint(string checkpointID, Vector2 respawnLocation) {
        CurrentCheckpointID = checkpointID;
        GlobalRespawnLocation = respawnLocation;
    }

    public bool IsCurrentCheckpoint(string checkpointID) {
        if (CurrentCheckpointID == null) return false;
        
        return checkpointID == CurrentCheckpointID;
    }

    public void Save() {
        _game.Call("save_game");
        _currentRoom = _game.Call("get_room_name").AsString();
    }
}
