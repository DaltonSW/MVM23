using Godot;
using System;
using System.Collections.Generic;
using MVM23.Scripts;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// Credits:
// https://www.youtube.com/watch?v=QEHOiORnXIk - Shoutouts to Jon Topielski for the world's most efficient tutorial

public class Dialogue
{
    public string Name { get; set;  }
    public List<string> Paragraphs { get; set;  }

    public Dialogue()
    {
        // Needs to exist for YAML Deserialization and to not have angry squiggles
    }

    public Dialogue(string name, List<string> paragraphs)
    {
        Name = name;
        Paragraphs = paragraphs;
    }
}

public class Conversation
{
    public List<Dialogue> Dialogues { get; set; }
    public string Description { get; set; }

    public Conversation()
    {
        // Needs to exist for YAML Deserialization and to not have angry squiggles
    }

    public Conversation(List<Dialogue> dialogues, string description)
    {
        Dialogues = dialogues;
        Description = description;
    }
}

public partial class Textbox : CanvasLayer
{
    private enum State
    {
        Ready,    // Neutral state. Not printing, ready to go
        Printing, // Currently printing text
        Finished, // Finished printing, awaiting for input
        Empty     // Out of dialogue to print. Should close the box  
    }

    private State _currentState = State.Ready;
    
    private Conversation _conversation;
    
    private Dialogue _currentDialogue;
    
    private string _curParagraph;
    
    private Label _nameLabel;
    private Label _endLabel;

    private TextField _textField;

    private bool _paragraphIsPrinting;
    
    private double _endLabelTimeVisible;

    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {       
        // TODO: Parse conversation
        // _conversation = new Conversation(new List<Dialogue>()
        // {
        //     new ("Dalton", new List<string> { "[b]Wow [/b] that's a line", "And here's [i]another [/i] one!" }),
        //     new ("Brandon", new List<string> { "[green]Yeah [/green] those sure [red]are [/red]!" }),
        //     new ("Jasper", new List<string> { "[small]bark bark[/small]", "Bark bark [big]Bark BARK[/big]", "[b][big]woof woof[/big][/b]" })
        // },
        //     "Testing");

        _conversation = LoadConversation("sample");

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
        switch(_currentState)
        {
            case State.Empty:
                Visible = false;
                return;
            case State.Printing:
                // TODO: Fix "hit action to insta-advance"
                // if (Input.IsActionJustPressed("ui_accept"))
                // {
                //     _textField.FinishCurrentPrint();
                //     _endLabel.Visible = true;
                //     ChangeState(State.Finished);
                //     return;    
                // }
                return;
                
            case State.Ready:
                if (!LoadMoreDialogue())
                {
                    ChangeState(State.Empty);
                    return;
                }
                _textField.DrawParagraph(_curParagraph, ThemeConsts.DefaultDrawMode);
                _nameLabel.Text = _currentDialogue.Name;
                _nameLabel.Visible = true;
                ChangeState(State.Printing);
                return;
            case State.Finished:
                if (Input.IsActionJustPressed("ui_accept"))
                {
                    ChangeState(State.Ready);
                    _endLabel.Visible = false;
                    return;
                }
                _endLabelTimeVisible += delta;
                var pos = _endLabel.Position;
                pos.Y += (float)Math.Sin(_endLabelTimeVisible * 7) * 0.1F;
                _endLabel.Position = pos;
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static Conversation LoadConversation(string filename)
    {
        var deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        
        var conversation = deserializer.Deserialize<Conversation>(FileAccess.GetFileAsString($"res://Dialogue/{filename}.yaml"));
        return conversation;
    }

    private void WhenTextFinished()
    {
        _endLabel.Visible = true;
        ChangeState(State.Finished);
    }

    private bool LoadMoreDialogue()
    {
        if (_currentDialogue == null)
        {
            if (_conversation.Dialogues.Count <= 0) return false;
            _currentDialogue = _conversation.Dialogues[0];
            _conversation.Dialogues.RemoveAt(0);
            _curParagraph = _currentDialogue.Paragraphs[0];
            _currentDialogue.Paragraphs.RemoveAt(0);
            return true;
        }
        
        if (_currentDialogue.Paragraphs.Count > 0)
        {
            _curParagraph = _currentDialogue.Paragraphs[0];
            _currentDialogue.Paragraphs.RemoveAt(0);
            return true;
        }

        if (_conversation.Dialogues.Count <= 0) return false;

        _currentDialogue = _conversation.Dialogues[0];
        _conversation.Dialogues.RemoveAt(0);
        _curParagraph = _currentDialogue.Paragraphs[0];
        _currentDialogue.Paragraphs.RemoveAt(0);
        return true;

    }

    private void ChangeState(State newState)
    {
        // GD.Print($"Changing from {_currentState} to {newState}");
        _currentState = newState;
    }
}
