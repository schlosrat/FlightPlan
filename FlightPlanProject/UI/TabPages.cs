using FlightPlan.KTools.UI;
using FPUtilities;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using MuMech;
using SpaceWarp.API.Assets;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace FlightPlan;

public class BasePageContent : IPageContent
{
    public BasePageContent()
    {
        this.MainUI = FlightPlanUI.Instance;
        this.Plugin = FlightPlanPlugin.Instance;
    }
    protected FlightPlanUI MainUI;
    protected FlightPlanPlugin Plugin;


    protected PatchedConicsOrbit Orbit => MainUI.Orbit;
    protected CelestialBodyComponent ReferenceBody => MainUI.ReferenceBody;

    public virtual string Name => throw new NotImplementedException();

    public virtual GUIContent Icon => throw new NotImplementedException();

    public bool IsRunning => false;


    bool _uiVisible;
    public bool UIVisible { get => _uiVisible; set => _uiVisible = value; }

    public virtual bool IsActive => throw new NotImplementedException();

    public virtual void OnGUI()
    {
        throw new NotImplementedException();
    }
}

public class OwnshipManeuversPage : BasePageContent
{
    public override string Name => "Own Orbit";

    readonly Texture2D _tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/OSM_50.png");

    public override GUIContent Icon => new(_tabIcon, "Ownship Maneuvers");

    public override bool IsActive => true;

    public override void OnGUI()
    {
        FPStyles.DrawSectionHeader("Ownship Maneuvers");
        BurnTimeOption.Instance.OptionSelectionGUI();
        MainUI.DrawToggleButton("Circularize", ManeuverType.circularize);
        // GUILayout.EndHorizontal();

        FPSettings.PeAltitude_km = MainUI.DrawToggleButtonWithTextField("New Pe", ManeuverType.newPe, FPSettings.PeAltitude_km, "km");
        MainUI.TargetPeR = FPSettings.PeAltitude_km * 1000 + ReferenceBody.radius;

        if (MainUI.Orbit.eccentricity < 1)
        {
            FPSettings.ApAltitude_km = MainUI.DrawToggleButtonWithTextField("New Ap", ManeuverType.newAp, FPSettings.ApAltitude_km, "km");
            MainUI.TargetApR = FPSettings.ApAltitude_km * 1000 + ReferenceBody.radius;
            MainUI.DrawToggleButton("New Pe & Ap", ManeuverType.newPeAp);
        }

        FPSettings.TargetInc_deg = MainUI.DrawToggleButtonWithTextField("New Inclination", ManeuverType.newInc, FPSettings.TargetInc_deg, "°");

        if (Plugin._experimental.Value)
        {
            FPSettings.TargetLAN_deg = MainUI.DrawToggleButtonWithTextField("New LAN", ManeuverType.newLAN, FPSettings.TargetLAN_deg, "°");

            // FPSettings.TargetNodeLong_deg = DrawToggleButtonWithTextField("New Node Longitude", ref newNodeLon, FPSettings.TargetNodeLong_deg, "°");
        }

        FPSettings.TargetSMA_km = MainUI.DrawToggleButtonWithTextField("New SMA", ManeuverType.newSMA, FPSettings.TargetSMA_km, "km");
        MainUI.TargetSMA = FPSettings.TargetSMA_km * 1000 + ReferenceBody.radius;
    }
}

public class TargetPageShip2Ship : BasePageContent
{
    public override string Name => "Target";

    readonly Texture2D _tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/TRM_50_Ship2Ship.png");

    public override GUIContent Icon => new(_tabIcon, "Target Relative Maneuvers");

    public override bool IsActive
    {
        //get => Plugin._currentTarget != null  // If there is a target
        //    && (Plugin._currentTarget.IsVessel || Plugin._currentTarget.IsPart) // And the target is a vessel or a part of a vessel (docking port?)
        //    && Plugin._currentTarget?.orbit.referenceBody.Name == referenceBody.Name; // If the ActiveVessel and the target are both orbiting the same body
        get
        {
            if (Plugin._currentTarget == null) return false;
            if (!Plugin._currentTarget.IsVessel && !Plugin._currentTarget.IsPart) return false;
            string referenceBodyName = Plugin._currentTarget.IsPart
                ? Plugin._currentTarget.Part.PartOwner.SimulationObject.Vessel.Orbit.referenceBody.Name
                : Plugin._currentTarget.Orbit.referenceBody.Name;
            return referenceBodyName == ReferenceBody.Name;
        }
    }

