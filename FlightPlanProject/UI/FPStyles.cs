
using UnityEngine;

using FlightPlan.KTools.UI;

namespace FlightPlan;

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
        return GUILayout.Button(txt, KBaseStyle.big_button, GUILayout.Height(SquareButton_size), GUILayout.Width(SquareButton_size));
    }

    public static bool SquareButton(Texture2D icon)
    {
        return GUILayout.Button(icon, KBaseStyle.big_button, GUILayout.Height(SquareButton_size), GUILayout.Width(SquareButton_size));
    }


    static int spacingAfterHeader = 5;

    public static void DrawSectionHeader(string sectionName, string value = "", float labelWidth = -1, GUIStyle valueStyle = null) // was (string sectionName, ref bool isPopout, string value = "")
    {
        if (valueStyle == null) valueStyle = KBaseStyle.label;

        
        GUILayout.BeginHorizontal();
        // Don't need popout buttons for ROC
        // isPopout = isPopout ? !CloseButton() : UI_Tools.SmallButton("⇖", popoutBtnStyle);

        if (labelWidth < 0)
            GUILayout.Label($"<b>{sectionName}</b> ");
        else
            GUILayout.Label($"<b>{sectionName}</b> ", GUILayout.Width(labelWidth));
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, valueStyle);
        GUILayout.Space(5);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterHeader);
    }

}


