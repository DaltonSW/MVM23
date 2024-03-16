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
            
            // Boss Doors
            { "Boss1Entrance", false },
            { "Boss1Exit", false },
            { "Boss2Entrance", false },
            { "Boss2Exit", false },
            { "Boss3Entrance", false },
            { "Boss3Exit", false },
            
            // Key Doors
            { "DoorToWorldTwo", false },
            { "WorldTwoBossDoor", false },
            { "WorldThreeDoorOne", false },
            { "WorldThreeDoorTwo", false },
            { "DoubleDashDoor", false },

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
            
            // Health Upgrades
            {"Health1", false},
            {"Health2", false},
            {"Health3", false},
            {"Health4", false},
            {"Health5", false},
            {"Health6", false},
            {"Health7", false},
            {"Health8", false},
            {"Health9", false},
            {"Health10", false},
            {"Health11", false},
            {"Health12", false},
            {"Health13", false},

            // Testing
            { "TestLever", false },
            { "TestDoor1", false },
            { "TestDoor2", false }
        };
    }

    public void SetObjectAsActivated(string objectID) {
        WorldObjects[objectID] = true;
        switch (objectID) {
            case "KeyToWorldTwo":
                WorldObjects["DoorToWorldTwo"] = true;
                break;
            case "WorldTwoBossKey":
                WorldObjects["WorldTwoBossDoor"] = true;
                break;
            case "WorldThreeKeyOne":
                WorldObjects["WorldThreeDoorOne"] = true;
                break;
            case "WorldThreeKeyTwo":
                WorldObjects["WorldThreeDoorTwo"] = true;
                break;
            case "DoubleDash":
                WorldObjects["DoubleDashDoor"] = true;
                break;
        }
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

    public void SetRespawnLocation(Vector2 respawnLocation) {
        GlobalRespawnLocation = respawnLocation;
    }

    public void Save() {
        _game.Call("save_game");
        _currentRoom = _game.Call("get_room_name").AsString();
    }
}
