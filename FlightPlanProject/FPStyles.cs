
using UnityEngine;

using SpaceWarp.API.UI;
using UnityEngine.UIElements;
using System.Reflection.Emit;
using K2D2.Controller;

using FlightPlan.KTools;
namespace FlightPlan.KTools.UI;

public class FPStyles
{
    private static bool guiLoaded = false;

    public static bool Init()
    {
        if (guiLoaded)
            return true;

        if (!KBaseStyle.Init())
            return false;

        // Load specific icon and style here
        k2d2_big_icon = AssetsLoader.loadIcon("k2d2_big_icon");
        mnc_icon = AssetsLoader.loadIcon("mnc_icon_white_100");

        status = new GUIStyle(GUI.skin.GetStyle("Label"));
        status.alignment = TextAnchor.MiddleLeft;
        status.margin = new RectOffset(0, 0, 0, 0);
        status.padding = new RectOffset(0, 0, 0, 0);

       guiLoaded = true;
        return true;
    }




    public static Texture2D k2d2_big_icon;
    public static Texture2D mnc_icon;

    public static GUIStyle status;

    const int SquareButton_size = 60;

    public static bool SquareButton(string txt)
    {
        return GUILayout.Button(txt, KBaseStyle.big_button, GUILayout.Height(SquareButton_size),  GUILayout.Width(SquareButton_size));
    }

    public static bool SquareButton(Texture2D icon)
    {
        return GUILayout.Button(icon, KBaseStyle.big_button, GUILayout.Height(SquareButton_size),  GUILayout.Width(SquareButton_size));
    }

}


