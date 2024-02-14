namespace MVM23.Scripts;

using Godot;
using System;
using System.Collections.Generic;
using static Utilities.Tokenizer;

public partial class TextField : Panel
{
private const string TestString = "The [red]quick [green]brown[/green][/red][big]fox [blue]jumps over the[/blue][/big] [small]lazy[green] dog[/green][/small].";

    private readonly List<Token> _visibleTokens = new();
    private List<Token> _remainingTokens = new();

    private Token _currentToken;

    private Font _font;
    private Label _endLabel;

    private bool _textRemaining;
    private const float AlphaIncrement = 0.015F;
    private float _curAlpha;
    
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _font = ThemeDB.FallbackFont;
        _endLabel = GetNode<Label>("../../Label");
        _endLabel.Visible = false;

        _remainingTokens = TokenizeString(TestString);

        _currentToken = new Token("null", TokenType.Undefined);

        if (_remainingTokens.Count > 0)
        {
            _textRemaining = true;
        }
    }

    public override void _Draw()
    {
        // var curLine = 1;
        var curLinePos = 0F;
        var height = _font.GetHeight(20);
        foreach (var visToken in _visibleTokens)
        {
            DrawString(_font, new Vector2(curLinePos, height), visToken.Text, HorizontalAlignment.Left, -1F, visToken.FontSize, visToken.Color);
            curLinePos += _font.GetStringSize(visToken.Text, HorizontalAlignment.Left, -1F, visToken.FontSize).X;
        }

        if (_currentToken.Type == TokenType.Undefined)
        {
            if (_remainingTokens.Count > 0)
            {
                _currentToken = _remainingTokens[0];
                _remainingTokens.RemoveAt(0);    
                _curAlpha = 0F;
            }
            else
            {
                _textRemaining = false;
                _endLabel.Visible = true;
                return;
            }
        }
        
        var curTokenColor = _currentToken.Color;
        curTokenColor.A = (float)Math.Min(_curAlpha + AlphaIncrement, 1.0);
        _curAlpha = curTokenColor.A;
        DrawString(_font, new Vector2(curLinePos, height), _currentToken.Text, HorizontalAlignment.Left, -1F, _currentToken.FontSize, curTokenColor);
        _currentToken.Color = curTokenColor;

        if (_curAlpha < 1.0F) return;
        
        _visibleTokens.Add(_currentToken);
        _currentToken = new Token("null", TokenType.Undefined);
    }



    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (_textRemaining)
        {
            QueueRedraw();
        }
    }
}
