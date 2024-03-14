using Godot;
using System;

public partial class BossProjectile : CharacterBody2D
{
    private Random _random;

    private Sprite2D _sprite;

    [Export] public int Damage = 1;

    [Export] private int _speed = 400;
    [Export] private int _spread = 15;
    [Export] private int _allowedBounces = 4;
    [Export] private float _disappearTime = 0.12F;
    private int _currentBounces;
    private bool _isDisappearing;
    private double _disappearTimeRemaining;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");

        _random = new Random();
        RotationDegrees += _random.Next(-_spread, _spread);
        Velocity = new Vector2(_speed, 0).Rotated(Rotation);
    }

    public override void _Process(double delta)
    {
        if (_isDisappearing)
        {
            IsDisappearing(delta);
            return;
        }

        var collision = MoveAndCollide(Velocity * (float)delta);

        if (collision != null)
        {
            var collName = collision.GetCollider().Get("name").ToString();
            if (collName.Contains("Player"))
            {
                var player = (Player)collision.GetCollider();
                player.TakeDamage(Damage);
                FreeSelf();
            }
            
            Velocity = Velocity.Bounce(collision.GetNormal());
            _currentBounces += 1;
        }

        _sprite.GlobalRotation = 0;
        if (_currentBounces >= _allowedBounces)
        {
            FreeSelf();
        }
    }

    private void IsDisappearing(double delta) {
        _disappearTimeRemaining -= delta;
        Modulate = new Color(1, 1, 1, (float)(_disappearTimeRemaining / _disappearTime));
        if (_disappearTimeRemaining < 0)
        {
            QueueFree();
        }
    }

    private void FreeSelf()
    {
        _isDisappearing = true;
        _disappearTimeRemaining = _disappearTime;
    }
}
