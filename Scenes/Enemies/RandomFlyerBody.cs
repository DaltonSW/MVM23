using Godot;
using System;

public partial class RandomFlyerBody : CharacterBody2D
{
    public XDirectionManager XDirMan { get; private set; }
    
    public override void _Ready()
    {
        XDirMan = GetNode<XDirectionManager>("XDirectionManager");
    }
}
