using Godot;
using System;
using System.Collections.Generic;
using MVM23.Scripts;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// Credits:
// Jon Topielski - https://www.youtube.com/watch?v=QEHOiORnXIk

public class Dialogue {
    public string Name { get; set; }
    public List<string> Paragraphs { get; set; }

    public Dialogue() {
        // Needs to exist for YAML Deserialization and to not have angry squiggles
    }

    public Dialogue(string name, List<string> paragraphs) {
        Name = name;
        Paragraphs = paragraphs;
    }
}

public class Conversation {
    public List<Dialogue> Dialogues { get; set; }
    public string Description { get; set; }

    public Conversation() {
        // Needs to exist for YAML Deserialization and to not have angry squiggles
    }

    public Conversation(List<Dialogue> dialogues, string description) {
        Dialogues = dialogues;
        Description = description;
    }
}

public partial class Textbox : CanvasLayer {
    private enum TextboxState {
        Ready,    // Neutral state. Not printing, ready to go
        Printing, // Currently printing text
        Finished, // Finished printing, awaiting for input
        Empty     // Out of dialogue to print. Should close the box  
    }

    private TextboxState _currentState = TextboxState.Ready;

    private Conversation _conversation;

    private Dialogue _currentDialogue;

    private string _curParagraph;

    private Label _nameLabel;
    private Label _endLabel;

    private TextField _textField;

    private bool _paragraphIsPrinting;

    private double _endLabelTimeVisible;

    [Export] public string DialogueID = "sample";


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        ProcessMode = ProcessModeEnum.Always;

        LoadConversation();
        ThemeConsts.Initialize(); // TODO: Eventually move this to whatever global node we have

        _textField = GetNode<TextField>("ParentBox/Background/InnerBox/TextField");
        _textField.TextFinishedPrinting += WhenTextFinished; // Attach signal listener 

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
    public override void _Process(double delta) {
        switch (_currentState) {
            case TextboxState.Empty:
                Visible = false;
                GetTree().Paused = false;
                QueueFree();
                return;
            case TextboxState.Printing:
                // TODO: Fix "hit action to insta-advance"
                // if (Input.IsActionJustPressed("ui_accept"))
                // {
                //     _textField.FinishCurrentPrint();
                //     _endLabel.Visible = true;
                //     ChangeState(State.Finished);
                //     return;    
                // }
                return;

            case TextboxState.Ready:
                if (!LoadMoreDialogue()) {
                    ChangeState(TextboxState.Empty);
                    return;
                }
                _textField.DrawParagraph(_curParagraph, ThemeConsts.DefaultDrawMode);
                _nameLabel.Text = _currentDialogue.Name;
                _nameLabel.Visible = true;
                ChangeState(TextboxState.Printing);
                return;
            case TextboxState.Finished:
                if (Input.IsActionJustPressed("interact")) {
                    ChangeState(TextboxState.Ready);
                    _endLabel.Visible = false;
                    return;
                }
                _endLabelTimeVisible += delta;
                var pos = _endLabel.Position;
                pos.Y += (float)Math.Sin(_endLabelTimeVisible * 7) * 0.1F;
                _endLabel.Position = pos;
                return;
            default:
                throw new InvalidOperationException();
        }
    }

    private void LoadConversation() {
        var deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();

        var conversation =
            deserializer.Deserialize<Conversation>(
                FileAccess.GetFileAsString($"res://Assets/Dialogue/{DialogueID}.yaml"));
        _conversation = conversation;
    }

    private void WhenTextFinished() {
        _endLabel.Visible = true;
        ChangeState(TextboxState.Finished);
    }

    private bool LoadMoreDialogue() {
        if (_currentDialogue == null) {
            if (_conversation.Dialogues.Count <= 0) return false;
            _currentDialogue = _conversation.Dialogues[0];
            _conversation.Dialogues.RemoveAt(0);
            _curParagraph = _currentDialogue.Paragraphs[0];
            _currentDialogue.Paragraphs.RemoveAt(0);
            return true;
        }

        if (_currentDialogue.Paragraphs.Count > 0) {
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

    private void ChangeState(TextboxState newState) {
        // GD.Print($"Changing from {_currentState} to {newState}");
        _currentState = newState;
    }
}