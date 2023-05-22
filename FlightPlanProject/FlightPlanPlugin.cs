using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FlightPlan.KTools;
using FlightPlan.KTools.UI;
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
    planetaryXfer,
    advancedPlanetaryXfer,
    fixAp,
    fixPe
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
    //public List<String> InputFields = new List<String>();

    // GUI stuff
    static bool Loaded = false;
    private bool _interfaceEnabled = false;
    private bool _GUIenabled = true;
    private Rect _windowRect = Rect.zero;
    private int _windowWidth = 250; //384px on 1920x1080

   

    public FlightPlanUI MainUI;

    // Config parameters
    internal ConfigEntry<bool> _experimental;
    internal ConfigEntry<bool> _autoLaunchMNC;
    internal ConfigEntry<double> _smallError;
    internal ConfigEntry<double> _largeError;

    // mod-wide Data
    internal VesselComponent _activeVessel;
    internal SimulationObjectModel _currentTarget;
    internal ManeuverNodeData _currentNode = null;
    List<ManeuverNodeData> ActiveNodes;

    private GameInstance game;

    // App bar Button(s)
    private const string _ToolbarFlightButtonID = "BTN-FlightPlanFlight";
    // private const string _ToolbarOABButtonID = "BTN-FlightPlanOAB";

    private static string _assemblyFolder;
    private static string _AssemblyFolder =>
        _assemblyFolder ?? (_assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

    private static string _settingsPath;
    private static string _SettingsPath =>
        _settingsPath ?? (_settingsPath = Path.Combine(_AssemblyFolder, "settings.json"));

    //public ManualLogSource Logger;
    public new static ManualLogSource Logger { get; set; }

    // private string MNCGUID = "com.github.xyz3211.maneuver_node_controller";

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        KBaseSettings.Init(_SettingsPath);
        MainUI = new FlightPlanUI(this);

        Instance = this;

        game = GameManager.Instance.Game;
        Logger = base.Logger;

        // Subscribe to messages that indicate it's OK to raise the GUI
        // StateChanges.FlightViewEntered += message => _GUIenabled = true;
        // StateChanges.Map3DViewEntered += message => _GUIenabled = true;

        // Subscribe to messages that indicate it's not OK to raise the GUI
        // StateChanges.FlightViewLeft += message => _GUIenabled = false;
        // StateChanges.Map3DViewLeft += message => _GUIenabled = false;
        // StateChanges.VehicleAssemblyBuilderEntered += message => _GUIenabled = false;
        // StateChanges.KerbalSpaceCenterStateEntered += message => _GUIenabled = false;
        // StateChanges.BaseAssemblyEditorEntered += message => _GUIenabled = false;
        // StateChanges.MainMenuStateEntered += message => _GUIenabled = false;
        // StateChanges.ColonyViewEntered += message => _GUIenabled = false;
        // StateChanges.TrainingCenterEntered += message => _GUIenabled = false;
        // StateChanges.MissionControlEntered += message => _GUIenabled = false;
        // StateChanges.TrackingStationEntered += message => _GUIenabled = false;
        // StateChanges.ResearchAndDevelopmentEntered += message => _GUIenabled = false;
        // StateChanges.LaunchpadEntered += message => _GUIenabled = false;
        // StateChanges.RunwayEntered += message => _GUIenabled = false;

        Logger.LogInfo("Loaded");
        if (Loaded)
        {
            Destroy(this);
        }
        Loaded = true;

        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);

        Appbar.RegisterAppButton(
            "Flight Plan",
            _ToolbarFlightButtonID,
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/Icon.png"),
            ToggleButton);

        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(FlightPlanPlugin).Assembly);

        // Fetch a configuration value or create a default one if it does not exist

        FPStatus.Init(this);
        
        _experimental  = Config.Bind<bool>("Experimental Section", "Experimental Features",      false, "Enable/Disable _experimental.Value features for testing - Warrantee Void if Enabled!");
        _autoLaunchMNC = Config.Bind<bool>("Experimental Section", "Launch Maneuver Node Controller", false, "Enable/Disable automatically launching the Maneuver Node Controller GUI (if installed) when _experimental.Value nodes are created");
        _smallError = Config.Bind<double>("Status Reporting Section", "Small % Error Threashold", 1, "Percent error threshold used to assess quality of maneuver node goal for warning (yellow) status");
        _largeError = Config.Bind<double>("Status Reporting Section", "Large % Error Threashold", 2, "Percent error threshold used to assess quality of maneuver node goal for error (red) status");

        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        Logger.LogInfo($"Experimental Features: {_experimental.Value}");
    }

    private void ToggleButton(bool toggle)
    {
        _interfaceEnabled = toggle;
        GameObject.Find(_ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(_interfaceEnabled);
    }

    void Awake()
    {

    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            ToggleButton(!_interfaceEnabled);
            Logger.LogInfo("Update: UI toggled with hotkey");
        }

        if (MainUI != null)
            MainUI.Update();
    }  

    void save_rect_pos()
    {
        KBaseSettings.WindowXPos = (int)_windowRect.xMin;
        KBaseSettings.WindowYPos = (int)_windowRect.yMin;
    }

    /// <summary>
    /// Draws a simple UI window when <code>this._isWindowOpen</code> is set to <code>true</code>.
    /// </summary>
    private void OnGUI()
    {
        _GUIenabled = false;
        var _gameState = Game?.GlobalGameState?.GetState();
        if (_gameState == GameState.Map3DView) _GUIenabled = true;
        if (_gameState == GameState.FlightView) _GUIenabled = true;
        //if (Game.GlobalGameState.GetState() == GameState.TrainingCenter) _GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.TrackingStation) _GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.VehicleAssemblyBuilder) _GUIenabled = false;
        //// if (Game.GlobalGameState.GetState() == GameState.MissionControl) _GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.Loading) _GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.KerbalSpaceCenter) _GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.Launchpad) _GUIenabled = false;
        //if (Game.GlobalGameState.GetState() == GameState.Runway) _GUIenabled = false;

        _activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        _currentTarget = _activeVessel?.TargetObject;

        // Set the UI
        if (_interfaceEnabled && _GUIenabled && _activeVessel != null)
        {
            FPStyles.Init();
            WindowTool.CheckMainWindowPos(ref _windowRect);
            GUI.skin = KBaseStyle.Skin;

            _windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                _windowRect,
                FillWindow,
                "<color=#696DFF>FLIGHT PLAN</color>",
                GUILayout.Height(0),
                GUILayout.Width(_windowWidth));

            save_rect_pos();
            // Draw the tool tip if needed
            ToolTipsManager.DrawToolTips();

            // check editor focus and unset Input if needed
            UI_Fields.CheckEditor();
        }
    }

    private ManeuverNodeData GetCurrentNode()
    {
        ActiveNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(Game.ViewController.GetActiveVehicle(true).Guid);
        return (ActiveNodes.Count() > 0) ? ActiveNodes[0] : null;
    }

    /// <summary>
    /// Defines the content of the UI window drawn in the <code>OnGui</code> method.
    /// </summary>
    /// <param name="windowID"></param>
    private void FillWindow(int windowID)
    {
        TopButtons.Init(_windowRect.width);

        // Place the Plugin's main Icon in the upper left
        GUI.Label(new Rect(9, 2, 29, 29), KBaseStyle.Icon, KBaseStyle.IconsLabel);

        // Place a close window Icon in the upper right
        if ( TopButtons.Button(KBaseStyle.Cross))
            CloseWindow();

        _currentNode = GetCurrentNode();

        MainUI.OnGUI();

        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    // This method sould be called at the top of FillWindow to enable Toggle buttons to work like radio buttons

    private void CloseWindow()
    {
        GameObject.Find(_ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
        _interfaceEnabled = false;
        ToggleButton(_interfaceEnabled);

        UI_Fields.GameInputState = true;
    }

    private IEnumerator CreateManeuverNode(Vector3d deltaV, double burnUT, double burnOffsetFactor = -0.5)
    {
        Vector3d burnParams;
        double UT = Game.UniverseModel.UniversalTime;
        var orbit = _activeVessel.Orbit;
        var Orbiter = _activeVessel.Orbiter;
        var ManeuverPlanSolver = Orbiter?.ManeuverPlanSolver;

        burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(ActiveVessel.orbit, _deltaV, burnUT);
        Logger.LogDebug($"CreateManeuverNode: Solution Found: _deltaV      [{deltaV.x:F3}, {deltaV.y:F3}, {deltaV.z:F3}] m/s = {deltaV.magnitude:F3} m/s {FPUtility.SecondsToTimeString(burnUT - UT)} from now");
        Logger.LogDebug($"CreateManeuverNode: Solution Found: burnParams  [{burnParams.x:F3}, {burnParams.y:F3}, {burnParams.z:F3}] m/s  = {burnParams.magnitude:F3} m/s {FPUtility.SecondsToTimeString(burnUT - UT)} from now");
        NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, burnOffsetFactor);
        _currentNode = NodeManagerPlugin.Instance.currentNode;

        yield return (object)new WaitForFixedUpdate();

        // Having this here can sometimes result in a weird double node.
        // ManeuverPlanSolver.UpdateManeuverTrajectory();

        FlightPlanUI.Instance.CheckNodeQuality();
        // StartCoroutine(TestPerturbedOrbit(orbit, burnUT, _deltaV));

        // Recalculate node based on the Offset time
        //var nodeTimeAdj = -CurrentNode.BurnDuration / 2;
        //var burnStartTime = CurrentNode.Time + nodeTimeAdj;
        //Logger.LogDebug($"BurnDuration: {CurrentNode.BurnDuration}, Adjusting start of burn by {nodeTimeAdj}s");
        //_deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnStartTime);
        //burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnStartTime, _deltaV); // OrbitalManeuverCalculator.DvToBurnVec(ActiveVessel.orbit, _deltaV, burnUT);
        ////var burnParamsT1 = orbit.DeltaVToManeuverNodeCoordinates(_UT, _deltaV);
        ////var burnParamsT2 = orbit.DeltaVToManeuverNodeCoordinates(_UT, ActiveVessel, _deltaV);
        ////Logger.LogDebug($"OG burnParams               [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - _UT} s from _UT");
        ////Logger.LogDebug($"Test: burnParamsT1          [{burnParamsT1.x}, {burnParamsT1.y}, {burnParamsT1.z}] m/s = {burnParamsT1.magnitude} m/s");
        ////Logger.LogDebug($"Test: burnParamsT2          [{burnParamsT2.x}, {burnParamsT2.y}, {burnParamsT2.z}] m/s = {burnParamsT2.magnitude} m/s");
        //Logger.LogDebug($"Solution Found: _deltaV      [{_deltaV.x}, {_deltaV.y}, {_deltaV.z}] m/s = {_deltaV.magnitude} m/s {burnUT - _UT} s from _UT");
        //Logger.LogDebug($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - _UT} s from _UT");
        //CurrentNode.BurnVector = burnParams;
        //UpdateNode(CurrentNode);

        // Update the node
    }

    // Flight Plan API Methods
    public bool Circularize(double burnUT, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"Circularize {BurnTimeOption.TimeRefDesc}");
        //var startTimeOffset = 60;
        //var burnUT = _UT + startTimeOffset;
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(_orbit, burnUT);

        FPStatus.Ok($"Ready to Circularize {BurnTimeOption.TimeRefDesc}");

        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
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
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNewPe {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Pe");
        //var TimeToAp = orbit.TimeToAp;
        //double burnUT, _e;
        //_e = orbit.eccentricity;
        //if (_e < 1)
        //    burnUT = _UT + TimeToAp;
        //else
        //    burnUT = _UT + 30;

        FPStatus.Ok($"Ready to Change Pe {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: TargetPeR {newPe} m, currentPeR {_orbit.Periapsis} m, body.radius {_orbit.referenceBody.radius} m");
        // Debug.Log($"Seeking Solution: TargetPeR {TargetPeR} m, currentPeR {orbit.Periapsis} m, body.radius {orbit.ReferenceBody.radius} m");
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(_orbit, burnUT, newPe);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
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
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNewAp {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Ap");
        //var TimeToPe = orbit.TimeToPe;
        //var burnUT = _UT + TimeToPe;

        FPStatus.Ok($"Ready to Change Ap {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: TargetApR {newAp} m, currentApR {_orbit.Apoapsis} m");
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(_orbit, burnUT, newAp);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
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
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"Ellipticize: Set New Pe and Ap {BurnTimeOption.TimeRefDesc}");


        FPStatus.Ok($"Ready to Ellipticize {BurnTimeOption.TimeRefDesc}");

        if (newPe > newAp)
        {
            (newPe, newAp) = (newAp, newPe);
            FPStatus.Warning("Pe Setting > Ap Setting");
        }

        Logger.LogDebug($"Seeking Solution: TargetPeR {newPe} m, TargetApR {newAp} m, body.radius {_orbit.referenceBody.radius} m");
        // var burnUT = _UT + 30;
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(_orbit, burnUT, newPe, newAp);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error("Set New Pe and Ap: Solution Not Found !");
            return false;
        }
    }

    public bool SetInclination(double burnUT, double inclination, double burnOffsetFactor)
    {
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetInclination: Set New Inclination {inclination}° {BurnTimeOption.TimeRefDesc}");
        // double burnUT, TAN, TDN;
        Vector3d _deltaV;

        FPStatus.Ok($"Ready to Change Inclination {BurnTimeOption.TimeRefDesc}");

        _deltaV = OrbitalManeuverCalculator.DeltaVToChangeInclination(_orbit, burnUT, inclination);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error("Set New Inclination: Solution Not Found !");
            return false;
        }
    }
    
    public bool SetNewLAN(double burnUT, double newLANvalue, double burnOffsetFactor)
    {
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNewLAN: Set New LAN {newLANvalue}° {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = _UT + 30;

        if (Math.Abs(_orbit.inclination) < 10)
            FPStatus.Warning($"WARNING: Orbital plane has low inclination of {_orbit.inclination:N2}° (recommend i > 10°). Maneuver many not be accurate.");
        else
            FPStatus.Warning($"Experimental LAN Change {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: newLANvalue {newLANvalue}°");
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToShiftLAN(_orbit, burnUT, newLANvalue);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error("Set New LAN: Solution Not Found !");
            return false;
        }
    }

    public bool SetNodeLongitude(double burnUT, double newNodeLongValue, double burnOffsetFactor)
    {
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNodeLongitude: Set Node Longitude {newNodeLongValue}° {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = _UT + 30;

        FPStatus.Warning($"Experimental Node Longitude Change {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: newNodeLongValue {newNodeLongValue}°");
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToShiftNodeLongitude(_orbit, burnUT, newNodeLongValue);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error("Shift Node Longitude: Solution Not Found !");
            return false;
        }
    }

    public bool SetNewSMA(double burnUT, double newSMA, double burnOffsetFactor)
    {
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNewSMA {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = _UT + 30;

        FPStatus.Ok($"Ready to Change SMA Change {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: newSMA {newSMA} m");
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVForSemiMajorAxis(_orbit, burnUT, newSMA);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
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
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"MatchPlanes: Match Planes with {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        double burnUTout = _UT + 1;

        FPStatus.Ok($"Ready to Match Planes with {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");

        Vector3d _deltaV = Vector3d.zero;
        if (time_ref == TimeRef.REL_ASCENDING)
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(_orbit, _currentTarget.Orbit as PatchedConicsOrbit, _UT, out burnUTout);
        else if (time_ref == TimeRef.REL_DESCENDING)
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(_orbit, _currentTarget.Orbit as PatchedConicsOrbit, _UT, out burnUTout);
        else if (time_ref == TimeRef.REL_NEAREST_AD)
        {
            if (_orbit.TimeOfAscendingNode(_currentTarget.Orbit as PatchedConicsOrbit, _UT) < _orbit.TimeOfDescendingNode(_currentTarget.Orbit as PatchedConicsOrbit, _UT))
                _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(_orbit, _currentTarget.Orbit as PatchedConicsOrbit, _UT, out burnUTout);
            else
                _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(_orbit, _currentTarget.Orbit as PatchedConicsOrbit, _UT, out burnUTout);
        }
        else if (time_ref == TimeRef.REL_HIGHEST_AD)
        {
            var anTime = _orbit.TimeOfAscendingNode(_currentTarget.Orbit as PatchedConicsOrbit, _UT);
            var dnTime = _orbit.TimeOfDescendingNode(_currentTarget.Orbit as PatchedConicsOrbit, _UT);
            if (_orbit.Radius(anTime) > _orbit.Radius(dnTime))
                _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(_orbit, _currentTarget.Orbit as PatchedConicsOrbit, _UT, out burnUTout);
            else
                _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(_orbit, _currentTarget.Orbit as PatchedConicsOrbit, _UT, out burnUTout);
        }
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUTout, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error($"Match Planes with {_currentTarget.Name} at AN: Solution Not Found!");
            return false;
        }
    }

    public bool HohmannTransfer(double burnUT, double burnOffsetFactor)
    {
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"HohmannTransfer: Hohmann Transfer to {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        // Debug.Log("Hohmann Transfer");
        double _burnUTout;
        Vector3d _deltaV;

        FPStatus.Warning($"Ready to Transfer to {_currentTarget.Name}");

        bool _simpleTransfer = false;
        bool _intercept_only;
        if (_simpleTransfer)
        {
            double offsetDist = 0;
            if (_currentTarget.IsCelestialBody)
            {
                offsetDist = _currentTarget.CelestialBody.radius + FPSettings.InterceptDistanceCelestial * 1000;
                Logger.LogDebug($"HohmannTransfer: OffsetDist for celestial encounter {offsetDist/1000:N2} km");
            }
            else
            {
                offsetDist = FPSettings.InterceptDistanceVessel;
                Logger.LogDebug($"HohmannTransfer: OffsetDist for non-celestial encounter {offsetDist:N2} m");
            }
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(_orbit, _currentTarget.Orbit as PatchedConicsOrbit, _UT, out _burnUTout, offsetDist);
        }
        else
        {
            if (_currentTarget.IsCelestialBody)
            {
                _intercept_only = true;
            }
            else
            {
                _intercept_only = true;
            }
            //bool _anExists = _orbit.AscendingNodeExists(_currentTarget.Orbit as PatchedConicsOrbit);
            //bool _dnExists = _orbit.DescendingNodeExists(_currentTarget.Orbit as PatchedConicsOrbit);
            //double _anTime = _orbit.TimeOfAscendingNode(_currentTarget.Orbit as PatchedConicsOrbit, _UT);
            //double _dnTime = _orbit.TimeOfDescendingNode(_currentTarget.Orbit as PatchedConicsOrbit, _UT);
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(_orbit, _currentTarget.Orbit as PatchedConicsOrbit, _UT, out _burnUTout, intercept_only: _intercept_only);
        }

        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, _burnUTout, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error($"Hohmann Transfer to {_currentTarget.Name}: Solution Not Found !");
            return false;
        }
    }

    public bool InterceptTgt(double burnUT, double tgtUT, double burnOffsetFactor)
    {
        // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
        // Adapted from call found in MechJebModuleScriptActionRendezvous.cs for "Get Closer"
        // Similar to code in MechJebModuleRendezvousGuidance.cs for "Get Closer" Button code.

        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"InterceptTgt: Intercept {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        // var burnUT = _UT + 30;
        double _interceptUT = _UT + tgtUT;
        double _offsetDistance;
        Vector3d _deltaV;

        FPStatus.Warning($"Experimental Intercept of {_currentTarget.Name} Ready");

        Logger.LogDebug($"Seeking Solution: InterceptTime {FPSettings.InterceptTime} s");
        if (_currentTarget.IsCelestialBody) // For a target that is a celestial
            _offsetDistance = _currentTarget.Orbit.referenceBody.radius + 50000;
        else
            _offsetDistance = 100;
        (_deltaV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(_orbit, burnUT, _currentTarget.Orbit as PatchedConicsOrbit, _interceptUT, _offsetDistance);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error($"Intercept {_currentTarget.Name}: No Solution Found !");
            return false;
        }
    }

    public bool CourseCorrection(double burnUT, double interceptDistance, double burnOffsetFactor)
    {
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"CourseCorrection: Course Correction burn to improve trajectory to {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        double _burnUTout;
        Vector3d _deltaV;

        FPStatus.Ok("Course Correction Ready");

        if (_currentTarget.IsCelestialBody) // For a target that is a celestial
        {
            if (interceptDistance < 0)
                interceptDistance = _currentTarget.CelestialBody.radius + 50000; // m (PeR at celestial target)
            else
                interceptDistance += _currentTarget.CelestialBody.radius;
            Logger.LogDebug($"Seeking Solution for Celestial Target with Pe {interceptDistance}");
            // double _finalPeR = _currentTarget.CelestialBody.radius + 50000; // m (PeR at celestial target)
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(_orbit, _UT, _currentTarget.Orbit as PatchedConicsOrbit, _currentTarget.Orbit.referenceBody, interceptDistance, out _burnUTout);
        }
        else // For a tartget that is not a celestial
        {
            if (interceptDistance < 0)
                interceptDistance = 100; // m (PeR at celestial target)
            Logger.LogDebug($"Seeking Solution for Non-Celestial Target with closest approach {interceptDistance}");
            // double _caDistance = 100; // m (closest approach to non-celestial target)
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(_orbit, _UT, _currentTarget.Orbit as PatchedConicsOrbit, interceptDistance, out _burnUTout);
        }
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, _burnUTout, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error($"Course Correction for tragetory to {_currentTarget.Name}: No Solution Found !");
            return false;
        }
    }

    public bool MoonReturn(double burnUT, double targetMRPeR, double burnOffsetFactor)
    {
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;
        Vector3d _deltaV;

        Logger.LogDebug($"MoonReturn: Return from {_orbit.referenceBody.Name} {BurnTimeOption.TimeRefDesc}");
        var _e = _orbit.eccentricity;

        FPStatus.Warning($"Ready to Return from {_orbit.referenceBody.Name}?");

        if (_e > 0.2)
        {
            FPStatus.Error($"Moon Return: Starting Orbit Eccentrity Too Large {_e.ToString("F2")} is > 0.2");
            return false;
        }
        else
        {
            double _burnUTout;
            // double primaryRaidus = orbit.ReferenceBody.orbit.ReferenceBody.radius + 100000; // m
            Logger.LogDebug($"Moon Return Attempting to Solve...");
            (_deltaV, _burnUTout) = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(_orbit, _UT, targetMRPeR);
            if (_deltaV != Vector3d.zero)
            {
                StartCoroutine(CreateManeuverNode(_deltaV, _burnUTout, burnOffsetFactor));
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
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"MatchVelocity: Match Velocity with {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");

        FPStatus.Ok($"Ready to Match Velocity with {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");

        // double closestApproachTime = orbit.NextClosestApproachTime(_currentTarget.orbit as PatchedConicsOrbit, _UT + 2); //+2 so that closestApproachTime is definitely > _UT
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(_orbit, burnUT, _currentTarget.Orbit as PatchedConicsOrbit);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, burnUT, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error($"Match Velocity with {_currentTarget.Name} at Closest Approach: No Solution Found!");
            return false;
        }
    }

    public bool PlanetaryXfer(double burnUT, double burnOffsetFactor)
    {
        double _UT = Game.UniverseModel.UniversalTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"PlanetaryXfer: Transfer to {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        double _burnUTout, _burnUTout2;
        bool _syncPhaseAngle = true;

        FPStatus.Warning($"Experimental Transfer to {_currentTarget.Name} Ready");

        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(_orbit, _UT, _currentTarget.Orbit as PatchedConicsOrbit, _syncPhaseAngle, out _burnUTout);
        // Vector3d _deltaV2 = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryLambertTransferEjection(_orbit, _UT, _currentTarget.orbit as PatchedConicsOrbit, out _burnUTout2);
        if (_deltaV != Vector3d.zero)
        {
            StartCoroutine(CreateManeuverNode(_deltaV, _burnUTout, burnOffsetFactor));
            return true;
        }
        else
        {
            FPStatus.Error($"Planetary Transfer to {_currentTarget.Name}: No Solution Found!");
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
        //    Game.SpaceSimulation.Maneuvers.GetNodesForVessel(ActiveVessel.SimulationObject.GlobalId);

        Logger.LogDebug($"TestPerturbedOrbit: patchList.Count = {NodeManagerPlugin.Instance.Nodes.Count}");

        if (NodeManagerPlugin.Instance.Nodes.Count == 0)
        {
            Logger.LogDebug($"TestPerturbedOrbit: No future patches to compare to.");
        }
        else
        {
            PatchedConicsOrbit hypotheticalOrbit = o.PerturbedOrbit(burnUT, dV);
            ManeuverPlanSolver maneuverPlanSolver = _activeVessel.Orbiter?.ManeuverPlanSolver;
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
