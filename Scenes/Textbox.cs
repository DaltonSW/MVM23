using Godot;
using System;
using System.Collections.Generic;
using MVM23.Scripts;
using MVM23.Scripts.Utilities;
using static MVM23.Scripts.Utilities.Tokenizer;

public class Dialogue
{
    public string Name { get; set; }
    public List<List<Token>> Paragraphs { get; set; }

    public Dialogue(string name, List<string> paragraphs)
    {
        Name = name;
        Paragraphs = new List<List<Token>>();
        foreach (var paragraph in paragraphs)
        {
            var tokens = TokenizeString(paragraph);
            Paragraphs.Add(tokens);
        }
    }
}

public partial class Textbox : CanvasLayer
{
    private const string TestStringComplex =
        "What...? I have [b]NO [/b] idea what you're talking about! You are out of your [red][big]DAMN [/big][/red]MIND!!! I [i]cannot [/i]believe you're coming to me with these accusations.";
    
    private List<Dialogue> _conversation;
    private Dialogue _currentDialogue;
    
    private Label _nameLabel;
    private Label _endLabel;

    private TextField _textField;

    private bool _paragraphIsPrinting;
    
    private double _endLabelTimeVisible;

    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {       
        ThemeConsts.Initialize(); // TODO: Eventually move this to whatever global node we have

        _textField = GetNode<TextField>("ParentBox/Background/InnerBox/TextField");
        _textField.TextFinishedPrinting += WhenTextFinished;
        
        _nameLabel = GetNode<Label>("ParentBox/Background/SpeakerName");
        _nameLabel.Visible = false;
        _nameLabel.LabelSettings.Font = ThemeConsts.BoldText;
        _nameLabel.LabelSettings.FontSize = ThemeConsts.RegularTextSize;
        
        _endLabel = GetNode<Label>("ParentBox/Background/EndLabel");
        _endLabel.Visible = false;
        _endLabel.LabelSettings.Font = ThemeConsts.BoldText;
        _endLabel.LabelSettings.FontSize = ThemeConsts.RegularTextSize;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!_paragraphIsPrinting)
        {
            _textField.DrawParagraph(TestStringComplex, ThemeConsts.DefaultDrawMode);
            _nameLabel.Text = _currentDialogue.Name;
            _paragraphIsPrinting = true;    
        }
        
        if (!_endLabel.Visible) return;
        _endLabelTimeVisible += delta;
        var pos = _endLabel.Position;
        pos.Y += (float)Math.Sin(_endLabelTimeVisible * 7) * 0.1F;
        _endLabel.Position = pos;
    }

    private void WhenTextFinished()
    {
        _endLabel.Visible = true;
        _paragraphIsPrinting = false;
    }
}
