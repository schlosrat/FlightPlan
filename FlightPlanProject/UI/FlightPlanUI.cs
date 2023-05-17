using BepInEx.Logging;
using FPUtilities;
using KSP.Game;
using UnityEngine;
using FlightPlan.KTools.UI;
using KSP.Sim.impl;
using MuMech;

namespace FlightPlan;

public class FlightPlanUI
{
    private static FlightPlanUI _instance;
    public static FlightPlanUI Instance { get => _instance; }

    public FlightPlanUI(FlightPlanPlugin main_plugin)
    {
        _instance = this;
        this.Plugin = main_plugin;
        BodySelection = new TargetSelection(main_plugin);
        BurnOptions = new BurnTimeOption();
    }

    TabsUI Tabs = new TabsUI();

    public PatchedConicsOrbit Orbit;
    public CelestialBodyComponent ReferenceBody;

    public void Update()
    {
        if (InitDone)
        {
            ReferenceBody = null;
            Orbit = null;
            var vessel = FlightPlanPlugin.Instance._activeVessel;
            if (vessel == null)
                return;

            Orbit = vessel.Orbit;
            if (Orbit != null)
                ReferenceBody = Orbit.referenceBody;
           

            Tabs.Update();
        }
   
    }

    public ManeuverType ManeuverType = ManeuverType.None;

    public static TimeRef TimeRef = TimeRef.None;

    public void SetManeuverType(ManeuverType type)
    {
        ManeuverType = type;
        ManeuverTypeDesc = BurnTimeOption.Instance.SetOptionsList(type);
    }

    public string ManeuverTypeDesc;


    FlightPlanPlugin Plugin;

    public ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("FlightPlanUI");

    TargetSelection BodySelection; // Name this something clearer to distinguish it from the type/class?
    BurnTimeOption BurnOptions;

    // int spacingAfterEntry = 5;

    public void DrawSoloToggle(string toggleStr, ref bool toggle)
    {
        GUILayout.Space(FPStyles.SpacingAfterSection);
        GUILayout.BeginHorizontal();
        toggle = GUILayout.Toggle(toggle, toggleStr, KBaseStyle.Toggle); // was section_toggle
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(-FPStyles.SpacingAfterSection);
    }

