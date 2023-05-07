using FlightPlan.KTools.UI;
using FPUtilities;
using KSP.Sim.impl;
using SpaceWarp.API.Assets;
using UnityEngine;

namespace FlightPlan;

public class BasePageContent : PageContent
{
    public BasePageContent()
    {
        this.main_ui = FlightPlanUI.Instance;
        this.plugin = FlightPlanPlugin.Instance;
    }
    protected FlightPlanUI main_ui;
    protected FlightPlanPlugin plugin;


    protected PatchedConicsOrbit orbit => main_ui.orbit;
    protected CelestialBodyComponent referenceBody => main_ui.referenceBody;

    public virtual string Name => throw new NotImplementedException();

    public virtual GUIContent Icon => throw new NotImplementedException();

    public bool isRunning => false;


    bool ui_visible;
    public bool UIVisible { get => ui_visible; set => ui_visible = value; }

    public virtual bool isActive => throw new NotImplementedException();

    public virtual void onGUI()
    {
        throw new NotImplementedException();
    }
}

public class OwnshipManeuversPage : BasePageContent
{
    public override string Name => "Own Orbit";

    // readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/OwnshipManeuver_50v2.png");
    readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/Capsule_v3_50.png");

    public override GUIContent Icon => new(tabIcon, "Ownship Maneuvers");

    public override bool isActive => true;

    public override void onGUI()
    {
        FPStyles.DrawSectionHeader("Ownship Maneuvers");
        BurnTimeOption.Instance.OptionSelectionGUI();
        main_ui.DrawToggleButton("Circularize", ManeuverType.circularize);
        // GUILayout.EndHorizontal();

        FPSettings.pe_altitude_km = main_ui.DrawToggleButtonWithTextField("New Pe", ManeuverType.newPe, FPSettings.pe_altitude_km, "km");
        main_ui.targetPeR = FPSettings.pe_altitude_km * 1000 + referenceBody.radius;

        if (main_ui.orbit.eccentricity < 1)
        {
            FPSettings.ap_altitude_km = main_ui.DrawToggleButtonWithTextField("New Ap", ManeuverType.newAp, FPSettings.ap_altitude_km, "km");
            main_ui.targetApR = FPSettings.ap_altitude_km * 1000 + referenceBody.radius;
            main_ui.DrawToggleButton("New Pe & Ap", ManeuverType.newPeAp);
        }

        FPSettings.target_inc_deg = main_ui.DrawToggleButtonWithTextField("New Inclination", ManeuverType.newInc, FPSettings.target_inc_deg, "°");

        if (plugin.experimental.Value)
        {
            FPSettings.target_lan_deg = main_ui.DrawToggleButtonWithTextField("New LAN", ManeuverType.newLAN, FPSettings.target_lan_deg, "°");

            // FPSettings.target_node_long_deg = DrawToggleButtonWithTextField("New Node Longitude", ref newNodeLon, FPSettings.target_node_long_deg, "°");
        }

        FPSettings.target_sma_km = main_ui.DrawToggleButtonWithTextField("New SMA", ManeuverType.newSMA, FPSettings.target_sma_km, "km");
        main_ui.targetSMA = FPSettings.target_sma_km * 1000 + referenceBody.radius;
    }
}

public class TargetPage : BasePageContent
{
    public override string Name => "Target";

    // readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/TargetRelManeuver_50v2.png");
    readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/Capsule_x2v3_50.png");

    public override GUIContent Icon => new(tabIcon, "Target Relative Maneuvers");

    public override bool isActive
    {
        get => plugin.currentTarget != null  // If there is a target
            && plugin.currentTarget.Orbit != null // And the target is not a star
            && plugin.currentTarget.Orbit.referenceBody.Name == referenceBody.Name; // If the activeVessel and the currentTarget are both orbiting the same body
    }

