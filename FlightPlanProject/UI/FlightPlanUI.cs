using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using FPUtilities;
using HarmonyLib;
using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.UI.Binding;
using MuMech;
using NodeManager;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI.Appbar;
using System.Collections;
using System.Reflection;
using UnityEngine;

using FlightPlan.KTools.UI;
using FlightPlan.KTools;
using K2D2;
using static System.Net.Mime.MediaTypeNames;
using KSP.Messages.PropertyWatchers;

namespace FlightPlan;



public class FlightPlanUI
{
    private static FlightPlanUI _instance;
    public static FlightPlanUI Instance { get => _instance; }

    public FlightPlanUI(FlightPlanPlugin main_plugin)
    {
        _instance = this;
        this.plugin = main_plugin;
        body_selection = new BodySelection(main_plugin);
        burn_options = new BurnTimeOption();
    }

    public ManeuverType maneuver_type = ManeuverType.None;

    public static TimeRef time_ref = TimeRef.None;

    public void SetManeuverType(ManeuverType type)
    {
        maneuver_type = type;
        maneuver_type_desc = BurnTimeOption.Instance.setOptionsList(type);
    }

    public string maneuver_type_desc;


    FlightPlanPlugin plugin;

    public ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("FlightPlanUI");

    BodySelection body_selection;
    BurnTimeOption burn_options;

    int spacingAfterEntry = 5;