    public override void OnGUI()
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit targetOrbit;
        string recommendedManeuver;
        const int maxPhasingOrbits = 5;
        const double closestApproachLimit1 = 3000;
        const double closestApproachLimit2 = 100;
        double targetDistance;

        FPStyles.DrawSectionHeader("Target Relative Maneuvers");

        BurnTimeOption.Instance.OptionSelectionGUI();

        if (Plugin._currentTarget.IsVessel)
        {
            // DockingPortSelectionGUI();
            targetOrbit = Plugin._currentTarget.Orbit as PatchedConicsOrbit;
            targetDistance = (Orbit.SwappedAbsolutePositionAtUT(UT) - targetOrbit.SwappedAbsolutePositionAtUT(UT)).magnitude;
        }
        else
        {
            targetOrbit = Plugin._currentTarget.Part.PartOwner.SimulationObject.Vessel.Orbit;
            targetDistance = (Orbit.SwappedAbsolutePositionAtUT(UT) - targetOrbit.SwappedAbsolutePositionAtUT(UT)).magnitude;
        }
        if (targetDistance < closestApproachLimit1)
            TargetSelection.SelectDockingPort = UI_Tools.SmallToggleButton(TargetSelection.SelectDockingPort, "Select Docking Port", "Select Docking Port");

        double synodicPeriod = Orbit.SynodicPeriod(targetOrbit);
        double timeToClosestApproach = Orbit.NextClosestApproachTime(targetOrbit, UT + 1);
        double closestApproach = (Orbit.SwappedAbsolutePositionAtUT(timeToClosestApproach) - targetOrbit.SwappedAbsolutePositionAtUT(timeToClosestApproach)).magnitude;
        double relativeInc = Orbit.inclination - targetOrbit.inclination;
        double phase = Orbit.PhaseAngle(targetOrbit, UT);
        double transfer = Orbit.Transfer(targetOrbit, out _);
        double nextWindow = synodicPeriod * (transfer - phase) / 360;
        while (nextWindow < 0) nextWindow += synodicPeriod;
        MainUI.DrawEntry("Target Orbit:", $"{targetOrbit.PeriapsisArl / 1000:N0} km x {targetOrbit.ApoapsisArl / 1000:N0} km");
        MainUI.DrawEntry("Current Orbit:", $"{Orbit.PeriapsisArl / 1000:N0} km x {Orbit.ApoapsisArl / 1000:N0} km");
        MainUI.DrawEntry("Relative Inclination:", $"{relativeInc:N2} deg");
        MainUI.DrawEntry("Synodic Period", FPUtility.SecondsToTimeString(synodicPeriod), " ");
        MainUI.DrawEntry("Next Window:", FPUtility.SecondsToTimeString(nextWindow));
        MainUI.DrawEntry("Next Closest Apporoach:", FPUtility.SecondsToTimeString(timeToClosestApproach));
        if (closestApproach > 1000)
            MainUI.DrawEntry("Separation at CA:", $"{closestApproach/1000:N1} km");
        else
        {
            MainUI.DrawEntry("Separation at CA:", $"{closestApproach:N1} m");
            MainUI.DrawEntry("Relative Velocity:", $"{(Orbit.SwappedOrbitalVelocityAtUT(UT) - targetOrbit.SwappedOrbitalVelocityAtUT(UT)).magnitude:N1} m/s");
        }

        MainUI.DrawToggleButton("Match Planes", ManeuverType.matchPlane);
        FPSettings.ApAltitude_km = MainUI.DrawToggleButtonWithTextField("New Ap", ManeuverType.newAp, FPSettings.ApAltitude_km, "km");
        MainUI.DrawToggleButton("Circularize", ManeuverType.circularize);
        MainUI.DrawToggleButton("Hohmann Transfer", ManeuverType.hohmannXfer);
        MainUI.DrawToggleButton("Match Velocity", ManeuverType.matchVelocity);

        if (Plugin._experimental.Value)
        {
            FPSettings.InterceptTime = MainUI.DrawToggleButtonWithTextField("Intercept", ManeuverType.interceptTgt, FPSettings.InterceptTime, "s", true);
        }
        // MainUI.DrawToggleButton("Course Correction", ManeuverType.courseCorrection);

