using System.Globalization;
using UnityEngine;

namespace FlightPlan.KTools.UI;

public class SimpleAccordion
{
    public delegate void OnChapterUI();

    public class Chapter
    {
        public string Title;
        public OnChapterUI ChapterUI;
        public bool Opened = false;

        public Chapter(string Title, OnChapterUI chapterUI)
        {
            this.Title = Title;
            this.ChapterUI = chapterUI;
        }
    }

    public List<Chapter> Chapters = new();
    public bool SingleChapter = false;

    public void OnGui()
    {
        GUILayout.BeginVertical();

        for (int i = 0; i < Chapters.Count; i++)
        {
            Chapter _chapter = Chapters[i];
            GUIStyle _style = _chapter.Opened ? KBaseStyle.FoldoutOpen : KBaseStyle.FoldoutClose;
            if (GUILayout.Button(_chapter.Title, _style))
            {
                _chapter.Opened = !_chapter.Opened;

                if (_chapter.Opened && SingleChapter)
                {
                    for (int j = 0; j < Chapters.Count; j++)
                    {
                        if (i != j)
                            Chapters[j].Opened = false;
                    }
                }
            }

            if (_chapter.Opened)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();

                _chapter.ChapterUI();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

        }
        GUILayout.EndVertical();
    }

    public void AddChapter(string title, OnChapterUI chapterUI)
    {
        Chapters.Add(new Chapter(title, chapterUI));
    }


    public int Count
    {
        get { return Chapters.Count; }
    }

}

public class TopButtons
{
    static Rect Position = Rect.zero;
    const int SPACE = 25;

    /// <summary>
    /// Must be called before any Button call
    /// </summary>
    /// <param name="widthWindow"></param>
    static public void Init(float widthWindow)
    {
        Position = new Rect(widthWindow - 5, 4, 23, 23);
    }

    static public bool Button(string txt)
    {
        Position.x -= SPACE;
        return GUI.Button(Position, txt, KBaseStyle.SmallButton);
    }
    static public bool Button(Texture2D icon)
    {
        Position.x -= SPACE;
        return GUI.Button(Position, icon, KBaseStyle.IconButton);
    }

    static public bool Toggle(bool value, string txt)
    {
        Position.x -= SPACE;
        return GUI.Toggle(Position, value, txt, KBaseStyle.SmallButton);
    }

    static public bool Toggle(bool value, Texture2D icon)
    {
        Position.x -= SPACE;
        return GUI.Toggle(Position, value, icon, KBaseStyle.IconButton);
    }
}

/// <summary>
/// A set of simple tools for UI
/// </summary>
/// TODO : remove static, make it singleton
public class UI_Tools
{

    public static int GetEnumValue<T>(T inputEnum) where T : struct, IConvertible
    {
        Type _t = typeof(T);
        if (!_t.IsEnum)
        {
            throw new ArgumentException("Input type must be an enum.");
        }

        return inputEnum.ToInt32(CultureInfo.InvariantCulture.NumberFormat);
    }

    public static TEnum EnumGrid<TEnum>(string label, TEnum value, string[] labels) where TEnum : struct, Enum
    {
        int _intValue = value.GetHashCode();
        UI_Tools.Label(label);
        int _result = GUILayout.SelectionGrid(_intValue, labels, labels.Length);

        return (TEnum)Enum.ToObject(typeof(TEnum), _result);
    }

    public static bool Toggle(bool isOn, string txt, string tooltip = null)
    {
        if (tooltip != null)
            return GUILayout.Toggle(isOn, new GUIContent(txt, tooltip), KBaseStyle.Toggle);
        else
            return GUILayout.Toggle(isOn, txt, KBaseStyle.Toggle);
    }

