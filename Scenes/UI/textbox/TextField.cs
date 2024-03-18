using MVM23.Scripts.Utilities;

namespace MVM23.Scripts;

using Godot;
using System;
using System.Collections.Generic;
using static Tokenizer;

public partial class TextField : Panel {
    [Signal]
    public delegate void TextFinishedPrintingEventHandler();

    private readonly List<Token> _visibleTokens = new();
    private readonly List<Token> _remainingTokens = new();
    private List<Token> _initialTokens = new();

    private Token _currentToken;

    private Font _font;

    private bool _textRemaining;
    private const float AlphaIncrement = 0.2F;
    private int _curCharIndex;
    private float _curAlpha;
    private float _curLinePos;
    private float _curLineNum;
    private Vector2 _textBoxSize;


    public enum DrawModes {
        Undefined = -1,
        WordByWord = 0,
        CharByChar = 1,
        CharButWholeStrongWord = 2,
    }

    private DrawModes _drawMode = DrawModes.WordByWord;

    public void DrawParagraph(string paragraph, DrawModes drawMode) {
        ClearTokens();
        _drawMode = drawMode;
        _initialTokens = TokenizeString(paragraph);
        _drawMode = drawMode;
        for (var i = 0; i < _initialTokens.Count; i++) {
            var token = _initialTokens[i];
            var nextToken = new Token("", TokenFlags.Undefined);
            if (i + 1 < _initialTokens.Count) {
                nextToken = _initialTokens[i + 1];
            }
            if (nextToken.Flags != TokenFlags.Undefined && !nextToken.IsPunctuation) {
                token.Text += " ";
                token.UpdateStringSize();
            }
                
            _remainingTokens.Add(token);
        }

        _textRemaining = true;
    }

    private void ClearTokens() {
        _visibleTokens.Clear();
        _initialTokens.Clear();
        _remainingTokens.Clear();
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        _font = ThemeDB.FallbackFont;

        _currentToken = new Token("null", TokenFlags.Undefined);
    }

