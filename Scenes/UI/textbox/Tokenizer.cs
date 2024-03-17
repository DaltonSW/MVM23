using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

namespace MVM23.Scripts.Utilities;

public static class Tokenizer
{
    public class Token
    {
        public string Text { get; set; }
        public TokenFlags Flags { get; set; }
        public Color Color { get; set; }
        public int FontSize { get; set; }
        public FontFile Font { get; set; }
        public float StringSize { get; set; }

        public Token(string text, TokenFlags flags)
        {
            Text = text;
            Flags = flags;
            Color = GetColor();
            FontSize = GetFontSize();
            Font = GetFont();
            StringSize = GetTokenStringSize();
        }
        
        private Color GetColor()
        {
            var color = new Color(Colors.Black);
            if (IsFlagSet(Flags, TokenFlags.Red))
            {
                color += Colors.Red;
            }
            if (IsFlagSet(Flags, TokenFlags.Green))
            {
                color += Colors.Green;
            }
            if (IsFlagSet(Flags, TokenFlags.Blue))
            {
                color += Colors.Blue;
            }

            if (color == Colors.Black)
            {
                color = Colors.White;
            }

            return color;
        }

        private int GetFontSize()
        {
            var size = ThemeConsts.RegularTextSize;
        
            if (IsFlagSet(Flags, TokenFlags.Big))
            {
                size += 5;
            }
            else if (IsFlagSet(Flags, TokenFlags.Small))
            {
                size -= 5;
            }

            return size;
        }

        private FontFile GetFont()
        {
            if (IsFlagSet(TokenFlags.Code, Flags))
            {
                return ThemeConsts.CodeText;
            }
            
            if (IsFlagSet(TokenFlags.Bold, Flags) && IsFlagSet(TokenFlags.Italic, Flags))
            {
                return ThemeConsts.BoldItalicText;
            }
            
            if (IsFlagSet(TokenFlags.Bold, Flags))
            {
                return ThemeConsts.BoldText;
            }
            
            if (IsFlagSet(TokenFlags.Italic, Flags))
            {
                return ThemeConsts.ItalicText;
            }
            
            return ThemeConsts.RegularText;
        }

        private float GetTokenStringSize()
        {
            if (Font == null)
            {
                return 0;
            }
            return Font.GetStringSize(Text, HorizontalAlignment.Left, -1F, FontSize).X;
        }
    }
    
    [Flags]
    public enum TokenFlags
    {
        Undefined = -1,
        Normal = 0,
        Big = 1,
        Small = 2,
        
        Red = 4,
        Green = 8,
        Blue = 16,
        
        Bold = 32,
        Italic = 64,
        Code = 128,
        
        GOD = 256,
        Demon = 512,
        TeamIntegrity = 1024,
        Ability = 2048,
        
        ClearFlag = int.MaxValue
    }

    private static void SetFlag(TokenFlags flag)
    {
        _currentFlags |= flag;
    }

    private static void RemoveFlag(TokenFlags flag)
    {
        _currentFlags &= flag ^ TokenFlags.ClearFlag;
    }

    private static bool IsFlagSet(TokenFlags overall, TokenFlags flag)
    {
        return (overall & flag) > 0;
    }

    private static TokenFlags _currentFlags = TokenFlags.Normal;

    public static List<Token> TokenizeString(string text)
    {
        var step1Tokens = Regex.Split(text, @"(\[\/?[^\]]+\])");

        // Pattern to handle words, punctuation marks separately, and ensure spaces are included correctly
        const string pattern = @"([\w+']+[\s]?)|([.,?!]\s?)";

        var tokens = new List<Token>();
        foreach (var token in step1Tokens)
        {
            if (token.StartsWith("["))
            {
                var negate = false;
                var tokenText = token;
                if (token.Contains('/'))
                {
                    negate = true;
                    tokenText = Regex.Replace(token, "/", "");
                }
                
                var tokenType = tokenText switch
                {
                    "[big]" => TokenFlags.Big,
                    "[small]" => TokenFlags.Small,
                    
                    "[red]" => TokenFlags.Red,
                    "[green]" => TokenFlags.Green,
                    "[blue]" => TokenFlags.Blue,
                    
                    "[bold]" => TokenFlags.Bold,
                    "[b]" => TokenFlags.Bold,
                    "[italic]" => TokenFlags.Italic,
                    "[i]" => TokenFlags.Italic,
                    
                    "[ability]" => TokenFlags.Ability,
                    "[demon]" => TokenFlags.Demon,
                    "[god]" => TokenFlags.GOD,
                    "[ti]" => TokenFlags.TeamIntegrity,
                    
                    _ => TokenFlags.Normal
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
                // Non-tag, apply further splitting
                var matches = Regex.Matches(token, pattern);
                foreach (Match match in matches)
                {
                    if (!string.IsNullOrEmpty(match.Value))
                    {
                        tokens.Add(new Token(match.Value, _currentFlags));
                    }
                }
            }
        }

        return tokens;
    }
    
    private static List<Token> TokenizeStringOriginal(string text)
    {
        var tokens = new List<Token>();

        const string pattern = @"(\[\/?[^\]]+\])|(\w+)|([.,?!])";
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
                    "[big]" => TokenFlags.Big,
                    "[small]" => TokenFlags.Small,
                    
                    "[red]" => TokenFlags.Red,
                    "[green]" => TokenFlags.Green,
                    "[blue]" => TokenFlags.Blue,
                    
                    "[bold]" => TokenFlags.Bold,
                    "[b]" => TokenFlags.Bold,
                    "[italic]" => TokenFlags.Italic,
                    "[i]" => TokenFlags.Italic,
                    
                    _ => TokenFlags.Normal
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
                tokens.Add(new Token(tokenText, _currentFlags));
            }
        }
        
        return tokens;
    }
}