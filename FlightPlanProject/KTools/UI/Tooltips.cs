using UnityEngine;

namespace FlightPlan.KTools.UI;

public class ToolTipsManager
{

    public static void SetToolTip(string tooltip)
    {
        if (Event.current.type == EventType.Repaint)
        {
            if (LastToolTip != tooltip)
            {
                //FlightPlanPlugin.Logger.LogDebug("changed");

                if (!string.IsNullOrEmpty(tooltip))
                {
                    Show = true;
                    ShowTime = Time.time + DELAY;
                    DrawToolTip = tooltip;
                }
                else
                {
                    Show = false;
                }
            }

            LastToolTip = tooltip;
        }
    }

    static float ShowTime;
    const float DELAY = 0.5f;
    static bool Show = false;

    static Vector2 Offset = new Vector2(20, 10);

    static string LastToolTip;
    static string DrawToolTip;
    public static void DrawToolTips()
    {
        if (!Show)
            return;

        if (Time.time > ShowTime)
        {
            GUI.skin.button.CalcMinMaxWidth(new GUIContent(DrawToolTip), out float _minWidth, out float _maxWidth);
            Rect _tooltipPos = new Rect(Input.mousePosition.x + Offset.x, Screen.height - Input.mousePosition.y + Offset.y, _maxWidth, 10);
            WindowTool.CheckWindowPos(ref _tooltipPos);

            GUILayout.Window(3, _tooltipPos, WindowFunction, "", GUI.skin.button);
        }
    }

    static void WindowFunction(int windowID)
    {
        //FlightPlanPlugin.Logger.LogDebug(DrawToolTip);
        GUILayout.Label(DrawToolTip);
    }
}

