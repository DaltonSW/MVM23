namespace MVM23.Scripts;

using Godot;
using System;
using System.Collections.Generic;
using static Utilities.Tokenizer;

public partial class TextField : Panel
{
    private const string TestStringFormatting = "The [red]quick [green]brown[/green][/red][big]fox [blue]jumps over the[/blue][/big] [small]lazy[green] dog[/green][/small].";

    private const string TestStringWrapping =
        "Hendrerit et sea. Est nonumy amet ea ut imperdiet lorem diam et sea diam velit tation dolor erat velit ea. Dolore duo dolores accusam at tempor accusam et eos eos dignissim sadipscing justo vulputate accusam vel. Erat sed ea sea takimata eum odio kasd iusto consetetur illum erat. Stet lorem sed ea dolor dolore no amet vel in.";

    private readonly List<Token> _visibleTokens = new();
    private List<Token> _remainingTokens = new();

    private Token _currentToken;

    private Font _font;
    
    private Label _endLabel;

    private bool _textRemaining;
    private const float AlphaIncrement = 0.05F;
    private float _curAlpha;
    private float _curLinePos;
    private float _curLineNum;
    private Vector2 _textBoxSize;
    
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ThemeConsts.Initialize();

        _font = ThemeDB.FallbackFont;
        
        _endLabel = GetNode<Label>("../../Label");
        _endLabel.Visible = false;

        _remainingTokens = TokenizeString(TestStringWrapping);

        _currentToken = new Token("null", TokenFlags.Undefined);

        if (_remainingTokens.Count > 0)
        {
            _textRemaining = true;
        }
    }

    public override void _Draw()
    {
        _textBoxSize = GetRect().Size;

        _curLineNum = 1;
        _curLinePos = 0F;
        var height = ThemeConsts.RegularText.GetHeight(20);
        
        foreach (var visToken in _visibleTokens)
        {
            DrawString(visToken.Font, new Vector2(_curLinePos, height * _curLineNum), visToken.Text, HorizontalAlignment.Left, -1F, visToken.FontSize, visToken.Color);
            _curLinePos += visToken.StringSize;
            if (_curLinePos < _textBoxSize.X * 0.9) continue;
            _curLineNum += 1;
            _curLinePos = 0;

            if (height * _curLineNum >= _textBoxSize.Y)
            {
                return;
            }
        }

        if (_currentToken.Flags == TokenFlags.Undefined)
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
        DrawString(_currentToken.Font, new Vector2(_curLinePos, height * _curLineNum), _currentToken.Text, HorizontalAlignment.Left, -1F, _currentToken.FontSize, curTokenColor);
        _currentToken.Color = curTokenColor;

        if (_curAlpha < 1.0F) return;
        
        _visibleTokens.Add(_currentToken);
        _currentToken = new Token("null", TokenFlags.Undefined);
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
