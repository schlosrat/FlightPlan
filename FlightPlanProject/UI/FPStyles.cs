
using UnityEngine;

using FlightPlan.KTools.UI;

namespace FlightPlan;

public class FPStyles
{
    private static bool _guiLoaded = false;

    public static bool Init()
    {
        if (_guiLoaded)
            return true;

        if (!KBaseStyle.Init())
            return false;

        KBaseStyle.Skin.window.fixedWidth = 300; // Must fit with max_width given to DrawTabs (TabsUI.cs)

        // Load specific Icon and style here
        K2D2BigIcon = AssetsLoader.LoadIcon("k2d2_big_icon");
        MNCIcon = AssetsLoader.LoadIcon("mnc_icon_white_100");

        Status = new GUIStyle(GUI.skin.GetStyle("Label"));
        Status.alignment = TextAnchor.MiddleLeft;
        Status.margin = new RectOffset(0, 0, 0, 0);
        Status.padding = new RectOffset(0, 0, 0, 0);

        _guiLoaded = true;
        return true;
    }

    public static Texture2D K2D2BigIcon;
    public static Texture2D MNCIcon;

    public static GUIStyle Status;

    const int SquareButtonSize = 60;

    public static bool SquareButton(string txt)
    {
        return GUILayout.Button(txt, KBaseStyle.BigButton, GUILayout.Height(SquareButtonSize), GUILayout.Width(SquareButtonSize));
    }

    public static bool SquareButton(Texture2D icon)
    {
        return GUILayout.Button(icon, KBaseStyle.BigButton, GUILayout.Height(SquareButtonSize), GUILayout.Width(SquareButtonSize));
    }

    public static int SpacingAfterHeader = 5;
    public static int SpacingAfterSection = 5;
    public static int SpacingAfterEntry = 0;

    public static void DrawSectionHeader(string sectionName, string value = "", float labelWidth = -1, GUIStyle valueStyle = null) // was (string sectionName, ref bool isPopout, string value = "")
    {
        if (valueStyle == null) valueStyle = KBaseStyle.Label;

        GUILayout.BeginHorizontal();

        if (labelWidth < 0)
            GUILayout.Label($"<b>{sectionName}</b> ");
        else
            GUILayout.Label($"<b>{sectionName}</b> ", GUILayout.Width(labelWidth));
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, valueStyle);
        GUILayout.Space(5);
        GUILayout.EndHorizontal();
        GUILayout.Space(SpacingAfterHeader);
    }

}


