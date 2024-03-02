using Godot;

namespace MVM23.Scripts;

public class ThemeConsts {
    private static FontFile AgaveRegular { get; set; }
    private static FontFile AgaveBold { get; set; }
    private static FontFile LemonMilk { get; set; }
    private static FontFile GontserratRegular { get; set; }
    private static FontFile GontserratBold { get; set; }
    private static FontFile GontserratItalic { get; set; }
    private static FontFile GontserratBoldItalic { get; set; }
    private static FontFile VCROSD { get; set; }
    private static FontFile EASVHS { get; set; }

    public static int RegularTextSize { get; private set; }
    public static FontFile RegularText { get; private set; }
    public static FontFile BoldText { get; private set; }
    public static FontFile ItalicText { get; private set; }
    public static FontFile BoldItalicText { get; private set; }
    public static FontFile CodeText { get; private set; }

    public static TextField.DrawModes DefaultDrawMode { get; private set; }

    public static void Initialize() {
        RegularTextSize = 20;
        DefaultDrawMode = TextField.DrawModes.CharByChar;

        AgaveRegular = new FontFile();
        AgaveRegular.LoadDynamicFont("res://Assets/Fonts/Agave Regular.TTF");

        AgaveBold = new FontFile();
        AgaveBold.LoadDynamicFont("res://Assets/Fonts/Agave Bold.TTF");

        LemonMilk = new FontFile();
        LemonMilk.LoadDynamicFont("res://Assets/Fonts/LEMONMILK.OTF");

        GontserratRegular = new FontFile();
        GontserratRegular.LoadDynamicFont("res://Assets/Fonts/Gontserrat Regular.ttf");

        GontserratBold = new FontFile();
        GontserratBold.LoadDynamicFont("res://Assets/Fonts/Gontserrat Bold.ttf");

        GontserratBoldItalic = new FontFile();
        GontserratBoldItalic.LoadDynamicFont("res://Assets/Fonts/Gontserrat Bold Italic.ttf");

        GontserratItalic = new FontFile();
        GontserratItalic.LoadDynamicFont("res://Assets/Fonts/Gontserrat Italic.ttf");

        VCROSD = new FontFile();
        VCROSD.LoadDynamicFont("res://Assets/Fonts/VCR OSD.ttf");

        EASVHS = new FontFile();
        EASVHS.LoadDynamicFont("res://Assets/Fonts/EASVHS.ttf");

        RegularText = EASVHS;
        // RegularText.FontStyle = TextServer.FontStyle.FixedWidth;
        BoldText = EASVHS;
        BoldText.FontStyle = TextServer.FontStyle.FixedWidth;
        BoldItalicText = GontserratBoldItalic;
        BoldItalicText.FontStyle = TextServer.FontStyle.FixedWidth;
        ItalicText = GontserratItalic;
        ItalicText.FontStyle = TextServer.FontStyle.FixedWidth;

        CodeText = AgaveRegular; // Any `code` font should already be monospaced
    }
}