using BepInEx.Configuration;
using BepInEx.Logging;
using FlightPlan.KTools.UI;
using KSP.Game;
using MuMech;
using UnityEngine;

namespace FlightPlan;

public class FPStatus
{
    // Status of last Flight Plan function
    public enum Status
    {
        VIRGIN,
        OK,
        WARNING,
        ERROR
    }

    static public Status status = Status.VIRGIN; // Everyone starts out this way...

    static string statusText;
    static double statusTime = 0; // UT of last status update

    static ConfigEntry<string> initialStatusText;
    static ConfigEntry<double> statusPersistence;
    static ConfigEntry<double> statusFadeTime;

    static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("FPStatus");

    public static void K2D2Status(string txt, double duration)
    {
        statusText = txt;
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        statusTime = UT + duration;
    }

    public static void Ok(string txt)
    {
        set(Status.OK, txt);
        Logger.LogInfo(txt);
    }

    public static void Warning(string txt)
    {
        set(Status.WARNING, txt);
        Logger.LogWarning(txt);
    }

    public static void Error(string txt)
    {
        set(Status.ERROR, txt);
        Logger.LogError(txt);
    }

    static void set(Status status, string txt)
    {
        FPStatus.status = status;
        statusText = txt;
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        statusTime = UT + statusPersistence.Value;
    }

    public static void Init(FlightPlanPlugin plugin)
    {
        statusPersistence = plugin.Config.Bind<double>("Status Settings Section", "Satus Hold Time", 20, "Controls time delay (in seconds) before status beings to fade");
        statusFadeTime = plugin.Config.Bind<double>("Status Settings Section", "Satus Fade Time", 20, "Controls the time (in seconds) it takes for status to fade");
        initialStatusText = plugin.Config.Bind<string>("Status Settings Section", "Initial Status", "Virgin", "Controls the status reported at startup prior to the first command");

        // Set the initial and Default values based on config parameters. These don't make sense to need live update, so there're here instead of useing the configParam.Value elsewhere
        statusText = initialStatusText.Value;
    }

    public static void DrawUI(double UT)
    {
        // Indicate status of last GUI function
        float transparency = 1;
        if (UT > statusTime) transparency = (float)MuUtils.Clamp(1 - (UT - statusTime) / statusFadeTime.Value, 0, 1);

        var status_style = FPStyles.status;
        //if (status == Status.VIRGIN)
        //    status_style = FPStyles.label;  
        if (status == Status.OK)
            status_style.normal.textColor = new Color(0, 1, 0, transparency); // FPStyles.phase_ok;
        if (status == Status.WARNING)
            status_style.normal.textColor = new Color(1, 1, 0, transparency); // FPStyles.phase_warning;
        if (status == Status.ERROR)
            status_style.normal.textColor = new Color(1, 0, 0, transparency); // FPStyles.phase_error;

        UI_Tools.Separator();
        FPStyles.DrawSectionHeader("Status:", statusText, 60, status_style);
    }
}
