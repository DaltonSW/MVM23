using Godot;

namespace MVM23.Scripts;

public class ThemeConsts {
    private static FontFile EASVHS { get; set; }
    private static FontFile CodeFont { get; set; }

    public static int RegularTextSize { get; set; }
    public static FontFile RegularText { get; set; }
    public static FontFile BoldText { get; set; }
    public static FontFile ItalicText { get; set; }
    public static FontFile BoldItalicText { get; set; }
    public static FontFile CodeText { get; set; }
    
    public static Color DemonColor = Color.Color8(184, 17, 25);
    public static Color GODColor = Color.Color8(255, 174, 244);
    public static Color SystemColor = Color.Color8(255, 255, 255);
    public static Color TeamIntegrityColor = Color.Color8(120, 255, 254);

    public static TextField.DrawModes DefaultDrawMode { get; private set; }

    public static void Initialize() {
        RegularTextSize = 14;
        DefaultDrawMode = TextField.DrawModes.CharByChar;

        EASVHS = new FontFile();
        EASVHS.LoadDynamicFont("res://Assets/Fonts/EASVHS.ttf");

        CodeFont = new FontFile();
        CodeFont.LoadDynamicFont("res://Assets/Fonts/Pixelify.ttf");

        RegularText = EASVHS;
        RegularText.FontStyle = TextServer.FontStyle.FixedWidth;
        CodeText = CodeFont;
        CodeText.FontStyle = TextServer.FontStyle.FixedWidth;
    }
}
