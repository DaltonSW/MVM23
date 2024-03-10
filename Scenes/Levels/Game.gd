# This is the main script of the game. It manages the current map and some other stuff.
extends "res://addons/MetroidvaniaSystem/Template/Scripts/MetSysGame.gd"

const SaveManager = preload("res://addons/MetroidvaniaSystem/Template/Scripts/SaveManager.gd")
const SAVE_PATH = "user://example_save_data.sav"

@export var starting_map: String

# Called when the node enters the scene tree for the first time.
func _ready():
    # A trick for static object reference (before static vars were a thing).
    get_script().set_meta(&"singleton", self)
    
    MetSys.reset_state()
    set_player($Player)
    
    if FileAccess.file_exists(SAVE_PATH):
        var save_manager := SaveManager.new()
        save_manager.load_from_text(SAVE_PATH)
        
        var loaded_starting_map: String = save_manager.get_value("current_room")
        if not loaded_starting_map.is_empty(): # Some compatibility problem.
            starting_map = loaded_starting_map
    else:
        MetSys.set_save_data()
    
    room_loaded.connect(init_room, CONNECT_DEFERRED)
    
    load_room(starting_map)
    
    add_module("RoomTransitions.gd")
    


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    pass

func init_room():
    MetSys.get_current_room_instance().adjust_camera_limits($Player/Camera2D)
    # player.on_enter()