    public override void onGUI()
    {
        FPStyles.DrawSectionHeader("Target Relative Maneuvers");

        BurnTimeOption.Instance.OptionSelectionGUI();

        main_ui.DrawToggleButton("Match Planes", ManeuverType.matchPlane);
        main_ui.DrawToggleButton("Hohmann Transfer", ManeuverType.hohmannXfer);
        main_ui.DrawToggleButton("Course Correction", ManeuverType.courseCorrection);

        if (plugin.experimental.Value)
        {
            FPSettings.interceptT = main_ui.DrawToggleButtonWithTextField("Intercept", ManeuverType.interceptTgt, FPSettings.interceptT, "s");
            main_ui.DrawToggleButton("Match Velocity", ManeuverType.matchVelocity);
        }
    }
}

public class InterplanetaryPage : BasePageContent
{
    public override string Name => "Target";

    // readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/TargetRelManeuver_50v2.png");
    readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/Planet2_blue_50.png");

    public override GUIContent Icon => new(tabIcon, "Orbital Transfer Maneuvers");

    public override bool isActive
    {
        get => plugin.currentTarget != null // If the activeVessel is orbiting a planet and the current target is not the body the active vessel is orbiting
            && plugin.experimental.Value // No maneuvers relative to a star
            && !referenceBody.IsStar && plugin.currentTarget.IsCelestialBody
            && referenceBody.Orbit.referenceBody.IsStar && (plugin.currentTarget.Name != referenceBody.Name)
            && plugin.currentTarget.Orbit != null
            && plugin.currentTarget.Orbit.referenceBody.IsStar; // exclude targets that are a moon
    }
    public override void onGUI()
    {
        FPStyles.DrawSectionHeader("Orbital Transfer Maneuvers");
        BurnTimeOption.Instance.OptionSelectionGUI();
        main_ui.DrawToggleButton("Interplanetary Transfer", ManeuverType.planetaryXfer);
    }
}


public class MoonPage : BasePageContent
{
    public override string Name => "Moon";

    // readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/TargetRelManeuver_50v2.png");
    readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/Moon_white_50.png");

    public override GUIContent Icon => new(tabIcon, "Orbital Transfer Maneuvers");

    public override bool isActive
    {
        get => !referenceBody.IsStar // not orbiting a star
                && !referenceBody.Orbit.referenceBody.IsStar && orbit.eccentricity < 1;
        // If the activeVessle is at a moon (a celestial in orbit around another celestial that's not also a star)
    }
    public override void onGUI()
    {
        FPStyles.DrawSectionHeader("Orbital Transfer Maneuvers");

        var parentPlanet = referenceBody.Orbit.referenceBody;
        FPSettings.mr_altitude_km = main_ui.DrawToggleButtonWithTextField("Moon Return", ManeuverType.moonReturn, FPSettings.mr_altitude_km, "km");
        main_ui.targetMRPeR = FPSettings.mr_altitude_km * 1000 + parentPlanet.radius;
    }
}

public class ResonantOrbitPage : BasePageContent
{
    // Specialized buttons just for this tab
    private bool nSatUp, nSatDown, nOrbUp, nOrbDown, setTgtPe, setTgtAp, setTgtSync, setTgtSemiSync, setTgtMinLOS;

    private double synchronousAlt;
    private double semiSynchronousAlt;
    private double minLOSAlt;
    private string targetAltitude = "600";      // String planned altitide for deployed satellites (destiantion orbit)
    private double target_alt_km = 600;         // Double planned altitide for deployed satellites (destiantion orbit)
    private double satPeriod;                   // The period of the destination orbit
    private double xferPeriod;                  // The period of the resonant deploy orbit (xferPeriod = resonance*satPeriod)
    private bool dive_error = false;
    public static double Ap2 { get; set; } // The resonant deploy orbit apoapsis
    public static double Pe2 { get; set;  } // The resonant deploy orbit periapsis

    public override string Name => "Resonant Orbit";

    // readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/ResonantOrbit_50v2.png");
    readonly Texture2D tabIcon = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/Satellite2_50.png");

    public override GUIContent Icon => new(tabIcon, "Resonant Orbit Maneuvers");

    public override bool isActive => true;

