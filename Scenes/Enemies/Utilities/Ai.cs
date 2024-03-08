using Godot;
using System;
using System.Linq;

public interface IAi
{
    void _PhysicsProcess(double delta) {}
    XDirection NextXDirection(XDirection current) => current;

    float FootSpeed();
}

/// Checks a line of sight every physics frame
/// to find a target.
/// Combines two other AIs, one for before a target
/// is acquired, and one for after.
public class NoticeTarget : IAi
{
    private readonly Area2D _lineOfSight;
    private readonly IAi _noTarget;
    private readonly Func<Node2D, IAi> _createBehaviorWithTarget;

    private IAi _activeAi;

    public NoticeTarget(Area2D lineOfSight, IAi noTarget, Func<Node2D, IAi> createBehaviorWithTarget)
    {
        _lineOfSight = lineOfSight;
        _noTarget = noTarget;
        _createBehaviorWithTarget = createBehaviorWithTarget;

        _activeAi = _noTarget;
    }

    public void _PhysicsProcess(double delta)
    {
        if (_activeAi == _noTarget)
        {
            // Find target
            var maybeTarget = _lineOfSight.GetOverlappingBodies()
                .FirstOrDefault(body => body.IsInGroup("enemy_target"), null);
            if (maybeTarget is Node2D target)
            {
                _activeAi = _createBehaviorWithTarget(target);
            }
        }
        _activeAi._PhysicsProcess(delta);
    }

    public XDirection NextXDirection(XDirection current) => _activeAi.NextXDirection(current);
    public float FootSpeed() => _activeAi.FootSpeed();
}

/// Moves slowly, switching directions when it sees an edge.
public class Patrol : IAi
{
    private RayCast2D _edgeDetector;

    public Patrol(RayCast2D edgeDetector)
    {
        _edgeDetector = edgeDetector;
    }

    public XDirection NextXDirection(XDirection current) =>
        EdgeAhead()
            ? current.Opposite()
            : current;

    private bool EdgeAhead() => !_edgeDetector.IsColliding();

    public float FootSpeed() => 800;
}

/// Wraps another AI to override its movement
/// so it stops when a drop is detected.
/// All other calls are forwarded/delegated to the wrapped AI.
public class AvoidDrops : IAi
{
    private RayCast2D _dropDetector;
    private IAi _nextPriority;

    public AvoidDrops(RayCast2D dropDetector, IAi nextPriority)
    {
        _dropDetector = dropDetector;
        _nextPriority = nextPriority;
    }

    public float FootSpeed() =>
        DropAhead()
            ? 0
            : _nextPriority.FootSpeed();

    private bool DropAhead() => !_dropDetector.IsColliding();

    public XDirection NextXDirection(XDirection current) =>
        _nextPriority.NextXDirection(current);

    public void _PhysicsProcess(double delta)
    {
        _nextPriority._PhysicsProcess(delta);
    }
}