        recommendedManeuver = "None";
        if (targetDistance < closestApproachLimit2)
            recommendedManeuver = "Ready for docking";
        else if (relativeInc > 1)
            recommendedManeuver = "Next Action: Match planes for rendezvous";
        else if (nextWindow / Orbit.period > maxPhasingOrbits)
            recommendedManeuver = $"Next intercept window would be {nextWindow/Orbit.period:N1} orbits away, which is more than the maximum of {maxPhasingOrbits} phasing orbits. Increase phasing rate by establishing a new phasing orbit at {(targetOrbit.semiMajorAxis - ReferenceBody.radius)*2:N0} km.";
        else if (closestApproach > closestApproachLimit1)
            recommendedManeuver = $"Next Action: Perform Hohmann Transfer to target";
        else if (closestApproach > closestApproachLimit2)
            recommendedManeuver = $"Next Action: Close distance to target. HINT: Point at target, burn GENTLY toward target, Match Velocity at closest approch. Rinse and repeat until distance < {closestApproachLimit2} m";

        MainUI.DrawEntry(recommendedManeuver);

    }
    private void DockingPortSelectionGUI()
    {
        MainUI.DrawEntry("Docking Port Selection GUI", "Comming Soon!");
    }
}

public class TargetPageShip2Celestial : BasePageContent
{
    public override string Name => "Target";

    // readonly Texture2D _tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/TargetRelManeuver_50v2.png");
    readonly Texture2D _tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/TRM_50_Ship2Celestial.png");

    public override GUIContent Icon => new(_tabIcon, "Target Relative Maneuvers");

    public override bool IsActive
    {
        get
        {
            if (Plugin._currentTarget == null) return false;  // If there is no target, then no tab
            if (!Plugin._currentTarget.IsCelestialBody) return false; // If the target is not a Celestial, then no tab
            if (Plugin._currentTarget.Orbit == null) return false; // And the target is a star, then no tab
            return Plugin._currentTarget.Orbit.referenceBody.Name == ReferenceBody.Name; // If the ActiveVessel and the _currentTarget are both orbiting the same body
        }
    }

    public override void OnGUI()
    {
        FPStyles.DrawSectionHeader("Target Relative Maneuvers");

        BurnTimeOption.Instance.OptionSelectionGUI();

        MainUI.DrawToggleButton("Match Planes", ManeuverType.matchPlane);
        MainUI.DrawToggleButton("Hohmann Transfer", ManeuverType.hohmannXfer);
        MainUI.DrawToggleButton("Course Correction", ManeuverType.courseCorrection);

        if (Plugin._experimental.Value)
        {
            FPSettings.InterceptTime = MainUI.DrawToggleButtonWithTextField("Intercept", ManeuverType.interceptTgt, FPSettings.InterceptTime, "s");
            MainUI.DrawToggleButton("Match Velocity", ManeuverType.matchVelocity);
        }
    }
}

public class InterplanetaryPage : BasePageContent
{
    public override string Name => "Target";

    readonly Texture2D _tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/OTM_50_Planet.png");

    public override GUIContent Icon => new(_tabIcon, "Orbital Transfer Maneuvers");

    public override bool IsActive
    {
        get
        {
            if (Plugin._currentTarget == null) return false; // If there is no target then no tab
            if (ReferenceBody.IsStar) return false; // If we're orbiting a star then no tab
            if (!Plugin._currentTarget.IsCelestialBody) return false; // the current target is not a celestial object then no tab
            if (!ReferenceBody.Orbit.referenceBody.IsStar) return false; // If we're not at a planet then no tab
            if (Plugin._currentTarget.Orbit == null) return false; // if current target is a star then no tab
            if (!Plugin._currentTarget.Orbit.referenceBody.IsStar) return false; // If our target is not a planet then no tab
            return Plugin._currentTarget.Name != ReferenceBody.Name;// if we're targeting the same planet we're orbiting then no tab
        }
    }