    public override void _Draw() {
        if (!_textRemaining) {
            return;
        }
        _textBoxSize = GetRect().Size;
        _curLineNum = 1;
        _curLinePos = 0F;

        switch (_drawMode) {
            case DrawModes.CharByChar:
                DrawCharByChar();
                break;
            case DrawModes.WordByWord:
                DrawWordByWord();
                break;
            case DrawModes.CharButWholeStrongWord:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

    }

    private void DrawCharByChar() {
        var height = ThemeConsts.RegularText.GetHeight(ThemeConsts.RegularTextSize + 6);

        foreach (var visToken in _visibleTokens) {
            DrawString(visToken.Font, new Vector2(_curLinePos, height * _curLineNum), visToken.Text,
                HorizontalAlignment.Left, -1F, visToken.FontSize, visToken.Color);
            _curLinePos += visToken.StringSize;
            if (_curLinePos < _textBoxSize.X * 0.9) continue;
            _curLineNum += 1;
            _curLinePos = 0;

            if (height * _curLineNum < _textBoxSize.Y) continue;

            TextFinished();
            return;
        }

        if (_currentToken.Flags == TokenFlags.Undefined) {
            if (_remainingTokens.Count > 0) {
                _currentToken = _remainingTokens[0];
                _remainingTokens.RemoveAt(0);
                _curAlpha = 0F;
                _curCharIndex = 0;
            }
            else {
                TextFinished();
                return;
            }
        }

        var subStrToDraw = _currentToken.Text[.._curCharIndex];
        DrawString(_currentToken.Font, new Vector2(_curLinePos, height * _curLineNum), subStrToDraw,
            HorizontalAlignment.Left, -1F, _currentToken.FontSize, _currentToken.Color);
        _curLinePos += _currentToken.Font.GetStringSize(subStrToDraw, fontSize: _currentToken.FontSize).X;

        var curCharColor = _currentToken.Color;
        curCharColor.A = (float)Math.Min(_curAlpha + AlphaIncrement, 1.0);
        _curAlpha = curCharColor.A;
        var charToDraw = _currentToken.Text[_curCharIndex].ToString();
        DrawChar(_currentToken.Font, new Vector2(_curLinePos, height * _curLineNum), charToDraw, _currentToken.FontSize,
            curCharColor);
        _curLinePos += _currentToken.Font.GetStringSize(charToDraw, fontSize: _currentToken.FontSize).X;
        // _curLinePos += _currentToken.Font.GetCharSize()

        if (_curAlpha < 1.0F) return;

        _curAlpha = 0;
        _curCharIndex++;
        if (_curCharIndex < _currentToken.Text.Length) return;

        _curCharIndex = 0;
        _visibleTokens.Add(_currentToken);
        _currentToken = new Token("null", TokenFlags.Undefined);
    }

    private void DrawWordByWord() {
        var height = ThemeConsts.RegularText.GetHeight(ThemeConsts.RegularTextSize);

        foreach (var visToken in _visibleTokens) {
            DrawString(visToken.Font, new Vector2(_curLinePos, height * _curLineNum), visToken.Text,
                HorizontalAlignment.Left, -1F, visToken.FontSize, visToken.Color);
            _curLinePos += visToken.StringSize;
            if (_curLinePos < _textBoxSize.X * 0.9) continue;
            _curLineNum += 1;
            _curLinePos = 0;

            if (height * _curLineNum < _textBoxSize.Y) continue;

            TextFinished();
            return;
        }

        if (_currentToken.Flags == TokenFlags.Undefined) {
            if (_remainingTokens.Count > 0) {
                _currentToken = _remainingTokens[0];
                _remainingTokens.RemoveAt(0);
                _curAlpha = 0F;
            }
            else {
                TextFinished();
            }
        }

        var curTokenColor = _currentToken.Color;
        curTokenColor.A = (float)Math.Min(_curAlpha + AlphaIncrement, 1.0);
        _curAlpha = curTokenColor.A;
        DrawString(_currentToken.Font, new Vector2(_curLinePos, height * _curLineNum), _currentToken.Text,
            HorizontalAlignment.Left, -1F, _currentToken.FontSize, curTokenColor);
        _currentToken.Color = curTokenColor;

        if (_curAlpha < 1.0F) return;

        _visibleTokens.Add(_currentToken);
        _currentToken = new Token("null", TokenFlags.Undefined);
    }


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (!_textRemaining) return;
        QueueRedraw();
    }

    public void FinishCurrentPrint() {
        _visibleTokens.Clear();
        foreach (var token in _initialTokens) {
            var color = token.Color;
            color.A = 1F;
            token.Color = color;
            _visibleTokens.Add(token);
        }
        _currentToken.Flags = TokenFlags.Undefined;
        EmitSignal(SignalName.TextFinishedPrinting);
    }

    private void TextFinished() {
        _textRemaining = false;
        EmitSignal(SignalName.TextFinishedPrinting);
    }
}


// "Proper way" to implement that will account for kerning:
// extends Control
//
// var glyphs = []
//
// func _ready() -> void:
//      var font = get_theme_font("font")
//      var font_size = 16
//
//      # Preprocessing
//      var text_line = TextLine.new()
//      text_line.add_string("text", font, font_size) # You can add multiple spans of text with different font and size.
//
//      var ts = TextServerManager.get_primary_interface()
//      glyphs = ts.shaped_text_get_glyphs(text_line.get_rid()) # Store "glyphs" for actual rendering
//
//  func _draw() -> void:
//      var color = Color(1, 0, 0)
//      var position = Vector2(100, 100)
//      var ci = get_canvas_item()
//
//      var ts = TextServerManager.get_primary_interface()
//      # Draw shaped glyphs left-to-right.
//      for gl in glyphs:
//          if gl["font_rid"].is_valid():
//              ts.font_draw_glyph(gl["font_rid"], ci, gl["font_size"], position + gl["offset"], gl["index"], color)
//              # Do your custom drawing, you can check which part of original string glyph correspond to by checking gl["start"] and gl["end"]
//          else:
//              ts.draw_hex_code_box(ci, gl["font_size"], position + gl["offset"], gl["index"], color) # Glyph not found in the font.
//
//          position.x = position.x + gl["advance"]