# This is the main script of the game. It manages the current map and some other stuff.
extends "res://addons/MetroidvaniaSystem/Template/Scripts/MetSysGame.gd"
class_name Game

const SaveManager = preload("res://addons/MetroidvaniaSystem/Template/Scripts/SaveManager.gd")
const SAVE_PATH = "user://example_save_data.sav"

var pause_menu

@export var starting_map: String

# Called when the node enters the scene tree for the first time.
func _ready():
    # A trick for static object reference (before static vars were a thing).
    get_script().set_meta(&"singleton", self)
    
    room_loaded.connect(init_room, CONNECT_DEFERRED)    
    
    load_game()
    
    add_module("RoomTransitions.gd")

    pause_menu = get_node("UI/PauseMenu")
    pause_menu.visible = false
    

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    if Input.is_action_just_pressed("pause"):
        toggle_pause()
    pass

func init_room():
    MetSys.get_current_room_instance().adjust_camera_limits($Player/Camera2D)
    
func load_game():
    MetSys.reset_state()
    set_player($Player)
    
    if FileAccess.file_exists(SAVE_PATH):
        var save_manager := SaveManager.new()
        save_manager.load_from_text(SAVE_PATH)
        
        $Player.Abilities = save_manager.get_value("player_abilities")
        $Player.MaxHealth = save_manager.get_value("player_max_health")
        
        $WSM.WorldObjects = save_manager.get_value("world_objects")
        $WSM.CurrentCheckpointID = save_manager.get_value("current_checkpoint")
        $WSM.GlobalRespawnLocation = save_manager.get_value("global_respawn_location")
        
        $Player.global_position = $WSM.GlobalRespawnLocation    
        
        var loaded_starting_map: String = save_manager.get_value("current_room")
        if not loaded_starting_map.is_empty(): # Some compatibility problem.
            starting_map = loaded_starting_map
    else:
        MetSys.set_save_data()
    
    load_room(starting_map)
    
func save_game():
    var save_manager := SaveManager.new()
    
    save_manager.set_value("current_room", MetSys.get_current_room_name())
    
    save_manager.set_value("player_abilities", $Player.Abilities)
    save_manager.set_value("player_max_health", $Player.MaxHealth)
    
    save_manager.set_value("world_objects", $WSM.WorldObjects)
    save_manager.set_value("current_checkpoint", $WSM.CurrentCheckpointID)
    save_manager.set_value("global_respawn_location", $WSM.GlobalRespawnLocation)
    
    save_manager.save_as_text(SAVE_PATH)

func toggle_pause():
    pause_menu.visible = !pause_menu.visible
    get_tree().paused = !get_tree().paused

func get_room_name():
    return MetSys.get_current_room_name()