    public override void OnGUI()
    {
        FPStyles.DrawSectionHeader("Orbital Transfer Maneuvers");
        BurnTimeOption.Instance.OptionSelectionGUI();

        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit targetOrbit = Plugin._currentTarget.Orbit as PatchedConicsOrbit;
        double synodicPeriod = ReferenceBody.Orbit.SynodicPeriod(targetOrbit);
        double phase = ReferenceBody.Orbit.PhaseAngle(targetOrbit, UT);
        double transfer = Plugin._activeVessel.Orbit.referenceBody.Orbit.Transfer(targetOrbit, out double _transferTime);
        double nextWindow = synodicPeriod * (transfer - phase) / 360;
        if (nextWindow < 0) nextWindow += synodicPeriod;
        // Display Transfer Info
        MainUI.DrawEntry($"Phase Angle to {Plugin._currentTarget.Name}", phase.ToString(), "°");
        MainUI.DrawEntry("Transfer Window Phase Angle", transfer.ToString(), "°");
        MainUI.DrawEntry("Transfer Time", FPUtility.SecondsToTimeString(_transferTime), " ");
        MainUI.DrawEntry("Synodic Period", FPUtility.SecondsToTimeString(synodicPeriod), " ");
        MainUI.DrawEntry("Time to Next Window", FPUtility.SecondsToTimeString(nextWindow), " ");
        MainUI.DrawEntry("Aproximate Eject DeltaV", DeltaV().ToString(), "m/s");

        if (Plugin._experimental.Value) // No maneuvers relative to a star
        {
            MainUI.DrawToggleButton("Interplanetary Transfer", ManeuverType.planetaryXfer);
        }
        else
        {
            // Let the user know they need to switch on Experimental Features to get this functionality
            GUILayout.Label("No non-experimental capabilities available. Turn on <b>Experimental Features</b> in the Flight Plan <b>Configuration Menu</b> (Press Alt-M, click Open Configuration Manager) to access maneuvers from this tab.", KBaseStyle.Warning);
        }
    }

    double Phase()
    {
        // GameInstance game = GameManager.Instance.Game;
        // Plugin._activeVessel
        // SimulationObjectModel target = Plugin._currentTarget; // game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;
        CelestialBodyComponent cur = Plugin._activeVessel.Orbit.referenceBody; // game.ViewController.GetActiveVehicle(true)?.GetSimVessel().orbit.referenceBody;

        // This deals with if we're at a moon and backing thing off so that cur would be the planet about which this moon is orbitting
        while (cur.Orbit.referenceBody.Name != Plugin._currentTarget.Orbit.referenceBody.Name)
        {
            cur = cur.Orbit.referenceBody;
        }

        CelestialBodyComponent star = Plugin._currentTarget.CelestialBody.GetRelevantStar();
        Vector3d to = star.coordinateSystem.ToLocalPosition(Plugin._currentTarget.Position); // radius vector of destination planet
        Vector3d from = star.coordinateSystem.ToLocalPosition(cur.Position); // radius vector of origin planet

        double phase = Vector3d.SignedAngle(to, from, Vector3d.up);
        return Math.Round(phase, 1);
    }

    double Transfer(out double time)
    {
        // GameInstance game = GameManager.Instance.Game;
        // SimulationObjectModel target = Plugin._currentTarget; // game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;
        CelestialBodyComponent cur = Plugin._activeVessel.Orbit.referenceBody; // game.ViewController.GetActiveVehicle(true)?.GetSimVessel().orbit.referenceBody;

        double ellipseA, transfer;

        // This deals with if we're at a moon and backing thing off so that cur would be the planet about which this moon is orbitting
        while (cur.Orbit.referenceBody.Name != Plugin._currentTarget.Orbit.referenceBody.Name)
        {
            cur = cur.Orbit.referenceBody;
        }

        IKeplerOrbit targetOrbit = Plugin._currentTarget.Orbit;
        IKeplerOrbit currentOrbit = cur.Orbit;

        ellipseA = (targetOrbit.semiMajorAxis + currentOrbit.semiMajorAxis) / 2;
        time = Mathf.PI * Mathf.Sqrt((float)((ellipseA) * (ellipseA) * (ellipseA)) / ((float)targetOrbit.referenceBody.gravParameter));

        transfer = 180 - ((time / targetOrbit.period) * 360);
        while (transfer < -180) { transfer += 360; }
        return Math.Round(transfer, 1);
    }

