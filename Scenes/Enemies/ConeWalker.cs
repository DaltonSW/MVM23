using Godot;

public partial class ConeWalker : CharacterBody2D
{

    private AnimatedSprite2D _sprite;
    private XDirectionManager _xDirMan;

    private float _gravity;

    private IAi _ai;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var lineOfSight = GetNode<Area2D>("Shapes/LineOfSight");
        _sprite = GetNode<AnimatedSprite2D>("Shapes/AnimatedSprite2D");
        var dropAheadRayCast = GetNode<RayCast2D>("Shapes/DropAheadRayCast");
        var edgeAheadRayCast = GetNode<RayCast2D>("Shapes/EdgeAheadRayCast");
        _xDirMan = GetNode<XDirectionManager>("XDirectionManager");

        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

        _ai = new AvoidDrops(dropAheadRayCast,
                new NoticeTarget(lineOfSight,
                    new Patrol(edgeAheadRayCast),
                    target => new Chase(this, target)));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        ChangeAnimation(
            Velocity.IsZeroApprox()
                ? "idle"
                : "walk");
    }

    public override void _PhysicsProcess(double delta)
    {
        _ai._PhysicsProcess(delta);
        _xDirMan.Direction = _ai.NextXDirection(_xDirMan.Direction);
        Velocity = CalcVelocity(delta);
        MoveAndSlide();
    }

    private Vector2 CalcVelocity(double delta)
    {
        var velocity = Velocity;

        // fake friction
        velocity.X = 0;

        if (IsOnFloor())
        {
            // foot
            velocity += (float)delta * _ai.FootSpeed() * _xDirMan.Direction.UnitVector();
        }

        // gravity
        velocity += (float)delta * _gravity * Vector2.Down;

        return velocity;
    }

    // TODO: reuse with Player's
    private void ChangeAnimation(string animation) {
        if (_sprite.Animation != animation)
            _sprite.Play(animation);
    }

    public void _Hurt()
    {
        if (!IsQueuedForDeletion())
            QueueFree();
    }

}

/// Moves quickly, always switching directions toward a given target.
public class Chase : IAi
{
    private Node2D _self;
    private Node2D _target;

    public Chase(Node2D self, Node2D target)
    {
        _self = self;
        _target = target;
    }
    
    public XDirection NextXDirection(XDirection current) =>
        _self.XDirectionTo(_target);

    public float FootSpeed() => 6000;
}

