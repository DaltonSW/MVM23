# This is the main script of the game. It manages the current map and some other stuff.
extends "res://addons/MetroidvaniaSystem/Template/Scripts/MetSysGame.gd"
class_name Game

var pause_menu = preload("res://Scenes/UI/PauseMenu/PauseMenu.tscn")
var main_font = preload("res://Assets/Fonts/EASVHS.ttf")
var code_font = preload("res://Assets/Fonts/Dogica.otf")

const SaveManager = preload("res://addons/MetroidvaniaSystem/Template/Scripts/SaveManager.gd")
const SAVE_PATH = "user://CultOfTheClosedCircuit.sav"

@export var starting_map: String

# Called when the node enters the scene tree for the first time.
func _ready():
    # A trick for static object reference (before static vars were a thing).
    get_script().set_meta(&"singleton", self)
    
    room_loaded.connect(init_room, CONNECT_DEFERRED)    
    
    load_game()
    
    add_module("RoomTransitions.gd")
    

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
    play_world_music()    
    if Input.is_action_just_pressed("pause"):
        show_pause_menu()
    pass

func init_room():
    MetSys.get_current_room_instance().adjust_camera_limits($Player/Camera2D)

func get_main_font():
    return main_font
    
func get_code_font():
    return code_font

func play_world_music():
    var current_room_name = MetSys.get_current_room_name()
    if "World 1" in current_room_name:
        if "Boss" in current_room_name:
            $AudioManager.play_music("W1_Boss")
        else:
            $AudioManager.play_music("W1")
    elif "World 2" in current_room_name:
        if "Boss" in current_room_name:
            $AudioManager.play_music("W2_Boss")
        else:
            $AudioManager.play_music("W2")
    elif "Virtual" in current_room_name:
        if "Boss" in current_room_name:
            $AudioManager.play_music("W3_Boss")
        else:
            $AudioManager.play_music("W3")

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
    
    load_room_wrapper(starting_map)
    
func load_room_wrapper(new_map):
    load_room(new_map)

func teleport_player(new_map, tele_pos):
    load_room_wrapper(new_map)
    $Player.global_position = tele_pos
   
static func print_hello():
    print("Hello!") 

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
    instance.SetGame(self)
    play_pause_music()

func play_pause_music():
    var current_room_name = MetSys.get_current_room_name()
    if "World 1" in current_room_name:
        $AudioManager.play_music("W1_Pause")
    elif "World 2" in current_room_name:
        $AudioManager.play_music("W2_Pause")
    elif "Virtual" in current_room_name:
        $AudioManager.play_music("W3_Pause")

func get_room_name():
    return MetSys.get_current_room_name()
