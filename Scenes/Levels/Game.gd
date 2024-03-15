# This is the main script of the game. It manages the current map and some other stuff.
extends "res://addons/MetroidvaniaSystem/Template/Scripts/MetSysGame.gd"
class_name Game

var pause_menu = preload("res://Scenes/UI/PauseMenu/PauseMenu.tscn")

const SaveManager = preload("res://addons/MetroidvaniaSystem/Template/Scripts/SaveManager.gd")
const SAVE_PATH = "user://example_save_data.sav"

@export var starting_map: String

# Called when the node enters the scene tree for the first time.
func _ready():
    # A trick for static object reference (before static vars were a thing).
    get_script().set_meta(&"singleton", self)
    
    room_loaded.connect(init_room, CONNECT_DEFERRED)    
    
    load_game()
    
    add_module("RoomTransitions.gd")
    

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    if Input.is_action_just_pressed("pause"):
        show_pause_menu()
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
        
        if $Player.Abilities["Dash"]:
            $Player.MaxDashes = 1
        if $Player.Abilities["DoubleDash"]:
            $Player.MaxDashes = 2
        $Player.DashesAvailable = $Player.MaxDashes
        
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

func show_pause_menu():
    var instance = pause_menu.instantiate()
    $UI.add_child(instance)

func get_room_name():
    return MetSys.get_current_room_name()
