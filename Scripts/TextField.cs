using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class TextField : Panel
{
    private enum TokenType
    {
        Undefined,
        Normal,
        Bold,
        Quiet,
        Shaking,
    }
    
    private const string TestString = "The quick brown fox jumps *over* the lazy dog.";

    private readonly List<(TokenType, string)> _visibleTokens = new();
    private readonly List<(TokenType, string)> _remainingTokens = new();

    private (TokenType, string) _currentToken = (TokenType.Undefined, "null");

    private Font _font;
    private Label _endLabel;

    private bool _textRemaining;
    private const float AlphaIncrement = 0.02F;
    private float _curAlpha;
    
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _font = ThemeDB.FallbackFont;
        _endLabel = GetNode<Label>("../../Label");
        _endLabel.Visible = false;
        
        foreach (var word in TestString.Split(' '))
        {
            if (word.Contains('*'))
            {
                _remainingTokens.Add((TokenType.Bold, $"{Regex.Replace(word, "\\*", "")} "));
            }
            else
            {
                _remainingTokens.Add((TokenType.Normal, $"{word} "));
            }
        }

        if (_remainingTokens.Count > 0)
        {
            _textRemaining = true;
        }
    }

    public override void _Draw()
    {
        // var curLine = 1;
        var curLinePos = 0F;
        const int fontSize = 20;
        var height = _font.GetHeight(fontSize);
        foreach (var visToken in _visibleTokens)
        {
            var tokenColor = visToken.Item1 == TokenType.Normal ? Colors.White : Colors.Red;
            DrawString(_font, new Vector2(curLinePos, height), visToken.Item2, HorizontalAlignment.Left, -1F, fontSize, tokenColor);
            curLinePos += _font.GetStringSize(visToken.Item2, HorizontalAlignment.Left, -1F, fontSize).X;
        }

        if (_currentToken.Item1 == TokenType.Undefined)
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
        
        var curTokenColor = _currentToken.Item1 == TokenType.Normal ? Colors.White : Colors.Red;
        curTokenColor.A = (float)Math.Min(_curAlpha + AlphaIncrement, 1.0);
        _curAlpha = curTokenColor.A;
        DrawString(_font, new Vector2(curLinePos, height), _currentToken.Item2, HorizontalAlignment.Left, -1F, fontSize, curTokenColor);

        if (_curAlpha < 1.0F) return;
        
        _visibleTokens.Add(_currentToken);
        _currentToken = (TokenType.Undefined, "null");
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
