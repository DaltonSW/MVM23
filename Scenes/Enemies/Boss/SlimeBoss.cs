using Godot;
using System;

namespace MVM23;

public partial class SlimeBoss : CharacterBody2D {
    
    private enum State {
        Idle,
        Jumping,
        JumpSquat,
        Death
    }

    private enum Difficulty {
        Easy,
        Medium,
        Hard
    }

    [Export] private Difficulty _difficulty = Difficulty.Easy;

    private Random _random;

    private AnimatedSprite2D _sprite;
    
    // Health properties
    [Export] private float _maxHealth = 100;
    private float _currentHealth;
    
    // Attack properties
    [Export] public int MinIdleLoopsBeforeJump = 4;
    [Export] public int PercentChanceJump = 80;
    private int _completedIdleLoops;
    
    // Jump properties
    [Export] public float JumpHeight = 120;  //pixels
    [Export] public float TimeInAir = 0.18F; //honestly no idea
    [Export] public float JumpSpeedHoriz = 350F;
    private float _jumpSpeed;
    private float _gravity;

    private State _state;
    private bool _isFacingLeft;
    private bool _canFlip;

    private PackedScene _projectileScene;

    public override void _Ready() {
        _state = State.Idle;
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        
        _projectileScene = GD.Load<PackedScene>("res://Scenes/Enemies/Boss/boss_projectile.tscn");


        _random = new Random();
        
        _gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
        _jumpSpeed = (float)Math.Sqrt(2 * JumpHeight * _gravity);
        _completedIdleLoops = 0;
        _sprite.Play("idle");
        _canFlip = true;
    }


    public override void _PhysicsProcess(double delta) {
        var velocity = Velocity;

        switch (_state) {
            case State.Idle:
                IdleState();
                break;
            case State.Jumping:
                velocity = JumpingState(velocity, delta);
                break;
            case State.JumpSquat:
                if (_sprite.IsPlaying()) break;
                
                _state = State.Jumping;
                velocity = StartJump(velocity);
                _sprite.Play("jump");
                Shoot();
                break;
            case State.Death:
                if (!_sprite.IsPlaying())
                    QueueFree();
                break;
        }

        Velocity = velocity;
        
        var collision = MoveAndCollide(Velocity * (float)delta);
        if (collision == null) return;
        var normal = collision.GetNormal();
        
        if (normal == Vector2.Up)
        {
            if (_state == State.Jumping) {
                _state = State.Idle;
                _sprite.Play("idle");
                _completedIdleLoops = 0;
                if (_difficulty == Difficulty.Hard) Shoot(); 
            }
            Velocity = new Vector2(0, 0);
            _canFlip = true;
        }

        else if (normal == Vector2.Left || normal == Vector2.Right)
        {
            if (!_canFlip) return;
            _isFacingLeft = !_isFacingLeft;
            _sprite.FlipH = !_sprite.FlipH;
            _canFlip = false;
            Velocity = Velocity.Bounce(normal);
        }

        else
        {
            Velocity = Velocity.Bounce(normal);
        }
    }

    private void IdleState() {
        if (_sprite.IsPlaying()) return;
        
        _completedIdleLoops += 1;
        
        if (_completedIdleLoops >= MinIdleLoopsBeforeJump) {
            if (_random.Next(0, 100) >= PercentChanceJump + _completedIdleLoops - MinIdleLoopsBeforeJump) return;
                
            _completedIdleLoops = 0;
            _sprite.Play("jumpsquat");
            _state = State.JumpSquat;
            return;
        }
        _sprite.Play("idle");
    }
    
    private Vector2 JumpingState(Vector2 velocity, double delta) {
        velocity.Y += _gravity * (float)delta;

        if (IsOnFloor()) {
            _state = State.Idle;
            _canFlip = true;
            _sprite.Play("idle");
            velocity.X = 0;

            return velocity;
        }
        
        if (!IsOnWall()) return velocity;
        if (!_canFlip) return velocity;
            
        _canFlip = false;
        velocity.X *= -1;
        _isFacingLeft = !_isFacingLeft;
        _sprite.FlipH = !_sprite.FlipH;
        return velocity;
    }

    private Vector2 StartJump(Vector2 velocity) {
        var dir = _isFacingLeft ? -1 : 1;
        return new Vector2(velocity.X + JumpSpeedHoriz * dir, velocity.Y - _jumpSpeed);
    }
    
    private void Shoot() {
        if (_difficulty == Difficulty.Easy) return;
        
        foreach (var node in GetTree().GetNodesInGroup("spawns"))
        {
            var point = (Marker2D)node;
            var projectile = (BossProjectile)_projectileScene.Instantiate();
            projectile.GlobalPosition = point.GlobalPosition;
            projectile.GlobalRotation = point.GlobalRotation;
            GetParent().AddChild(projectile);
        }
    }

}
