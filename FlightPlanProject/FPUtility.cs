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
    public static VesselComponent ActiveVessel;
    public static ManeuverNodeData CurrentNode;
    // public static string LayoutPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MicroLayout.json");
    private static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ManeuverNodeController.Utility");
    public static GameStateConfiguration GameState;
    public static MessageCenter MessageCenter;
    // public static VesselDeltaVComponent VesselDeltaVComponentOAB;
    public static string InputDisableWindowAbbreviation = "WindowAbbreviation";
    public static string InputDisableWindowName = "WindowName";

    /// <summary>
    /// Refreshes the ActiveVessel and CurrentNode
    /// </summary>
    public static void RefreshActiveVesselAndCurrentManeuver()
    {
        ActiveVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        CurrentNode = ActiveVessel != null ? GameManager.Instance?.Game?.SpaceSimulation.Maneuvers.GetNodesForVessel(ActiveVessel.GlobalId).FirstOrDefault(): null;
    }

    public static void RefreshGameManager()
    {
        GameState = GameManager.Instance?.Game?.GlobalGameState?.GetGameState();
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
    //    int _minutes = ts.Minutes;
    //    int seconds = ts.Seconds;

    //    string _result = $"{degrees:N0}<color={Styles.UnitColorHex}>°</color> {_minutes:00}<color={Styles.UnitColorHex}>'</color> {seconds:00}<color={Styles.UnitColorHex}>\"</color>";

    //    return _result;
    //}

    public static string MetersToDistanceString(double heightInMeters)
    {
        return $"{heightInMeters:N0}";
    }

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

        double _cap = Math.Floor(seconds);

        string _result = "";
        string _spacing = "";
        if (addSpacing)
        {
            _spacing = " ";
        }

        if (seconds < 0)
        {
            _result += "-";
            seconds = Math.Abs(seconds);
        }

        int _days = (int)(_cap / 21600);
        int _hours = (int)((_cap - (_days * 21600)) / 3600);
        int _minutes = (int)((_cap - (_hours * 3600) - (_days * 21600)) / 60);
        double _secs = (seconds - (_days * 21600) - (_hours * 3600) - (_minutes * 60));

        if (_days > 0)
        {
            _result += $"{_days}{_spacing}d ";
        }

        if (_hours > 0 || _days > 0)
        {
            {
                _result += $"{_hours}:";
            }
        }

        if (_minutes > 0 || _hours > 0 || _days > 0)
        {
            if (_hours > 0 || _days > 0)
            {
                _result += $"{_minutes:00.}:";
            }
            else
            {
                _result += $"{_minutes}:";
            }
        }

        if (_minutes > 0 || _hours > 0 || _days > 0)
        {
            _result += returnLastUnit ? $"{_secs:00.00}{_spacing}" : $"{_secs:00.00}";
        }
        else
        {
            _result += returnLastUnit ? $"{_secs:00.00}{_spacing}" : $"{_secs:00.00}";
        }

        return _result;
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
    //    string _result = biome.type.ToString().ToLower().Replace('_', ' ');
    //    return _result.Substring(0, 1).ToUpper() + _result.Substring(1);
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
        try { return (ActiveVessel.TargetObject != null); }
        catch { return false; }
    }

    /// <summary>
    /// Checks if current vessel has a maneuver
    /// </summary>
    /// <returns></returns>
    public static bool ManeuverExists()
    {
        try { return (GameManager.Instance?.Game?.SpaceSimulation.Maneuvers.GetNodesForVessel(ActiveVessel.GlobalId).FirstOrDefault() != null); }
        catch { return false; }
    }


    internal static (int major, int minor, int patch)? GetModVersion(string modId)
    {
        BaseSpaceWarpPlugin _plugin = Chainloader.Plugins?.OfType<BaseSpaceWarpPlugin>().ToList().FirstOrDefault(p => p.SpaceWarpMetadata.ModID.ToLowerInvariant() == modId.ToLowerInvariant());
        string _versionString = _plugin?.SpaceWarpMetadata?.Version;

        string[] _versionNumbers = _versionString?.Split(new char[] { '.' }, 3);

        if (_versionNumbers != null && _versionNumbers.Length >= 1)
        {
            int _majorVersion = 0;
            int _minorVersion = 0;
            int _patchVersion = 0;

            if (_versionNumbers.Length >= 1)
                int.TryParse(_versionNumbers[0], out _majorVersion);
            if (_versionNumbers.Length >= 2)
                int.TryParse(_versionNumbers[1], out _minorVersion);
            if (_versionNumbers.Length == 3)
                int.TryParse(_versionNumbers[2], out _patchVersion);

            Logger.LogInfo($"Space Warp version {_majorVersion}.{_minorVersion}.{_patchVersion} detected.");

            return (_majorVersion, _minorVersion, _patchVersion);
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
        var _modVersion = GetModVersion(modId);

        if (!_modVersion.HasValue || _modVersion.Value == (0, 0, 0))
            return false;

        if (_modVersion.Value.Item1 < major)
            return true;
        else if (_modVersion.Value.Item1 > major)
            return false;

        if (_modVersion.Value.Item2 < minor)
            return true;
        else if (_modVersion.Value.Item2 > minor)
            return false;

        if (_modVersion.Value.Item3 < patch)
            return true;
        else
            return false;
    }
}
