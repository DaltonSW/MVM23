using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

namespace MVM23.Scripts.Utilities;

public static class Tokenizer
{
    // TODO: Pivot this to having each token store its font size, color, etc so we don't need to calculate on the fly when printing
    public class Token
    {
        public string Text { get; set; }
        public TokenType Type { get; set; }
        public Color Color { get; set; }
        public int FontSize { get; set; }

    public Token(string text, TokenType type)
        {
            Text = text;
            Type = type;
            Color = GetTokenColor(type);
            FontSize = GetTokenFontSize(type);
        }
    }
    
    [Flags]
    public enum TokenType
    {
        Undefined = -1,
        Normal = 0,
        Big = 1,
        Small = 2,
        Red = 4,
        Green = 8,
        Blue = 16,
        
        ClearFlag = int.MaxValue
    }
    
    private static Color GetTokenColor(TokenType tokenType)
    {
        var color = new Color(Colors.Black);
        if (IsFlagSet(tokenType, TokenType.Red))
        {
            color += Colors.Red;
        }
        if (IsFlagSet(tokenType, TokenType.Green))
        {
            color += Colors.Green;
        }
        if (IsFlagSet(tokenType, TokenType.Blue))
        {
            color += Colors.Blue;
        }

        if (color == Colors.Black)
        {
            color = Colors.White;
        }

        return color;
    }

    private static int GetTokenFontSize(TokenType tokenType)
    {
        var size = 20;
        
        if (IsFlagSet(tokenType, TokenType.Big))
        {
            size += 5;
        }
        else if (IsFlagSet(tokenType, TokenType.Small))
        {
            size -= 5;
        }

        return size;
    }

    private static void SetFlag(TokenType flag)
    {
        _currentType |= flag;
    }

    private static void RemoveFlag(TokenType flag)
    {
        _currentType &= flag ^ TokenType.ClearFlag;
    }

    public static bool IsFlagSet(TokenType overall, TokenType flag)
    {
        return (overall & flag) > 0;
    }

    private static TokenType _currentType = TokenType.Normal;

    public static List<Token> TokenizeString(string text)
    {
        var tokens = new List<Token>();

        const string pattern = @"\[[^\]]+\]|[^\[]+";
        var matches = Regex.Matches(text, pattern);
        
        foreach (Match match in matches)
        {
            var tokenText = match.Value;
            if (tokenText.StartsWith("[") && tokenText.EndsWith("]"))
            {
                var negate = false;
                if (tokenText.Contains('/'))
                {
                    negate = true;
                    tokenText = Regex.Replace(tokenText, "/", "");
                }
                
                var tokenType = tokenText switch
                {
                    "[big]" => TokenType.Big,
                    "[small]" => TokenType.Small,
                    "[red]" => TokenType.Red,
                    "[green]" => TokenType.Green,
                    "[blue]" => TokenType.Blue,
                    _ => TokenType.Normal
                };

                if (negate)
                {
                    RemoveFlag(tokenType);
                }
                else
                {
                    SetFlag(tokenType);
                }
            }
            else
            {
                tokens.Add(new Token(tokenText, _currentType));
            }
        }
        
        return tokens;
    }
}