    double DeltaV()
    {
        // GameInstance game = GameManager.Instance.Game;
        // SimulationObjectModel target = Plugin._currentTarget; // game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;
        CelestialBodyComponent cur = Plugin._activeVessel.Orbit.referenceBody; // game.ViewController.GetActiveVehicle(true)?.GetSimVessel().orbit.referenceBody;

        // This deals with if we're at a moon and backing thing off so that cur would be the planet about which this moon is orbitting
        while (cur.Orbit.referenceBody.Name != Plugin._currentTarget.Orbit.referenceBody.Name)
        {
            cur = cur.Orbit.referenceBody;
        }

        IKeplerOrbit targetOrbit = Plugin._currentTarget.Orbit;
        IKeplerOrbit currentOrbit = cur.Orbit;

        double sunEject;
        double ellipseA = (targetOrbit.semiMajorAxis + currentOrbit.semiMajorAxis) / 2;
        CelestialBodyComponent star = targetOrbit.referenceBody;

        sunEject = Mathf.Sqrt((float)(star.gravParameter) / (float)currentOrbit.semiMajorAxis) * (Mathf.Sqrt((float)targetOrbit.semiMajorAxis / (float)ellipseA) - 1);

        VesselComponent ship = Plugin._activeVessel; // game.ViewController.GetActiveVehicle(true)?.GetSimVessel(true);
        double eject = Mathf.Sqrt((2 * (float)(cur.gravParameter) * ((1 / (float)ship.Orbit.radius) - (float)(1 / cur.sphereOfInfluence))) + (float)(sunEject * sunEject));
        eject -= ship.Orbit.orbitalSpeed;

        return Math.Round(eject, 1);
    }
}


public class MoonPage : BasePageContent
{
    public override string Name => "Moon";

    readonly Texture2D _tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/OTM_50_Moon.png");

    public override GUIContent Icon => new(_tabIcon, "Orbital Transfer Maneuvers");

    public override bool IsActive
    {
        get
        {
            if (ReferenceBody.IsStar) return false; // if were orbiting a star, then no tab
            if (ReferenceBody.Orbit.referenceBody.IsStar) return false; // If we're orbiting a planet, then no tab
            return Orbit.eccentricity < 1;
        }
    }
    public override void OnGUI()
    {
        FPStyles.DrawSectionHeader("Orbital Transfer Maneuvers");

        var parentPlanet = ReferenceBody.Orbit.referenceBody;
        FPSettings.MoonReturnAltitude_km = MainUI.DrawToggleButtonWithTextField("Moon Return", ManeuverType.moonReturn, FPSettings.MoonReturnAltitude_km, "km");
        MainUI.TargetMRPeR = FPSettings.MoonReturnAltitude_km * 1000 + parentPlanet.radius;
    }
}

public class ResonantOrbitPage : BasePageContent
{
    // Specialized buttons just for this tab
    private bool _nSatUp, _nSatDown, _nOrbUp, _nOrbDown, _setTgtPe, _setTgtAp, _setTgtSync, _setTgtSemiSync, _setTgtMinLOS;

    // Data this class needs to share between it's methods
    private double _synchronousAlt;
    private double _semiSynchronousAlt;
    private double _minLOSAlt;
    private string _targetAltitude = "600";      // String planned altitide for deployed satellites (destiantion orbit)
    private double _target_alt_km = 600;         // Double planned altitide for deployed satellites (destiantion orbit)
    private double _satPeriod;                   // The period of the destination orbit
    private double _xferPeriod;                  // The period of the resonant deploy orbit (_xferPeriod = _resonance*_satPeriod)
    // private bool _dive_error = false;

    // Data other classes and methods will need (needed to handle fixAp and fixPe maneuvers)
    public static double Ap2 { get; set; } // The resonant deploy orbit apoapsis
    public static double Pe2 { get; set;  } // The resonant deploy orbit periapsis

    public override string Name => "Resonant Orbit";

    readonly Texture2D _tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/ROM_50.png");

    public override GUIContent Icon => new(_tabIcon, "Resonant Orbit Maneuvers");

    public override bool IsActive => true;

