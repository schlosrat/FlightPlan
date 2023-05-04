using FlightPlan.KTools;


namespace FlightPlan;

public class FPSettings
{
    public static double altitude_km
    {
        get => GeneralSettings.sfile.GetDouble("altitude_km", 100);
        set { GeneralSettings.sfile.SetDouble("altitude_km", value); }
    }

    public static double ap_altitude_km
    {
        get => GeneralSettings.sfile.GetDouble("ap_altitude_km", 200);
        set { GeneralSettings.sfile.SetDouble("ap_altitude_km", value); }
    }

    public static double pe_altitude_km
    {
        get => GeneralSettings.sfile.GetDouble("pe_altitude_km", 100);
        set { GeneralSettings.sfile.SetDouble("pe_altitude_km", value); }
    }

    public static double mr_altitude_km
    {
        get => GeneralSettings.sfile.GetDouble("mr_altitude_km", 100);
        set { GeneralSettings.sfile.SetDouble("mr_altitude_km", value); }
    }

    public static double target_sma_km
    {
        get => GeneralSettings.sfile.GetDouble("target_sma_km", 100);
        set { GeneralSettings.sfile.SetDouble("target_sma_km", value); }
    }

    public static double target_inc_deg
    {
        get => GeneralSettings.sfile.GetDouble("target_inc_deg", 0);
        set { GeneralSettings.sfile.SetDouble("target_inc_deg", value); }
    }

    public static double target_lan_deg
    {
        get => GeneralSettings.sfile.GetDouble("target_lan_deg", 0);
        set { GeneralSettings.sfile.SetDouble("target_lan_deg", value); }
    }

    public static double target_node_long_deg
    {
        get => GeneralSettings.sfile.GetDouble("target_node_long_deg", 0);
        set { GeneralSettings.sfile.SetDouble("target_node_long_deg", value); }
    }
    
    public static double interceptT
    {
        get => GeneralSettings.sfile.GetDouble("interceptT", 0);
        set { GeneralSettings.sfile.SetDouble("interceptT", value); }
    }

    public static double timeOffset
    {
        get => GeneralSettings.sfile.GetDouble("timeOffset", 30);
        set { GeneralSettings.sfile.SetDouble("timeOffset", value); }
    }
}