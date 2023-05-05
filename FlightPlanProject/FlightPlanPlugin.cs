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
using Microsoft.CodeAnalysis;

namespace FlightPlan;

/// <summary>
///  The selected mode in UI
/// </summary>
public enum ManeuverType
{
    None,
    circularize,
    newPe,
    newAp,
    newPeAp,
    newInc,
    newLAN,
    newNodeLon,
    newSMA,
    matchPlane,
    hohmannXfer,
    courseCorrection,
    interceptTgt,
    matchVelocity,
    moonReturn,
    planetaryXfer
}

/// <summary>
///  The selected time Reference
/// </summary>
public enum TimeRef
{
    None,
    COMPUTED,
    APOAPSIS,
    PERIAPSIS,
    CLOSEST_APPROACH,
    EQ_ASCENDING,
    EQ_DESCENDING,
    REL_ASCENDING,
    REL_DESCENDING,
    X_FROM_NOW,
    ALTITUDE,
    EQ_NEAREST_AD,
    EQ_HIGHEST_AD,
    REL_NEAREST_AD,
    REL_HIGHEST_AD
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
[BepInDependency(NodeManagerPlugin.ModGuid, NodeManagerPlugin.ModVer)]
public class FlightPlanPlugin : BaseSpaceWarpPlugin
{
    public static FlightPlanPlugin Instance { get; set; }

    // These are useful in case some other mod wants to add a dependency to this one
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    // Control game input state while user has clicked into a TextField.
    //public List<String> inputFields = new List<String>();

    // GUI stuff
    static bool loaded = false;
    private bool interfaceEnabled = false;
    private bool GUIenabled = true;
    private Rect windowRect = Rect.zero;
    private int windowWidth = 250; //384px on 1920x1080

   

    public FlightPlanUI main_ui;

    // Config parameters
    internal ConfigEntry<bool> experimental;
    internal ConfigEntry<bool> autoLaunchMNC;

    // mod-wide data
    internal VesselComponent activeVessel;
    internal SimulationObjectModel currentTarget;
    internal ManeuverNodeData currentNode = null;
    List<ManeuverNodeData> activeNodes;

    private GameInstance game;

    // App bar button(s)
    private const string ToolbarFlightButtonID = "BTN-FlightPlanFlight";
    // private const string ToolbarOABButtonID = "BTN-FlightPlanOAB";

    private static string _assemblyFolder;
    private static string AssemblyFolder =>
        _assemblyFolder ?? (_assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

    private static string _settingsPath;
    private static string SettingsPath =>
        _settingsPath ?? (_settingsPath = Path.Combine(AssemblyFolder, "settings.json"));

    //public ManualLogSource logger;
    public new static ManualLogSource Logger { get; set; }

    // private string MNCGUID = "com.github.xyz3211.maneuver_node_controller";

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        KBaseSettings.Init(SettingsPath);
        main_ui = new FlightPlanUI(this);

        Instance = this;

        game = GameManager.Instance.Game;
        Logger = base.Logger;

        // Subscribe to messages that indicate it's OK to raise the GUI
        // StateChanges.FlightViewEntered += message => GUIenabled = true;
        // StateChanges.Map3DViewEntered += message => GUIenabled = true;

        // Subscribe to messages that indicate it's not OK to raise the GUI
        // StateChanges.FlightViewLeft += message => GUIenabled = false;
        // StateChanges.Map3DViewLeft += message => GUIenabled = false;
        // StateChanges.VehicleAssemblyBuilderEntered += message => GUIenabled = false;
        // StateChanges.KerbalSpaceCenterStateEntered += message => GUIenabled = false;
        // StateChanges.BaseAssemblyEditorEntered += message => GUIenabled = false;
        // StateChanges.MainMenuStateEntered += message => GUIenabled = false;
        // StateChanges.ColonyViewEntered += message => GUIenabled = false;
        // StateChanges.TrainingCenterEntered += message => GUIenabled = false;
        // StateChanges.MissionControlEntered += message => GUIenabled = false;
        // StateChanges.TrackingStationEntered += message => GUIenabled = false;
        // StateChanges.ResearchAndDevelopmentEntered += message => GUIenabled = false;
        // StateChanges.LaunchpadEntered += message => GUIenabled = false;
        // StateChanges.RunwayEntered += message => GUIenabled = false;

        Logger.LogInfo("Loaded");
        if (loaded)
        {
            Destroy(this);
        }
        loaded = true;

        

        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);