    public static bool BigToggleButton(bool isOn, string txtRun, string txtStop)
    {
        // int height_bt = 30;
        int _minWidthBt = 150;

        string _txt = isOn ? txtStop : txtRun;
        // GUILayout.BeginHorizontal();
        // GUILayout.FlexibleSpace();
        isOn = GUILayout.Toggle(isOn, _txt, KBaseStyle.BigButton, GUILayout.MinWidth(_minWidthBt));
        // GUILayout.FlexibleSpace();
        // GUILayout.EndHorizontal();
        return isOn;
    }


    public static bool SmallToggleButton(bool isOn, string txtRun, string txtStop, int widthOverride=0)
    {
        // int height_bt = 30;
        int _minWidthBt = (widthOverride > 0) ? widthOverride: 150;

        string _txt = isOn ? txtStop : txtRun;
        
        isOn = GUILayout.Toggle(isOn, _txt, KBaseStyle.SmallButton, GUILayout.MinWidth(_minWidthBt));

        // isOn = GUILayout.Toggle(isOn, _txt, KBaseStyle.SmallButton, GUILayout.MinWidth(_minWidthBt));
      
        return isOn;
    }

    //public static bool ShortToggleButton(bool isOn, string txtRun, string txtStop)
    //{
    //    // int height_bt = 30;
    //    // int _minWidthBt = 150;

    //    var _txt = isOn ? txtStop : txtRun;

    //    isOn = GUILayout.Toggle(isOn, _txt, KBaseStyle.SmallButton);

    //    return isOn;
    //}

    public static bool BigButton(string txt)
    {
        // int height_bt = 30;
        int _minWidthBt = 150;

        return GUILayout.Button(txt, KBaseStyle.BigButton, GUILayout.MinWidth(_minWidthBt));
    }

    public static bool SmallButton(string txt)
    {
        return GUILayout.Button(txt, KBaseStyle.SmallButton);
    }

    public static bool CtrlButton(string txt)
    {
        return GUILayout.Button(txt, KBaseStyle.CtrlButton);
    }

    public static bool BigIconButton(string txt)
    {
        return GUILayout.Button(txt, KBaseStyle.BigiconButton);
    }

    public static bool ListButton(string txt)
    {
        return GUILayout.Button(txt, KBaseStyle.Button, GUILayout.ExpandWidth(true));
    }

    public static bool miniToggle(bool value, string txt, string tooltip)
    {
        return GUILayout.Toggle(value, new GUIContent(txt, tooltip), KBaseStyle.SmallButton, GUILayout.Height(20));
    }

    public static bool miniButton(string txt, string tooltip = "")
    {
        return GUILayout.Button(new GUIContent(txt, tooltip), KBaseStyle.SmallButton, GUILayout.Height(20));
    }

    public static bool ToolTipButton(string tooltip)
    {
        return GUILayout.Button(new GUIContent("?", tooltip), KBaseStyle.SmallButton, GUILayout.Width(16), GUILayout.Height(20));
    }

    static public bool BigIconButton(Texture2D icon)
    {
        return GUILayout.Button(icon, KBaseStyle.BigiconButton);
    }

    public static void Title(string txt)
    {
        GUILayout.Label($"<b>{txt}</b>", KBaseStyle.Title);
    }

    public static void Label(string txt, GUIStyle thisStyle = null)
    {
        if (thisStyle == null)
            GUILayout.Label(txt, KBaseStyle.Label);
        else
            GUILayout.Label(txt, thisStyle);
    }

    public static void OK(string txt)
    {
        GUILayout.Label(txt, KBaseStyle.PhaseOk);
    }

    public static void Warning(string txt)
    {
        GUILayout.Label(txt, KBaseStyle.PhaseWarning);
    }

    public static void Error(string txt)
    {
        GUILayout.Label(txt, KBaseStyle.PhaseError);
    }



    public static void Console(string txt)
    {
        GUILayout.Label(txt, KBaseStyle.ConsoleText);
    }

    public static void Mid(string txt)
    {
        GUILayout.Label(txt, KBaseStyle.MidText);
    }