    public override void OnGUI()
    {
        FPStyles.DrawSectionHeader("Resonant Orbit Maneuvers");
        BurnTimeOption.Instance.OptionSelectionGUI();

        // Data only needed inside this method
        double _synchronousPeriod = Plugin._activeVessel.mainBody.rotationPeriod;
        double _semiSynchronousPeriod = Plugin._activeVessel.mainBody.rotationPeriod / 2;
        _synchronousAlt = SMACalc(_synchronousPeriod);
        _semiSynchronousAlt = SMACalc(_semiSynchronousPeriod);
        int _n, _m;

        // Determine if synchronous or semi-synchronous orbits are possible for this body
        if (_synchronousAlt > Plugin._activeVessel.mainBody.sphereOfInfluence)
        {
            _synchronousAlt = -1;
        }
        if (_semiSynchronousAlt > Plugin._activeVessel.mainBody.sphereOfInfluence)
        {
            _semiSynchronousAlt = -1;
        }

        // Set the _resonance factors based on diving or not
        _m = FPSettings.NumSats * FPSettings.NumOrbits;
        if (FPSettings.DiveOrbit) // If we're going to dive under the target orbit for the deployment orbit
            _n = _m - 1;
        else // If not
            _n = _m + 1;
        double _resonance = (double)_n / _m;
        string _resonanceStr = String.Format("{0}/{1}", _n, _m);

        // Compute the minimum LOS altitude
        _minLOSAlt = MinLOSCalc(FPSettings.NumSats, Plugin._activeVessel.mainBody.radius, Plugin._activeVessel.mainBody.hasAtmosphere);

        MainUI.DrawEntry2Button("Payloads:", ref _nSatUp, "+", ref _nSatDown, "-", FPSettings.NumSats.ToString(), "", "/"); // was numSatellites
        MainUI.DrawEntry2Button("Deploy Orbits:", ref _nOrbUp, "+", ref _nOrbDown, "-", FPSettings.NumOrbits.ToString(), "", "/"); // was numOrbits
        MainUI.DrawEntry("Orbital Resonance", _resonanceStr, " ");

        MainUI.DrawEntryTextField("Target Altitude", ref _targetAltitude, "km"); // Tried" FPSettings.tgt_altitude_km 
        bool pass = double.TryParse(_targetAltitude, out _target_alt_km);

        MainUI.DrawEntryButton("Apoapsis", ref _setTgtAp, "⦾", $"{FPUtility.MetersToDistanceString(Plugin._activeVessel.Orbit.ApoapsisArl / 1000)}", "km");
        MainUI.DrawEntryButton("Periapsis", ref _setTgtPe, "⦾", $"{FPUtility.MetersToDistanceString(Plugin._activeVessel.Orbit.PeriapsisArl / 1000)}", "km");

        _satPeriod = PeriodCalc(_target_alt_km * 1000 + Plugin._activeVessel.mainBody.radius);

        if (_synchronousAlt > 0)
        {
            MainUI.DrawEntryButton("Synchronous Alt", ref _setTgtSync, "⦾", $"{FPUtility.MetersToDistanceString(_synchronousAlt / 1000)}", "km");
            MainUI.DrawEntryButton("Semi Synchronous Alt", ref _setTgtSemiSync, "⦾", $"{FPUtility.MetersToDistanceString(_semiSynchronousAlt / 1000)}", "km");
        }
        else if (_semiSynchronousAlt > 0)
        {
            MainUI.DrawEntry("Synchronous Alt", "Outside SOI", " ");
            MainUI.DrawEntryButton("Semi Synchronous Alt", ref _setTgtSemiSync, "⦾", $"{FPUtility.MetersToDistanceString(_semiSynchronousAlt / 1000)}", "km");
        }
        else
        {
            MainUI.DrawEntry("Synchronous Alt", "Outside SOI", " ");
            MainUI.DrawEntry("Semi Synchronous Alt", "Outside SOI", " ");
        }
        MainUI.DrawEntry("SOI Alt", $"{FPUtility.MetersToDistanceString(Plugin._activeVessel.mainBody.sphereOfInfluence / 1000)}", "km");
        if (_minLOSAlt > 0)
        {
            MainUI.DrawEntryButton("Min LOS Orbit Alt", ref _setTgtMinLOS, "⦾", $"{FPUtility.MetersToDistanceString(_minLOSAlt / 1000)}", "km");
        }
        else
        {
            MainUI.DrawEntry("Min LOS Orbit Alt", "Undefined", "km");
        }
        FPSettings.Occlusion = MainUI.DrawSoloToggle("<b>Occlusion</b>", FPSettings.Occlusion);
        if (FPSettings.Occlusion)
        {
            FPSettings.OccModAtm = MainUI.DrawEntryTextField("Atm", FPSettings.OccModAtm, "  ", KBaseStyle.TextInputStyle);
            // GUILayout.Space(-FPStyles.SpacingAfterEntry);
            FPSettings.OccModVac = MainUI.DrawEntryTextField("Vac", FPSettings.OccModVac, "  ", KBaseStyle.TextInputStyle);
            // GUILayout.Space(-FPStyles.SpacingAfterEntry);
        }

        // period1 = PeriodCalc(_target_alt_km*1000 + ActiveVessel.mainBody.radius);
        _xferPeriod = _resonance * _satPeriod;
        double _SMA2 = SMACalc(_xferPeriod);
        double _sSMA = _target_alt_km * 1000 + Plugin._activeVessel.mainBody.radius;
        double _divePe = 2.0 * _SMA2 - _sSMA;
        if (_divePe < Plugin._activeVessel.mainBody.radius) // No diving in the shallow end of the pool!
        {
            FPSettings.DiveOrbit = false;
            FPSettings.DiveOrbit = MainUI.DrawSoloToggle("<b>Dive</b>", FPSettings.DiveOrbit, true);
        }
        else
            FPSettings.DiveOrbit = MainUI.DrawSoloToggle("<b>Dive</b>", FPSettings.DiveOrbit);

        if (FPSettings.DiveOrbit)
        {
            Ap2 = _sSMA; // Diveing transfer orbits release at Apoapsis
            Pe2 = _divePe;
        }
        else
        {
            Pe2 = _sSMA; // Non-diving transfer orbits release at Periapsis
            Ap2 = 2.0 * _SMA2 - (Pe2);
        }
        double _ce = (Ap2 - Pe2) / (Ap2 + Pe2);
        
        MainUI.DrawEntry("Period", $"{FPUtility.SecondsToTimeString(_xferPeriod)}", "s");
        MainUI.DrawEntry("Apoapsis", $"{FPUtility.MetersToDistanceString((Ap2 - Plugin._activeVessel.mainBody.radius) / 1000)}", "km");
        MainUI.DrawEntry("Periapsis", $"{FPUtility.MetersToDistanceString((Pe2 - Plugin._activeVessel.mainBody.radius) / 1000)}", "km");
        MainUI.DrawEntry("Eccentricity", _ce.ToString("N3"), " ");
        double dV = BurnCalc(_sSMA, _sSMA, 0, Ap2, _SMA2, _ce, Plugin._activeVessel.mainBody.gravParameter);
        MainUI.DrawEntry("Injection Δv", dV.ToString("N3"), "m/s");

        double _errorPe = (Pe2 - Plugin._activeVessel.Orbit.Periapsis) / 1000;
        double _errorAp = (Ap2 - Plugin._activeVessel.Orbit.Apoapsis) / 1000;
        string _fixPeStr, _fixApStr;

        GUILayout.Space(-FPStyles.SpacingAfterSection);

        UI_Tools.Separator();

        if (_errorPe > 0)
            _fixPeStr = $"Raise to {((Pe2 - Plugin._activeVessel.mainBody.radius) / 1000):N2} km";
        else
            _fixPeStr = $"Lower to {((Pe2 - Plugin._activeVessel.mainBody.radius) / 1000):N2} km";
        if (_errorAp > 0)
            _fixApStr = $"Raise to {((Ap2 - Plugin._activeVessel.mainBody.radius) / 1000):N2} km";
        else
            _fixApStr = $"Lower to {((Ap2 - Plugin._activeVessel.mainBody.radius) / 1000):N2} km";
        if (Plugin._activeVessel.Orbit.Apoapsis < Pe2)
        {
            MainUI.DrawToggleButtonWithLabel("Fix Ap", ManeuverType.fixAp, _fixApStr, "", 55);
        }
        else if (Plugin._activeVessel.Orbit.Periapsis > Ap2)
        {
            MainUI.DrawToggleButtonWithLabel("Fix Pe", ManeuverType.fixPe, _fixPeStr, "", 55);
        }
        else
        {
            if (Pe2 > Plugin._activeVessel.mainBody.radius)
                MainUI.DrawToggleButtonWithLabel("Fix Pe", ManeuverType.fixPe, _fixPeStr, "", 55);
            MainUI.DrawToggleButtonWithLabel("Fix Ap", ManeuverType.fixAp, _fixApStr, "", 55);
        }

        HandleButtons();
    }

