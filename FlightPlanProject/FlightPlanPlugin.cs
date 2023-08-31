using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FPUtilities;
using HarmonyLib;
using KSP.Game;
using KSP.Messages;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.UI.Binding;
using MuMech;
using NodeManager;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Game.Messages;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI.Appbar;
using System.Collections;
using System.Reflection;
using UitkForKsp2;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;


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
    REL_HIGHEST_AD,
    LIMITED_TIME,
    PORKCHOP,
    NEXT_WINDOW,
    ASAP
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
[BepInDependency(UitkForKsp2Plugin.ModGuid, UitkForKsp2Plugin.ModVer)]
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
    public static bool InterfaceEnabled = false;
    // private bool _GUIenabled = true;
    // private Rect _windowRect = Rect.zero;
    // public int windowWidth = 250; //384px on 1920x1080

    private ConfigEntry<KeyboardShortcut> _keybind;
    private ConfigEntry<KeyboardShortcut> _keybind2;

    // public FlightPlanUI MainUI;

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

    // private GameInstance game;

    FpUiController controller;

    // App bar Button(s)
    public const string _ToolbarFlightButtonID = "BTN-FlightPlanFlight";
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

    public static MessageCenter MessageCenter;

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        // KBaseSettings.Init(_SettingsPath);
        // MainUI = new FlightPlanUI(this);

        Instance = this;

        // game = GameManager.Instance.Game;
        Logger = base.Logger;

        // Load UITK GUI
        var fpUxml = AssetManager.GetAsset<VisualTreeAsset>($"{Info.Metadata.GUID}/fp_ui/fp_ui.uxml");
        var fpWindow = Window.CreateFromUxml(fpUxml, "Flight Plan Main Window", transform, true);
        fpWindow.hideFlags |= HideFlags.HideAndDontSave;

        // Initialze an instance of the UITK GUI controller
        controller = fpWindow.gameObject.AddComponent<FpUiController>();

        // Setup keybindings with default values
        _keybind = Config.Bind(
        new ConfigDefinition("Keybindings", "First Keybind"),
        new KeyboardShortcut(KeyCode.P, KeyCode.LeftAlt),
        new ConfigDescription("Keybind to open mod window")
        );

        _keybind2 = Config.Bind(
        new ConfigDefinition("Keybindings", "Second Keybind"),
        new KeyboardShortcut(KeyCode.P, KeyCode.RightAlt, KeyCode.AltGr),
        new ConfigDescription("Keybind to open mod window")
        );

        // Subscribe to messages that indicate it's OK to raise the GUI
        //StateChanges.FlightViewEntered += message =>
        //{
        //  FpUiController.GUIenabled = true;
        //  Logger.LogInfo($"FlightViewEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        StateChanges.Map3DViewEntered += message =>
        {
            FpUiController.GUIenabled = true;
            Logger.LogDebug($"Map3DViewEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        };

        //StateChanges.GameStateChanged += (message, previousState, newState) => {
        //  FpUiController._GUIenabled = newState == GameState.FlightView || newState == GameState.Map3DView;
        //};



        // Subscribe to messages that indicate it's not OK to raise the GUI
        //StateChanges.FlightViewLeft += message =>
        //{
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"FlightViewLeft message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        StateChanges.Map3DViewLeft += message =>
        {
            FpUiController.GUIenabled = false;
            Logger.LogDebug($"Map3DViewLeft message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        };
        //StateChanges.VehicleAssemblyBuilderEntered += message =>
        //{
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"VehicleAssemblyBuilderEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.KerbalSpaceCenterStateEntered += message =>
        //{
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"KerbalSpaceCenterStateEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.BaseAssemblyEditorEntered += message =>
        //{
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"BaseAssemblyEditorEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.MainMenuStateEntered += message =>
        //{ 
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"MainMenuStateEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.ColonyViewEntered += message =>
        //{ 
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"ColonyViewEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.TrainingCenterEntered += message =>
        //{ 
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"TrainingCenterEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //statechanges.trainingcenterloaded += message =>
        //{
        //  fpuicontroller.guienabled = false;
        //  logger.loginfo($"trainingcenterloaded message received, fpuicontroller.guienabled = {fpuicontroller.guienabled}");
        //};
        //StateChanges.MissionControlEntered += message =>
        //{ 
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"MissionControlEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.TrackingStationEntered += message =>
        //{ 
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"TrackingStationEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.TrackingStationLoadedMessage += message =>
        //{
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"TrackingStationLoadedMessage message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.ResearchAndDevelopmentEntered += message =>
        //{ 
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"ResearchAndDevelopmentEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.LaunchpadEntered += message =>
        //{ 
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"LaunchpadEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};
        //StateChanges.RunwayEntered += message =>
        //{ 
        //  FpUiController.GUIenabled = false;
        //  Logger.LogInfo($"RunwayEntered message received, FpUiController.GUIenabled = {FpUiController.GUIenabled}");
        //};

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
            AssetManager.GetAsset<Texture2D>($"{Info.Metadata.GUID}/images/Icon.png"),
            ToggleButton);

        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(FlightPlanPlugin).Assembly);

        // Fetch a configuration value or create a default one if it does not exist

        FPStatus.Init(this);

        _experimental = Config.Bind<bool>("Experimental Section", "Experimental Features", false, "Enable/Disable _experimental.Value features for testing - Warrantee Void if Enabled!");
        _autoLaunchMNC = Config.Bind<bool>("Experimental Section", "Launch Maneuver Node Controller", false, "Enable/Disable automatically launching the Maneuver Node Controller GUI (if installed) when _experimental.Value nodes are created");
        _smallError = Config.Bind<double>("Status Reporting Section", "Small % Error Threashold", 1, "Percent error threshold used to assess quality of maneuver node goal for warning (yellow) status");
        _largeError = Config.Bind<double>("Status Reporting Section", "Large % Error Threashold", 2, "Percent error threshold used to assess quality of maneuver node goal for error (red) status");

        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        Logger.LogInfo($"Experimental Features: {_experimental.Value}");

        RefreshGameManager();

        // Subscribe to the GameStateEnteredMessage so we can control if the GUI should be displaid upon entering this state
        MessageCenter.Subscribe<GameStateEnteredMessage>(new Action<MessageCenterMessage>(this.GameStateEntered));

        Logger.LogInfo("GameStateEnteredMessage message subscribed");

        // Subscribe to the GameStateLeftMessage so we can control if the GUI should be disabled upon leaving this state
        MessageCenter.Subscribe<GameStateLeftMessage>(new Action<MessageCenterMessage>(this.GameStateLeft));

        Logger.LogInfo("GameStateLeftMessage message subscribed");

        // Subscribe to the GameStateChangedMessage so we can control if the GUI should be disabled given this state change
        MessageCenter.Subscribe<GameStateChangedMessage>(new Action<MessageCenterMessage>(this.GameStateChanged));

        Logger.LogInfo("GameStateChangedMessage message subscribed");

        // Subscribe to the TrainingCenterLoadedMessage so we can control if the GUI should be disabled given this state change
        MessageCenter.Subscribe<TrainingCenterLoadedMessage>(new Action<MessageCenterMessage>(this.TrainingCenterLoaded));

        Logger.LogInfo("TrainingCenterLoadedMessage message subscribed");

        // Subscribe to the TrackingStationLoadedMessage so we can control if the GUI should be disabled given this state change
        MessageCenter.Subscribe<TrackingStationLoadedAudioCueMessage>(new Action<MessageCenterMessage>(this.TrackingStationLoaded));

        Logger.LogInfo("TrackingStationLoadedAudioCueMessage message subscribed");

    }

    public static GameStateConfiguration ThisGameState;
    public static GameState LastGameState;
    public static CurtainContext ThisCurtainContext;

    public static void RefreshGameManager()
    {
        ThisGameState = GameManager.Instance?.Game?.GlobalGameState?.GetGameState();
        LastGameState = (GameState)(GameManager.Instance?.Game?.GlobalGameState?.GetLastState());
        MessageCenter = GameManager.Instance?.Game?.Messages;
        ThisCurtainContext = (CurtainContext)(GameManager.Instance?.Game?.UI.Curtain.CurtainContextData.CurtainContext);
        Logger.LogDebug($"RefreshGameManager ThisCurtainContext = {ThisCurtainContext}");

        // Log out every type of message in the game...
        //foreach (var type in typeof(GameManager).Assembly.GetTypes())
        //{
        //  if (typeof(MessageCenterMessage).IsAssignableFrom(type) && !type.IsAbstract)
        //  {
        //    Logger.LogInfo(type.Name);
        //  }
        //}
    }

    private void GameStateChanged(MessageCenterMessage message)
    {
        RefreshGameManager();
        Logger.LogDebug($"GameStateChanged Message Recived. GameState: {LastGameState}  ->  {ThisGameState.GameState}");
        if (ThisGameState.GameState == GameState.FlightView || ThisGameState.GameState == GameState.Map3DView)
        {
            FpUiController.GUIenabled = true;
        }
        else
        {
            FpUiController.GUIenabled = false;
            FpUiController.container.style.display = DisplayStyle.None;
        }
        Logger.LogDebug($"GameStateChanged FpUiController.GUIenabled = {FpUiController.GUIenabled}");
    }

    private void GameStateEntered(MessageCenterMessage message)
    {
        RefreshGameManager();
        Logger.LogDebug($"GameStateEntered Message Recived. GameState: {LastGameState}  ->  {ThisGameState.GameState}");
        if (ThisGameState.GameState == GameState.FlightView || ThisGameState.GameState == GameState.Map3DView)
        {
            FpUiController.GUIenabled = true;
        }
        else
        {
            FpUiController.GUIenabled = false;
            FpUiController.container.style.display = DisplayStyle.None;
        }
        Logger.LogDebug($"GameStateEntered FpUiController.GUIenabled = {FpUiController.GUIenabled}");
    }

    private void GameStateLeft(MessageCenterMessage message)
    {
        RefreshGameManager();
        Logger.LogDebug($"GameStateLeft Message Recived. GameState: {ThisGameState.GameState}");

        // if (ThisGameState.GameState == GameState.FlightView || ThisGameState.GameState == GameState.Map3DView)
        FpUiController.GUIenabled = false;
        FpUiController.container.style.display = DisplayStyle.None;

        Logger.LogDebug($"GameStateLeft FpUiController.GUIenabled = {FpUiController.GUIenabled}");
    }

    private void TrackingStationLoaded(MessageCenterMessage message)
    {
        RefreshGameManager();
        Logger.LogDebug($"TrackingStationLoadedAudioCue Message Recived. GameState: {LastGameState}  ->  {ThisGameState.GameState}");

        // if (ThisGameState.GameState == GameState.FlightView || ThisGameState.GameState == GameState.Map3DView)
        FpUiController.GUIenabled = false;
        FpUiController.container.style.display = DisplayStyle.None;

        Logger.LogDebug($"TrackingStationLoadedAudioCue FpUiController.GUIenabled = {FpUiController.GUIenabled}");
    }

    private void TrainingCenterLoaded(MessageCenterMessage message)
    {
        RefreshGameManager();
        Logger.LogDebug($"TrainingCenterLoaded Message Recived. GameState: {LastGameState}  ->  {ThisGameState.GameState}");

        // if (ThisGameState.GameState == GameState.FlightView || ThisGameState.GameState == GameState.Map3DView)
        FpUiController.GUIenabled = false;
        FpUiController.container.style.display = DisplayStyle.None;

        Logger.LogDebug($"TrainingCenterLoaded FpUiController.GUIenabled = {FpUiController.GUIenabled}");
    }


    public void ToggleButton(bool toggle)
    {
        InterfaceEnabled = toggle;
        GameObject.Find(_ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(InterfaceEnabled);
        controller.SetEnabled(toggle);
    }

    void Awake()
    {

    }

    void Update()
    {
        if ((_keybind != null && _keybind.Value.IsDown()) || (_keybind2 != null && _keybind2.Value.IsDown()))
        {
            ToggleButton(!InterfaceEnabled);
            if (_keybind != null && _keybind.Value.IsDown())
                Logger.LogDebug($"Update: UI toggled with _keybind, hotkey {_keybind.Value}");
            if (_keybind2 != null && _keybind2.Value.IsDown())
                Logger.LogDebug($"Update: UI toggled with _keybind2, hotkey {_keybind2.Value}");
        }
        //if (_keybind != null && _keybind.Value.IsDown())
        //{
        //  ToggleButton(!_interfaceEnabled);
        //  Logger.LogInfo($"Update: UI toggled with _keybind, hotkey {_keybind.Value}");
        //}
        //if (_keybind2 != null && _keybind2.Value.IsDown())
        //{
        //  ToggleButton(!_interfaceEnabled);
        //  Logger.LogInfo($"Update: UI toggled with _keybind2, hotkey {_keybind2.Value}");
        //}
        //if (MainUI != null)
        //  MainUI.Update();
    }

    //void save_rect_pos()
    //{
    //  KBaseSettings.WindowXPos = (int)_windowRect.xMin;
    //  KBaseSettings.WindowYPos = (int)_windowRect.yMin;
    //}

    /// <summary>
    /// Draws a simple UI window when <code>this._isWindowOpen</code> is set to <code>true</code>.
    /// </summary>
    private void OnGUI()
    {
        //_GUIenabled = false;
        //var _gameState = Game?.GlobalGameState?.GetState();
        //if (_gameState == GameState.Map3DView) _GUIenabled = true;
        //if (_gameState == GameState.FlightView) _GUIenabled = true;
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

        //// Set the UI
        //if (_interfaceEnabled && _GUIenabled && _activeVessel != null)
        //{
        //  FPStyles.Init();
        //  WindowTool.CheckMainWindowPos(ref _windowRect);
        //  GUI.skin = KBaseStyle.Skin;

        //  _windowRect = GUILayout.Window(
        //      GUIUtility.GetControlID(FocusType.Passive),
        //      _windowRect,
        //      FillWindow,
        //      "<color=#696DFF>FLIGHT PLAN</color>",
        //      GUILayout.Height(0),
        //      GUILayout.Width(windowWidth));

        //  save_rect_pos();
        //  // Draw the tool tip if needed
        //  ToolTipsManager.DrawToolTips();

        //  // check editor focus and unset Input if needed
        //  UI_Fields.CheckEditor();
        //}
    }

    private ManeuverNodeData GetCurrentNode()
    {
        ActiveNodes = Game.SpaceSimulation.Maneuvers.GetNodesForVessel(Game.ViewController.GetActiveVehicle(true).Guid);
        return (ActiveNodes.Count() > 0) ? ActiveNodes[0] : null;
    }

    /// <summary>
    /// Defines the content of the UI window drawn in the <code>OnGui</code> method.
    /// </summary>
    /// <param name="windowID"></param>
    //private void FillWindow(int windowID)
    //{
    //  TopButtons.Init(_windowRect.width);

    //  // Place the Plugin's main Icon in the upper left
    //  GUI.Label(new Rect(9, 2, 29, 29), KBaseStyle.Icon, KBaseStyle.IconsLabel);

    //  // Place a close window Icon in the upper right
    //  if (TopButtons.Button(KBaseStyle.Cross))
    //    CloseWindow();

    //  _currentNode = GetCurrentNode();

    //  MainUI.OnGUI();

    //  GUI.DragWindow(new Rect(0, 0, 10000, 500));
    //}

    // This method sould be called at the top of FillWindow to enable Toggle buttons to work like radio buttons

    private void CloseWindow()
    {
        GameObject.Find(_ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
        InterfaceEnabled = false;
        ToggleButton(InterfaceEnabled);

        // UI_Fields.GameInputState = true;
    }

    private bool CreateManeuverNodeCaller(Vector3d deltaV, double burnUT, double burnOffsetFactor = -0.5)
    {
        bool makeNode = true;
        if (NodeManagerPlugin.Instance.Nodes.Count > 0)
        { 
            for (int i = 0; i <  NodeManagerPlugin.Instance.Nodes.Count; i++)
            {
                double deltaT = NodeManagerPlugin.Instance.Nodes[i].Time - burnUT;
                if (Math.Abs(deltaT) < 30)
                {
                    makeNode = false;
                    FPStatus.Error($"Requested node {deltaT:N3}s from Node {i+1}. Aborting node creation.");
                }
            }
        }
        if (makeNode)
            StartCoroutine(CreateManeuverNode(deltaV, burnUT, burnOffsetFactor));
        //CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
        return makeNode;
    }
    private IEnumerator CreateManeuverNode(Vector3d deltaV, double burnUT, double burnOffsetFactor = -0.5) // IEnumerator
    {
        Vector3d burnParams;
        double UT = Game.UniverseModel.UniverseTime;
        var orbit = _activeVessel.Orbit;
        var Orbiter = _activeVessel.Orbiter;
        var ManeuverPlanSolver = Orbiter?.ManeuverPlanSolver;

        // Get the local coordinate burnParams based on the burnUT and deltaV
        burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(ActiveVessel.orbit, _deltaV, burnUT);
        Logger.LogInfo($"CreateManeuverNode: Solution Found: _deltaV      [{deltaV.x:F3}, {deltaV.y:F3}, {deltaV.z:F3}] m/s = {deltaV.magnitude:F3} m/s {FPUtility.SecondsToTimeString(burnUT - UT)} from now");
        Logger.LogInfo($"CreateManeuverNode: Solution Found: burnParams  [{burnParams.x:F3}, {burnParams.y:F3}, {burnParams.z:F3}] m/s  = {burnParams.magnitude:F3} m/s {FPUtility.SecondsToTimeString(burnUT - UT)} from now");

        // Create a node with the burnParams at burnUT - No burnOffsetFactor applied... yet.
        NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, 0); // burnOffsetFactor
        _currentNode = NodeManagerPlugin.Instance.currentNode;

        // Wait for things to settle
        yield return (object)new WaitForSeconds(0.1f);  // WaitForFixedUpdate();

        // Adjust the time on the node so it will start earlier (or later? when would you ever want later?)
        if (burnOffsetFactor != 0)
        {
            // Calculate the time needed for the adjustment
            double nodeTimeAdj = _currentNode.BurnDuration * burnOffsetFactor;
            Logger.LogInfo($"CreateManeuverNode:         Node.Time = {FPUtility.SecondsToTimeString(_currentNode.Time - UT)} from now");
            Logger.LogInfo($"CreateManeuverNode:      BurnDuration = {FPUtility.SecondsToTimeString(_currentNode.BurnDuration)}");
            Logger.LogInfo($"CreateManeuverNode:       nodeTimeAdj = {FPUtility.SecondsToTimeString(nodeTimeAdj)}");

            // Apply the time adjust
            ManeuverPlanComponent maneuverPlanComponent = (_activeVessel?.SimulationObject)?.FindComponent<ManeuverPlanComponent>();
            maneuverPlanComponent.UpdateTimeOnNode(_currentNode.NodeID, _currentNode.Time += nodeTimeAdj);
            Logger.LogInfo($"CreateManeuverNode: Updated Node.Time = {FPUtility.SecondsToTimeString(_currentNode.Time - UT)} from now");

            // Convert the new burnVector to deltaV (at this point _currentNode.Time should have been updated to be at the new time, right?)
            Vector3d newDeltaV = orbit.BurnVecToDv(_currentNode.Time, _currentNode.BurnVector);
            Logger.LogInfo($"CreateManeuverNode: newDeltaV      [{newDeltaV.x:F3}, {newDeltaV.y:F3}, {newDeltaV.z:F3}] m/s  = {newDeltaV.magnitude:F3} m/s");
            // Compute the change needed for this newDeltaV to equal the original
            Vector3d deltaDeltaV = deltaV - newDeltaV;
            Logger.LogInfo($"CreateManeuverNode: deltaDeltaV    [{deltaDeltaV.x:F3}, {deltaDeltaV.y:F3}, {deltaDeltaV.z:F3}] m/s  = {deltaDeltaV.magnitude:F3} m/s");
            // Convert this to a burnVector
            Vector3d newBurnParams = orbit.DeltaVToManeuverNodeCoordinates(_currentNode.Time, deltaDeltaV);
            Logger.LogInfo($"CreateManeuverNode: newBurnParams  [{newBurnParams.x:F3}, {newBurnParams.y:F3}, {newBurnParams.z:F3}] m/s  = {newBurnParams.magnitude:F3} m/s");
            maneuverPlanComponent.UpdateChangeOnNode(_currentNode.NodeID, newBurnParams);
            Logger.LogInfo($"CreateManeuverNode: BurnVector     [{_currentNode.BurnVector.x:F3}, {_currentNode.BurnVector.y:F3}, {_currentNode.BurnVector.z:F3}] m/s  = {_currentNode.BurnVector.magnitude:F3} m/s");

            maneuverPlanComponent.UpdateNodeDetails(_currentNode);
        }


        //IPatchedOrbit targetOrbit = _currentNode.IsOnManeuverTrajectory ? (IPatchedOrbit)_currentNode.ManeuverTrajectoryPatch : (IPatchedOrbit)_activeVessel.SimulationObject.Vessel.Orbiter.PatchedConicSolver.FindPatchContainingUT(_currentNode.Time);
        //Logger.LogInfo($"CreateManeuverNode: IsOnManeuverTrajectory = {_currentNode.IsOnManeuverTrajectory}");
        //Logger.LogInfo($"CreateManeuverNode: targetOrbit = {targetOrbit}");

        //Logger.LogInfo($"CreateManeuverNode: targetOrbit.ReferenceFrame.transform.Guid = {targetOrbit.ReferenceFrame.transform.Guid}");
        //Logger.LogInfo($"CreateManeuverNode: this.SimulationObject.Vessel.Orbit.ReferenceFrame.transform.Guid = {_activeVessel.SimulationObject.Vessel.Orbit.ReferenceFrame.transform.Guid}");
        //Vector3d newLocalPosition;
        //Vector3d normalized;
        //if (targetOrbit.ReferenceFrame.transform.Guid != _activeVessel.SimulationObject.Vessel.Orbit.ReferenceFrame.transform.Guid)
        //{
        //  ITransformModel viewerTransform = OrbitRenderer.GetViewerTransform(targetOrbit.referenceBody.transform, _activeVessel.SimulationObject.Vessel.Orbit.referenceBody.SimulationObject.transform);
        //  _currentNode.SimTransform.SetParent(viewerTransform.celestialFrame);
        //  RelativeOrbitSolver relativeOrbitSolver = new RelativeOrbitSolver(viewerTransform, (IOrbit)targetOrbit, (ISimulationModelMap)Game.UniverseModel);
        //  newLocalPosition = relativeOrbitSolver.GetOrbitPositionTargetToViewerAtUt(_currentNode.Time);
        //  normalized = relativeOrbitSolver.GetOrbitVelocityTargetToViewerAtUt(_currentNode.Time).normalized;
        //}
        //else
        //{
        //  _currentNode.SimTransform.SetParent(_activeVessel.SimulationObject.Vessel.Orbit.referenceBody.SimulationObject.transform.celestialFrame);
        //  Vector3d vector3d = targetOrbit.GetRelativePositionAtUTZup(_currentNode.Time);
        //  newLocalPosition = vector3d.SwapYAndZ;
        //  vector3d = targetOrbit.GetOrbitalVelocityAtUTZup(_currentNode.Time);
        //  vector3d = vector3d.SwapYAndZ;
        //  normalized = vector3d.normalized;
        //}
        //_currentNode.SimTransform.SetLocalPosition(newLocalPosition);
        //Vector3d up = Vector3d.Cross(-newLocalPosition.normalized, normalized);
        //_currentNode.SimTransform.SetLocalRotation(QuaternionD.LookRotation(normalized, up));

        //Logger.LogInfo($"CreateManeuverNode: IsOnManeuverTrajectory = {_currentNode.IsOnManeuverTrajectory}");
        //Logger.LogInfo($"CreateManeuverNode: IsOnManeuverTrajectory = {_currentNode.IsOnManeuverTrajectory}");

        // Having this here can sometimes result in a weird double node.
        // ManeuverPlanSolver.UpdateManeuverTrajectory();

        // FpUiController.Instance.CheckNodeQuality();
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
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"Circularize {BurnTimeOption.TimeRefDesc}");
        //var startTimeOffset = 60;
        //var burnUT = _UT + startTimeOffset;
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(_orbit, burnUT);

        FPStatus.Ok($"Ready to Circularize {BurnTimeOption.TimeRefDesc}");

        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error("Circularize Now: Solution Not Found!");
            return false;
        }
    }

    public bool SetNewPe(double burnUT, double newPe, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNewPe {BurnTimeOption.TimeRefDesc}");
        // FlightPlanPlugin.Logger.LogDebug("Set New Pe");
        //var TimeToAp = orbit.TimeToAp;
        //double burnUT, _e;
        //_e = orbit.eccentricity;
        //if (_e < 1)
        //    burnUT = _UT + TimeToAp;
        //else
        //    burnUT = _UT + 30;

        FPStatus.Ok($"Ready to Change Pe {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: TargetPeR_km {newPe} m, currentPeR {_orbit.Periapsis} m, body.radius {_orbit.referenceBody.radius} m");
        // FlightPlanPlugin.Logger.LogDebug($"Seeking Solution: TargetPeR_km {TargetPeR_km} m, currentPeR {orbit.Periapsis} m, body.radius {orbit.ReferenceBody.radius} m");
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(_orbit, burnUT, newPe);
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error("Set New Pe: Solution Not Found!");
            return false;
        }
    }

    public bool SetNewAp(double burnUT, double newAp, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNewAp {BurnTimeOption.TimeRefDesc}");
        // FlightPlanPlugin.Logger.LogDebug("Set New Ap");
        //var TimeToPe = orbit.TimeToPe;
        //var burnUT = _UT + TimeToPe;

        FPStatus.Ok($"Ready to Change Ap {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: TargetApR_km {newAp} m, currentApR {_orbit.Apoapsis} m");
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(_orbit, burnUT, newAp);
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error("Set New Ap: Solution Not Found!");
            return false;
        }
    }

    public bool Ellipticize(double burnUT, double newAp, double newPe, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"Ellipticize: Set New Pe and Ap {BurnTimeOption.TimeRefDesc}");


        FPStatus.Ok($"Ready to Ellipticize {BurnTimeOption.TimeRefDesc}");

        if (newPe > newAp)
        {
            (newPe, newAp) = (newAp, newPe);
            FPStatus.Warning("Pe Setting > Ap Setting");
        }

        Logger.LogDebug($"Seeking Solution: TargetPeR_km {newPe} m, TargetApR_km {newAp} m, body.radius {_orbit.referenceBody.radius} m");
        // var burnUT = _UT + 30;
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(_orbit, burnUT, newPe, newAp);
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error("Set New Pe and Ap: Solution Not Found !");
            return false;
        }
    }

    public bool SetInclination(double burnUT, double inclination, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetInclination: Set New Inclination {inclination}° {BurnTimeOption.TimeRefDesc}");
        // double burnUT, TAN, TDN;
        Vector3d _deltaV;

        FPStatus.Ok($"Ready to Change Inclination {BurnTimeOption.TimeRefDesc}");

        _deltaV = OrbitalManeuverCalculator.DeltaVToChangeInclination(_orbit, burnUT, inclination);
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error("Set New Inclination: Solution Not Found !");
            return false;
        }
    }

    public bool SetNewLAN(double burnUT, double newLANvalue, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNewLAN: Set New LAN {newLANvalue}° {BurnTimeOption.TimeRefDesc}");
        // FlightPlanPlugin.Logger.LogDebug("Set New Ap");
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
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error("Set New LAN: Solution Not Found !");
            return false;
        }
    }

    public bool SetNodeLongitude(double burnUT, double newNodeLongValue, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNodeLongitude: Set Node Longitude {newNodeLongValue}° {BurnTimeOption.TimeRefDesc}");
        // FlightPlanPlugin.Logger.LogDebug("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = _UT + 30;

        FPStatus.Warning($"Experimental Node Longitude Change {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: newNodeLongValue {newNodeLongValue}°");
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToShiftNodeLongitude(_orbit, burnUT, newNodeLongValue);
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error("Shift Node Longitude: Solution Not Found !");
            return false;
        }
    }

    public bool SetNewSMA(double burnUT, double newSMA, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        Logger.LogDebug($"SetNewSMA {BurnTimeOption.TimeRefDesc}");
        // FlightPlanPlugin.Logger.LogDebug("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = _UT + 30;

        FPStatus.Ok($"Ready to Change SMA Change {BurnTimeOption.TimeRefDesc}");

        Logger.LogDebug($"Seeking Solution: newSMA {newSMA} m");
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVForSemiMajorAxis(_orbit, burnUT, newSMA);
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error("Set New SMA: Solution Not Found!");
            return false;
        }
    }

    // No longer takes double burnUT. Need to sort out how this can be called as an API method
    public bool MatchPlanes(TimeRef time_ref, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        PatchedConicsOrbit tgtOrbit = null;
        if (_currentTarget.IsPart)
        {
            tgtOrbit = _currentTarget.Part.PartOwner.SimulationObject.Orbit as PatchedConicsOrbit;
        }
        else if (_currentTarget.IsVessel || _currentTarget.IsCelestialBody)
        {
            tgtOrbit = _currentTarget.Orbit as PatchedConicsOrbit;
        }

        Logger.LogDebug($"MatchPlanes: Match Planes with {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        double burnUTout = _UT + 1;

        FPStatus.Ok($"Ready to Match Planes with {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");

        Vector3d _deltaV = Vector3d.zero;
        if (time_ref == TimeRef.REL_ASCENDING)
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(_orbit, tgtOrbit, _UT, out burnUTout);
        else if (time_ref == TimeRef.REL_DESCENDING)
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(_orbit, tgtOrbit, _UT, out burnUTout);
        else if (time_ref == TimeRef.REL_NEAREST_AD)
        {
            if (_orbit.TimeOfAscendingNode(tgtOrbit, _UT) < _orbit.TimeOfDescendingNode(tgtOrbit, _UT))
                _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(_orbit, tgtOrbit, _UT, out burnUTout);
            else
                _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(_orbit, tgtOrbit, _UT, out burnUTout);
        }
        else if (time_ref == TimeRef.REL_HIGHEST_AD)
        {
            var anTime = _orbit.TimeOfAscendingNode(tgtOrbit, _UT);
            var dnTime = _orbit.TimeOfDescendingNode(tgtOrbit, _UT);
            if (_orbit.Radius(anTime) > _orbit.Radius(dnTime))
                _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(_orbit, tgtOrbit, _UT, out burnUTout);
            else
                _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(_orbit, tgtOrbit, _UT, out burnUTout);
        }
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUTout, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error($"Match Planes with {_currentTarget.Name} at AN: Solution Not Found!");
            return false;
        }
    }

    public bool HohmannTransfer(double burnUT, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        PatchedConicsOrbit tgtOrbit = null;
        if (_currentTarget.IsPart)
        {
            tgtOrbit = _currentTarget.Part.PartOwner.SimulationObject.Orbit as PatchedConicsOrbit;
        }
        else if (_currentTarget.IsVessel || _currentTarget.IsCelestialBody)
        {
            tgtOrbit = _currentTarget.Orbit as PatchedConicsOrbit;
        }

        Logger.LogDebug($"HohmannTransfer: Hohmann Transfer to {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        // FlightPlanPlugin.Logger.LogDebug("Hohmann Transfer");
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
                offsetDist = _currentTarget.CelestialBody.radius + FpUiController.TargetInterceptDistanceCelestial_m;
                Logger.LogDebug($"HohmannTransfer: OffsetDist for celestial encounter {offsetDist / 1000:N2} km");
            }
            else
            {
                offsetDist = FpUiController.TargetInterceptDistanceVessel_m;
                Logger.LogDebug($"HohmannTransfer: OffsetDist for non-celestial encounter {offsetDist:N2} m");
            }
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(_orbit, tgtOrbit, _UT, out _burnUTout, offsetDist);
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
            //bool _anExists = _orbit.AscendingNodeExists(tgtOrbit);
            //bool _dnExists = _orbit.DescendingNodeExists(tgtOrbit);
            //double _anTime = _orbit.TimeOfAscendingNode(tgtOrbit, _UT);
            //double _dnTime = _orbit.TimeOfDescendingNode(tgtOrbit, _UT);
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(_orbit, tgtOrbit, _UT, out _burnUTout, intercept_only: _intercept_only);
        }

        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, _burnUTout, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error($"Hohmann Transfer to {_currentTarget.Name}: Solution Not Found !");
            return false;
        }
    }

    public bool InterceptTgt(double burnUT, double tgtUT, double burnOffsetFactor = -0.5)
    {
        // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
        // Adapted from call found in MechJebModuleScriptActionRendezvous.cs for "Get Closer"
        // Similar to code in MechJebModuleRendezvousGuidance.cs for "Get Closer" Button code.

        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        PatchedConicsOrbit tgtOrbit = null;
        if (_currentTarget.IsPart)
        {
            tgtOrbit = _currentTarget.Part.PartOwner.SimulationObject.Orbit as PatchedConicsOrbit;
        }
        else if (_currentTarget.IsVessel || _currentTarget.IsCelestialBody)
        {
            tgtOrbit = _currentTarget.Orbit as PatchedConicsOrbit;
        }

        Logger.LogDebug($"InterceptTgt: Intercept {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        // var burnUT = _UT + 30;
        double _interceptUT = _UT + tgtUT;
        double _offsetDistance;
        Vector3d _deltaV;

        FPStatus.Warning($"Experimental Intercept of {_currentTarget.Name} Ready");

        Logger.LogDebug($"Seeking Solution: InterceptTime {FpUiController.TargetInterceptTime_s} s");
        if (_currentTarget.IsCelestialBody) // For a target that is a celestial
            _offsetDistance = _currentTarget.Orbit.referenceBody.radius + 50000;
        else
            _offsetDistance = 100;
        (_deltaV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(_orbit, burnUT, tgtOrbit, _interceptUT, _offsetDistance);
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error($"Intercept {_currentTarget.Name}: No Solution Found !");
            return false;
        }
    }

    public bool CourseCorrection(double burnUT, double interceptDistance, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        PatchedConicsOrbit tgtOrbit = null;
        if (_currentTarget.IsPart)
        {
            tgtOrbit = _currentTarget.Part.PartOwner.SimulationObject.Orbit as PatchedConicsOrbit;
        }
        else if (_currentTarget.IsVessel || _currentTarget.IsCelestialBody)
        {
            tgtOrbit = _currentTarget.Orbit as PatchedConicsOrbit;
        }

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
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(_orbit, _UT, tgtOrbit, _currentTarget.Orbit.referenceBody, interceptDistance, out _burnUTout);
        }
        else // For a tartget that is not a celestial
        {
            if (interceptDistance < 0)
                interceptDistance = 100; // m
            Logger.LogDebug($"Seeking Solution for Non-Celestial Target with closest approach {interceptDistance}");
            // double _caDistance = 100; // m (closest approach to non-celestial target)
            _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(_orbit, _UT, tgtOrbit, interceptDistance, out _burnUTout);
        }
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, _burnUTout, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error($"Course Correction for tragetory to {_currentTarget.Name}: No Solution Found !");
            return false;
        }
    }

    public bool MoonReturn(double burnUT, double targetMRPeR, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
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
                return CreateManeuverNodeCaller(_deltaV, _burnUTout, burnOffsetFactor);
            }
            else
            {
                FPStatus.Error("Moon Return: No Solution Found!");
                return false;
            }
        }
    }

    public bool MatchVelocity(double burnUT, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        PatchedConicsOrbit tgtOrbit = null;
        if (_currentTarget.IsPart)
        {
            tgtOrbit = _currentTarget.Part.PartOwner.SimulationObject.Orbit as PatchedConicsOrbit;
        }
        else if (_currentTarget.IsVessel || _currentTarget.IsCelestialBody)
        {
            tgtOrbit = _currentTarget.Orbit as PatchedConicsOrbit;
        }


        Logger.LogDebug($"MatchVelocity: Match Velocity with {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");

        FPStatus.Ok($"Ready to Match Velocity with {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");

        // double closestApproachTime = orbit.NextClosestApproachTime(tgtOrbit, _UT + 2); //+2 so that closestApproachTime is definitely > _UT
        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(_orbit, burnUT, tgtOrbit);
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, burnUT, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error($"Match Velocity with {_currentTarget.Name} at Closest Approach: No Solution Found!");
            return false;
        }
    }

    public bool PlanetaryXfer(double burnUT, double burnOffsetFactor = -0.5)
    {
        double _UT = Game.UniverseModel.UniverseTime;
        PatchedConicsOrbit _orbit = _activeVessel.Orbit;

        PatchedConicsOrbit tgtOrbit = null;
        if (_currentTarget.IsPart)
        {
            tgtOrbit = _currentTarget.Part.PartOwner.SimulationObject.Orbit as PatchedConicsOrbit;
        }
        else if (_currentTarget.IsVessel || _currentTarget.IsCelestialBody)
        {
            tgtOrbit = _currentTarget.Orbit as PatchedConicsOrbit;
        }

        Logger.LogDebug($"PlanetaryXfer: Transfer to {_currentTarget.Name} {BurnTimeOption.TimeRefDesc}");
        double _burnUTout, _burnUTout2;

        bool _syncPhaseAngle = true;
        // Check the BurnOptionsDropdown
        if (FpUiController.BurnOptionsDropdown.value == BurnTimeOption.TextTimeRef[TimeRef.NEXT_WINDOW])
            _syncPhaseAngle = true;
        else
            _syncPhaseAngle = false;

        // Add checks for potential bad things like MJ
        // Check preconditions
        //if (!_currentTarget.NormalTargetExists)
        //  throw new OperationException(
        //      Localizer.Format("#MechJeb_transfer_Exception1")); //"must select a target for the interplanetary transfer."

        if (_orbit.referenceBody.referenceBody == null)
            Logger.LogDebug($"PlanetaryXfer: doesn't make sense to plot an interplanetary transfer from an orbit around {_orbit.referenceBody.Name}");
        //throw new OperationException(Localizer.Format("#MechJeb_transfer_Exception2",
        //      _orbit.referenceBody.Name
        //          .LocalizeRemoveGender())); //doesn't make sense to plot an interplanetary transfer from an orbit around <<1>>

        if (_orbit.referenceBody.referenceBody != tgtOrbit.referenceBody)
        {
            if (_orbit.referenceBody == tgtOrbit.referenceBody)
                Logger.LogDebug($"PlanetaryXfer: use regular Hohmann transfer function to intercept another body orbiting {_orbit.referenceBody.Name}");
            //throw new OperationException(Localizer.Format("#MechJeb_transfer_Exception3",
            //    _orbit.referenceBody.Name
            //        .LocalizeRemoveGender())); //use regular Hohmann transfer function to intercept another body orbiting <<1>>
            Logger.LogDebug($"PlanetaryXfer: an interplanetary transfer from within {_orbit.referenceBody.Name}'s sphere of influence must target a body that orbits {_orbit.referenceBody.Name}'s parent, {_orbit.referenceBody.referenceBody.Name}");
            //throw new OperationException(Localizer.Format("#MechJeb_transfer_Exception4", _orbit.referenceBody.Name.LocalizeRemoveGender(),
            //    _orbit.referenceBody.Name.LocalizeRemoveGender(),
            //    _orbit.referenceBody.referenceBody.Name
            //        .LocalizeRemoveGender())); //"an interplanetary transfer from within "<<1>>"'s sphere of influence must target a body that orbits "<<2>>"'s parent, "<<3>>.
        }

        // Simple warnings
        if (_orbit.referenceBody.Orbit.RelativeInclination(tgtOrbit) > 30)
        {
            Logger.LogWarning($"PlanetaryXfer: target's orbital plane is at a {_orbit.RelativeInclination(tgtOrbit).ToString("F0")}º angle to {_orbit.referenceBody.Name}'s orbital plane (recommend at most 30º). Planned interplanetary transfer may not intercept target properly.");

            //ErrorMessage = Localizer.Format("#MechJeb_transfer_errormsg1", _orbit.RelativeInclination(tgtOrbit).ToString("F0"),
            //    _orbit.referenceBody.Name
            //        .LocalizeRemoveGender()); //"Warning: target's orbital plane is at a"<<1>>"º angle to "<<2>>"'s orbital plane (recommend at most 30º). Planned interplanetary transfer may not intercept target properly."
        }
        else
        {
            double relativeInclination = Vector3d.Angle(_orbit.OrbitNormal(), _orbit.referenceBody.Orbit.OrbitNormal());
            if (relativeInclination > 10)
            {
                Logger.LogWarning($"PlanetaryXfer: Recommend starting interplanetary transfers from {_orbit.referenceBody.Name} from an orbit in the same plane as {_orbit.referenceBody.Name}'s orbit around {_orbit.referenceBody.referenceBody.Name}. Starting orbit around {_orbit.referenceBody.Name} is inclined {relativeInclination.ToString("F1")}º with respect to {_orbit.referenceBody.Name}'s orbit around {_orbit.referenceBody.referenceBody.Name} (recommend < 10º). Planned transfer may not intercept target properly.");

                //ErrorMessage = Localizer.Format("#MechJeb_transfer_errormsg2", _orbit.referenceBody.Name.LocalizeRemoveGender(),
                //    _orbit.referenceBody.Name.LocalizeRemoveGender(), _orbit.referenceBody.referenceBody.Name.LocalizeRemoveGender(),
                //    _orbit.referenceBody.Name.LocalizeRemoveGender(), relativeInclination.ToString("F1"),
                //    _orbit.referenceBody.Name.LocalizeRemoveGender(),
                //    _orbit.referenceBody.referenceBody.Name
                //        .LocalizeRemoveGender()); //Warning: Recommend starting interplanetary transfers from  <<1>> from an orbit in the same plane as "<<2>>"'s orbit around "<<3>>". Starting orbit around "<<4>>" is inclined "<<5>>"º with respect to "<<6>>"'s orbit around "<<7>> " (recommend < 10º). Planned transfer may not intercept target properly."
            }
            else if (_orbit.eccentricity > 0.2)
            {
                Logger.LogWarning($"PlanetaryXfer: Recommend starting interplanetary transfers from a near-circular orbit (eccentricity < 0.2). Planned transfer is starting from an orbit with eccentricity {_orbit.eccentricity.ToString("F2")} and so may not intercept target properly.");

                //ErrorMessage = Localizer.Format("#MechJeb_transfer_errormsg3",
                //    _orbit.eccentricity.ToString(
                //        "F2")); //Warning: Recommend starting interplanetary transfers from a near-circular orbit (eccentricity < 0.2). Planned transfer is starting from an orbit with eccentricity <<1>> and so may not intercept target properly.
            }
        }
        FPStatus.Warning($"Experimental Transfer to {_currentTarget.Name} Ready");

        Vector3d _deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(_orbit, _UT, tgtOrbit, _syncPhaseAngle, out _burnUTout);
        // Vector3d _deltaV2 = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryLambertTransferEjection(_orbit, _UT, tgtOrbit, out _burnUTout2);
        if (_deltaV != Vector3d.zero)
        {
            return CreateManeuverNodeCaller(_deltaV, _burnUTout, burnOffsetFactor);
        }
        else
        {
            FPStatus.Error($"Planetary Transfer to {_currentTarget.Name}: No Solution Found!");
            return false;
        }
    }

    //private IEnumerator TestPerturbedOrbit(PatchedConicsOrbit o, double burnUT, Vector3d dV)
    //{
    //    // This code compares the orbit info returned from a PerturbedOrbit orbit call with the
    //    // info for the orbit in the next patch. It should be called after creating a maneuver
    //    // node for the active vessel that applies the burn vector associated with the dV to
    //    // make sure that PerturbedOrbit is correctly predicting the effect of delta V on the
    //    // current orbit.

    //    // NodeManagerPlugin.Instance.RefreshNodes();
    //    yield return (object)new WaitForFixedUpdate();

    //    //List<ManeuverNodeData> patchList =
    //    //    Game.SpaceSimulation.Maneuvers.GetNodesForVessel(ActiveVessel.SimulationObject.GlobalId);

    //    Logger.LogDebug($"TestPerturbedOrbit: patchList.Count = {NodeManagerPlugin.Instance.Nodes.Count}");

    //    if (NodeManagerPlugin.Instance.Nodes.Count == 0)
    //    {
    //        Logger.LogDebug($"TestPerturbedOrbit: No future patches to compare to.");
    //    }
    //    else
    //    {
    //        PatchedConicsOrbit hypotheticalOrbit = o.PerturbedOrbit(burnUT, dV);
    //        ManeuverPlanSolver maneuverPlanSolver = _activeVessel.Orbiter?.ManeuverPlanSolver;
    //        var PatchedConicsList = maneuverPlanSolver?.PatchedConicsList;
    //        PatchedConicsOrbit nextOrbit; // = PatchedConicsList[0];
    //        if (NodeManagerPlugin.Instance.Nodes[0].ManeuverTrajectoryPatch != null) { nextOrbit = NodeManagerPlugin.Instance.Nodes[0].ManeuverTrajectoryPatch; }
    //        else { nextOrbit = maneuverPlanSolver.ManeuverTrajectory[0] as PatchedConicsOrbit; }

    //        // IPatchedOrbit orbit = null;

    //        Logger.LogDebug($"thisOrbit:{o}");
    //        Logger.LogDebug($"nextOrbit:{nextOrbit}");
    //        Logger.LogDebug($"nextOrbit: inc = {PatchedConicsList[0].inclination.ToString("n3")}");
    //        Logger.LogDebug($"nextOrbit: ecc = {PatchedConicsList[0].eccentricity.ToString("n3")}");
    //        Logger.LogDebug($"nextOrbit: sma = {PatchedConicsList[0].semiMajorAxis.ToString("n3")}");
    //        Logger.LogDebug($"nextOrbit: lan = {PatchedConicsList[0].longitudeOfAscendingNode.ToString("n3")}");
    //        Logger.LogDebug($"nextOrbit: ApA = {(PatchedConicsList[0].ApoapsisArl / 1000).ToString("n3")}");
    //        Logger.LogDebug($"nextOrbit: PeA = {(PatchedConicsList[0].PeriapsisArl / 1000).ToString("n3")}");
    //        Logger.LogDebug($"hypotheticalOrbit:{hypotheticalOrbit}");
    //    }
    //}
}
