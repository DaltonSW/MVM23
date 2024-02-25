using Godot;
using System;

public partial class XDirectionManager : Node2D
{
    private Node2D _parent;

    /// The current direction the parent is facing.
    private XDirection _direction;

    public XDirection Direction
    {
        get => _direction;
        set
        {
            if (value != _direction)
            {
                // Flip
                _parent.TransformScale(scale =>
                    scale.MapX(x => x *= -1));
            }
            _direction = value;
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _parent = GetParent<Node2D>();

        // This point should be positioned so that it
        // is in the direction the parent is facing.
        // This should give a good indication regardless
        // of how the parent is initially scaled.
        var pointInDirectionFacing = GetNode<Node2D>("PointInDirectionFacing");
        _direction = _parent.XDirectionTo(pointInDirectionFacing);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