    private double OccModCalc(bool hasAtmo) // Specific to Resonant Orbits
    {
        double _occMod;
        if (FPSettings.Occlusion)
        {
            if (hasAtmo)
            {
                _occMod = FPSettings.OccModAtm;
            }
            else
            {
                _occMod = FPSettings.OccModVac;
            }
        }
        else
        {
            _occMod = 1;
        }
        return _occMod;
    }

    private double MinLOSCalc(int numSat, double radius, bool hasAtmo) // Specific to Resonant Orbits
    {
        if (numSat > 2)
        {
            return (radius * OccModCalc(hasAtmo)) / (Math.Cos(0.5 * (2.0 * Math.PI / numSat))) - radius;
        }
        else
        {
            return -1;
        }
    }

    public double SMACalc(double period) // General Purpose: Compute SMA given orbital period - RELOCATE TO ?
    {
        double _SMA;
        _SMA = Math.Pow((period * Math.Sqrt(Plugin._activeVessel.mainBody.gravParameter) / (2.0 * Math.PI)), (2.0 / 3.0));
        return _SMA;
    }

    public double PeriodCalc(double SMA) // General Purpose: Compute orbital period given SMA - RELOCATE TO ?
    {
        double _period;
        _period = (2.0 * Math.PI * Math.Pow(SMA, 1.5)) / Math.Sqrt(Plugin._activeVessel.mainBody.gravParameter);
        return _period;
    }