    public override void onGUI()
    {
        FPStyles.DrawSectionHeader("Resonant Orbit Maneuvers");
        BurnTimeOption.Instance.OptionSelectionGUI();
        double synchronousPeriod = plugin.activeVessel.mainBody.rotationPeriod;
        double semiSynchronousPeriod = plugin.activeVessel.mainBody.rotationPeriod / 2;
        synchronousAlt = plugin.SMACalc(synchronousPeriod);
        semiSynchronousAlt = plugin.SMACalc(semiSynchronousPeriod);
        int n, m;

        //string targetAltitude = "600";      // String planned altitide for deployed satellites (destiantion orbit)
        //double target_alt_km = 600;         // Double planned altitide for deployed satellites (destiantion orbit)
        //double satPeriod;                   // The period of the destination orbit
        //double xferPeriod;                  // The period of the resonant deploy orbit (xferPeriod = resonance*satPeriod)
        //double Ap2;                         // The resonant deploy orbit apoapsis
        //double Pe2;                         // The resonant deploy orbit periapsis

        if (synchronousAlt > plugin.activeVessel.mainBody.sphereOfInfluence)
        {
            synchronousAlt = -1;
        }
        if (semiSynchronousAlt > plugin.activeVessel.mainBody.sphereOfInfluence)
        {
            semiSynchronousAlt = -1;
        }

        if (FPSettings.dive_orbit) // If we're going to dive under the target orbit for the deployment orbit
        {
            m = FPSettings.num_sats * FPSettings.num_orbits;
            n = m - 1;
        }
        else // If not
        {
            m = FPSettings.num_sats * FPSettings.num_orbits;
            n = m + 1;
        }
        double resonance = (double)n / m;
        string resonanceStr = String.Format("{0}/{1}", n, m);

        // Compute the minimum LOS altitude
        minLOSAlt = plugin.minLOSCalc(FPSettings.num_sats, plugin.activeVessel.mainBody.radius, plugin.activeVessel.mainBody.hasAtmosphere);

        main_ui.DrawEntry2Button("Payloads:", ref nSatDown, "-", ref nSatUp, "+", FPSettings.num_sats.ToString()); // was numSatellites
        main_ui.DrawEntry2Button("Deploy Orbits:", ref nOrbDown, "-", ref nOrbUp, "+", FPSettings.num_orbits.ToString()); // was numOrbits
        main_ui.DrawEntry("Orbital Resonance", resonanceStr, " ");

        main_ui.DrawEntryTextField("Target Altitude", ref targetAltitude, "km"); // Tried" FPSettings.tgt_altitude_km 

        main_ui.DrawEntryButton("Apoapsis", ref setTgtAp, "⦾", $"{FPUtility.MetersToDistanceString(plugin.activeVessel.Orbit.ApoapsisArl / 1000)}", "km");
        main_ui.DrawEntryButton("Periapsis", ref setTgtPe, "⦾", $"{FPUtility.MetersToDistanceString(plugin.activeVessel.Orbit.PeriapsisArl / 1000)}", "km");

        satPeriod = plugin.periodCalc(target_alt_km * 1000 + plugin.activeVessel.mainBody.radius);

        if (synchronousAlt > 0)
        {
            main_ui.DrawEntryButton("Synchronous Alt", ref setTgtSync, "⦾", $"{FPUtility.MetersToDistanceString(synchronousAlt / 1000)}", "km");
            main_ui.DrawEntryButton("Semi Synchronous Alt", ref setTgtSemiSync, "⦾", $"{FPUtility.MetersToDistanceString(semiSynchronousAlt / 1000)}", "km");
        }
        else if (semiSynchronousAlt > 0)
        {
            main_ui.DrawEntry("Synchronous Alt", "Outside SOI", " ");
            main_ui.DrawEntryButton("Semi Synchronous Alt", ref setTgtSemiSync, "⦾", $"{FPUtility.MetersToDistanceString(semiSynchronousAlt / 1000)}", "km");
        }
        else
        {
            main_ui.DrawEntry("Synchronous Alt", "Outside SOI", " ");
            main_ui.DrawEntry("Semi Synchronous Alt", "Outside SOI", " ");
        }
        main_ui.DrawEntry("SOI Alt", $"{FPUtility.MetersToDistanceString(plugin.activeVessel.mainBody.sphereOfInfluence / 1000)}", "km");
        if (minLOSAlt > 0)
        {
            main_ui.DrawEntryButton("Min LOS Orbit Alt", ref setTgtMinLOS, "⦾", $"{FPUtility.MetersToDistanceString(minLOSAlt / 1000)}", "km");
        }
        else
        {
            main_ui.DrawEntry("Min LOS Orbit Alt", "Undefined", "km");
        }
        FPSettings.occlusion = main_ui.DrawSoloToggle("<b>Occlusion</b>", FPSettings.occlusion);
        if (FPSettings.occlusion)
        {
            FPSettings.occ_mod_atm = main_ui.DrawEntryTextField("Atm", FPSettings.occ_mod_atm);
            //try { occModAtm = double.Parse(occModAtmStr); }
            //catch { occModAtm = 1.0; }
            FPSettings.occ_mod_vac = main_ui.DrawEntryTextField("Vac", FPSettings.occ_mod_vac);
            //try { occModVac = double.Parse(occModVacStr); }
            //catch { occModVac = 1.0; }
        }

        FPSettings.dive_orbit = main_ui.DrawSoloToggle("<b>Dive</b>", FPSettings.dive_orbit, dive_error);

        // period1 = periodCalc(target_alt_km*1000 + activeVessel.mainBody.radius);
        xferPeriod = resonance * satPeriod;
        double SMA2 = plugin.SMACalc(xferPeriod);
        double sSMA = target_alt_km * 1000 + plugin.activeVessel.mainBody.radius;
        if (FPSettings.dive_orbit)
        {
            Ap2 = sSMA; // Diveing transfer orbits release at Apoapsis
            Pe2 = 2.0 * SMA2 - (Ap2);
            if (Pe2 < plugin.activeVessel.mainBody.radius)
                dive_error = true;
            else
                dive_error = false;
        }
        else
        {
            Pe2 = sSMA; // Non-diving transfer orbits release at Periapsis
            Ap2 = 2.0 * SMA2 - (Pe2);
        }
        double ce = (Ap2 - Pe2) / (Ap2 + Pe2);
        //main_ui.DrawEntry("Period", $"{FPUtility.SecondsToTimeString(xferPeriod)}", "s");
        //main_ui.DrawEntry("Apoapsis", $"{FPUtility.MetersToDistanceString((Ap2 - plugin.activeVessel.mainBody.radius) / 1000)}", "km");
        //main_ui.DrawEntry("Periapsis", $"{FPUtility.MetersToDistanceString((Pe2 - plugin.activeVessel.mainBody.radius) / 1000)}", "km");
        //main_ui.DrawEntry("Eccentricity", ce.ToString("N3"), " ");
        double dV = plugin.burnCalc(sSMA, sSMA, 0, Ap2, SMA2, ce, plugin.activeVessel.mainBody.gravParameter);
        main_ui.DrawEntry("Injection Δv", dV.ToString("N3"), "m/s");

        double errorPe = (Pe2 - plugin.activeVessel.Orbit.Periapsis) / 1000;
        double errorAp = (Ap2 - plugin.activeVessel.Orbit.Apoapsis) / 1000;
        string fixPeStr, fixApStr;

        GUILayout.Space(-FPStyles.spacingAfterSection);

        UI_Tools.Separator();

        if (errorPe > 0)
            fixPeStr = $"Raise Pe by {errorPe:N2} km to {((Pe2 - plugin.activeVessel.mainBody.radius) / 1000):N2} km";
        else
            fixPeStr = $"Lower Pe by {(-errorPe):N2} km to {((Pe2 - plugin.activeVessel.mainBody.radius) / 1000):N2} km";
        if (errorAp > 0)
            fixApStr = $"Raise Ap by {errorAp:N2} km to {((Ap2 - plugin.activeVessel.mainBody.radius) / 1000):N2} km";
        else
            fixApStr = $"Lower Ap by {(-errorAp):N2} km to {((Ap2 - plugin.activeVessel.mainBody.radius) / 1000):N2} km";
        if (plugin.activeVessel.Orbit.Apoapsis < Pe2)
        {
            main_ui.DrawToggleButton(fixApStr, ManeuverType.fixAp);
            // main_ui.DrawSoloToggle(fixApStr, ref fixAp);
            // fixPe = false;
            // _toggles["fixPe"] = false;
        }
        else if (plugin.activeVessel.Orbit.Periapsis > Ap2)
        {
            main_ui.DrawToggleButton(fixPeStr, ManeuverType.fixPe);
            // main_ui.DrawSoloToggle(fixPeStr, ref fixPe);
            // fixAp = false;
            // _toggles["fixAp"] = false;
        }
        else
        {
            // main_ui.DrawSoloToggle(fixPeStr, ref fixPe);
            // main_ui.DrawSoloToggle(fixApStr, ref fixAp);
            if (Pe2 > plugin.activeVessel.mainBody.radius)
                main_ui.DrawToggleButton(fixPeStr, ManeuverType.fixPe);
            main_ui.DrawToggleButton(fixApStr, ManeuverType.fixAp);
        }

        handleButtons();
    }
    private void handleButtons()
    {
        if (nSatDown || nSatUp || nOrbDown || nOrbUp || setTgtPe || setTgtAp || setTgtSync || setTgtSemiSync || setTgtMinLOS)
        {
            // burnParams = Vector3d.zero;
            if (nSatDown && FPSettings.num_sats > 2)
            {
                FPSettings.num_sats--;
                // numSatellites = FPSettings.num_sats.ToString();
            }
            else if (nSatUp)
            {
                FPSettings.num_sats++;
                // numSatellites = FPSettings.num_sats.ToString();
            }
            else if (nOrbDown && FPSettings.num_orbits > 1)
            {
                FPSettings.num_orbits--;
                // numOrbits = FPSettings.num_orb.ToString();
            }
            else if (nOrbUp)
            {
                FPSettings.num_orbits++;
                // numOrbits = FPSettings.num_orb.ToString();
            }
            else if (setTgtPe)
            {
                // Logger.LogInfo($"handleButtons: Setting tgt_altitude_km to Periapsis {activeVessel.Orbit.PeriapsisArl / 1000.0} km");
                target_alt_km = plugin.activeVessel.Orbit.PeriapsisArl / 1000.0;
                targetAltitude = target_alt_km.ToString("N3");
                // Logger.LogInfo($"handleButtons: tgt_altitude_km set to {targetAltitude} km");
            }
            else if (setTgtAp)
            {
                // Logger.LogInfo($"handleButtons: Setting tgt_altitude_km to Apoapsis {activeVessel.Orbit.ApoapsisArl / 1000.0} km");
                target_alt_km = plugin.activeVessel.Orbit.ApoapsisArl / 1000.0;
                targetAltitude = target_alt_km.ToString("N3");
                // Logger.LogInfo($"handleButtons: tgt_altitude_km set to {targetAltitude} km");

            }
            else if (setTgtSync)
            {
                // Logger.LogInfo($"handleButtons: Setting tgt_altitude_km to synchronousAlt {synchronousAlt / 1000.0} km");
                target_alt_km = synchronousAlt / 1000.0;
                targetAltitude = target_alt_km.ToString("N3");
                // Logger.LogInfo($"handleButtons: tgt_altitude_km set to {targetAltitude} km");

            }
            else if (setTgtSemiSync)
            {
                // Logger.LogInfo($"handleButtons: Setting tgt_altitude_km to semiSynchronousAlt {semiSynchronousAlt / 1000.0} km");
                target_alt_km = semiSynchronousAlt / 1000.0;
                targetAltitude = target_alt_km.ToString("N3");
                // Logger.LogInfo($"handleButtons: tgt_altitude_km set to {targetAltitude} km");

            }
            else if (setTgtMinLOS)
            {
                // Logger.LogInfo($"handleButtons: Setting tgt_altitude_km to minLOSAlt {minLOSAlt / 1000.0} km");
                target_alt_km = minLOSAlt / 1000.0;
                targetAltitude = target_alt_km.ToString("N3");
                // Logger.LogInfo($"handleButtons: tgt_altitude_km set to {targetAltitude} km");

            }
        }
    }

}

