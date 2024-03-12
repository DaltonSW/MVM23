extends Node2D

# Called when the node enters the scene tree for the first time.
func _ready():
    if MetSys.register_storable_object(self, queue_free):
        return

func open():
    MetSys.store_object(self)
    queue_free()
