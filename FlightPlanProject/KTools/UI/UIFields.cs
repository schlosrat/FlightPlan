using UnityEngine;
using System.Text.RegularExpressions;
using KSP.Game;
using BepInEx.Logging;

namespace FlightPlan.KTools.UI;

public class UI_Fields
{
    public static Dictionary<string, string> TempDict = new Dictionary<string, string>();
    public static List<string> InputFields = new List<string>();
    static bool InputState = true;


    public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("KTools.UI_Fields");

    static public bool GameInputState
    {
        get { return InputState; }
        set
        {
            if (InputState != value)
            {
                Logger.LogWarning("input mode changed");

                if (value)
                    GameManager.Instance.Game.Input.Enable();
                else
                    GameManager.Instance.Game.Input.Disable();
            }
            InputState = value;
        }
    }

    static public void CheckEditor()
    {
        GameInputState = !InputFields.Contains(GUI.GetNameOfFocusedControl());
    }

    public static double DoubleField(string entryName, double value, GUIStyle thisStyle = null, bool parseAsTime = false)
    {
        string _textValue;
        string timeFormat = "HH:mm:ss";
        if (TempDict.ContainsKey(entryName))
            // always use temp value
            _textValue = TempDict[entryName];
        else
        {
            if (parseAsTime)
            {
                _textValue = value.ToString(timeFormat);
            }
            else
                _textValue = value.ToString();
        }
        
        if (!InputFields.Contains(entryName))
            InputFields.Add(entryName);

        Color _normal = GUI.color;
        bool _parsed;
        double num;
        if (parseAsTime)
        {
            if (_textValue == timeFormat || _textValue.Length < 1)
            {
                _parsed = true;
                num = 0;
            }
            else
            {
                _parsed = TimeSpan.TryParse(_textValue, out TimeSpan ts);
                num = ts.TotalSeconds;
            }
        }
        else
            _parsed = double.TryParse(_textValue, out num);
        if (!_parsed) GUI.color = Color.red;

        GUI.SetNextControlName(entryName);
        if (thisStyle != null)
            _textValue = GUILayout.TextField(_textValue, thisStyle, GUILayout.Width(90));
        else
            _textValue = GUILayout.TextField(_textValue, GUILayout.Width(90));

        GUI.color = _normal;

        // save filtered temp value
        TempDict[entryName] = _textValue;
        if (_parsed)
            return num;

        return value;
    }

    /// Simple Integer Field. for the moment there is a trouble. keys are sent to KSP2 events if focus is in the field
    public static int IntField(string entryName, string label, int value, int min, int max, string tooltip = "")
    {
        string _textValue = value.ToString();

        if (TempDict.ContainsKey(entryName))
            // always use temp value
            _textValue = TempDict[entryName];

        if (!InputFields.Contains(entryName))
            InputFields.Add(entryName);

        GUILayout.BeginHorizontal();

        if (!string.IsNullOrEmpty(label))
        {
            GUILayout.Label(label);
        }

        GUI.SetNextControlName(entryName);
        string _typedText = GUILayout.TextField(_textValue, GUILayout.Width(100));
        _typedText = Regex.Replace(_typedText, @"[^\d-]+", "");

        // save filtered temp value
        TempDict[entryName] = _typedText;
        bool _ok = true;

        if (!int.TryParse(_typedText, out int _result))
        {
            _ok = false;
        }
        if (_result < min) {
            _ok = false;
            _result = value;
        }
        else if (_result > max) {
            _ok = false;
            _result = value;
        }

        if (!_ok)
            GUILayout.Label("!!!", GUILayout.Width(30));

        if (!string.IsNullOrEmpty(tooltip))
        {
            UI_Tools.ToolTipButton(tooltip);
        }

        GUILayout.EndHorizontal();
        return _result;
    }
}
