using UnityEngine;

namespace FlightPlan.KTools.UI;

public class WindowTool
{
    /// <summary>
    ///  checks if the window is in screen
    /// </summary>
    /// <param name="windowFrame"></param>
    public static void CheckWindowPos(ref Rect windowFrame)
    {
        if (windowFrame.xMax > Screen.width)
        {
            float _dx = Screen.width - windowFrame.xMax;
            windowFrame.x += _dx;
        }
        if (windowFrame.yMax > Screen.height)
        {
            float _dy = Screen.height - windowFrame.yMax;
            windowFrame.y += _dy;
        }
        if (windowFrame.xMin < 0)
        {
            windowFrame.x = 0;
        }
        if (windowFrame.yMin < 0)
        {
            windowFrame.y = 0;
        }
    }

    /// <summary>
    /// check the window pos and load settings if not set
    /// </summary>
    /// <param name="windowFrame"></param>
    public static void CheckMainWindowPos(ref Rect windowFrame)
    {
        if (windowFrame == Rect.zero)
        {
            int _xPos = KBaseSettings.WindowXPos;
            int _yPos = KBaseSettings.WindowYPos;

            if (_xPos == -1)
            {
                _xPos = 100;
                _yPos = 50;
            }

            windowFrame = new Rect(_xPos, _yPos, 500, 100);
        }

        CheckWindowPos(ref windowFrame);
    }
}
