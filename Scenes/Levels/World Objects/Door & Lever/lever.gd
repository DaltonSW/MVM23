extends Area2D

const Door = preload("res://Scenes/Levels/World Objects/Door & Lever/door.gd")

@export var associated_ID: String

var associated_object
var player

# Called when the node enters the scene tree for the first time.
func _ready():
    associated_object = get_node("../%s" % associated_ID)
    player = get_node("../../Player")
    
    if MetSys.register_storable_object(self, queue_free):
        return

func flip():
    MetSys.store_object(self)
    associated_object.open()
    queue_free()

func _process(delta):
    if overlaps_body(player) and Input.is_action_just_pressed("interact"):
        pass