    private double BurnCalc(double sAp, double sSMA, double se, double cAp, double cSMA, double ce, double bGM)
    {
        double sta = 0;
        double cta = 0;
        if (cAp == sAp) cta = 180;
        double sr = sSMA * (1 - Math.Pow(se, 2)) / (1 + (se * Math.Cos(sta)));
        double sdv = Math.Sqrt(bGM * ((2 / sr) - (1 / sSMA)));

        double cr = cSMA * (1 - Math.Pow(ce, 2)) / (1 + (ce * Math.Cos(cta)));
        double cdv = Math.Sqrt(bGM * ((2 / sr) - (1 / cSMA)));

        return Math.Round(100 * Math.Abs(sdv - cdv)) / 100;
    }

    private void HandleButtons()
    {
        if (_nSatDown || _nSatUp || _nOrbDown || _nOrbUp || _setTgtPe || _setTgtAp || _setTgtSync || _setTgtSemiSync || _setTgtMinLOS)
        {
            // burnParams = Vector3d.zero;
            if (_nSatDown && FPSettings.NumSats > 2)
            {
                FPSettings.NumSats--;
                // numSatellites = FPSettings.NumSats.ToString();
            }
            else if (_nSatUp)
            {
                FPSettings.NumSats++;
                // numSatellites = FPSettings.NumSats.ToString();
            }
            else if (_nOrbDown && FPSettings.NumOrbits > 1)
            {
                FPSettings.NumOrbits--;
                // numOrbits = FPSettings.num_orb.ToString();
            }
            else if (_nOrbUp)
            {
                FPSettings.NumOrbits++;
                // numOrbits = FPSettings.num_orb.ToString();
            }
            else if (_setTgtPe)
            {
                // Logger.LogInfo($"HandleButtons: Setting tgt_altitude_km to Periapsis {ActiveVessel.orbit.PeriapsisArl / 1000.0} km");
                _target_alt_km = Plugin._activeVessel.Orbit.PeriapsisArl / 1000.0;
                _targetAltitude = _target_alt_km.ToString("N3");
                // Logger.LogInfo($"HandleButtons: tgt_altitude_km set to {_targetAltitude} km");
            }
            else if (_setTgtAp)
            {
                // Logger.LogInfo($"HandleButtons: Setting tgt_altitude_km to Apoapsis {ActiveVessel.orbit.ApoapsisArl / 1000.0} km");
                _target_alt_km = Plugin._activeVessel.Orbit.ApoapsisArl / 1000.0;
                _targetAltitude = _target_alt_km.ToString("N3");
                // Logger.LogInfo($"HandleButtons: tgt_altitude_km set to {_targetAltitude} km");

            }
            else if (_setTgtSync)
            {
                // Logger.LogInfo($"HandleButtons: Setting tgt_altitude_km to _synchronousAlt {_synchronousAlt / 1000.0} km");
                _target_alt_km = _synchronousAlt / 1000.0;
                _targetAltitude = _target_alt_km.ToString("N3");
                // Logger.LogInfo($"HandleButtons: tgt_altitude_km set to {_targetAltitude} km");

            }
            else if (_setTgtSemiSync)
            {
                // Logger.LogInfo($"HandleButtons: Setting tgt_altitude_km to _semiSynchronousAlt {_semiSynchronousAlt / 1000.0} km");
                _target_alt_km = _semiSynchronousAlt / 1000.0;
                _targetAltitude = _target_alt_km.ToString("N3");
                // Logger.LogInfo($"HandleButtons: tgt_altitude_km set to {_targetAltitude} km");

            }
            else if (_setTgtMinLOS)
            {
                // Logger.LogInfo($"HandleButtons: Setting tgt_altitude_km to _minLOSAlt {_minLOSAlt / 1000.0} km");
                _target_alt_km = _minLOSAlt / 1000.0;
                _targetAltitude = _target_alt_km.ToString("N3");
                // Logger.LogInfo($"HandleButtons: tgt_altitude_km set to {_targetAltitude} km");

            }
        }
    }

}

