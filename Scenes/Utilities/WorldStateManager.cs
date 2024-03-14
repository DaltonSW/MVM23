using Godot;

namespace MVM23;

public partial class WorldStateManager : Node {
    
    private Godot.Collections.Dictionary<string, bool> WorldObjects { get; set; }
    private Godot.Collections.Dictionary<string, bool> PlayerAbilities { get; set; }
    
    public string RoomToLoad { get; set; }
    
    private GodotObject _game;

    public override void _Ready() {
        // New stateful things should be added in format "<World>/<Object>/<Specifier>"
        _game = GetNode<GodotObject>("/root/Game");
        
        WorldObjects = new Godot.Collections.Dictionary<string, bool>
        {
            // Doors
            { "World1/Door/CrossroadsRight", false },
            { "World1/Door/BigRoomBottomLeft", false },
            { "World1/Door/BigRoomTopRight", false },

            // Levers
            { "World1/Lever/Crossroads", false },

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

    public void SaveCurrentRoom() {
        RoomToLoad = _game.Call("get_room_name").AsString();
    }

    public Godot.Collections.Dictionary<string, bool> GetWorldObjects() {
        return WorldObjects;
    }
}