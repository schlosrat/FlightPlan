using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using UnityEngine;
using BepInEx.Logging;
using KSP.Messages;
using KSP.Sim.DeltaV;
using BepInEx.Bootstrap;
using SpaceWarp.API.Mods;

namespace FPUtilities;

public static class FPUtility
{
    public static VesselComponent activeVessel;
    public static ManeuverNodeData currentNode;
    // public static string LayoutPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MicroLayout.json");
    private static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ManeuverNodeController.Utility");
    public static GameStateConfiguration gameState;
    public static MessageCenter MessageCenter;
    // public static VesselDeltaVComponent VesselDeltaVComponentOAB;
    public static string InputDisableWindowAbbreviation = "WindowAbbreviation";
    public static string InputDisableWindowName = "WindowName";

    /// <summary>
    /// Refreshes the activeVessel and currentNode
    /// </summary>
    public static void RefreshActiveVesselAndCurrentManeuver()
    {
        activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        currentNode = activeVessel != null ? GameManager.Instance?.Game?.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.GlobalId).FirstOrDefault(): null;
    }

    public static void RefreshGameManager()
    {
        gameState = GameManager.Instance?.Game?.GlobalGameState?.GetGameState();
        // MessageCenter = GameManager.Instance?.Game?.Messages;
    }

    //public static void RefreshStagesOAB()
    //{
    //    VesselDeltaVComponentOAB = GameManager.Instance?.Game?.OAB?.Current?.Stats?.MainAssembly?.VesselDeltaV;
    //}

    //public static string DegreesToDMS(double degreeD)
    //{
    //    var ts = TimeSpan.FromHours(Math.Abs(degreeD));
    //    int degrees = (int)Math.Floor(ts.TotalHours);
    //    int minutes = ts.Minutes;
    //    int seconds = ts.Seconds;

    //    string result = $"{degrees:N0}<color={Styles.UnitColorHex}>°</color> {minutes:00}<color={Styles.UnitColorHex}>'</color> {seconds:00}<color={Styles.UnitColorHex}>\"</color>";

    //    return result;
    //}

    //public static string MetersToDistanceString(double heightInMeters)
    //{
    //    return $"{heightInMeters:N0}";
    //}

    public static string SecondsToTimeString(double seconds, bool addSpacing = true, bool returnLastUnit = false)
    {
        if (seconds == Double.PositiveInfinity)
        {
            return "∞";
        }
        else if (seconds == Double.NegativeInfinity)
        {
            return "-∞";
        }

        var cap = Math.Floor(seconds);

        string result = "";
        string spacing = "";
        if (addSpacing)
        {
            spacing = " ";
        }

        if (seconds < 0)
        {
            result += "-";
            seconds = Math.Abs(seconds);
        }

        int days = (int)(cap / 21600);
        int hours = (int)((cap - (days * 21600)) / 3600);
        int minutes = (int)((cap - (hours * 3600) - (days * 21600)) / 60);
        double secs = (seconds - (days * 21600) - (hours * 3600) - (minutes * 60));

        if (days > 0)
        {
            result += $"{days}{spacing}d ";
        }

        if (hours > 0 || days > 0)
        {
            {
                result += $"{hours}:";
            }
        }

        if (minutes > 0 || hours > 0 || days > 0)
        {
            if (hours > 0 || days > 0)
            {
                result += $"{minutes:00.}:";
            }
            else
            {
                result += $"{minutes}:";
            }
        }

        if (minutes > 0 || hours > 0 || days > 0)
        {
            result += returnLastUnit ? $"{secs:00.00}{spacing}" : $"{secs:00.00}";
        }
        else
        {
            result += returnLastUnit ? $"{secs:00.00}{spacing}" : $"{secs:00.00}";
        }

        return result;
    }

    //public static string SituationToString(VesselSituations situation)
    //{
    //    return situation switch
    //    {
    //        VesselSituations.PreLaunch => "Pre-Launch",
    //        VesselSituations.Landed => "Landed",
    //        VesselSituations.Splashed => "Splashed down",
    //        VesselSituations.Flying => "Flying",
    //        VesselSituations.SubOrbital => "Suborbital",
    //        VesselSituations.Orbiting => "Orbiting",
    //        VesselSituations.Escaping => "Escaping",
    //        _ => "UNKNOWN",
    //    };
    //}

    //public static string BiomeToString(BiomeSurfaceData biome)
    //{
    //    string result = biome.type.ToString().ToLower().Replace('_', ' ');
    //    return result.Substring(0, 1).ToUpper() + result.Substring(1);
    //}

    /// <summary>
	/// Validates if user entered a 3 character string
	/// </summary>
	/// <param name="abbreviation">String that will be shortened to 3 characters</param>
	/// <returns>Uppercase string shortened to 3 characters. If abbreviation is empty returns "CUS"</returns>
	// public static string ValidateAbbreviation(string abbreviation)
    // {
    //     if (String.IsNullOrEmpty(abbreviation))
    //         return "CUS";
    //     return abbreviation.Substring(0, Math.Min(abbreviation.Length, 3)).ToUpperInvariant();
    // }

    /// <summary>
    /// Check if current vessel has an active target (celestial body or vessel)
    /// </summary>
    /// <returns></returns>
    public static bool TargetExists()
    {
        try { return (activeVessel.TargetObject != null); }
        catch { return false; }
    }

    /// <summary>
    /// Checks if current vessel has a maneuver
    /// </summary>
    /// <returns></returns>
    public static bool ManeuverExists()
    {
        try { return (GameManager.Instance?.Game?.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.GlobalId).FirstOrDefault() != null); }
        catch { return false; }
    }


    internal static (int major, int minor, int patch)? GetModVersion(string modId)
    {
        var plugin = Chainloader.Plugins?.OfType<BaseSpaceWarpPlugin>().ToList().FirstOrDefault(p => p.SpaceWarpMetadata.ModID.ToLowerInvariant() == modId.ToLowerInvariant());
        string versionString = plugin?.SpaceWarpMetadata?.Version;

        string[] versionNumbers = versionString?.Split(new char[] { '.' }, 3);

        if (versionNumbers != null && versionNumbers.Length >= 1)
        {
            int majorVersion = 0;
            int minorVersion = 0;
            int patchVersion = 0;

            if (versionNumbers.Length >= 1)
                int.TryParse(versionNumbers[0], out majorVersion);
            if (versionNumbers.Length >= 2)
                int.TryParse(versionNumbers[1], out minorVersion);
            if (versionNumbers.Length == 3)
                int.TryParse(versionNumbers[2], out patchVersion);

            Logger.LogInfo($"Space Warp version {majorVersion}.{minorVersion}.{patchVersion} detected.");

            return (majorVersion, minorVersion, patchVersion);
        }
        else return null;
    }

    /// <summary>
    /// Check if installed mod is older than the specified version
    /// </summary>
    /// <param name="modId">SpaceWarp mod ID</param>
    /// <param name="major">Specified major version (X.0.0)</param>
    /// <param name="minor">Specified minor version (0.X.0)</param>
    /// <param name="patch">Specified patch version (0.0.X)</param>
    /// <returns>True = installed mod is older. False = installed mod has the same version or it's newer or version isn't declared or version declared is gibberish that cannot be parsed</returns>
    internal static bool IsModOlderThan (string modId, int major, int minor, int patch)
    {
        var modVersion = GetModVersion(modId);

        if (!modVersion.HasValue || modVersion.Value == (0, 0, 0))
            return false;

        if (modVersion.Value.Item1 < major)
            return true;
        else if (modVersion.Value.Item1 > major)
            return false;

        if (modVersion.Value.Item2 < minor)
            return true;
        else if (modVersion.Value.Item2 > minor)
            return false;

        if (modVersion.Value.Item3 < patch)
            return true;
        else
            return false;
    }
}