    public bool DrawSoloToggle(string toggleStr, bool toggle, bool error=false)
    {
        GUILayout.Space(FPStyles.SpacingAfterSection);
        GUILayout.BeginHorizontal();
        if (error)
        {
            GUILayout.Toggle(toggle, toggleStr, KBaseStyle.ToggleError);
            toggle = false;
        }
        else
            toggle = GUILayout.Toggle(toggle, toggleStr, KBaseStyle.Toggle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(-FPStyles.SpacingAfterSection);
        return toggle;
    }

    public void DrawEntry(string entryName, string value = "", string unit = "")
    {
        GUILayout.BeginHorizontal();
        UI_Tools.Label(entryName);
        if (value.Length > 0)
        {
            GUILayout.FlexibleSpace();
            UI_Tools.Label(value);
            if (unit.Length > 0)
            {
                GUILayout.Space(5);
                UI_Tools.Label(unit);
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(FPStyles.SpacingAfterEntry);
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
        GUILayout.Space(FPStyles.SpacingAfterEntry);
    }

    public void DrawEntry2Button(string entryName, ref bool button1, string button1Str, ref bool button2, string button2Str, string value, string unit = "", string divider = "")
    {
        GUILayout.BeginHorizontal();
        UI_Tools.Console(entryName);
        GUILayout.FlexibleSpace();
        button1 = UI_Tools.CtrlButton(button1Str);
        if (divider.Length > 0)
            UI_Tools.Console(divider);
        button2 = UI_Tools.CtrlButton(button2Str);
        UI_Tools.Console(value);
        GUILayout.Space(5);
        UI_Tools.Console(unit);
        GUILayout.EndHorizontal();
        GUILayout.Space(FPStyles.SpacingAfterEntry);
    }

    public void DrawEntryTextField(string entryName, ref string textEntry, string unit = "")
    {
        Color normal;

        if (!UI_Fields.InputFields.Contains(entryName))
            UI_Fields.InputFields.Add(entryName);

        GUILayout.BeginHorizontal();
        UI_Tools.Label(entryName, KBaseStyle.Label); // was: NameLabelStyle
        GUILayout.FlexibleSpace();
        normal = GUI.color;
        bool parsed = double.TryParse(textEntry, out _);
        if (!parsed) GUI.color = Color.red;
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, KBaseStyle.TextInputStyle);
        GUI.color = normal;
        GUILayout.Space(5);
        UI_Tools.Label(unit); // was: , KBaseStyle.UnitLabelStyle
        GUILayout.EndHorizontal();
        GUILayout.Space(FPStyles.SpacingAfterEntry);
    }

    public double DrawEntryTextField(string entryName, double value, string unit = "", GUIStyle thisStyle = null)
    {
        if (!UI_Fields.InputFields.Contains(entryName))
            UI_Fields.InputFields.Add(entryName);

        GUILayout.BeginHorizontal();
        if (thisStyle != null)
            UI_Tools.Label(entryName, KBaseStyle.Label); // NameLabelStyle
        else
            UI_Tools.Label(entryName);
        // UI_Tools.Label(entryName, thisStyle ?? KBaseStyle.NameLabelStyle);
        GUILayout.FlexibleSpace();
        GUI.SetNextControlName(entryName);
        value = UI_Fields.DoubleField(entryName, value, thisStyle ?? KBaseStyle.TextInputStyle);
        GUILayout.Space(3);
        if (thisStyle != null)
            UI_Tools.Label(unit); // , KBaseStyle.UnitLabelStyle
        else
            UI_Tools.Label(entryName);
        // UI_Tools.Label(unit, thisStyle ?? KBaseStyle.UnitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(FPStyles.SpacingAfterTallEntry);
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
        GUILayout.Space(FPStyles.SpacingAfterEntry);
        return value;
    }

    public double DrawToggleButtonWithTextField(string runString, ManeuverType type, double value, string unit = "", bool parseAsTime = false)
    {
        GUILayout.BeginHorizontal();

        DrawToggleButton(runString, type);
        GUILayout.Space(10);

        value = UI_Fields.DoubleField(runString, value, null, parseAsTime);

        GUILayout.Space(3);
        UI_Tools.Label(unit, KBaseStyle.UnitLabelStyle);
        GUILayout.EndHorizontal();

        GUILayout.Space(FPStyles.SpacingAfterEntry);
        return value;
    }

    public void DrawToggleButtonWithLabel(string runString, ManeuverType type, string label = "", string unit = "", int widthOverride = 0)
    {
        GUILayout.BeginHorizontal();

        DrawToggleButton(runString, type, widthOverride);
        GUILayout.Space(10);

        UI_Tools.Label(label, KBaseStyle.NameLabelStyle);

        GUILayout.Space(3);
        UI_Tools.Label(unit, KBaseStyle.UnitLabelStyle);
        GUILayout.EndHorizontal();

        GUILayout.Space(FPStyles.SpacingAfterTallEntry);
    }


    public void DrawToggleButton(string txt, ManeuverType this_maneuver_type, int widthOverride = 0)
    {
        bool active = ManeuverType == this_maneuver_type;
        bool result = UI_Tools.SmallToggleButton(active, txt, txt, widthOverride);
        if (result != active) { SetManeuverType(result ? this_maneuver_type : ManeuverType.None); }
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

    bool InitDone = false;

    void CreateTabs()
    {
        if (!InitDone)
        {
            Tabs.Pages.Add(new OwnshipManeuversPage());
            Tabs.Pages.Add(new TargetPageShip2Ship());
            Tabs.Pages.Add(new TargetPageShip2Celestial());
            Tabs.Pages.Add(new InterplanetaryPage());
            Tabs.Pages.Add(new MoonPage());
            Tabs.Pages.Add(new ResonantOrbitPage());

            Tabs.Init();

            InitDone = true;
        }
    }

    public void OnGUI()
    {
        CreateTabs();

        // All Tabs get the current situation
        DrawEntry("Situation", String.Format("{0} {1}", SituationToString(FlightPlanPlugin.Instance._activeVessel.Situation), FlightPlanPlugin.Instance._activeVessel.mainBody.bodyName));

        if (BodySelection.ListGUI())
            return;

        if (BurnOptions.ListGUI())
            return;

        // game = GameManager.Instance.Game;
        //ActiveNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        //CurrentNode = (ActiveNodes.Count() > 0) ? ActiveNodes[0] : null;
        FPUtility.RefreshActiveVesselAndCurrentManeuver();
        
        BodySelection.TargetSelectionGUI();
        Tabs.OnGUI();

        // If the selected option is to do an activity "at an altitude", then present an input field for the altitude to use
        if (TimeRef == TimeRef.ALTITUDE)
        {
            FPSettings.Altitude_km = DrawLabelWithTextField("Maneuver Altitude", FPSettings.Altitude_km, "km");
        }
        if (TimeRef == TimeRef.X_FROM_NOW)
        {
            FPSettings.TimeOffset = DrawLabelWithTextField("Time From Now", FPSettings.TimeOffset, "s");
        }

        // Draw the GUI Status at the end of this tab
        double _UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        if (Plugin._currentNode == null && FPStatus.status != FPStatus.Status.VIRGIN && FPStatus.status != FPStatus.Status.ERROR)
        {
            FPStatus.Ok("");
        }
        DrawGUIStatus(_UT);

        // If the selected option is to do an activity "at an altitude", then make sure the altitude is possible for the orbit
        if (TimeRef == TimeRef.ALTITUDE)
        {
            if (FPSettings.Altitude_km * 1000 < Orbit.Periapsis)
            {
                FPSettings.Altitude_km = Math.Ceiling(Orbit.Periapsis) / 1000;
                if (GUI.GetNameOfFocusedControl() != "Maneuver Altitude")
                    UI_Fields.TempDict["Maneuver Altitude"] = FPSettings.Altitude_km.ToString();
            }
            if (Orbit.eccentricity < 1 && FPSettings.Altitude_km * 1000 > Orbit.Apoapsis)
            {
                FPSettings.Altitude_km = Math.Floor(Orbit.Apoapsis) / 1000;
                if (GUI.GetNameOfFocusedControl() != "Maneuver Altitude")
                    UI_Fields.TempDict["Maneuver Altitude"] = FPSettings.Altitude_km.ToString();
            }
        }

        ManeuverDescription = $"{ManeuverTypeDesc} {BurnTimeOption.TimeRefDesc}";
    }

    public string ManeuverDescription;


    public FPOtherModsInterface OtherMods = null;

    private void DrawGUIStatus(double UT)
    {
        FPStatus.DrawUI(UT);

        // Indication to User that its safe to type, or why vessel controls aren't working

        if (OtherMods == null)
        {
            // init mode detection only when first needed
            OtherMods = new FPOtherModsInterface();
            OtherMods.CheckModsVersions();
        }

        OtherMods.OnGUI( Plugin._currentNode);
        GUILayout.Space(FPStyles.SpacingAfterEntry);
    }

    // Radius Computed from Inputs
    public double TargetPeR;
    public double TargetApR;
    public double TargetSMA;
    public double TargetMRPeR;

    /// <summary>
    /// final creation of the Node by calling the main Plugin
    /// </summary>
    public void MakeNode()
    {
        if (ManeuverType == ManeuverType.None)
            return;

        BurnTimeOption.Instance.SetBurnTime();
        double _requestedBurnTime = BurnTimeOption.RequestedBurnTime;

        var Orbiter = FPUtility.ActiveVessel.Orbiter;
        var ManeuverPlanSolver = Orbiter?.ManeuverPlanSolver;

        bool _pass = false;
        bool _launchMNC = false;
        double pError = 0;
        double largeErr = 0.02;
        double smallErr = 0.01;
        var vesselOrbit = Plugin._activeVessel.Orbit;
        var target = Plugin._currentTarget;
        PatchedConicsOrbit targetOrbit = null;
        if (target != null)
            targetOrbit = target.Orbit as PatchedConicsOrbit;

        switch (ManeuverType)
        {
            case ManeuverType.circularize: // Working
                _pass = Plugin.Circularize(_requestedBurnTime, -0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var thisEcc = vesselOrbit.eccentricity;
                    var nextEcc = PatchedConicsList[0].eccentricity;
                    pError = nextEcc / thisEcc;
                    if (pError >= largeErr)
                        FPStatus.Error($"Warning: Requested Eccentricity 0, got {nextEcc:N3}, off by {pError * 100:N3}%");
                    else if (pError >= smallErr)
                        FPStatus.Warning($"Warning: Requested Eccentricity 0, got {nextEcc:N3}, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested Eccentricity 0, got {nextEcc:N3}, off by {pError * 100:N3}%");
                }
                break;
            case ManeuverType.newPe: // Working
                if (TargetPeR < Orbit.Apoapsis || Orbit.eccentricity >= 1)
                    _pass = Plugin.SetNewPe(_requestedBurnTime, TargetPeR, -0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var thisPe = vesselOrbit.Apoapsis;
                    var nextPe = PatchedConicsList[0].Periapsis;
                    pError = (TargetPeR - nextPe) / (TargetPeR - thisPe);
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested Periapsis {(TargetPeR - ReferenceBody.radius) / 1000:N1} km, got {(nextPe - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) > 0.01)
                        FPStatus.Warning($"Warning: Requested Periapsis {(TargetPeR - ReferenceBody.radius) / 1000:N1} km, got {(nextPe - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested Periapsis {(TargetPeR - ReferenceBody.radius) / 1000:N1} km, got {(nextPe - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                }
                else
                    FPStatus.Error($"Unable to set Pe above current Ap");
                break;
            case ManeuverType.newAp:// Working
                if (TargetApR > Orbit.Periapsis)
                    _pass = Plugin.SetNewAp(_requestedBurnTime, TargetApR, -0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var thisAp = vesselOrbit.Apoapsis;
                    var nextAp = PatchedConicsList[0].Apoapsis;
                    pError = (TargetApR - nextAp) / (TargetApR - thisAp);
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested Apoapsis {(TargetApR - ReferenceBody.radius) / 1000:N1} km, got {(nextAp - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) >= smallErr)
                        FPStatus.Warning($"Warning: Requested Apoapsis {(TargetApR - ReferenceBody.radius) / 1000:N1} km, got {(nextAp - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested Apoapsis {(TargetApR - ReferenceBody.radius) / 1000:N1} km, got {(nextAp - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                }
                else
                    FPStatus.Error($"Unable to set Ap below current Pe");
                break;
            case ManeuverType.newPeAp:// Working
                _pass = Plugin.Ellipticize(_requestedBurnTime, TargetApR, TargetPeR, -0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var thisEcc = vesselOrbit.eccentricity;
                    var nextEcc = PatchedConicsList[0].eccentricity;
                    double targetEcc = (TargetApR - TargetPeR) / (TargetApR + TargetPeR);
                    var nextPe = PatchedConicsList[0].Periapsis;
                    var nextAp = PatchedConicsList[0].Apoapsis;
                    var errorPe = Math.Abs(TargetPeR - nextPe);
                    var errorAp = Math.Abs(TargetApR - nextAp);
                    pError = (targetEcc - nextEcc) / (targetEcc - thisEcc);
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested Eccentricity {targetEcc:N3}, got {nextEcc:N3}, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) >= smallErr)
                        FPStatus.Warning($"Warning: Requested Eccentricity {targetEcc:N3}, got {nextEcc:N3}, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested Eccentricity {targetEcc:N3}, got {nextEcc:N3}, off by {pError * 100:N3}%");
                }
                break;
            case ManeuverType.newInc:// Working
                _pass = Plugin.SetInclination(_requestedBurnTime, FPSettings.TargetInc_deg, -0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var thisInc = vesselOrbit.inclination;
                    var nextInc = PatchedConicsList[0].inclination;
                    pError = (FPSettings.TargetInc_deg - nextInc)/(FPSettings.TargetInc_deg - thisInc);
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested Inclination {FPSettings.TargetInc_deg:N1}°, got {nextInc:N1}°, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) >= smallErr)
                        FPStatus.Warning($"Warning: Requested Inclination {FPSettings.TargetInc_deg:N1}°, got {nextInc:N1}°, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested Inclination {FPSettings.TargetInc_deg:N1}°, got {nextInc:N1}°, off by {pError*100:N3}%");
                }
                break;
            case ManeuverType.newLAN: // Untested
                _pass = Plugin.SetNewLAN(_requestedBurnTime, FPSettings.TargetLAN_deg, -0.5);
                _launchMNC = true;
                break;
            case ManeuverType.newNodeLon: // Untested
                _pass = Plugin.SetNodeLongitude(_requestedBurnTime, FPSettings.TargetNodeLong_deg, -0.5);
                _launchMNC = true;
                break;
            case ManeuverType.newSMA: // Working
                _pass = Plugin.SetNewSMA(_requestedBurnTime, TargetSMA, -0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var thisSMA = vesselOrbit.semiMajorAxis;
                    var nextSMA = PatchedConicsList[0].semiMajorAxis;
                    pError = (TargetSMA - nextSMA) / (TargetSMA - thisSMA);
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested SMA {TargetSMA/1000:N1} km, got {nextSMA/1000:N1} km, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) >= smallErr)
                        FPStatus.Warning($"Warning: Requested SMA {TargetSMA / 1000:N1} km, got {nextSMA / 1000:N1} km, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested SMA {TargetSMA / 1000:N1} km, got {nextSMA / 1000:N1} km, off by {pError * 100:N3}%");
                }
                break;
            case ManeuverType.matchPlane: // Working
                _pass = Plugin.MatchPlanes(TimeRef, -0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var thisInc = vesselOrbit.inclination;
                    var nextInc = PatchedConicsList[0].inclination;
                    pError = (targetOrbit.inclination - nextInc) / (targetOrbit.inclination - thisInc);
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested Inclination {FPSettings.TargetInc_deg:N1}°, got {nextInc:N1}°, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) >= smallErr)
                        FPStatus.Warning($"Warning: Requested Inclination {FPSettings.TargetInc_deg:N1}°, got {nextInc:N1}°, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable Results: Requested Inclination {FPSettings.TargetInc_deg:N1}°, got {nextInc:N1}°, off by {pError * 100:N3}%");
                }
                break;
            case ManeuverType.hohmannXfer: // Works if we start in a good enough orbit (reasonably circular, close to target's orbital plane)
                _pass = Plugin.HohmannTransfer(_requestedBurnTime, -0.5);
                _launchMNC = true;
                break;
            case ManeuverType.interceptTgt: // Experimental
                _pass = Plugin.InterceptTgt(_requestedBurnTime, FPSettings.InterceptTime, -0.5);
                _launchMNC = true;
                break;
            case ManeuverType.courseCorrection: // Experimental Works at least some times...
                var thisCATime = vesselOrbit.NextClosestApproachTime(targetOrbit, _requestedBurnTime);
                if (target.IsCelestialBody)
                {
                    _pass = Plugin.CourseCorrection(_requestedBurnTime, FPSettings.InterceptDistanceCelestial * 1000, -0.5);
                    if (_pass)
                    {
                        var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                        var nextCATime = PatchedConicsList[1].NextClosestApproachTime(targetOrbit, _requestedBurnTime);
                        var thisCA = (vesselOrbit.WorldBCIPositionAtUT(thisCATime) - targetOrbit.WorldBCIPositionAtUT(thisCATime)).magnitude;
                        var nextCA = (PatchedConicsList[1].WorldBCIPositionAtUT(nextCATime) - targetOrbit.WorldBCIPositionAtUT(nextCATime)).magnitude;
                        pError = (FPSettings.InterceptDistanceCelestial * 1000 - nextCA) / (FPSettings.InterceptDistanceCelestial * 1000 - thisCA);
                        if (Math.Abs(pError) >= largeErr)
                            FPStatus.Error($"Warning: Requested Intercept {FPSettings.InterceptDistanceCelestial:N1} km, got {nextCA / 1000:N1} km, off by {pError * 100:N3}%");
                        else if (Math.Abs(pError) >= smallErr)
                            FPStatus.Warning($"Warning: Requested Intercept {FPSettings.InterceptDistanceCelestial:N1} km, got {nextCA / 1000:N1} km, off by {pError * 100:N3}%");
                        //else
                        //    FPStatus.Ok($"Acceptable: Requested Intercept {FPSettings.InterceptDistanceCelestial:N1} km, got {nextCA / 1000:N1} km, off by {pError * 100:N3}%");
                    }
                }
                else
                {
                    _pass = Plugin.CourseCorrection(_requestedBurnTime, FPSettings.InterceptDistanceVessel, -0.5);
                    if (_pass)
                    {
                        var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                        var nextCATime = PatchedConicsList[0].NextClosestApproachTime(targetOrbit, _requestedBurnTime);
                        var thisCA = (vesselOrbit.WorldBCIPositionAtUT(thisCATime) - targetOrbit.WorldBCIPositionAtUT(thisCATime)).magnitude;
                        var nextCA = (PatchedConicsList[0].WorldBCIPositionAtUT(nextCATime) - targetOrbit.WorldBCIPositionAtUT(nextCATime)).magnitude;
                        pError = (FPSettings.InterceptDistanceVessel - nextCA) / (FPSettings.InterceptDistanceVessel - thisCA);
                        if (Math.Abs(pError) >= largeErr)
                            FPStatus.Error($"Warning: Requested Intercept {FPSettings.InterceptDistanceVessel:N1} m, got {nextCA:N1} m, off by {pError * 100:N3}%");
                        else if (Math.Abs(pError) >= smallErr)
                            FPStatus.Warning($"Warning: Requested Intercept {FPSettings.InterceptDistanceVessel:N1} m, got {nextCA:N1} m, off by {pError * 100:N3}%");
                        //else
                        //    FPStatus.Ok($"Acceptable: Requested Intercept {FPSettings.InterceptDistanceVessel:N1} m, got {nextCA:N1} m, off by {pError * 100:N3}%");
                    }
                }
                _launchMNC = true;
                break;
            case ManeuverType.moonReturn: // Working
                _pass = Plugin.MoonReturn(_requestedBurnTime, TargetMRPeR, -0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var nextPe = PatchedConicsList[1].Periapsis;
                    pError = (TargetMRPeR - nextPe)/(TargetMRPeR - ReferenceBody.referenceBody.radius);
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested Periapsis {(TargetMRPeR - ReferenceBody.referenceBody.radius) / 1000:N1} km, got {(nextPe - ReferenceBody.referenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) >= smallErr)
                        FPStatus.Warning($"Warning: Requested Periapsis {(TargetMRPeR - ReferenceBody.referenceBody.radius) / 1000:N1} km, got {(nextPe - ReferenceBody.referenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested Periapsis {(TargetMRPeR - ReferenceBody.radius) / 1000:N1} km, got {(nextPe - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                }
                // _launchMNC = true;
                break;
            case ManeuverType.matchVelocity: // Working
                _pass = Plugin.MatchVelocity(_requestedBurnTime, -0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var tgtVel = targetOrbit.WorldOrbitalVelocityAtUT(_requestedBurnTime);
                    var thisVel = vesselOrbit.WorldOrbitalVelocityAtUT(_requestedBurnTime);
                    var nextVel = PatchedConicsList[0].WorldOrbitalVelocityAtUT(_requestedBurnTime);
                    pError = (tgtVel - nextVel).magnitude / (tgtVel - thisVel).magnitude;
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested Velocity {tgtVel.magnitude:N1} m/s, got {nextVel.magnitude:N1} m/s, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) >= smallErr)
                        FPStatus.Warning($"Warning: Requested Velocity {tgtVel.magnitude:N1} m/s, got {nextVel.magnitude:N1} m/s, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested Periapsis {tgtVel.magnitude:N1} m/s, got {nextVel.magnitude:N1} m/s, off by {pError * 100:N3}%");
                }
                break;
            case ManeuverType.planetaryXfer: // Mostly working, but you'll probably need to tweak the departure and also need a course correction
                _pass = Plugin.PlanetaryXfer(_requestedBurnTime, -0.5);
                _launchMNC = true;
                break;
            case ManeuverType.fixAp: // Working
                _pass = Plugin.SetNewAp(_requestedBurnTime, ResonantOrbitPage.Ap2, - 0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var thisAp = vesselOrbit.Apoapsis;
                    var nextAp = PatchedConicsList[0].Apoapsis;
                    pError = (TargetApR - nextAp) / (TargetApR - thisAp);
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested Apoapsis {(TargetApR - ReferenceBody.radius) / 1000:N1} km, got {(nextAp - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) >= smallErr)
                        FPStatus.Warning($"Warning: Requested Apoapsis {(TargetApR - ReferenceBody.radius) / 1000:N1} km, got {(nextAp - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested Apoapsis {(TargetApR - ReferenceBody.radius) / 1000:N1} km, got {(nextAp - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                }
                break;
            case ManeuverType.fixPe: // Working
                _pass = Plugin.SetNewPe(_requestedBurnTime, ResonantOrbitPage.Pe2, - 0.5);
                if (_pass)
                {
                    var PatchedConicsList = ManeuverPlanSolver?.PatchedConicsList;
                    var thisPe = vesselOrbit.Apoapsis;
                    var nextPe = PatchedConicsList[0].Periapsis;
                    pError = (TargetPeR - nextPe) / (TargetPeR - thisPe);
                    if (Math.Abs(pError) >= largeErr)
                        FPStatus.Error($"Warning: Requested Periapsis {(TargetPeR - ReferenceBody.radius) / 1000:N1} km, got {(nextPe - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    else if (Math.Abs(pError) >= smallErr)
                        FPStatus.Warning($"Warning: Requested Periapsis {(TargetPeR - ReferenceBody.radius) / 1000:N1} km, got {(nextPe - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                    //else
                    //    FPStatus.Ok($"Acceptable: Requested Periapsis {(TargetPeR - ReferenceBody.radius) / 1000:N1} km, got {(nextPe - ReferenceBody.radius) / 1000:N1} km, off by {pError * 100:N3}%");
                }
                break;
        }

        if (_pass && Plugin._autoLaunchMNC.Value && (_launchMNC || Math.Abs(pError) >= smallErr))
            FPOtherModsInterface.instance.CallMNC();
    }
}