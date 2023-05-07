using BepInEx.Logging;
using FPUtilities;
using KSP.Game;
using UnityEngine;
using FlightPlan.KTools.UI;
using KSP.Sim.impl;

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

    TabsUI tabs = new TabsUI();

    public PatchedConicsOrbit orbit;
    public CelestialBodyComponent referenceBody;

    public void Update()
    {
        if (init_done)
        {
            referenceBody = null;
            orbit = null;
            var vessel = FlightPlanPlugin.Instance.activeVessel;
            if (vessel == null)
                return;

            orbit = vessel.Orbit;
            if (orbit != null)
                referenceBody = orbit.referenceBody;
           

            tabs.Update();
        }
   
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

    public void DrawSoloToggle(string toggleStr, ref bool toggle)
    {
        GUILayout.Space(FPStyles.spacingAfterSection);
        GUILayout.BeginHorizontal();
        toggle = GUILayout.Toggle(toggle, toggleStr, KBaseStyle.toggle); // was section_toggle
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(-FPStyles.spacingAfterSection);
    }

    public bool DrawSoloToggle(string toggleStr, bool toggle, bool error=true)
    {
        GUILayout.Space(FPStyles.spacingAfterSection);
        GUILayout.BeginHorizontal();
        if (error)
            toggle = GUILayout.Toggle(toggle, toggleStr, KBaseStyle.toggle_error);
        else
            toggle = GUILayout.Toggle(toggle, toggleStr, KBaseStyle.toggle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(-FPStyles.spacingAfterSection);
        return toggle;
    }

    //public void DrawToggleButton(string txt, ManeuverType maneuveur_type)
    //{
    //    bool active = maneuver_type == maneuveur_type;

    //    bool result = UI_Tools.SmallToggleButton(active, txt, txt);
    //    if (result != active)
    //    {
    //        if (!active)
    //            SetManeuverType(maneuveur_type);
    //        else
    //            SetManeuverType(ManeuverType.None);
    //    }
    //}

    public void DrawEntry(string entryName, string value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        UI_Tools.Label(entryName);
        GUILayout.FlexibleSpace();
        UI_Tools.Label(value);
        if (unit.Length > 0)
        {
            GUILayout.Space(5);
            UI_Tools.Label(unit);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(FPStyles.spacingAfterEntry);
    }

    public void DrawEntryButton(string entryName, ref bool button, string buttonStr, string value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        UI_Tools.Label(entryName);
        GUILayout.FlexibleSpace();
        button = UI_Tools.CtrlButton(buttonStr);
        UI_Tools.Label(value);
        GUILayout.Space(5);
        UI_Tools.Label(unit);
        GUILayout.EndHorizontal();
        GUILayout.Space(FPStyles.spacingAfterEntry);
    }

    public void DrawEntry2Button(string entryName, ref bool button1, string button1Str, ref bool button2, string button2Str, string value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        UI_Tools.Console(entryName);
        GUILayout.FlexibleSpace();
        button1 = UI_Tools.CtrlButton(button1Str);
        button2 = UI_Tools.CtrlButton(button2Str);
        UI_Tools.Console(value);
        GUILayout.Space(5);
        UI_Tools.Console(unit);
        GUILayout.EndHorizontal();
        GUILayout.Space(FPStyles.spacingAfterEntry);
    }

    public void DrawEntryTextField(string entryName, ref string textEntry, string unit = "")
    {
        double num;
        Color normal;

        GUILayout.BeginHorizontal();
        GUILayout.Label(entryName, KBaseStyle.nameLabelStyle);
        GUILayout.FlexibleSpace();
        normal = GUI.color;
        bool parsed = double.TryParse(textEntry, out num);
        if (!parsed) GUI.color = Color.red;
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, KBaseStyle.textInputStyle);
        GUI.color = normal;
        GUILayout.Space(5);
        GUILayout.Label(unit, KBaseStyle.unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(FPStyles.spacingAfterEntry);
    }

    public double DrawEntryTextField(string entryName, double value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(entryName, KBaseStyle.nameLabelStyle);
        GUILayout.FlexibleSpace();
        GUI.SetNextControlName(entryName);
        value = UI_Fields.DoubleField(entryName, value, KBaseStyle.textInputStyle);
        GUILayout.Space(5);
        GUILayout.Label(unit, KBaseStyle.unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(FPStyles.spacingAfterEntry);
        return value;
    }

    public double DrawLabelWithTextField(string entryName, double value, string unit = "")
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

    public double DrawToggleButtonWithTextField(string runString, ManeuverType type, double value, string unit = "", string stopString = "")
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

    public void DrawToggleButton(string txt, ManeuverType maneuveur_type)
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

    private string SituationToString(VesselSituations situation)
    {
        return situation switch
        {
            VesselSituations.PreLaunch => "Pre-Launch",
            VesselSituations.Landed => "Landed",
            VesselSituations.Splashed => "Splashed down",
            VesselSituations.Flying => "Flying",
            VesselSituations.SubOrbital => "Suborbital",
            VesselSituations.Orbiting => "Orbiting",
            VesselSituations.Escaping => "Escaping",
            _ => "UNKNOWN",
        };
    }

    bool init_done = false;

    void createTabs()
    {
        if (!init_done)
        {
            tabs.pages.Add(new OwnshipManeuversPage());
            tabs.pages.Add(new TargetPage());
            tabs.pages.Add(new InterplanetaryPage());
            tabs.pages.Add(new MoonPage());
            tabs.pages.Add(new ResonantOrbitPage());

            tabs.Init();

            init_done = true;
        }
    }

    public void OnGUI()
    {
        createTabs();

        // All tabs get the current situation
        DrawEntry("Situation", String.Format("{0} {1}", SituationToString(FlightPlanPlugin.Instance.activeVessel.Situation), FlightPlanPlugin.Instance.activeVessel.mainBody.bodyName));

        if (body_selection.listGui())
            return;

        if (burn_options.listGui())
            return;

        // game = GameManager.Instance.Game;
        //activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        //currentNode = (activeNodes.Count() > 0) ? activeNodes[0] : null;
        FPUtility.RefreshActiveVesselAndCurrentManeuver();
        
        body_selection.BodySelectionGUI();
        tabs.onGUI();

        // If the selected option is to do an activity "at an altitude", then present an input field for the altitude to use
        if (time_ref == TimeRef.ALTITUDE)
        {
            FPSettings.altitude_km = DrawLabelWithTextField("Maneuver Altitude", FPSettings.altitude_km, "km");
        }
        if (time_ref == TimeRef.X_FROM_NOW)
        {
            FPSettings.timeOffset = DrawLabelWithTextField("Time From Now", FPSettings.timeOffset, "s");
        }

        // Draw the GUI status at the end of this tab
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
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

    /// <summary>
    /// final creation of the Node by calling the main plugin
    /// </summary>
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
        case ManeuverType.fixAp: // Working
            pass = plugin.SetNewAp(requestedBurnTime, ResonantOrbitPage.Ap2, - 0.5);
            break;
        case ManeuverType.fixPe: // Working
            pass = plugin.SetNewPe(requestedBurnTime, ResonantOrbitPage.Pe2, - 0.5);
            break;
        }

        if (pass && plugin.autoLaunchMNC.Value)
            FPOtherModsInterface.instance.callMNC();
    }
}