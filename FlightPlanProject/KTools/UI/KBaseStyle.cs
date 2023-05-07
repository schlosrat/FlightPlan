using UnityEngine;
using SpaceWarp.API.UI;

namespace FlightPlan.KTools.UI;

public class KBaseStyle
{
    public static bool Init()
    {
        return BuildStyles();
    }

    public static GUISkin Skin;
    private static bool _guiLoaded = false;

    public static bool BuildStyles()
    {
        if (_guiLoaded)
            return true;

        Skin = CopySkin(Skins.ConsoleSkin);

        BuildFrames();
        BuildSliders();
        BuildButtons();
        BuildTabs();
        BuildFoldout();
        BuildToggle();
        BuildProgressBar();
        BuildIcons();
        BuildLabels();

        _guiLoaded = true;
        return true;
    }

    public static GUIStyle Error, Warning, Label, MidText, ConsoleText, PhaseOk, PhaseWarning, PhaseError;
    public static GUIStyle IconsLabel, Title, SliderText, TextInputStyle, NameLabelStyle, ValueLabelStyle, UnitLabelStyle;
    public static string UnitColorHex;

    static void BuildLabels()
    {

        IconsLabel = new GUIStyle(GUI.skin.GetStyle("Label"))
        {
            border = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            overflow = new RectOffset(0, 0, 0, 0)
        };

        Error = new GUIStyle(GUI.skin.GetStyle("Label"));
        Warning = new GUIStyle(GUI.skin.GetStyle("Label"));
        Error.normal.textColor = Color.red;
        Warning.normal.textColor = Color.yellow;
        //labelColor = GUI.Skin.GetStyle("Label").normal.textColor;

        PhaseOk = new GUIStyle(GUI.skin.GetStyle("Label"));
        PhaseOk.normal.textColor = ColorTools.ParseColor("#00BC16");
        // PhaseOk.fontSize = 20;

        PhaseWarning = new GUIStyle(GUI.skin.GetStyle("Label"));
        PhaseWarning.normal.textColor = ColorTools.ParseColor("#BC9200");
        // PhaseWarning.fontSize = 20;

        PhaseError = new GUIStyle(GUI.skin.GetStyle("Label"));
        PhaseError.normal.textColor = ColorTools.ParseColor("#B30F0F");
        // PhaseError.fontSize = 20;

        ConsoleText = new GUIStyle(GUI.skin.GetStyle("Label"));
        ConsoleText.normal.textColor = ColorTools.ParseColor("#B6B8FA");
        // ConsoleText.fontSize = 15;
        ConsoleText.padding = new RectOffset(0, 0, 0, 0);
        ConsoleText.margin = new RectOffset(0, 0, 0, 0);

        SliderText = new GUIStyle(ConsoleText);
        SliderText.normal.textColor = ColorTools.ParseColor("#C0C1E2");

        MidText = new GUIStyle(SliderText);

        SliderText.margin = new RectOffset(5, 0, 0, 0);
        SliderText.contentOffset = new Vector2(8, 5);

        Label = new GUIStyle(GUI.skin.GetStyle("Label"))
        {
            // Label.fontSize = 17;
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0)
        };

        Title = new GUIStyle();
        Title.normal.textColor = ColorTools.ParseColor("#C0C1E2");
        // Title.fontSize = 19;

        TextInputStyle = new GUIStyle(GUI.skin.GetStyle("textField")) // was (_spaceWarpUISkin.textField)
        {
            alignment = TextAnchor.LowerCenter,
            padding = new RectOffset(10, 10, 0, 0),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 18,
            fixedWidth = 100, //(float)(_windowWidth / 4),
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        NameLabelStyle = new GUIStyle(GUI.skin.GetStyle("Label")); // was (_spaceWarpUISkin.Label);
        NameLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);

