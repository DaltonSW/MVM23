using Godot;

namespace MVM23;

public partial class WorldStateManager : Node {
    public Godot.Collections.Dictionary<string, bool> WorldObjects { get; set; }

    public override void _Ready() {
        // New stateful things should be added in format "<World>/<Object>/<Specifier>"
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

    public Godot.Collections.Dictionary<string, bool> GetWorldObjects() {
        return WorldObjects;
    }
}