extends Node

const SAVE_PATH = "user://CultOfTheClosedCircuit.sav"

# Called when the node enters the scene tree for the first time.
func _ready():
    pass # Replace with function body.


func does_save_file_exist():
    return FileAccess.file_exists(SAVE_PATH)
    
func delete_save_file():
    DirAccess.remove_absolute(SAVE_PATH)

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    pass