        ValueLabelStyle = new GUIStyle(GUI.skin.GetStyle("Label")) // was (_spaceWarpUISkin.Label)
        {
            alignment = TextAnchor.MiddleRight
        };
        ValueLabelStyle.normal.textColor = new Color(.6f, .7f, 1, 1);

        UnitLabelStyle = new GUIStyle(ValueLabelStyle)
        {
            fixedWidth = 24,
            alignment = TextAnchor.MiddleLeft
        };
        UnitLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);

        UnitColorHex = ColorUtility.ToHtmlStringRGBA(UnitLabelStyle.normal.textColor);

    }

    public static GUIStyle Separator;
    static void BuildFrames()
    {
        // Define the GUIStyle for the _window
        GUIStyle _window = new GUIStyle(Skin.window)
        {
            border = new RectOffset(25, 25, 35, 25),
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(10, 10, 44, 10),
            overflow = new RectOffset(0, 0, 0, 0),

            // _window.fontSize = 20;
            contentOffset = new Vector2(31, -40)
        };

        // Set the background color of the _window
        _window.normal.background = AssetsLoader.LoadIcon("window");
        _window.normal.textColor = Color.black;
        SetAllFromNormal(_window);
        _window.alignment = TextAnchor.UpperLeft;
        _window.stretchWidth = true;
        // _window.fontSize = 20;
        _window.contentOffset = new Vector2(31, -40);
        Skin.window = _window;

        // Define the GUIStyle for the _box
        GUIStyle _box = new(_window);
        _box.normal.background = AssetsLoader.LoadIcon("Box");
        SetAllFromNormal(_box);
        _box.border = new RectOffset(10, 10, 10, 10);
        _box.margin = new RectOffset(0, 0, 0, 0);
        _box.padding = new RectOffset(10, 10, 10, 10);
        _box.overflow = new RectOffset(0, 0, 0, 0);
        Skin.box = _box;
        Skin.scrollView = _box;


        // define the V scrollbar
        GUIStyle _verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar);

        _verticalScrollbar.normal.background = AssetsLoader.LoadIcon("VerticalScroll");
        SetAllFromNormal(_verticalScrollbar);
        _verticalScrollbar.border = new RectOffset(5, 5, 5, 5);
        _verticalScrollbar.fixedWidth = 10;

        Skin.verticalScrollbar = _verticalScrollbar;

        GUIStyle _verticalScrollbarThumb = new(GUI.skin.verticalScrollbarThumb);

        _verticalScrollbarThumb.normal.background = AssetsLoader.LoadIcon("VerticalScroll_thumb");
        SetAllFromNormal(_verticalScrollbarThumb);
        _verticalScrollbarThumb.border = new RectOffset(5, 5, 5, 5);
        _verticalScrollbarThumb.fixedWidth = 10;

        Skin.verticalScrollbarThumb = _verticalScrollbarThumb;

        // Separator
        Separator = new GUIStyle(GUI.skin.box);
        Separator.normal.background = AssetsLoader.LoadIcon("line");
        Separator.border = new RectOffset(2, 2, 0, 0);
        Separator.margin = new RectOffset(10, 10, 5, 5);
        Separator.fixedHeight = 3;
        SetAllFromNormal(Separator);
    }

    public static GUIStyle SliderLine, SliderNode;

    static void BuildSliders()
    {
        SliderLine = new GUIStyle(GUI.skin.horizontalSlider);
        SliderLine.normal.background = AssetsLoader.LoadIcon("Slider");
        SetAllFromNormal(SliderLine);
        SliderLine.border = new RectOffset(5, 5, 0, 0);

        SliderLine.border = new RectOffset(12, 14, 0, 0);
        SliderLine.fixedWidth = 0;
        SliderLine.fixedHeight = 21;
        SliderLine.margin = new RectOffset(0, 0, 2, 5);

        SliderNode = new GUIStyle(GUI.skin.horizontalSliderThumb);
        SliderNode.normal.background = AssetsLoader.LoadIcon("SliderNode");
        SetAllFromNormal(SliderNode);
        SliderNode.border = new RectOffset(0, 0, 0, 0);
        SliderNode.fixedWidth = 21;
        SliderNode.fixedHeight = 21;

    }

    // icons
    public static Texture2D Gear, Icon, MNCIcon, Cross;

    static void BuildIcons()
    {
        // icons
        Gear = AssetsLoader.LoadIcon("Gear");
        Icon = AssetsLoader.LoadIcon("Icon");
        
        // MNCIcon = AssetsLoader.LoadIcon("mnc_new_icon_50");
        Cross = AssetsLoader.LoadIcon("Cross");
    }

    public static GUIStyle ProgressBarEmpty, ProgressBarFull;

    static void BuildProgressBar()
    {
        // progress bar
        ProgressBarEmpty = new GUIStyle(GUI.skin.box);
        ProgressBarEmpty.normal.background = AssetsLoader.LoadIcon("progress_empty");
        ProgressBarEmpty.border = new RectOffset(2, 2, 2, 2);
        ProgressBarEmpty.margin = new RectOffset(5, 5, 5, 5);
        ProgressBarEmpty.fixedHeight = 20;
        SetAllFromNormal(ProgressBarEmpty);

        ProgressBarFull = new GUIStyle(ProgressBarEmpty);
        ProgressBarFull.normal.background = AssetsLoader.LoadIcon("progress_full");
        SetAllFromNormal(ProgressBarEmpty);
    }


    public static GUIStyle BigiconButton, IconButton, SmallButton, BigButton, Button, CtrlButton;

    static void BuildButtons()
    {
        // Button std
        Button = new GUIStyle(GUI.skin.GetStyle("Button"));
        Button.normal.background = AssetsLoader.LoadIcon("BigButton_Normal");
        Button.normal.textColor = ColorTools.ParseColor("#FFFFFF");
        SetAllFromNormal(Button);

        Button.hover.background = AssetsLoader.LoadIcon("BigButton_hover");
        Button.active.background = AssetsLoader.LoadIcon("BigButton_hover");
        // Button.active.background = AssetsLoader.LoadIcon("BigButton_on");
        // Button.onNormal = Button.active;
        // SetFromOn(Button);

        Button.border = new RectOffset(5, 5, 5, 5);
        Button.padding = new RectOffset(4, 4, 4, 4);
        Button.overflow = new RectOffset(0, 0, 0, 0);
        // Button.fontSize = 20;
        Button.alignment = TextAnchor.MiddleCenter;
        Skin.button = Button;

        // Small Button
        SmallButton = new GUIStyle(GUI.skin.GetStyle("Button"));
        SmallButton.normal.background = AssetsLoader.LoadIcon("Small_Button");
        SetAllFromNormal(SmallButton);
        SmallButton.hover.background = AssetsLoader.LoadIcon("Small_Button_hover");
        SmallButton.active.background = AssetsLoader.LoadIcon("Small_Button_active");
        SmallButton.onNormal = SmallButton.active;
        SetFromOn(SmallButton);

        SmallButton.border = new RectOffset(5, 5, 5, 5);
        SmallButton.padding = new RectOffset(2, 2, 2, 2);
        SmallButton.overflow = new RectOffset(0, 0, 0, 0);
        SmallButton.alignment = TextAnchor.MiddleCenter;
        // SmallButton.fixedHeight = 16;

        BigButton = new GUIStyle(GUI.skin.GetStyle("Button"));
        BigButton.normal.background = AssetsLoader.LoadIcon("BigButton_Normal");
        BigButton.normal.textColor = ColorTools.ParseColor("#FFFFFF");
        SetAllFromNormal(BigButton);

        BigButton.hover.background = AssetsLoader.LoadIcon("BigButton_Hover");
        BigButton.active.background = AssetsLoader.LoadIcon("BigButton_Active");
        BigButton.onNormal = BigButton.active;
        SetFromOn(BigButton);

        BigButton.border = new RectOffset(5, 5, 5, 5);
        BigButton.padding = new RectOffset(8, 8, 10, 10);
        BigButton.overflow = new RectOffset(0, 0, 0, 0);
        // BigButton.fontSize = 20;
        BigButton.alignment = TextAnchor.MiddleCenter;

        // Small Button
        IconButton = new GUIStyle(SmallButton)
        {
            padding = new RectOffset(4, 4, 4, 4)
        };

        BigiconButton = new GUIStyle(IconButton)
        {
            fixedWidth = 50,
            fixedHeight = 50,
            fontStyle = FontStyle.Bold
        };

        CtrlButton = new GUIStyle(SmallButton) // GUI.Skin.GetStyle("Button")) // was: _spaceWarpUISkin.Button)
        {
            //alignment = TextAnchor.MiddleCenter,
            //padding = new RectOffset(0, 0, 0, 3),
            //contentOffset = new Vector2(0, 2),
            fixedHeight = 16
            //fixedWidth = 16,
            //fontSize = 16,
            //clipping = TextClipping.Overflow,
            //margin = new RectOffset(0, 0, 10, 0)
        };
        CtrlButton.normal.background = AssetsLoader.LoadIcon("Small_Button");
        SetAllFromNormal(CtrlButton);
        CtrlButton.hover.background = AssetsLoader.LoadIcon("Small_Button_hover");
        CtrlButton.active.background = AssetsLoader.LoadIcon("Small_Button_active");
        CtrlButton.onNormal = CtrlButton.active;
        SetFromOn(CtrlButton);

    }

    public static GUIStyle TabNormal, TabActive;
    static void BuildTabs()
    {
        TabNormal = new GUIStyle(Button)
        {
            border = new RectOffset(5, 5, 5, 5),
            padding = new RectOffset(10, 10, 5, 5),
            overflow = new RectOffset(0, 0, 0, 0),
            // BigButton.fontSize = 20;
            alignment = TextAnchor.MiddleCenter,
            stretchWidth = true
        };

        TabNormal.normal.background = AssetsLoader.LoadIcon("Tab_Normal");
        SetAllFromNormal(TabNormal);

        TabNormal.hover.background = AssetsLoader.LoadIcon("Tab_Hover");
        TabNormal.active.background = AssetsLoader.LoadIcon("Tab_Active");
        TabNormal.onNormal = TabNormal.active;
        SetFromOn(TabNormal);


        TabActive = new GUIStyle(TabNormal);
        TabActive.normal.background = AssetsLoader.LoadIcon("Tab_On_normal");
        SetAllFromNormal(TabActive);

        TabActive.hover.background = AssetsLoader.LoadIcon("Tab_On_hover");
        TabActive.active.background = AssetsLoader.LoadIcon("Tab_On_Active");
        TabActive.onNormal = TabActive.active;
        SetFromOn(TabActive);
    }


    public static GUIStyle FoldoutClose, FoldoutOpen;

    static void BuildFoldout()
    {

        FoldoutClose = new GUIStyle(SmallButton)
        {
            fixedHeight = 30,
            padding = new RectOffset(23, 2, 2, 2),
            border = new RectOffset(23, 7, 27, 3)
        };

        FoldoutClose.normal.background = AssetsLoader.LoadIcon("Chapter_Off_Normal");
        FoldoutClose.normal.textColor = ColorTools.ParseColor("#D4D4D4");
        FoldoutClose.alignment = TextAnchor.MiddleLeft;
        SetAllFromNormal(FoldoutClose);
        FoldoutClose.hover.background = AssetsLoader.LoadIcon("Chapter_Off_Hover");
        FoldoutClose.active.background = AssetsLoader.LoadIcon("Chapter_Off_Active");

        FoldoutOpen = new GUIStyle(FoldoutClose);
        FoldoutOpen.normal.background = AssetsLoader.LoadIcon("Chapter_On_Normal");
        FoldoutOpen.normal.textColor = ColorTools.ParseColor("#8BFF95");
        SetAllFromNormal(FoldoutOpen);

        FoldoutOpen.hover.background = AssetsLoader.LoadIcon("Chapter_On_Hover");
        FoldoutOpen.active.background = AssetsLoader.LoadIcon("Chapter_On_Active");
    }

    public static GUIStyle Toggle, ToggleError;
    static void BuildToggle()
    {
        // Toggle Button
        Toggle = new GUIStyle(GUI.skin.GetStyle("Button"));
        Toggle.normal.background = AssetsLoader.LoadIcon("Toggle_Off");
        Toggle.normal.textColor = ColorTools.ParseColor("#C0C1E2");


        SetAllFromNormal(Toggle);
        Toggle.onNormal.background = AssetsLoader.LoadIcon("Toggle_On");
        Toggle.onNormal.textColor = ColorTools.ParseColor("#C0E2DC");
        SetFromOn(Toggle);
        Toggle.fixedHeight = 32;
        Toggle.stretchWidth = false;

        Toggle.border = new RectOffset(45, 5, 5, 5);
        Toggle.padding = new RectOffset(34, 16, 0, 0);
        Toggle.overflow = new RectOffset(0, 0, 0, 2);

        ToggleError = new GUIStyle(Toggle);
        ToggleError.normal.textColor = Color.red;
    }

   
    /// <summary>
    /// copy all styles from normal state to others
    /// </summary>
    /// <param name="style"></param>
    private static void SetAllFromNormal(GUIStyle style)
    {
        style.hover = style.normal;
        style.active = style.normal;
        style.focused = style.normal;
        style.onNormal = style.normal;
        style.onHover = style.normal;
        style.onActive = style.normal;
        style.onFocused = style.normal;
    }

    /// <summary>
    /// copy all styles from onNormal state to on others
    /// </summary>
    /// <param name="style"></param>
    private static void SetFromOn(GUIStyle style)
    {
        style.onHover = style.onNormal;
        style.onActive = style.onNormal;
        style.onFocused = style.onNormal;
    }

    /// <summary>
    /// do a full copy of a Skin
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    private static GUISkin CopySkin(GUISkin source)
    {
        GUISkin copy = new GUISkin
        {
            font = source.font,
            box = new GUIStyle(source.box),
            label = new GUIStyle(source.label),
            textField = new GUIStyle(source.textField),
            textArea = new GUIStyle(source.textArea),
            button = new GUIStyle(source.button),
            toggle = new GUIStyle(source.toggle),
            window = new GUIStyle(source.window),

            horizontalSlider = new GUIStyle(source.horizontalSlider),
            horizontalSliderThumb = new GUIStyle(source.horizontalSliderThumb),
            verticalSlider = new GUIStyle(source.verticalSlider),
            verticalSliderThumb = new GUIStyle(source.verticalSliderThumb),

            horizontalScrollbar = new GUIStyle(source.horizontalScrollbar),
            horizontalScrollbarThumb = new GUIStyle(source.horizontalScrollbarThumb),
            horizontalScrollbarLeftButton = new GUIStyle(source.horizontalScrollbarLeftButton),
            horizontalScrollbarRightButton = new GUIStyle(source.horizontalScrollbarRightButton),

            verticalScrollbar = new GUIStyle(source.verticalScrollbar),
            verticalScrollbarThumb = new GUIStyle(source.verticalScrollbarThumb),
            verticalScrollbarUpButton = new GUIStyle(source.verticalScrollbarUpButton),
            verticalScrollbarDownButton = new GUIStyle(source.verticalScrollbarDownButton),

            scrollView = new GUIStyle(source.scrollView)
        };

        return copy;

    }

}