    public static int IntSlider(string txt, int value, int min, int max, string postfix = "", string tooltip = "")
    {
        string _content = txt + $" : {value} " + postfix;

        GUILayout.Label(_content, KBaseStyle.SliderText);
        GUILayout.BeginHorizontal();
        value = (int)GUILayout.HorizontalSlider((int)value, min, max, KBaseStyle.SliderLine, KBaseStyle.SliderNode);
        if (value < min) value = min;
        if (value > max) value = max;

        if (!string.IsNullOrEmpty(tooltip))
        {
            UI_Tools.ToolTipButton(tooltip);
        }
        GUILayout.EndHorizontal();
        return value;
    }

    public static float HeadingSlider(string txt, float value, string tooltip = "")
    {
        string _valueStr = value.ToString("N" + 1);
        string _content = $"{txt} : {_valueStr} °";
        GUILayout.Label(_content, KBaseStyle.SliderText);
        GUILayout.BeginHorizontal();
        value = GUILayout.HorizontalSlider(value, -180, 180, KBaseStyle.SliderLine, KBaseStyle.SliderNode);

        int _step = 45;
        float _precision = 5;
        int _index = Mathf.RoundToInt(value / _step);
        float _rounded = _index * _step;

        float _delta = Mathf.Abs(_rounded - value);
        if (_delta < _precision)
            value = _rounded;

        _index = _index + 4;
        string[] _directions = { "S", "SW", "W", "NW", "N", "NE", "E", "SE", "S", "??" };
        GUILayout.Label(_directions[_index], GUILayout.Width(15));
        if (!string.IsNullOrEmpty(tooltip))
        {
            UI_Tools.ToolTipButton(tooltip);
        }

        GUILayout.EndHorizontal();
        // GUILayout.Label($"_rounded {_rounded} _index {_index}, _delta {_delta}");
        return value;

    }

    public static void Separator()
    {
        GUILayout.Box("", KBaseStyle.Separator);
    }

    public static void ProgressBar(double value, double min, double max)
    {
        ProgressBar((float)value, (float)min, (float)max);
    }

    public static void ProgressBar(float value, float min, float max)
    {
        float _ratio = Mathf.InverseLerp(min, max, value);

        GUILayout.Box("", KBaseStyle.ProgressBarEmpty, GUILayout.ExpandWidth(true));
        Rect _lastRect = GUILayoutUtility.GetLastRect();

        _lastRect.width = Mathf.Clamp(_lastRect.width * _ratio, 4, 10000000);
        GUI.Box(_lastRect, "", KBaseStyle.ProgressBarFull);
    }

    public static float FloatSlider(float value, float min, float max, string tooltip = "")
    {
        // simple float slider
        GUILayout.BeginHorizontal();
        value = GUILayout.HorizontalSlider(value, min, max, KBaseStyle.SliderLine, KBaseStyle.SliderNode);

        if (!string.IsNullOrEmpty(tooltip))
        {
            UI_Tools.ToolTipButton(tooltip);
        }
        GUILayout.EndHorizontal();

        value = Mathf.Clamp(value, min, max);
        return value;
    }

    public static float FloatSliderTxt(string txt, float value, float min, float max, string postfix = "", string tooltip = "", int precision = 2)
    {
        // simple float slider with a printed value
        string _valueStr = value.ToString("N" + precision);

        string _content = $"{txt} : {_valueStr} {postfix}";

        GUILayout.Label(_content, KBaseStyle.SliderText);
        value = FloatSlider(value, min, max, tooltip);
        return value;
    }

    public static void Right_Left_Text(string right_txt, string left_txt)
    {
        // text aligned to right and left with a SPACE in between
        GUILayout.BeginHorizontal();
        UI_Tools.Mid(right_txt);
        GUILayout.FlexibleSpace();
        UI_Tools.Mid(left_txt);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    public static Vector2 BeginScrollView(Vector2 scrollPos, int height)
    {
        return GUILayout.BeginScrollView(scrollPos, false, true,
            GUILayout.MinWidth(250),
            GUILayout.Height(height));
    }
}

