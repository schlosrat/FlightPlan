using FlightPlan.Tools;


namespace FlightPlan;

public class FPSettings
{
    public static SettingsFile s_settings_file = null;
    public static string s_settings_path;

    public static void Init(string settings_path)
    {
        s_settings_file = new SettingsFile(settings_path);
    }

    public static int window_x_pos
    {
        get => s_settings_file.GetInt("window_x_pos", 70);
        set { s_settings_file.SetInt("window_x_pos", value); }
    }

    public static int window_y_pos
    {
        get => s_settings_file.GetInt("window_y_pos", 50);
        set { s_settings_file.SetInt("window_y_pos", value); }
    }

    public static double ap_altiude_km
    {
        get => s_settings_file.GetDouble("ap_altiude_km", 100);
        set { s_settings_file.SetDouble("ap_altiude_km", value); }
    }

    public static double pe_altiude_km
    {
        get => s_settings_file.GetDouble("pe_altiude_km", 100);
        set { s_settings_file.SetDouble("pe_altiude_km", value); }
    }
    

}