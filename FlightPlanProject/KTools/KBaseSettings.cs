using UnityEngine;


namespace FlightPlan.KTools;

public class KBaseSettings
{
    public static SettingsFile SFile = null;
    public static string SettingsPath;

    public static void Init(string settingsPath)
    {
        SFile = new SettingsFile(settingsPath);
    }

    // each setting is defined by an accessor pointing on s_settings_file
    // the convertion to type is made here
    // this way we can have any kind of settings without hard work

    public static int WindowXPos
    {
        get => SFile.GetInt("WindowXPos", 70);
        set { SFile.SetInt("WindowXPos", value); }
    }

    public static int WindowYPos
    {
        get => SFile.GetInt("WindowYPos", 50);
        set { SFile.SetInt("WindowYPos", value); }
    }

    public static int MainTabIndex
    {
        get { return SFile.GetInt("MainTabIndex", 0); }
        set { SFile.SetInt("MainTabIndex", value); }
    }
}