        Appbar.RegisterAppButton(
            "Flight Plan",
            ToolbarFlightButtonID,
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            ToggleButton);

        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(FlightPlanPlugin).Assembly);

        // Fetch a configuration value or create a default one if it does not exist

        FPStatus.Init(this);
        
         
        experimental  = Config.Bind<bool>("Experimental Section", "Experimental Features",      false, "Enable/Disable experimental.Value features for testing - Warrantee Void if Enabled!");
        autoLaunchMNC = Config.Bind<bool>("Experimental Section", "Launch Maneuver Node Controller", false, "Enable/Disable automatically launching the Maneuver Node Controller GUI (if installed) when experimental.Value nodes are created");
    
        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        Logger.LogInfo($"Experimental Features: {experimental.Value}");
    }

    private void ToggleButton(bool toggle)
    {
        interfaceEnabled = toggle;
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(interfaceEnabled);
    }

    void Awake()
    {
  

    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            ToggleButton(!interfaceEnabled);
            Logger.LogInfo("Update: UI toggled with hotkey");
        }

        if (main_ui != null)
            main_ui.Update();
    }  

    void save_rect_pos()
    {
        KBaseSettings.window_x_pos = (int)windowRect.xMin;
        KBaseSettings.window_y_pos = (int)windowRect.yMin;
    }

    /// <summary>
    /// Draws a simple UI window when <code>this._isWindowOpen</code> is set to <code>true</code>.
    /// </summary>
    private void OnGUI()
    {
        GUIenabled = false;
        var gameState = Game?.GlobalGameState?.GetState();
        if (gameState == GameState.Map3DView) GUIenabled = true;
        if (gameState == GameState.FlightView) GUIenabled = true;
        //if (Game.GlobalGameState.GetState() == GameState.TrainingCenter) GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.TrackingStation) GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.VehicleAssemblyBuilder) GUIenabled = false;
        //// if (Game.GlobalGameState.GetState() == GameState.MissionControl) GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.Loading) GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.KerbalSpaceCenter) GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.Launchpad) GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.Runway) GUIenabled = false;

        activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        currentTarget = activeVessel?.TargetObject;

        // Set the UI
        if (interfaceEnabled && GUIenabled && activeVessel != null)
        {
            FPStyles.Init();
            WindowTool.check_main_window_pos(ref windowRect);
            GUI.skin = KBaseStyle.skin;

            windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                windowRect,
                FillWindow,
                "<color=#696DFF>FLIGHT PLAN</color>",
                GUILayout.Height(0),
                GUILayout.Width(windowWidth));

            save_rect_pos();
            // Draw the tool tip if needed
            ToolTipsManager.DrawToolTips();

            // check editor focus and unset Input if needed
            UI_Fields.CheckEditor();
        }
    }

    private ManeuverNodeData getCurrentNode()
    {
        activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        return (activeNodes.Count() > 0) ? activeNodes[0] : null;
    }

    /// <summary>
    /// Defines the content of the UI window drawn in the <code>OnGui</code> method.
    /// </summary>
    /// <param name="windowID"></param>
    private void FillWindow(int windowID)
    {
        TopButtons.Init(windowRect.width);
        if ( TopButtons.Button(KBaseStyle.cross))
            CloseWindow();

        GUI.Label(new Rect(9, 2, 29, 29), KBaseStyle.icon, KBaseStyle.icons_label);

        currentNode = getCurrentNode();

        main_ui.OnGUI();

        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    // This method sould be called at the top of FillWindow to enable toggle buttons to work like radio buttons

    private void CloseWindow()
    {
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
        interfaceEnabled = false;
        ToggleButton(interfaceEnabled);

        UI_Fields.GameInputState = true;
    }


   

    private void CreateManeuverNode(Vector3d deltaV, double burnUT, double burnOffsetFactor = -0.5)
    {
        Vector3d burnParams;
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
        Logger.LogDebug($"CreateManeuverNod: Solution Found: deltaV      [{deltaV.x:F3}, {deltaV.y:F3}, {deltaV.z:F3}] m/s = {deltaV.magnitude:F3} m/s {(burnUT - UT):F3} s from UT");
        Logger.LogDebug($"CreateManeuverNod: Solution Found: burnParams  [{burnParams.x:F3}, {burnParams.y:F3}, {burnParams.z:F3}] m/s  = {burnParams.magnitude:F3} m/s {(burnUT - UT):F3} s from UT");
        NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, burnOffsetFactor);
        // StartCoroutine(TestPerturbedOrbit(orbit, burnUT, deltaV));

        // Recalculate node based on the offset time
        //var nodeTimeAdj = -currentNode.BurnDuration / 2;
        //var burnStartTime = currentNode.Time + nodeTimeAdj;
        //Logger.LogDebug($"BurnDuration: {currentNode.BurnDuration}, Adjusting start of burn by {nodeTimeAdj}s");
        //deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnStartTime);
        //burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnStartTime, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
        ////var burnParamsT1 = orbit.DeltaVToManeuverNodeCoordinates(UT, deltaV);
        ////var burnParamsT2 = orbit.DeltaVToManeuverNodeCoordinates(UT, activeVessel, deltaV);
        ////Logger.LogDebug($"OG burnParams               [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
        ////Logger.LogDebug($"Test: burnParamsT1          [{burnParamsT1.x}, {burnParamsT1.y}, {burnParamsT1.z}] m/s = {burnParamsT1.magnitude} m/s");
        ////Logger.LogDebug($"Test: burnParamsT2          [{burnParamsT2.x}, {burnParamsT2.y}, {burnParamsT2.z}] m/s = {burnParamsT2.magnitude} m/s");
        //Logger.LogDebug($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
        //Logger.LogDebug($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
        //currentNode.BurnVector = burnParams;
        //UpdateNode(currentNode);

    }

    // Flight Plan API Methods
    public bool Circularize(double burnUT, double burnOffsetFactor = -0.5)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"Circularize {BurnTimeOption.TimeRefDesc}");
        //var startTimeOffset = 60;
        //var burnUT = UT + startTimeOffset;
        var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT);

        FPStatus.Ok($"Ready to Circularize {BurnTimeOption.TimeRefDesc}");

        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error("Circularize Now: Solution Not Found!");
            return false;
        }
    }

    public bool SetNewPe(double burnUT, double newPe, double burnOffsetFactor = -0.5)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNewPe {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Pe");
        //var TimeToAp = orbit.TimeToAp;
        //double burnUT, e;
        //e = orbit.eccentricity;
        //if (e < 1)
        //    burnUT = UT + TimeToAp;
        //else
        //    burnUT = UT + 30;


        FPStatus.Ok($"Ready to Change Pe {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: targetPeR {newPe} m, currentPeR {orbit.Periapsis} m, body.radius {orbit.referenceBody.radius} m");
        // Debug.Log($"Seeking Solution: targetPeR {targetPeR} m, currentPeR {orbit.Periapsis} m, body.radius {orbit.referenceBody.radius} m");
        var deltaV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, burnUT, newPe);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error("Set New Pe: Solution Not Found!");
            return false;
        }
    }

    public bool SetNewAp(double burnUT, double newAp, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNewAp {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Ap");
        //var TimeToPe = orbit.TimeToPe;
        //var burnUT = UT + TimeToPe;

        FPStatus.Ok($"Ready to Change Ap {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: targetApR {newAp} m, currentApR {orbit.Apoapsis} m");
        var deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, burnUT, newAp);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error("Set New Ap: Solution Not Found!");
            return false;
        }
    }

    public bool Ellipticize(double burnUT, double newAp, double newPe, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"Ellipticize: Set New Pe and Ap {BurnTimeOption.TimeRefDesc}");


        FPStatus.Ok($"Ready to Ellipticize {BurnTimeOption.TimeRefDesc}");

        if (newPe > newAp)
        {
            (newPe, newAp) = (newAp, newPe);
            FPStatus.Warning("Pe Setting > Ap Setting");
        }

        Logger.LogDebug($"Seeking Solution: targetPeR {newPe} m, targetApR {newAp} m, body.radius {orbit.referenceBody.radius} m");
        // var burnUT = UT + 30;
        var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(orbit, burnUT, newPe, newAp);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error("Set New Pe and Ap: Solution Not Found!");
            return false;
        }
    }

    public bool SetInclination(double burnUT, double inclination, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetInclination: Set New Inclination {inclination}° {BurnTimeOption.TimeRefDesc}");
        // double burnUT, TAN, TDN;
        Vector3d deltaV;

        FPStatus.Ok($"Ready to Change Inclination {BurnTimeOption.TimeRefDesc}");

        deltaV = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, burnUT, inclination);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error("Set New Inclination: Solution Not Found!");
            return false;
        }
    }
    
    public bool SetNewLAN(double burnUT, double newLANvalue, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNewLAN: Set New LAN {newLANvalue}° {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = UT + 30;

        FPStatus.Warning($"Experimental LAN Change {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: newLANvalue {newLANvalue}°");
        var deltaV = OrbitalManeuverCalculator.DeltaVToShiftLAN(orbit, burnUT, newLANvalue);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error("Set New LAN: Solution Not Found!");
            return false;
        }
    }

    public bool SetNodeLongitude(double burnUT, double newNodeLongValue, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNodeLongitude: Set Node Longitude {newNodeLongValue}° {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = UT + 30;

        FPStatus.Warning($"Experimental Node Longitude Change {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: newNodeLongValue {newNodeLongValue}°");
        var deltaV = OrbitalManeuverCalculator.DeltaVToShiftNodeLongitude(orbit, burnUT, newNodeLongValue);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error("Shift Node Longitude: Solution Not Found!");
            return false;
        }
    }

    public bool SetNewSMA(double burnUT, double newSMA, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNewSMA {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = UT + 30;

        FPStatus.Error($"Ready to Change SMA Change {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: newSMA {newSMA} m");
        var deltaV = OrbitalManeuverCalculator.DeltaVForSemiMajorAxis(orbit, burnUT, newSMA);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error("Set New SMA: Solution Not Found!");
            return false;
        }
    }
    
    // No longer takes double burnUT. Need to sort out how this can be called as an API method
    public bool MatchPlanes(TimeRef time_ref, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MatchPlanes: Match Planes with {currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        double burnUTout = UT + 1;

        FPStatus.Ok($"Ready to Match Planes with {currentTarget.Name} {BurnTimeOption.TimeRefDesc}");

        Vector3d deltaV = Vector3d.zero;
        if (time_ref == TimeRef.REL_ASCENDING)
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
        else if (time_ref == TimeRef.REL_DESCENDING)
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
        else if (time_ref == TimeRef.REL_NEAREST_AD)
        {
            if (orbit.TimeOfAscendingNode(currentTarget.Orbit as PatchedConicsOrbit, UT) < orbit.TimeOfDescendingNode(currentTarget.Orbit as PatchedConicsOrbit, UT))
                deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
            else
                deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
        }
        else if (time_ref == TimeRef.REL_HIGHEST_AD)
        {
            var anTime = orbit.TimeOfAscendingNode(currentTarget.Orbit as PatchedConicsOrbit, UT);
            var dnTime = orbit.TimeOfDescendingNode(currentTarget.Orbit as PatchedConicsOrbit, UT);
            if (orbit.Radius(anTime) > orbit.Radius(dnTime))
                deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
            else
                deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
        }
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUTout, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error($"Match Planes with {currentTarget.Name} at AN: Solution Not Found!");
            return false;
        }
    }

    public bool HohmannTransfer(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"HohmannTransfer: Hohmann Transfer to {currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Hohmann Transfer");
        double burnUTout;
        Vector3d deltaV;

        FPStatus.Warning($"Ready to Transfer to {currentTarget.Name}?");

        bool simpleTransfer = true;
        bool intercept_only = true;
        if (simpleTransfer)
        {
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
        }
        else
        {
            var anExists = orbit.AscendingNodeExists(currentTarget.Orbit as PatchedConicsOrbit);
            var dnExists = orbit.DescendingNodeExists(currentTarget.Orbit as PatchedConicsOrbit);
            double anTime = orbit.TimeOfAscendingNode(currentTarget.Orbit as PatchedConicsOrbit, UT);
            double dnTime = orbit.TimeOfDescendingNode(currentTarget.Orbit as PatchedConicsOrbit, UT);
            // burnUT = timeSelector.ComputeManeuverTime(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit);
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout, intercept_only: intercept_only, fixed_ut: false);
        }

        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUTout, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error($"Hohmann Transfer to {currentTarget.Name}: Solution Not Found!");
            return false;
        }
    }

    public bool InterceptTgt(double burnUT, double tgtUT, double burnOffsetFactor)
    {
        // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
        // Adapted from call found in MechJebModuleScriptActionRendezvous.cs for "Get Closer"
        // Similar to code in MechJebModuleRendezvousGuidance.cs for "Get Closer" button code.

        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"InterceptTgt: Intercept {currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        // var burnUT = UT + 30;
        var interceptUT = UT + tgtUT;
        double offsetDistance;

        FPStatus.Warning($"Experimental Intercept of {currentTarget.Name} Ready");

        Logger.LogDebug($"Seeking Solution: interceptT {FPSettings.interceptT} s");
        if (currentTarget.IsCelestialBody) // For a target that is a celestial
            offsetDistance = currentTarget.Orbit.referenceBody.radius + 50000;
        else
            offsetDistance = 100;
        var deltaV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, burnUT, currentTarget.Orbit as PatchedConicsOrbit, interceptUT, offsetDistance);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error($"Intercept {currentTarget.Name}: No Solution Found!");
            return false;
        }
    }

    public bool CourseCorrection(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"CourseCorrection: Course Correction burn to improve trajectory to {currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        double burnUTout;
        Vector3d deltaV;

        FPStatus.Ok("Course Correction Ready");

        if (currentTarget.IsCelestialBody) // For a target that is a celestial
        {
            Logger.LogDebug($"Seeking Solution for Celestial Target");
            double finalPeR = currentTarget.CelestialBody.radius + 50000; // m (PeR at celestial target)
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, currentTarget.Orbit.referenceBody, finalPeR, out burnUTout);
        }
        else // For a tartget that is not a celestial
        {
            Logger.LogDebug($"Seeking Solution for Non-Celestial Target");
            double caDistance = 100; // m (closest approach to non-celestial target)
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, caDistance, out burnUTout);
        }
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUTout, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error($"Course Correction for tragetory to {currentTarget.Name}: No Solution Found!");
            return false;
        }
    }

    public bool MoonReturn(double burnUT, double targetMRPeR, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MoonReturn: Return from {orbit.referenceBody.Name} {BurnTimeOption.TimeRefDesc}");
        var e = orbit.eccentricity;

        FPStatus.Warning($"Ready to Return from {orbit.referenceBody.Name}?");

        if (e > 0.2)
        {
            FPStatus.Error($"Moon Return: Starting Orbit Eccentrity Too Large {e.ToString("F2")} is > 0.2");
            return false;
        }
        else
        {
            double burnUTout;
            // double primaryRaidus = orbit.referenceBody.Orbit.referenceBody.radius + 100000; // m
            Logger.LogDebug($"Moon Return Attempting to Solve...");
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(orbit, UT, targetMRPeR, out burnUTout);
            if (deltaV != Vector3d.zero)
            {
                CreateManeuverNode(deltaV, burnUTout, burnOffsetFactor);
                return true;
            }
            else
            {
                FPStatus.Error("Moon Return: No Solution Found!");
                return false;
            }
        }
    }

    public bool MatchVelocity(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MatchVelocity: Match Velocity with {currentTarget.Name} {BurnTimeOption.TimeRefDesc}");

        FPStatus.Warning($"Experimental Velocity Match with {currentTarget.Name} Ready");

        // double closestApproachTime = orbit.NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT + 2); //+2 so that closestApproachTime is definitely > UT
        var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, burnUT, currentTarget.Orbit as PatchedConicsOrbit);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error($"Match Velocity with {currentTarget.Name} at Closest Approach: No Solution Found!");
            return false;
        }
    }

    public bool PlanetaryXfer(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"PlanetaryXfer: Transfer to {currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        double burnUTout, burnUT2;
        bool syncPhaseAngle = true;

        FPStatus.Warning($"Experimental Transfer to {currentTarget.Name} Ready");

        var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, syncPhaseAngle, out burnUTout);
        var deltaV2 = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryLambertTransferEjection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, out burnUT2);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUTout, burnOffsetFactor);
            return true;
        }
        else
        {
            FPStatus.Error($"Planetary Transfer to {currentTarget.Name}: No Solution Found!");
            return false;
        }
    }

    private IEnumerator TestPerturbedOrbit(PatchedConicsOrbit o, double burnUT, Vector3d dV)
    {
        // This code compares the orbit info returned from a PerturbedOrbit orbit call with the
        // info for the orbit in the next patch. It should be called after creating a maneuver
        // node for the active vessel that applies the burn vector associated with the dV to
        // make sure that PerturbedOrbit is correctly predicting the effect of delta V on the
        // current orbit.

        // NodeManagerPlugin.Instance.RefreshNodes();
        yield return (object)new WaitForFixedUpdate();

        //List<ManeuverNodeData> patchList =
        //    Game.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.SimulationObject.GlobalId);

        Logger.LogDebug($"TestPerturbedOrbit: patchList.Count = {NodeManagerPlugin.Instance.Nodes.Count}");

        if (NodeManagerPlugin.Instance.Nodes.Count == 0)
        {
            Logger.LogDebug($"TestPerturbedOrbit: No future patches to compare to.");
        }
        else
        {
            PatchedConicsOrbit hypotheticalOrbit = o.PerturbedOrbit(burnUT, dV);
            ManeuverPlanSolver maneuverPlanSolver = activeVessel.Orbiter?.ManeuverPlanSolver;
            var PatchedConicsList = maneuverPlanSolver?.PatchedConicsList;
            PatchedConicsOrbit nextOrbit; // = PatchedConicsList[0];
            if (NodeManagerPlugin.Instance.Nodes[0].ManeuverTrajectoryPatch != null) { nextOrbit = NodeManagerPlugin.Instance.Nodes[0].ManeuverTrajectoryPatch; }
            else { nextOrbit = maneuverPlanSolver.ManeuverTrajectory[0] as PatchedConicsOrbit; }


            // IPatchedOrbit orbit = null;

            Logger.LogDebug($"thisOrbit:{o}");
            Logger.LogDebug($"nextOrbit:{nextOrbit}");
            Logger.LogDebug($"nextOrbit: inc = {PatchedConicsList[0].inclination.ToString("n3")}");
            Logger.LogDebug($"nextOrbit: ecc = {PatchedConicsList[0].eccentricity.ToString("n3")}");
            Logger.LogDebug($"nextOrbit: sma = {PatchedConicsList[0].semiMajorAxis.ToString("n3")}");
            Logger.LogDebug($"nextOrbit: lan = {PatchedConicsList[0].longitudeOfAscendingNode.ToString("n3")}");
            Logger.LogDebug($"nextOrbit: ApA = {(PatchedConicsList[0].ApoapsisArl / 1000).ToString("n3")}");
            Logger.LogDebug($"nextOrbit: PeA = {(PatchedConicsList[0].PeriapsisArl / 1000).ToString("n3")}");
            Logger.LogDebug($"hypotheticalOrbit:{hypotheticalOrbit}");
        }
    }
}
