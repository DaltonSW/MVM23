using Godot;
using System;
using System.Collections.Generic;

namespace MVM23;

public partial class SlimeBoss : CharacterBody2D, IHittable {
    
    private enum State {
        Idle,
        Jumping,
        JumpSquat,
        Death,
        Dialogue
    }

    private enum Difficulty {
        Easy,
        Medium,
        Hard
    }

    private Door _entranceDoor;
    private Door _exitDoor;
    
    private readonly static Dictionary<Difficulty, (string, string)> DoorMapping = new()
    {
        {Difficulty.Easy, ("Boss1Entrance", "Boss1Exit")},
        {Difficulty.Medium, ("Boss2Entrance", "Boss2Exit")},
        {Difficulty.Hard, ("Boss3Entrance", "Boss3Exit")}
    };

    private readonly static Dictionary<Difficulty, (string, string)> DialogueMapping = new()
    {
        {Difficulty.Easy, ("Boss1PreFight", "Boss1PostFight")},
        {Difficulty.Medium, ("Boss2PreFight", "Boss2PostFight")},
        {Difficulty.Hard, ("Boss3PreFight", "Boss3PostFight")}
    };
    
    [Export] private Difficulty _difficulty = Difficulty.Easy;

    private Random _random;

    private AnimatedSprite2D _sprite;
    private HitManager _hitManager;
    private WorldStateManager _worldStateManager;
    
    // Health properties
    [Export] private int _maxHealth = 5;
    [Export] private int _currentHealth;
    
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
    private PackedScene _textboxScene;

    public override void _Ready() {
        _state = State.Dialogue;
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        _worldStateManager = GetNode<WorldStateManager>("/root/Game/WSM");
        
        _projectileScene = GD.Load<PackedScene>("res://Scenes/Enemies/Boss/boss_projectile.tscn");
        _textboxScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/textbox/textbox.tscn");

        _hitManager = new HitManager(this, _maxHealth + _maxHealth * (int)_difficulty, _sprite);

        _random = new Random();
        
        _gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
        _jumpSpeed = (float)Math.Sqrt(2 * JumpHeight * _gravity);
        _completedIdleLoops = 0;
        _sprite.Play("idle");
        _canFlip = true;
        
        _entranceDoor = GetNode<Door>($"../{DoorMapping[_difficulty].Item1}");
        _exitDoor = GetNode<Door>($"../{DoorMapping[_difficulty].Item2}");
        
    }
    
    public void TakeHit(Vector2 initialKnockbackVelocity)
    {
        _hitManager.TakeHit(initialKnockbackVelocity);
    }
    
    public void QueueDeath()
    {
        _worldStateManager.SetObjectAsActivated(DoorMapping[_difficulty].Item1);
        _worldStateManager.SetObjectAsActivated(DoorMapping[_difficulty].Item2);
        _entranceDoor.QueueFree();
        _exitDoor.QueueFree();
        SpawnTextbox(DialogueMapping[_difficulty].Item2);
        QueueFree();
    }

    public bool DeathQueued() => IsQueuedForDeletion();


    public override void _PhysicsProcess(double delta) {
        var velocity = Velocity;

        _hitManager._PhysicsProcess(delta);

        switch (_state) {
            case State.Dialogue:
                SpawnTextbox(DialogueMapping[_difficulty].Item1);
                _state = State.Idle;
                break;
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
        EnemyUtils.HitCollideeIfApplicable(this, collision, 200f);
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
    
    private void SpawnTextbox(string dialogue) {
        var textbox = _textboxScene.Instantiate<Textbox>();
        textbox.DialogueID = dialogue;
        GetParent().AddChild(textbox);
        GetTree().Paused = true;
    }
}
