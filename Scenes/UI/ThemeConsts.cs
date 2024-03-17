using Godot;

namespace MVM23.Scripts;

public class ThemeConsts {
    private static FontFile EASVHS { get; set; }
    private static FontFile TestFont { get; set; }

    public static int RegularTextSize { get; private set; }
    public static FontFile RegularText { get; set; }
    public static FontFile BoldText { get; private set; }
    public static FontFile ItalicText { get; private set; }
    public static FontFile BoldItalicText { get; private set; }
    public static FontFile CodeText { get; private set; }

    public static TextField.DrawModes DefaultDrawMode { get; private set; }

    public static void Initialize() {
        RegularTextSize = 14;
        DefaultDrawMode = TextField.DrawModes.CharByChar;

        EASVHS = new FontFile();
        EASVHS.LoadDynamicFont("res://Assets/Fonts/easvhs.ttf");

        TestFont = new FontFile();
        TestFont.LoadDynamicFont("res://Assets/Fonts/Dogica.otf");

        RegularText = EASVHS;
        BoldText = RegularText;
    }
}