    private void DrawEntryButton(string entryName, ref bool button, string buttonStr, string value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        UI_Tools.Label(entryName);
        GUILayout.FlexibleSpace();
        button = UI_Tools.SmallButton(buttonStr);
        UI_Tools.Label(value);
        GUILayout.Space(5);
        UI_Tools.Label(unit);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntry2Button(string entryName, ref bool button1, string button1Str, ref bool button2, string button2Str, string value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        UI_Tools.Console(entryName);
        GUILayout.FlexibleSpace();
        button1 = UI_Tools.SmallButton(button1Str);
        button2 = UI_Tools.SmallButton(button2Str);
        UI_Tools.Console(value);
        GUILayout.Space(5);
        UI_Tools.Console(unit);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private double DrawLabelWithTextField(string entryName, double value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        UI_Tools.Label(entryName);
        GUILayout.Space(10);

        value = UI_Fields.DoubleField(entryName, value);

        GUILayout.Space(3);
        UI_Tools.Label(unit);
        GUILayout.EndHorizontal();

        GUILayout.Space(spacingAfterEntry);
        return value;
    }
    private double DrawButtonWithTextField(string entryName, ref bool button, double value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        button = UI_Tools.SmallButton(entryName);
        GUILayout.Space(10);

        value = UI_Fields.DoubleField(entryName, value);

        GUILayout.Space(3);
        UI_Tools.Label(unit);
        GUILayout.EndHorizontal();

        GUILayout.Space(spacingAfterEntry);
        return value;
    }

    private double DrawToggleButtonWithTextField(string runString, ManeuverType type, double value, string unit = "", string stopString = "")
    {
        GUILayout.BeginHorizontal();
        if (stopString.Length < 1)
            stopString = runString;


        DrawToggleButton(runString, type);
        GUILayout.Space(10);

        value = UI_Fields.DoubleField(runString, value);

        GUILayout.Space(3);
        UI_Tools.Label(unit);
        GUILayout.EndHorizontal();

        GUILayout.Space(spacingAfterEntry);
        return value;
    }

    void DrawToggleButton(string txt, ManeuverType maneuveur_type)
    {
        bool active = maneuver_type == maneuveur_type;

        bool result = UI_Tools.SmallToggleButton(active, txt, txt);
        if (result != active)
        {
            if (!active)
                SetManeuverType(maneuveur_type);
            else
                SetManeuverType(ManeuverType.None);
        }
    }

    public void OnGUI()
    {
        if (body_selection.listGui())
            return;

        if (burn_options.listGui())
            return;

        var orbit = plugin.activeVessel.Orbit;
        var referenceBody = orbit.referenceBody;

        // game = GameManager.Instance.Game;
        //activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        //currentNode = (activeNodes.Count() > 0) ? activeNodes[0] : null;
        FPUtility.RefreshActiveVesselAndCurrentManeuver();
        
        body_selection.BodySelectionGUI();
        burn_options.OptionSelectionGUI();


        FPStyles.DrawSectionHeader("Ownship Maneuvers");
        DrawToggleButton("Circularize", ManeuverType.circularize);
        // GUILayout.EndHorizontal();

        FPSettings.pe_altitude_km = DrawToggleButtonWithTextField("New Pe", ManeuverType.newPe, FPSettings.pe_altitude_km, "km");
        targetPeR = FPSettings.pe_altitude_km * 1000 + referenceBody.radius;

        if (orbit.eccentricity < 1)
        {
            FPSettings.ap_altitude_km = DrawToggleButtonWithTextField("New Ap", ManeuverType.newAp, FPSettings.ap_altitude_km, "km");
            targetApR = FPSettings.ap_altitude_km * 1000 + referenceBody.radius;

            DrawToggleButton("New Pe & Ap", ManeuverType.newPeAp);
        }

        FPSettings.target_inc_deg = DrawToggleButtonWithTextField("New Inclination", ManeuverType.newInc, FPSettings.target_inc_deg, "°");

        if (plugin.experimental.Value)
        {
            FPSettings.target_lan_deg = DrawToggleButtonWithTextField("New LAN", ManeuverType.newLAN, FPSettings.target_lan_deg, "°");

            // FPSettings.target_node_long_deg = DrawToggleButtonWithTextField("New Node Longitude", ref newNodeLon, FPSettings.target_node_long_deg, "°");
        }

        FPSettings.target_sma_km = DrawToggleButtonWithTextField("New SMA", ManeuverType.newSMA, FPSettings.target_sma_km, "km");
        targetSMA = FPSettings.target_sma_km * 1000 + referenceBody.radius;

        if (plugin.currentTarget != null)
        {
            // If the activeVessel and the currentTarget are both orbiting the same body
            if (plugin.currentTarget.Orbit != null) // No maneuvers relative to a star
            {
                if (plugin.currentTarget.Orbit.referenceBody.Name == referenceBody.Name)
                {
                    FPStyles.DrawSectionHeader("Maneuvers Relative to Target");
                    DrawToggleButton("Match Planes", ManeuverType.matchPlane);

                    DrawToggleButton("Hohmann Transfer", ManeuverType.hohmannXfer);

                    DrawToggleButton("Course Correction", ManeuverType.courseCorrection);

                    if (plugin.experimental.Value)
                    {
                        FPSettings.interceptT = DrawToggleButtonWithTextField("Intercept", ManeuverType.interceptTgt, FPSettings.interceptT, "s");

                        DrawToggleButton("Match Velocity", ManeuverType.matchVelocity);
                    }
                }
            }

            if (plugin.experimental.Value)
            {
                // If the activeVessel is not orbiting a star
                if (!referenceBody.IsStar && plugin.currentTarget.IsCelestialBody) // not orbiting a start and target is celestial
                {
                    // If the activeVessel is orbiting a planet and the current target is not the body the active vessel is orbiting
                    if (referenceBody.Orbit.referenceBody.IsStar && (plugin.currentTarget.Name != referenceBody.Name) && plugin.currentTarget.Orbit != null)
                    {
                        if (plugin.currentTarget.Orbit.referenceBody.IsStar) // exclude targets that are a moon
                        {
                            FPStyles.DrawSectionHeader("Interplanetary Maneuvers");
                            DrawToggleButton("Interplanetary Transfer", ManeuverType.planetaryXfer);
                        }
                    }
                }
            }
        }

        // If the activeVessle is at a moon (a celestial in orbit around another celestial that's not also a star)
        if (!referenceBody.IsStar) // not orbiting a star
        {
            if (!referenceBody.Orbit.referenceBody.IsStar && orbit.eccentricity < 1) // not orbiting a planet, and e < 1
            {
                FPStyles.DrawSectionHeader("Moon Specific Maneuvers");

                var parentPlanet = referenceBody.Orbit.referenceBody;
                FPSettings.mr_altitude_km = DrawToggleButtonWithTextField("Moon Return", ManeuverType.moonReturn, FPSettings.mr_altitude_km, "km");
                targetMRPeR = FPSettings.mr_altitude_km * 1000 + parentPlanet.radius;
            }
        }

        // If the selected option is to do an activity "at an altitude", then present an input field for the altitude to use
        if (time_ref == TimeRef.ALTITUDE)
        {
            FPSettings.altitude_km = DrawLabelWithTextField("Maneuver Altitude", FPSettings.altitude_km, "km");
        }
        if (time_ref == TimeRef.X_FROM_NOW)
        {
            FPSettings.timeOffset = DrawLabelWithTextField("Time From Now", FPSettings.timeOffset, "s");
        }

        var UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        // if (statusText == "Virgin") statusTime = UT;
        if (plugin.currentNode == null && FPStatus.status != FPStatus.Status.VIRGIN)
        {
            FPStatus.Ok("");
        }
        DrawGUIStatus(UT);

        // If the selected option is to do an activity "at an altitude", then make sure the altitude is possible for the orbit
        if (time_ref == TimeRef.ALTITUDE)
        {
            if (FPSettings.altitude_km * 1000 < orbit.Periapsis)
            {
                FPSettings.altitude_km = Math.Ceiling(orbit.Periapsis) / 1000;
                if (GUI.GetNameOfFocusedControl() != "Maneuver Altitude")
                    UI_Fields.temp_dict["Maneuver Altitude"] = FPSettings.altitude_km.ToString();
            }
            if (orbit.eccentricity < 1 && FPSettings.altitude_km * 1000 > orbit.Apoapsis)
            {
                FPSettings.altitude_km = Math.Floor(orbit.Apoapsis) / 1000;
                if (GUI.GetNameOfFocusedControl() != "Maneuver Altitude")
                    UI_Fields.temp_dict["Maneuver Altitude"] = FPSettings.altitude_km.ToString();
            }
        }

        maneuver_description = $"{maneuver_type_desc} {BurnTimeOption.TimeRefDesc}";

        BurnTimeOption.Instance.setBurnTime();
    }

    public string maneuver_description;


    public FPOtherModsInterface other_mods = null;

    private void DrawGUIStatus(double UT)
    {
        FPStatus.DrawUI(UT);

        // Indication to User that its safe to type, or why vessel controls aren't working

        if (other_mods == null)
        {
            // init mode detection only when first needed
            other_mods = new FPOtherModsInterface();
            other_mods.CheckModsVersions();
        }

        other_mods.OnGUI( plugin.currentNode);
        GUILayout.Space(spacingAfterEntry);
    }


// Radius Computed from Inputs
    public double targetPeR;
    public double targetApR;
    public double targetSMA;
    public double targetMRPeR;

    public void MakeNode()
    {
        if (maneuver_type == ManeuverType.None)
            return;

        var requestedBurnTime = BurnTimeOption.requestedBurnTime;

        bool pass = false;
        switch (maneuver_type)
        {
        case ManeuverType.circularize: // Working
            pass = plugin.Circularize(requestedBurnTime, -0.5);
            break;
        case ManeuverType.newPe: // Working
            pass = plugin.SetNewPe(requestedBurnTime, targetPeR, -0.5);
            break;
        case ManeuverType.newAp:// Working
            pass = plugin.SetNewAp(requestedBurnTime, targetApR, -0.5);
            break;
        case ManeuverType.newPeAp:// Working: Not perfect, but pretty good results nevertheless
            pass = plugin.Ellipticize(requestedBurnTime, targetApR, targetPeR, -0.5);
            break;
        case ManeuverType.newInc:// Working
            pass = plugin.SetInclination(requestedBurnTime, FPSettings.target_inc_deg, -0.5);
            break;
        case ManeuverType.newLAN: // Untested
            pass = plugin.SetNewLAN(requestedBurnTime, FPSettings.target_lan_deg, -0.5);
            break;
        case ManeuverType.newNodeLon: // Untested
            pass = plugin.SetNodeLongitude(requestedBurnTime, FPSettings.target_node_long_deg, -0.5);
            break;
        case ManeuverType.newSMA: // Untested
            pass = plugin.SetNewSMA(requestedBurnTime, targetSMA, -0.5);
            break;
        case ManeuverType.matchPlane: // Working
            pass = plugin.MatchPlanes(time_ref, -0.5);
            break;
        case ManeuverType.hohmannXfer: // Works if we start in a good enough orbit (reasonably circular, close to target's orbital plane)
            pass = plugin.HohmannTransfer(requestedBurnTime, -0.5);
            break;
        case ManeuverType.interceptTgt: // Experimental
            pass = plugin.InterceptTgt(requestedBurnTime, FPSettings.interceptT, -0.5);
            break;
        case ManeuverType.courseCorrection: // Experimental Works at least some times...
            pass = plugin.CourseCorrection(requestedBurnTime, -0.5);
            break;
        case ManeuverType.moonReturn: // Works - but may give poor Pe, including potentially lithobreaking
            pass = plugin.MoonReturn(requestedBurnTime, targetMRPeR, -0.5);
            break;
        case ManeuverType.matchVelocity: // Experimental
            pass = plugin.MatchVelocity(requestedBurnTime, -0.5);
            break;
        case ManeuverType.planetaryXfer: // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
            pass = plugin.PlanetaryXfer(requestedBurnTime, -0.5);
            break;
        }

        if (pass && plugin.autoLaunchMNC.Value)
            FPOtherModsInterface.instance.callMNC();
    }
    



}