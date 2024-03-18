using Godot;
using System;

namespace MVM23;

// Paradigm and implementation taken from Fornclake / Los Alamos Steam Lab
// https://lasteamlab.com/documentation/game-design/godot/RPG/lessons/10-heart-ui.html
// https://www.youtube.com/watch?v=F_5BrQzIUdc

public partial class PlayerHealth : CanvasLayer {
    private const int HeartRowSize = 6;
    private const int HeartPixelOffset = 18;

   
    // What seems simplest is to instantiate
    // heart sprites dynamically based on health, but that seems
    // too inefficient. Maybe if we used Draw instead of sprites.
    //
    // TODO: maybe just call AddHeart from the health upgrade script.
    private const int MORE_THAN_MAX_POSSIBLE_HEARTS = 100;

    private Player _player;
    private Sprite2D _baseHeart;
    private Node _hearts;

    public override void _Ready() {
        _player = GetNode<Player>("/root/Game/Player");
        _hearts = GetNode<Node>("Hearts");
        _baseHeart = GetNode<Sprite2D>("baseHeart");
        _baseHeart.Visible = false;
            
        for (var i = 0; i < MORE_THAN_MAX_POSSIBLE_HEARTS; i++) {
            AddHeart();
        }
    }

    public override void _Process(double delta) {
        var numHearts = _player.CurrentHealth; 
        var i = 0;
        foreach (var node in _hearts.GetChildren()) {
            var heart = (Node2D)node;
            
            var xPos = (i % HeartRowSize) * HeartPixelOffset + HeartPixelOffset / 2;
            var yPos = (i / HeartRowSize) * HeartPixelOffset + HeartPixelOffset / 2;
            heart.Position = new Vector2(xPos, yPos);
            heart.Visible = i < numHearts;

            i++;
        }
    }

    public void AddHeart() {
        var newHeart = new Sprite2D();
        newHeart.Texture = _baseHeart.Texture;
        newHeart.Hframes = _baseHeart.Hframes;
        _hearts.AddChild(newHeart);
        newHeart.Visible = true;
    }
}
