using FlightPlan.KTools;


namespace FlightPlan;

public class FPSettings
{
    public static double Altitude_km
    {
        get => KBaseSettings.SFile.GetDouble("Altitude_km", 100);
        set { KBaseSettings.SFile.SetDouble("Altitude_km", value); }
    }

    public static double ApAltitude_km
    {
        get => KBaseSettings.SFile.GetDouble("ApAltitude_km", 200);
        set { KBaseSettings.SFile.SetDouble("ApAltitude_km", value); }
    }

    public static double PeAltitude_km
    {
        get => KBaseSettings.SFile.GetDouble("PeAltitude_km", 100);
        set { KBaseSettings.SFile.SetDouble("PeAltitude_km", value); }
    }

    public static double MoonReturnAltitude_km
    {
        get => KBaseSettings.SFile.GetDouble("MoonReturnAltitude_km", 100);
        set { KBaseSettings.SFile.SetDouble("MoonReturnAltitude_km", value); }
    }

    public static double TargetSMA_km
    {
        get => KBaseSettings.SFile.GetDouble("TargetSMA_km", 100);
        set { KBaseSettings.SFile.SetDouble("TargetSMA_km", value); }
    }

    public static double TargetInc_deg
    {
        get => KBaseSettings.SFile.GetDouble("TargetInc_deg", 0);
        set { KBaseSettings.SFile.SetDouble("TargetInc_deg", value); }
    }

    public static double TargetLAN_deg
    {
        get => KBaseSettings.SFile.GetDouble("TargetLAN_deg", 0);
        set { KBaseSettings.SFile.SetDouble("TargetLAN_deg", value); }
    }

    public static double TargetNodeLong_deg
    {
        get => KBaseSettings.SFile.GetDouble("TargetNodeLong_deg", 0);
        set { KBaseSettings.SFile.SetDouble("TargetNodeLong_deg", value); }
    }
    
    public static double InterceptTime
    {
        get => KBaseSettings.SFile.GetDouble("InterceptTime", 0);
        set { KBaseSettings.SFile.SetDouble("InterceptTime", value); }
    }

    public static double InterceptDistanceVessel
    {
        get => KBaseSettings.SFile.GetDouble("InterceptDistanceVessel", 100);
        set { KBaseSettings.SFile.SetDouble("InterceptDistanceVessel", value); }
    }

    public static double InterceptDistanceCelestial
    {
        get => KBaseSettings.SFile.GetDouble("InterceptDistanceCelestial", 50);
        set { KBaseSettings.SFile.SetDouble("InterceptDistanceCelestial", value); }
    }

    public static double TimeOffset
    {
        get => KBaseSettings.SFile.GetDouble("TimeOffset", 30);
        set { KBaseSettings.SFile.SetDouble("TimeOffset", value); }
    }

    public static int NumSats
    {
        get => KBaseSettings.SFile.GetInt("NumSats", 3);
        set { KBaseSettings.SFile.SetInt("NumSats", value); }
    }

    public static int NumOrbits
    {
        get => KBaseSettings.SFile.GetInt("NumOrbits", 1);
        set { KBaseSettings.SFile.SetInt("NumOrbits", value); }
    }

    public static bool DiveOrbit
    {
        get => KBaseSettings.SFile.GetBool("DiveOrbit", true);
        set { KBaseSettings.SFile.SetBool("DiveOrbit", value); }
    }

    public static bool Occlusion
    {
        get => KBaseSettings.SFile.GetBool("Occlusion", true);
        set { KBaseSettings.SFile.SetBool("Occlusion", value); }
    }

    public static double OccModAtm
    {
        get => KBaseSettings.SFile.GetDouble("OccModAtm", 0.75);
        set { KBaseSettings.SFile.SetDouble("OccModAtm", value); }
    }

    public static double OccModVac
    {
        get => KBaseSettings.SFile.GetDouble("OccModVac", 0.9);
        set { KBaseSettings.SFile.SetDouble("OccModVac", value); }
    }

    public static bool LimitedTime
    {
        get => KBaseSettings.SFile.GetBool("LimitedTime", true);
        set { KBaseSettings.SFile.SetBool("LimitedTime", value); }
    }

    public static bool Porkchop
    {
        get => KBaseSettings.SFile.GetBool("Porkchop", true);
        set { KBaseSettings.SFile.SetBool("Porkchop", value); }
    }
}