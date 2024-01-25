using BepInEx.Configuration;
using BepInEx.Logging;
using KSP.Game;

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

    private static readonly GameInstance Game = GameManager.Instance.Game;

    static public Status status = Status.VIRGIN; // Everyone starts out this way...

    static public string StatusText;
    static public double StatusTime = 0; // _UT of last Status update

    private static ConfigEntry<string> InitialStatusText;
    static public ConfigEntry<double> StatusPersistence;
    static public ConfigEntry<double> StatusFadeTime;

    private static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("FPStatus");

    public static void K2D2Status(string txt, double duration)
    {
        StatusText = txt;
        double _UT = Game.UniverseModel.UniverseTime;
        StatusTime = _UT + duration;
    }

    public static void Ok(string txt)
    {
        set(Status.OK, txt);
        if (txt.Length > 0)
            Logger.LogInfo(txt);
    }

    public static void Warning(string txt)
    {
        set(Status.WARNING, txt);
        if (txt.Length > 0)
            Logger.LogWarning(txt);
    }

    public static void Error(string txt)
    {
        set(Status.ERROR, txt);
        if (txt.Length > 0)
            Logger.LogError(txt);
    }

    private static void set(Status status, string txt)
    {
        FPStatus.status = status;
        StatusText = txt;
        double _UT = Game.UniverseModel.UniverseTime;
        StatusTime = _UT + StatusPersistence.Value;
    }

    public static void Init(FlightPlanPlugin plugin)
    {
        StatusPersistence = plugin.Config.Bind<double>("Status Settings Section", "Satus Hold Time", 20, "Controls time DELAY (in seconds) before Status beings to fade");
        StatusFadeTime = plugin.Config.Bind<double>("Status Settings Section", "Satus Fade Time", 20, "Controls the time (in seconds) it takes for Status to fade");
        InitialStatusText = plugin.Config.Bind<string>("Status Settings Section", "Initial Status", "Virgin", "Controls the Status reported at startup prior to the first command");

        // Set the initial and Default values based on config parameters. These don't make sense to need live update, so there're here instead of useing the configParam.Value elsewhere
        StatusText = InitialStatusText.Value;
    }

    //public static void DrawUI(double UT)
    //{
    //    // Indicate Status of last GUI function
    //    float _transparency = 1;
    //    if (UT > StatusTime) _transparency = (float)MuUtils.Clamp(1 - (UT - StatusTime) / StatusFadeTime.Value, 0, 1);

    //    var status_style = FPStyles.Status;
    //    //if (Status == Status.VIRGIN)
    //    //    status_style = FPStyles.Label;  
    //    if (status == Status.OK)
    //        status_style.normal.textColor = new Color(0, 1, 0, _transparency); // FPStyles.PhaseOk;
    //    if (status == Status.WARNING)
    //        status_style.normal.textColor = new Color(1, 1, 0, _transparency); // FPStyles.PhaseWarning;
    //    if (status == Status.ERROR)
    //        status_style.normal.textColor = new Color(1, 0, 0, _transparency); // FPStyles.PhaseError;

    //    UI_Tools.Separator();
    //    FPStyles.DrawSectionHeader("Status:", StatusText, 60, status_style);
    //}
}
