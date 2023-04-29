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

    public static double ap_altitude_km
    {
        get => s_settings_file.GetDouble("ap_altitude_km", 100);
        set { s_settings_file.SetDouble("ap_altitude_km", value); }
    }

    public static double pe_altitude_km
    {
        get => s_settings_file.GetDouble("pe_altitude_km", 100);
        set { s_settings_file.SetDouble("pe_altitude_km", value); }
    }

    public static double mr_altitude_km
    {
        get => s_settings_file.GetDouble("mr_altitude_km", 100);
        set { s_settings_file.SetDouble("mr_altitude_km", value); }
    }

    public static double target_inc_deg
    {
        get => s_settings_file.GetDouble("target_inc_deg", 0);
        set { s_settings_file.SetDouble("target_inc_deg", value); }
    }

    public static double interceptT
    {
        get => s_settings_file.GetDouble("interceptT", 0);
        set { s_settings_file.SetDouble("interceptT", value); }
    }

}