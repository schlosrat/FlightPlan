using FlightPlan.KTools;


namespace FlightPlan;

public class FPSettings
{
    public static double altitude_km
    {
        get => KBaseSettings.sfile.GetDouble("altitude_km", 100);
        set { KBaseSettings.sfile.SetDouble("altitude_km", value); }
    }

    public static double ap_altitude_km
    {
        get => KBaseSettings.sfile.GetDouble("ap_altitude_km", 200);
        set { KBaseSettings.sfile.SetDouble("ap_altitude_km", value); }
    }

    public static double pe_altitude_km
    {
        get => KBaseSettings.sfile.GetDouble("pe_altitude_km", 100);
        set { KBaseSettings.sfile.SetDouble("pe_altitude_km", value); }
    }

    public static double mr_altitude_km
    {
        get => KBaseSettings.sfile.GetDouble("mr_altitude_km", 100);
        set { KBaseSettings.sfile.SetDouble("mr_altitude_km", value); }
    }

    public static double target_sma_km
    {
        get => KBaseSettings.sfile.GetDouble("target_sma_km", 100);
        set { KBaseSettings.sfile.SetDouble("target_sma_km", value); }
    }

    public static double target_inc_deg
    {
        get => KBaseSettings.sfile.GetDouble("target_inc_deg", 0);
        set { KBaseSettings.sfile.SetDouble("target_inc_deg", value); }
    }

    public static double target_lan_deg
    {
        get => KBaseSettings.sfile.GetDouble("target_lan_deg", 0);
        set { KBaseSettings.sfile.SetDouble("target_lan_deg", value); }
    }

    public static double target_node_long_deg
    {
        get => KBaseSettings.sfile.GetDouble("target_node_long_deg", 0);
        set { KBaseSettings.sfile.SetDouble("target_node_long_deg", value); }
    }
    
    public static double interceptT
    {
        get => KBaseSettings.sfile.GetDouble("interceptT", 0);
        set { KBaseSettings.sfile.SetDouble("interceptT", value); }
    }

    public static double timeOffset
    {
        get => KBaseSettings.sfile.GetDouble("timeOffset", 30);
        set { KBaseSettings.sfile.SetDouble("timeOffset", value); }
    }
}