using Godot;

namespace MVM23.Scripts;

public class ThemeConsts {
    private static FontFile EASVHS { get; set; }

    public static int RegularTextSize { get; private set; }
    public static FontFile RegularText { get; set; }
    public static FontFile BoldText { get; private set; }
    public static FontFile ItalicText { get; private set; }
    public static FontFile BoldItalicText { get; private set; }
    public static FontFile CodeText { get; private set; }

    public static TextField.DrawModes DefaultDrawMode { get; private set; }

    public static void Initialize() {
        RegularTextSize = 20;
        DefaultDrawMode = TextField.DrawModes.CharByChar;

        // EASVHS = new FontFile();
        // EASVHS.LoadDynamicFont("res://Assets/Fonts/easvhs.ttf");

        // RegularText = EASVHS;
        // BoldText = EASVHS;
    }
}