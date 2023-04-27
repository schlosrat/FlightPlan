using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using FPUtilities;
using HarmonyLib;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.UI.Binding;
using ManeuverNodeController;
using MuMech;
using NodeManager;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using System.Collections;
using System.Reflection;
using UnityEngine;
using K2D2;

namespace FlightPlan;

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

    // Control click through to the game
    private bool gameInputState = true;
    public List<String> inputFields = new List<String>();

    // GUI stuff
    static bool loaded = false;
    private bool interfaceEnabled = false;
    private bool GUIenabled = true;
    private Rect _windowRect;
    private int windowWidth = Screen.width / 6; //384px on 1920x1080
    private int windowHeight = Screen.height / 4; //360px on 1920x1080
    private Rect closeBtnRect;

    // Status of last Flight Plan function
    private enum Status
    {
        VIRGIN,
        OK,
        WARNING,
        ERROR
    }
    private Status status = Status.VIRGIN; // Everyone starts out this way...
    private double statusTime = 0; // UT of last staus update
    private string statusText;

    // Config parameters
    private ConfigEntry<string> initialStatusText;
    private ConfigEntry<string> defaultTargetPeAStr;
    private ConfigEntry<string> defaultTargetApAStr;
    private ConfigEntry<string> defaultTargetMRPeAStr;
    private ConfigEntry<string> defaultTargetIncStr;
    private ConfigEntry<string> defaultInterceptTStr;
    private ConfigEntry<double> statusPersistence;
    private ConfigEntry<double> statusFadeTime;
    private ConfigEntry<bool> experimental;
    private ConfigEntry<bool> autoLaunchMNC;

    // Button bools
    private bool circAp, circPe, circNow, newPe, newAp, newPeAp, newInc, matchPlanesA, matchPlanesD, hohmannT, interceptAtTime, courseCorrection, moonReturn, matchVCA, matchVNow, planetaryXfer, launchMNC, executeNode;

    // Body selection.
    private string selectedBody = null;
    private List<string> bodies;
    private bool selectingBody = false;
    private static Vector2 scrollPositionBodies;

    // mod-wide data
    private VesselComponent activeVessel;
    private SimulationObjectModel currentTarget;
    private ManeuverNodeData currentNode = null;
    List<ManeuverNodeData> activeNodes;

    // Text Inputs
    private string targetPeAStr;   // m - This is a Configurable Parameter
    private string targetApAStr;   // m - This is a Configurable Parameter
    private string targetPeAStr1;  // m - This is initially set = targetPeAStr
    private string targetApAStr1;  // m - This is initially set = targetApAStr
    private string targetMRPeAStr; // m - This is initially set = targetApAStr
    private string targetIncStr;   // degrees - Default 0
    private string interceptTStr;  // s - This is a Configurable Parameter

    // Values from Text Inputs
    private double targetPeR;
    private double targetApR;
    private double targetPeR1;
    private double targetApR1;
    private double targetMRPeR;
    private double targetInc;
    private double interceptT;

    // GUI layout and style stuff
    private GUIStyle errorStyle, warnStyle, progradeStyle, normalStyle, radialStyle, labelStyle;
    private GameInstance game;
    private GUIStyle horizontalDivider = new GUIStyle();
    private GUISkin _spaceWarpUISkin;
    private GUIStyle tgtBtnStyle;
    private GUIStyle ctrlBtnStyle;
    private GUIStyle bigBtnStyle;
    private GUIStyle smallBtnStyle;
    // private GUIStyle mainWindowStyle;
    private GUIStyle textInputStyle;
    // private GUIStyle sectionToggleStyle;
    private GUIStyle closeBtnStyle;
    private GUIStyle nameLabelStyle;
    private GUIStyle valueLabelStyle;
    private GUIStyle unitLabelStyle;
    private GUIStyle statusStyle;
    private static GUIStyle boxStyle;
    // private string unitColorHex;
    private int spacingAfterHeader = -12;
    private int spacingAfterEntry = -5;
    // private int spacingAfterSection = 5;

    // App bar button(s)
    private const string ToolbarFlightButtonID = "BTN-FlightPlanFlight";
    // private const string ToolbarOABButtonID = "BTN-FlightPlanOAB";

    //public ManualLogSource logger;
    public new static ManualLogSource Logger { get; set; }

    // Refelction access variables for launching MNC & K2-D2
    private bool MNCLoaded, K2D2Loaded, checkK2D2status  = false;
    private PluginInfo MNC, K2D2;
    private Version mncMinVersion, k2d2MinVersion;
    private int mncVerCheck, k2d2VerCheck;
    private string k2d2Status;
    Type k2d2Type, mncType;
    PropertyInfo k2d2PropertyInfo, mncPropertyInfo;
    MethodInfo k2d2GetStatusMethodInfo, k2d2FlyNodeMethodInfo, k2d2ToggleMethodInfo, mncLaunchMNCMethodInfo;
    object k2d2Instance, mncInstance;

    // private string MNCGUID = "com.github.xyz3211.maneuver_node_controller";

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

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
        //StateChanges.BaseAssemblyEditorEntered += message => GUIenabled = false;
        //StateChanges.MainMenuStateEntered += message => GUIenabled = false;
        //StateChanges.ColonyViewEntered += message => GUIenabled = false;
        // StateChanges.TrainingCenterEntered += message => GUIenabled = false;
        //StateChanges.MissionControlEntered += message => GUIenabled = false;
        // StateChanges.TrackingStationEntered += message => GUIenabled = false;
        //StateChanges.ResearchAndDevelopmentEntered += message => GUIenabled = false;
        //StateChanges.LaunchpadEntered += message => GUIenabled = false;
        //StateChanges.RunwayEntered += message => GUIenabled = false;

        Logger.LogInfo($"ManeuverNodeControllerMod.ModGuid = {ManeuverNodeControllerMod.ModGuid}");
        if (Chainloader.PluginInfos.TryGetValue(ManeuverNodeControllerMod.ModGuid, out MNC))
        {
            MNCLoaded = true;
            Logger.LogInfo("Maneuver Node Controller installed and available");
            Logger.LogInfo($"MNC = {MNC}");
            // mncVersion = MNC.Metadata.Version;
            mncMinVersion = new Version(0, 8, 3);
            mncVerCheck = MNC.Metadata.Version.CompareTo(mncMinVersion);
            Logger.LogInfo($"mncVerCheck = {mncVerCheck}");

            // Reflections method to attempt the same thing more cleanly
            mncType = Type.GetType($"ManeuverNodeController.ManeuverNodeControllerMod, {ManeuverNodeControllerMod.ModGuid}");
            mncPropertyInfo = mncType!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            mncInstance = mncPropertyInfo.GetValue(null);
            mncLaunchMNCMethodInfo = mncPropertyInfo!.PropertyType.GetMethod("LaunchMNC");
        }
        // else MNCLoaded = false;
        Logger.LogInfo($"MNCLoaded = {MNCLoaded}");

        Logger.LogInfo($"K2D2_Plugin.ModGuid = {K2D2_Plugin.ModGuid}");
        if (Chainloader.PluginInfos.TryGetValue(K2D2_Plugin.ModGuid, out K2D2))
        {
            K2D2Loaded = true;
            Logger.LogInfo("K2-D2 installed and available");
            Logger.LogInfo($"K2D2 = {K2D2}");
            k2d2MinVersion = new Version(0, 8, 1);
            k2d2VerCheck = K2D2.Metadata.Version.CompareTo(k2d2MinVersion);
            Logger.LogInfo($"k2d2VerCheck = {k2d2VerCheck}");

            k2d2Type = Type.GetType($"K2D2.K2D2_Plugin, {K2D2_Plugin.ModGuid}");
            k2d2PropertyInfo = k2d2Type!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            k2d2Instance = k2d2PropertyInfo.GetValue(null);
            k2d2ToggleMethodInfo = k2d2PropertyInfo!.PropertyType.GetMethod("ToggleAppBarButton");
            k2d2FlyNodeMethodInfo = k2d2PropertyInfo!.PropertyType.GetMethod("FlyNode");
            k2d2GetStatusMethodInfo = k2d2PropertyInfo!.PropertyType.GetMethod("GetStatus");
        }
        // else K2D2Loaded = false;
        Logger.LogInfo($"K2D2Loaded = {K2D2Loaded}");

        //Logger.LogInfo($"NMLoaded = {NMLoaded}");
        //if (Chainloader.PluginInfos.TryGetValue(NodeManagerPlugin.Instance.ModGuid, out NM))
        //{
        //    NMLoaded = true;
        //    Logger.LogInfo("Node Manager installed and available");
        //    Logger.LogInfo($"MNC = {NM}");
        //}
        //else NMLoaded = false;
        //Logger.LogInfo($"NMLoaded = {NMLoaded}");

        // Setup the list of input field names (most are the same as the entry string text displayed in the GUI window)
        inputFields.Add("New Pe");
        inputFields.Add("New Ap");
        inputFields.Add("New Pe & Ap");
        inputFields.Add("New Ap & Pe"); // kludgy name for the second input in a two input line
        inputFields.Add("New Inclination");
        inputFields.Add("Intercept at Time");
        inputFields.Add("Select Target");

        Logger.LogInfo("Loaded");
        if (loaded)
        {
            Destroy(this);
        }
        loaded = true;

        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);

        _spaceWarpUISkin = Skins.ConsoleSkin;

        boxStyle = new GUIStyle(_spaceWarpUISkin.box); // GUI.skin.GetStyle("Box");

        //mainWindowStyle = new GUIStyle(_spaceWarpUISkin.window)
        //{
        //    padding = new RectOffset(8, 8, 20, 8),
        //    contentOffset = new Vector2(0, -22),
        //    fixedWidth = windowWidth
        //};

        textInputStyle = new GUIStyle(_spaceWarpUISkin.textField)
        {
            alignment = TextAnchor.LowerCenter,
            padding = new RectOffset(10, 10, 0, 0),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 20,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        tgtBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 25, // 16,
            // fixedWidth = (int)(windowWidth * 0.6),
            fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        ctrlBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 16,
            fixedWidth = 16, // windowWidth / 2,
            fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        bigBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 25, // 16,
            fixedWidth = (int)(windowWidth * 0.6),
            fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        smallBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(10, 10, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 25, // 16,
            // fixedWidth = 95,
            fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        //sectionToggleStyle = new GUIStyle(_spaceWarpUISkin.toggle)
        //{
        //    padding = new RectOffset(14, 0, 3, 3)
        //};

        statusStyle = new GUIStyle(_spaceWarpUISkin.label);

        nameLabelStyle = new GUIStyle(_spaceWarpUISkin.label);
        nameLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);

        valueLabelStyle = new GUIStyle(_spaceWarpUISkin.label)
        {
            alignment = TextAnchor.MiddleRight
        };
        valueLabelStyle.normal.textColor = new Color(.6f, .7f, 1, 1);

        unitLabelStyle = new GUIStyle(valueLabelStyle)
        {
            fixedWidth = 24,
            alignment = TextAnchor.MiddleLeft
        };
        unitLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);
        // unitColorHex = ColorUtility.ToHtmlStringRGBA(unitLabelStyle.normal.textColor);

        closeBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            fontSize = 12
        };

        closeBtnRect = new Rect(windowWidth - 23, 6, 16, 16);

        labelStyle = warnStyle = new GUIStyle(_spaceWarpUISkin.label); //  GUI.skin.GetStyle("Label"));
        errorStyle = new GUIStyle(_spaceWarpUISkin.label); //  GUI.skin.GetStyle("Label"));
        errorStyle.normal.textColor = Color.red;
        warnStyle = new GUIStyle(_spaceWarpUISkin.label); //  GUI.skin.GetStyle("Label"));
        warnStyle.normal.textColor = Color.yellow;
        horizontalDivider.fixedHeight = 2;
        horizontalDivider.margin = new RectOffset(0, 0, 4, 4);

        // progradeStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        // progradeStyle.normal.textColor = Color.yellow;
        // normalStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        // normalStyle.normal.textColor = Color.magenta;
        // radialStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        // radialStyle.normal.textColor = Color.cyan;
        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            "Flight Plan",
            "BTN-FlightPlan",
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            ToggleButton);

        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(FlightPlanPlugin).Assembly);

        // Fetch a configuration value or create a default one if it does not exist
        statusPersistence = Config.Bind<double>("Status Settings Section", "Satus Hold Time",      20, "Controls time delay (in seconds) before status beings to fade");
        statusFadeTime    = Config.Bind<double>("Status Settings Section", "Satus Fade Time",      20, "Controls the time (in seconds) it takes for status to fade");
        initialStatusText = Config.Bind<string>("Status Settings Section", "Initial Status", "Virgin", "Controls the status reported at startup prior to the first command");
        experimental  = Config.Bind<bool>("Experimental Section", "Experimental Features",      false, "Enable/Disable experimental.Value features for testing - Warrantee Void if Enabled!");
        autoLaunchMNC = Config.Bind<bool>("Experimental Section", "Launch Maneuver Node Controller", false, "Enable/Disable automatically launching the Maneuver Node Controller GUI (if installed) when experimental.Value nodes are created");
        defaultTargetPeAStr  = Config.Bind<string>("Defualt Inputs Section", "Target Pe Alt",        "80000", "Default Pe input (in meters) used to pre-populate text input field at startup");
        defaultTargetApAStr = Config.Bind<string>("Defualt Inputs Section", "Target Ap Alt",       "250000", "Default Ap input (in meters) used to pre-populate text input field at startup");
        defaultTargetIncStr = Config.Bind<string>("Defualt Inputs Section", "Target Inclination",       "0", "Default inclination input (in degrees) used to pre-populate text input field at startup");
        defaultInterceptTStr = Config.Bind<string>("Defualt Inputs Section", "Target Intercept Time",  "100", "Default intercept time (in seconds) used to pre-populate text input field at startup");
        defaultTargetMRPeAStr = Config.Bind<string>("Defualt Inputs Section", "Target Moon Return Pe Alt", "100000", "Default Moon Return Target Pe input (in meters) used to pre-populate text input field at startup");

        // Set the initial and defualt values based on config parameters. These don't make sense to need live update, so there're here instead of useing the configParam.Value elsewhere
        statusText     = initialStatusText.Value;
        targetPeAStr   = defaultTargetPeAStr.Value;
        targetApAStr   = defaultTargetApAStr.Value;
        targetPeAStr1  = defaultTargetPeAStr.Value;
        targetApAStr1  = defaultTargetApAStr.Value;
        targetMRPeAStr = defaultTargetMRPeAStr.Value;
        targetIncStr   = defaultTargetIncStr.Value;
        interceptTStr  = defaultInterceptTStr.Value;

        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        Logger.LogInfo($"Experimental Features: {experimental.Value}");
    }

    private void ToggleButton(bool toggle)
    {
        interfaceEnabled = toggle;
        GameObject.Find("BTN-FlightPlan")?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(toggle);
    }

    void Awake()
    {
        _windowRect = new Rect((Screen.width * 0.7f) - (windowWidth / 2), (Screen.height / 2) - (windowHeight / 2), 0, 0);
        // Logger = base.Logger;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            ToggleButton(!interfaceEnabled);
            Logger.LogInfo("UI toggled with hotkey");
        }
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
            GUI.skin = Skins.ConsoleSkin;

            _windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                _windowRect,
                FillWindow,
                "<color=#696DFF>// FLIGHT PLAN</color>",
                GUILayout.Height(windowHeight),
                GUILayout.Width(windowWidth)
            );

            if (gameInputState && inputFields.Contains(GUI.GetNameOfFocusedControl()))
            {
                // Logger.LogDebug($"OnGUI: Disabling Game Input: Focused Item '{GUI.GetNameOfFocusedControl()}'");
                gameInputState = false;
                GameManager.Instance.Game.Input.Disable();
            }
            else if (!gameInputState && !inputFields.Contains(GUI.GetNameOfFocusedControl()))
            {
                // Logger.LogDebug($"OnGUI: Enabling Game Input: FYI, Focused Item '{GUI.GetNameOfFocusedControl()}'");
                gameInputState = true;
                GameManager.Instance.Game.Input.Enable();
            }
            //if (selectingBody)
            //{
            //    // Do something here to disable mouse wheel control of zoom in and out.
            //    // Intent: allow player to scroll in the scroll view without causing the game to zoom in and out
            //    GameManager.Instance._game.MouseManager.enabled = false;
            //}
            //else
            //{
            //    // Do something here to re-enable mouse wheel control of zoom in and out.
            //    GameManager.Instance._game.MouseManager.enabled = true;
            //}
        }
        else
        {
            if (!gameInputState)
            {
                // Logger.LogDebug($"OnGUI: Enabling Game Input due to GUI disabled: FYI, Focused Item '{GUI.GetNameOfFocusedControl()}'");
                gameInputState = true;
                GameManager.Instance.Game.Input.Enable();
            }
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
        if (CloseButton())
        {
            CloseWindow();
        }

        // game = GameManager.Instance.Game;
        //activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        //currentNode = (activeNodes.Count() > 0) ? activeNodes[0] : null;
        FPUtility.RefreshActiveVesselAndCurrentManeuver();
        // FPNodeControl.RefreshManeuverNodes();
        currentNode = getCurrentNode();

        string tgtName;
        if (currentTarget == null)
            tgtName = "None";
        else
            tgtName = currentTarget.Name;
        DrawSectionHeader("Target", tgtName);
        BodySelectionGUI();

        var referenceBody = activeVessel.Orbit.referenceBody;

        DrawSectionHeader("Ownship Maneuvers");
        if (activeVessel.Orbit.eccentricity < 1)
        {
            DrawButton("Circularize at Ap", ref circAp);
        }
        DrawButton("Circularize at Pe", ref circPe);

        DrawButton("Circularize Now", ref circNow);

        DrawButtonWithTextField("New Pe", ref newPe, ref targetPeAStr, "m");
        if (double.TryParse(targetPeAStr, out targetPeR)) targetPeR += referenceBody.radius;
        else targetPeR = 0;

        if (activeVessel.Orbit.eccentricity < 1)
        {
            DrawButtonWithTextField("New Ap", ref newAp, ref targetApAStr, "m");
            if (double.TryParse(targetApAStr, out targetApR)) targetApR += referenceBody.radius;
            else targetApR = 0;

            DrawButtonWithDualTextField("New Pe & Ap", "New Ap & Pe", ref newPeAp, ref targetPeAStr1, ref targetApAStr1);
            if (double.TryParse(targetPeAStr1, out targetPeR1)) targetPeR1 += referenceBody.radius;
            else targetPeR1 = 0;
            if (double.TryParse(targetApAStr1, out targetApR1)) targetApR1 += referenceBody.radius;
            else targetApR1 = 0;
        }

        DrawButtonWithTextField("New Inclination", ref newInc, ref targetIncStr, "°");
        if (!double.TryParse(targetIncStr, out targetInc)) targetInc = 0;

        if (currentTarget != null)
        {
            // If the activeVessel and the currentTarget are both orbiting the same body
            if (currentTarget.Orbit != null) // No maneuvers relative to a star
            {
                if (currentTarget.Orbit.referenceBody.Name == referenceBody.Name)
                {
                    DrawSectionHeader("Maneuvers Relative to Target");
                    DrawButton("Match Planes at AN", ref matchPlanesA);
                    DrawButton("Match Planes at DN", ref matchPlanesD);

                    DrawButton("Hohmann Xfer", ref hohmannT);
                    DrawButton("Course Correction", ref courseCorrection);

                    if (experimental.Value)
                    {
                        DrawButtonWithTextField("Intercept at Time", ref interceptAtTime, ref interceptTStr, "s");
                        try { interceptT = double.Parse(interceptTStr); }
                        catch { interceptT = 100; }
                        DrawButton("Match Velocity @CA", ref matchVCA);
                        DrawButton("Match Velocity Now", ref matchVNow);
                    }
                }
            }

            if (experimental.Value)
            {
                // If the activeVessel is not orbiting a star
                if (!referenceBody.IsStar && currentTarget.IsCelestialBody) // not orbiting a start and target is celestial
                {
                    // If the activeVessel is orbiting a planet and the current target is not the body the active vessel is orbiting
                    if (referenceBody.Orbit.referenceBody.IsStar && (currentTarget.Name != referenceBody.Name) && currentTarget.Orbit != null)
                    {
                        if (currentTarget.Orbit.referenceBody.IsStar) // exclude targets that are a moon
                        {
                            DrawSectionHeader("Interplanetary Maneuvers");
                            DrawButton("Interplanetary Transfer", ref planetaryXfer);
                        }
                    }
                }

            }
        }

        // If the activeVessle is at a moon (a celestial in orbit around another celestial that's not also a star)
        if (!referenceBody.IsStar) // not orbiting a star
        {
            if (!referenceBody.Orbit.referenceBody.IsStar && activeVessel.Orbit.eccentricity < 1) // not orbiting a planet, and e < 1
            {
                DrawSectionHeader("Moon Specific Maneuvers");
                // DrawButton("Moon Return", ref moonReturn); // targetMRPeAAStr
                DrawButtonWithTextField("Moon Return", ref moonReturn, ref targetMRPeAStr, "m");
                if (double.TryParse(targetMRPeAStr, out targetMRPeR)) targetMRPeR += activeVessel.Orbit.referenceBody.radius;
                else targetMRPeR = 0;
            }
        }

        var UT = game.UniverseModel.UniversalTime;
        // if (statusText == "Virgin") statusTime = UT;
        if (currentNode == null && status != Status.VIRGIN)
        {
            status = Status.OK;
            statusText = "";
        }
        DrawGUIStatus(UT);

        handleButtons();

        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    private bool CloseButton()
    {
        return GUI.Button(closeBtnRect, "x", closeBtnStyle);
    }

    private void CloseWindow()
    {
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
        interfaceEnabled = false;
        Logger.LogDebug("CloseWindow: Restoring Game Input on window close.");
        // game.Input.Flight.Enable();
        GameManager.Instance.Game.Input.Enable();
        ToggleButton(interfaceEnabled);
    }

    private void BodySelectionGUI()
    {
        bodies = GameManager.Instance.Game.SpaceSimulation.GetBodyNameKeys().ToList();
        string baseName = "Select Target";
        GUILayout.BeginHorizontal();
        GUILayout.Label("Target Celestial Body: ", GUILayout.Width((float)(windowWidth * 0.6)));
        if (!selectingBody)
        {
            GUI.SetNextControlName(baseName);
            if (GUILayout.Button(selectedBody, tgtBtnStyle))
                selectingBody = true;
        }
        else
        {
            GUILayout.BeginVertical(boxStyle);
            GUI.SetNextControlName("Select Target");
            scrollPositionBodies = GUILayout.BeginScrollView(scrollPositionBodies, false, true, GUILayout.Height(150));
            int index = 0;
            foreach (string body in bodies)
            {
                var thisName = baseName + index.ToString("d2");
                if (!inputFields.Contains(thisName)) inputFields.Add(thisName);
                GUI.SetNextControlName(thisName);
                if (GUILayout.Button(body, tgtBtnStyle))
                {
                    selectedBody = body;
                    selectingBody = false;
                    activeVessel.SetTargetByID(game.UniverseModel.FindCelestialBodyByName(body).GlobalId);
                    currentTarget = activeVessel.TargetObject;
                }
                index++;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSectionHeader(string sectionName, string value = "", GUIStyle valueStyle = null) // was (string sectionName, ref bool isPopout, string value = "")
    {
        if (valueStyle == null) valueStyle = valueLabelStyle;
        GUILayout.BeginHorizontal();
        // Don't need popout buttons for ROC
        // isPopout = isPopout ? !CloseButton() : GUILayout.Button("⇖", popoutBtnStyle);

        GUILayout.Label($"<b>{sectionName}</b> ");
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, valueStyle);
        GUILayout.Space(5);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterHeader);
    }

    private void DrawEntryButton(string entryName, ref bool button, string buttonStr, string value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(entryName, nameLabelStyle);
        GUILayout.FlexibleSpace();
        button = GUILayout.Button(buttonStr, ctrlBtnStyle);
        GUILayout.Label(value, valueLabelStyle);
        GUILayout.Space(5);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntry2Button(string entryName, ref bool button1, string button1Str, ref bool button2, string button2Str, string value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(entryName, nameLabelStyle);
        GUILayout.FlexibleSpace();
        button1 = GUILayout.Button(button1Str, ctrlBtnStyle);
        button2 = GUILayout.Button(button2Str, ctrlBtnStyle);
        GUILayout.Label(value, valueLabelStyle);
        GUILayout.Space(5);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntryTextField(string entryName, ref string textEntry, string unit = "")
    {
        double num;
        Color normal;
        GUILayout.BeginHorizontal();
        GUILayout.Label(entryName, nameLabelStyle);
        GUILayout.FlexibleSpace();
        normal = GUI.color;
        bool parsed = double.TryParse(textEntry, out num);
        if (!parsed) GUI.color = Color.red;
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
        GUI.color = normal;
        GUILayout.Space(5);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawButton(string buttonStr, ref bool button)
    {
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(buttonStr, bigBtnStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawButtonWithTextField(string entryName, ref bool button, ref string textEntry, string unit = "")
    {
        double num;
        Color normal;
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(entryName, smallBtnStyle);
        GUILayout.Space(10);
        normal = GUI.color;
        bool parsed = double.TryParse(textEntry, out num);
        if (!parsed) GUI.color = Color.red;
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
        GUI.color = normal;
        GUILayout.Space(3);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawButtonWithDualTextField(string entryName1, string entryName2, ref bool button, ref string textEntry1, ref string textEntry2, string unit = "")
    {
        double num;
        Color normal;
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(entryName1, smallBtnStyle);
        GUILayout.Space(5);
        normal = GUI.color;
        bool parsed = double.TryParse(textEntry1, out num);
        if (!parsed) GUI.color = Color.red;
        GUI.SetNextControlName(entryName1);
        textEntry1 = GUILayout.TextField(textEntry1, textInputStyle);
        GUI.color = normal;
        GUILayout.Space(5);
        parsed = double.TryParse(textEntry2, out num);
        if (!parsed) GUI.color = Color.red;
        GUI.SetNextControlName(entryName2);
        textEntry2 = GUILayout.TextField(textEntry2, textInputStyle);
        GUI.color = normal;
        GUILayout.Space(3);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawGUIStatus(double UT)
    {
        // Indicate status of last GUI function
        float transparency = 1;
        if (UT > statusTime) transparency = (float)MuUtils.Clamp(1 - (UT - statusTime) / statusFadeTime.Value, 0, 1);
        if (status == Status.VIRGIN)
            statusStyle.normal.textColor = new Color(1, 1, 1, 1);
        if (status == Status.OK)
            statusStyle.normal.textColor = new Color(0, 1, 0, transparency);
        if (status == Status.WARNING)
            statusStyle.normal.textColor = new Color(1, 1, 0, transparency);
        if (status == Status.ERROR)
            statusStyle.normal.textColor = new Color(1, 0, 0, transparency);
        GUILayout.Box("", horizontalDivider);
        DrawSectionHeader("Status:", statusText, statusStyle);

        // Indication to User that its safe to type, or why vessel controls aren't working
        GUILayout.BeginHorizontal();
        string inputStateString = gameInputState ? "<b>Enabled</b>" : "<b>Disabled</b>";
        GUILayout.Label("Game Input: ", labelStyle);
        if (gameInputState)
            GUILayout.Label(inputStateString, labelStyle);
        else
            GUILayout.Label(inputStateString, warnStyle);
        GUILayout.FlexibleSpace();
        if (MNCLoaded && mncVerCheck >= 0)
        {
            launchMNC = GUILayout.Button("MNC", smallBtnStyle);
        }
        GUILayout.Space(10);
        if (K2D2Loaded && currentNode != null)
        {
            executeNode = GUILayout.Button("K2D2", smallBtnStyle);
        }
        GUILayout.EndHorizontal();
        if (checkK2D2status)
        {
            getK2D2Status();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"K2D2: {k2d2Status}", labelStyle);
            // GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(spacingAfterEntry);
    }

    private void callMNC()
    {
        if (MNCLoaded && mncVerCheck >= 0)
        {
            mncLaunchMNCMethodInfo!.Invoke(mncPropertyInfo.GetValue(null), null);
        }
    }

    private void callK2D2()
    {
        if (K2D2Loaded)
        {
            // Reflections method to attempt the same thing more cleanly
            if (k2d2VerCheck < 0)
            {
                k2d2ToggleMethodInfo!.Invoke(k2d2PropertyInfo.GetValue(null), new object[] { true });
            }
            else
            {
                k2d2FlyNodeMethodInfo!.Invoke(k2d2PropertyInfo.GetValue(null), null);
                checkK2D2status = true;
            }
        }
    }

    private void getK2D2Status()
    {
        if (K2D2Loaded)
        {
            if (k2d2VerCheck >= 0)
            {
                k2d2Status = (string)k2d2GetStatusMethodInfo!.Invoke(k2d2Instance, null);

                if (k2d2Status == "Done")
                {
                    if (currentNode.Time < Game.UniverseModel.UniversalTime)
                    {
                        NodeManagerPlugin.Instance.DeleteNodes(0);
                    }
                    checkK2D2status = false;
                }
            }
        }
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
    public bool CircularizeAtAP(double burnOffsetFactor = -0.5)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug("CircularizeAtAP");
        var TimeToAp = orbit.TimeToAp;
        var burnUT = UT + TimeToAp;
        var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT);

        status = Status.OK;
        statusText = "Ready to Circularize at Ap";
        statusTime = UT + statusPersistence.Value;

        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = "Circularize at Ap: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool CircularizeAtPe(double burnOffsetFactor = -0.5)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug("CircularizeAtPe");
        var TimeToPe = orbit.TimeToPe;
        var burnUT = UT + TimeToPe;
        var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT);

        status = Status.OK;
        statusText = "Ready to Circularize at Pe";
        statusTime = UT + statusPersistence.Value;

        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = "Circularize at Pe: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool CircularizeNow(double burnOffsetFactor = -0.5)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug("CircularizeNow");
        var startTimeOffset = 60;
        var burnUT = UT + startTimeOffset;
        var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT);

        status = Status.OK;
        statusText = "Ready to Circularize Now"; // "Ready to Circularize Now"
        statusTime = UT + statusPersistence.Value;

        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = "Circularize Now: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool SetNewPe(double newPe, double burnOffsetFactor = -0.5)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug("SetNewPe");
        // Debug.Log("Set New Pe");
        var TimeToAp = orbit.TimeToAp;
        double burnUT, e;
        e = orbit.eccentricity;
        if (e < 1)
            burnUT = UT + TimeToAp;
        else
            burnUT = UT + 30;

        status = Status.OK;
        statusText = "Ready to Change Pe";
        statusTime = UT + statusPersistence.Value;

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
            status = Status.ERROR;
            statusText = "Set New Pe: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool SetNewAp(double newAp, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug("SetNewAp");
        // Debug.Log("Set New Ap");
        var TimeToPe = orbit.TimeToPe;
        var burnUT = UT + TimeToPe;

        status = Status.OK;
        statusText = "Ready to Change Ap";
        statusTime = UT + statusPersistence.Value;

        Logger.LogDebug($"Seeking Solution: targetApR {newAp} m, currentApR {orbit.Apoapsis} m");
        var deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, burnUT, newAp);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = "Set New Ap: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool Ellipticize(double newAp, double newPe, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug("Ellipticize: Set New Pe and Ap");

        status = Status.OK;
        statusText = "Ready to Ellipticize"; // "Ready to Ellipticize";
        statusTime = UT + statusPersistence.Value;

        if (newPe > newAp)
        {
            (newPe, newAp) = (newAp, newPe);
            status = Status.WARNING;
            statusText = "Pe Setting > Ap Setting";
        }

        Logger.LogDebug($"Seeking Solution: targetPeR {newPe} m, targetApR {newAp} m, body.radius {orbit.referenceBody.radius} m");
        var burnUT = UT + 30;
        var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(orbit, burnUT, newPe, newAp);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = "Set New Pe and Ap: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool SetInclination(double inclination, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug("SetInclination: Set New Inclination");
        Logger.LogDebug($"Seeking Solution: targetInc {inclination}°");
        double burnUT, TAN, TDN;
        Vector3d deltaV, deltaV1, deltaV2;

        status = Status.OK;
        statusText = "Ready to Change Inclination";
        statusTime = UT + statusPersistence.Value;

        if (orbit.eccentricity < 1) // Eliptical orbit: Pick cheapest deltaV between AN and DN
        {
            TAN = orbit.TimeOfAscendingNodeEquatorial(UT);
            TDN = orbit.TimeOfDescendingNodeEquatorial(UT);
            deltaV1 = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, TAN, inclination);
            deltaV2 = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, TDN, inclination);
            if (deltaV1.magnitude < deltaV2.magnitude)
            {
                Logger.LogDebug($"Selecting maneuver at Ascending Node");
                burnUT = TAN;
                deltaV = deltaV1;
            }
            else
            {
                Logger.LogDebug($"Selecting maneuver at Descending Node");
                burnUT = TDN;
                deltaV = deltaV2;
            }
            Logger.LogDebug($"deltaV1 (AN) [{deltaV1.x}, {deltaV1.y}, {deltaV1.z}] = {deltaV1.magnitude} m/s {TAN - UT} s from UT");
            Logger.LogDebug($"deltaV2 (DN) [{deltaV2.x}, {deltaV2.y}, {deltaV2.z}] = {deltaV2.magnitude} m/s {TDN - UT} s from UT");
        }
        else // parabolic or hyperbolic orbit: Do it now!
        {
            burnUT = UT + 30;
            deltaV = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, burnUT, inclination);
        }
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = "Set New Inclination: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool MatchPlanesAtAN(double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MatchPlanesAtAN: Match Planes  with {currentTarget.Name} at AN");
        double burnUT;

        status = Status.OK;
        statusText = $"Ready to Match Planes with {currentTarget.Name} at AN";
        statusTime = UT + statusPersistence.Value;

        var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = $"Match Planes with {currentTarget.Name} at AN: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool MatchPlanesAtDN(double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MatchPlanesAtDN: Match Planes with {currentTarget.Name} at DN");
        double burnUT;

        status = Status.OK;
        statusText = $"Ready to Match Planes with {currentTarget.Name} at DN";
        statusTime = UT + statusPersistence.Value;

        var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = $"Match Planes with {currentTarget.Name} at DN: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool HohmannTransfer(double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"HohmannTransfer: Hohmann Transfer to {currentTarget.Name}");
        // Debug.Log("Hohmann Transfer");
        double burnUT;

        status = Status.WARNING;
        statusText = $"Ready to Transfer to {currentTarget.Name}?";
        statusTime = UT + statusPersistence.Value;

        var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = $"Hohmann Transfer to {currentTarget.Name}: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool InterceptTgtAtUT(double tgtUT, double burnOffsetFactor)
    {
        // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
        // Adapted from call found in MechJebModuleScriptActionRendezvous.cs for "Get Closer"
        // Similar to code in MechJebModuleRendezvousGuidance.cs for "Get Closer" button code.

        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"InterceptTgtAtUT: Intercept {currentTarget.Name} at Time");
        var burnUT = UT + 30;
        var interceptUT = UT + tgtUT;
        double offsetDistance;

        status = Status.WARNING;
        statusText = $"Experimental Intercept of {currentTarget.Name} Ready"; // $"Ready to Intercept {currentTarget.Name}";
        statusTime = UT + statusPersistence.Value;

        Logger.LogDebug($"Seeking Solution: interceptT {interceptT} s");
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
            status = Status.ERROR;
            statusText = $"Intercept {currentTarget.Name}: No Solution Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool CourseCorrection(double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"CourseCorrection: Course Correction burn to improve trajectory to {currentTarget.Name}");
        double burnUT;
        Vector3d deltaV;

        status = Status.OK;
        statusText = "Course Correction Ready"; // "Ready for Course Correction Burn";
        statusTime = UT + statusPersistence.Value;

        if (currentTarget.IsCelestialBody) // For a target that is a celestial
        {
            Logger.LogDebug($"Seeking Solution for Celestial Target");
            double finalPeR = currentTarget.CelestialBody.radius + 50000; // m (PeR at celestial target)
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, currentTarget.Orbit.referenceBody, finalPeR, out burnUT);
        }
        else // For a tartget that is not a celestial
        {
            Logger.LogDebug($"Seeking Solution for Non-Celestial Target");
            double caDistance = 100; // m (closest approach to non-celestial target)
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, caDistance, out burnUT);
        }
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = $"Course Correction for tragetory to {currentTarget.Name}: No Solution Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool MoonReturn(double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MoonReturn: Return from {orbit.referenceBody.Name}");
        var e = orbit.eccentricity;

        status = Status.WARNING;
        statusText = $"Ready to Return from {orbit.referenceBody.Name}?"; // $"Ready to Return from {orbit.referenceBody.Name}";
        statusTime = UT + statusPersistence.Value;

        if (e > 0.2)
        {
            status = Status.WARNING;
            statusText = "Moon Return: Starting Orbit Eccentrity Too Large";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            Logger.LogError($"Moon Return: Starting orbit eccentricty {e.ToString("F2")} is > 0.2");
            return false;
        }
        else
        {
            double burnUT;
            // double primaryRaidus = orbit.referenceBody.Orbit.referenceBody.radius + 100000; // m
            Logger.LogDebug($"Moon Return Attempting to Solve...");
            var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(orbit, UT, targetMRPeR, out burnUT);
            if (deltaV != Vector3d.zero)
            {
                CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
                return true;
            }
            else
            {
                status = Status.ERROR;
                statusText = "Moon Return: No Solution Found!";
                statusTime = UT + statusPersistence.Value;
                Logger.LogDebug(statusText);
                return false;
            }
        }
    }

    public bool MatchVelocityAtCA(double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MatchVelocityAtCA: Match Velocity with {currentTarget.Name} at Closest Approach");

        status = Status.WARNING;
        statusText = $"Experimental Velocity Match with {currentTarget.Name} Ready"; // $"Ready to Match Velocity with {currentTarget.Name}";
        statusTime = UT + statusPersistence.Value;

        double closestApproachTime = orbit.NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT + 2); //+2 so that closestApproachTime is definitely > UT
        var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, closestApproachTime, currentTarget.Orbit as PatchedConicsOrbit);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, closestApproachTime, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = $"Match Velocity with {currentTarget.Name} at Closest Approach: No Solution Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool MatchVelocityNow(double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MatchVelocityNow: Match Velocity with {currentTarget.Name} Now");
        var burnUT = UT + 30;

        status = Status.WARNING;
        statusText = $"Experimental Velocity Match with {currentTarget.Name} Ready"; // $"Ready to Match Velocity with {currentTarget.Name}";
        statusTime = UT + statusPersistence.Value;

        var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, burnUT, currentTarget.Orbit as PatchedConicsOrbit);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = $"Match Velocity with {currentTarget.Name} Now: No Solution Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool PlanetaryXfer(double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"PlanetaryXfer: Transfer to {currentTarget.Name}");
        double burnUT;
        bool syncPhaseAngle = true;

        status = Status.WARNING;
        statusText = $"Experimental Transfer to {currentTarget.Name} Ready"; // $"Ready to depart for {currentTarget.Name}";
        statusTime = UT + statusPersistence.Value;

        var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, syncPhaseAngle, out burnUT);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = $"Planetary Transfer to {currentTarget.Name}: No Solution Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    private void handleButtons()
    {
        if (circAp || circPe || circNow|| newPe || newAp || newPeAp || newInc || matchPlanesA || matchPlanesD || hohmannT || interceptAtTime || courseCorrection || moonReturn || matchVCA || matchVNow || planetaryXfer || launchMNC || executeNode)
        {
            bool pass;

            if (circAp) // Working
            {
                pass = CircularizeAtAP(-0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (circPe) // Working
            {
                pass = CircularizeAtPe(-0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (circNow) // Working
            {
                pass = CircularizeNow(-0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newPe) // Working
            {
                pass = SetNewPe(targetPeR, - 0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newAp) // Working
            {
                pass = SetNewAp(targetApR, - 0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newPeAp) // Working: Not perfect, but pretty good results nevertheless
            {
                pass = Ellipticize(targetApR1 , targetPeR1, - 0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newInc) // Working
            {
                pass = SetInclination(targetInc, -0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (matchPlanesA) // Working
            {
                pass = MatchPlanesAtAN(-0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (matchPlanesD) // Working
            {
                pass = MatchPlanesAtDN(-0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (hohmannT) // Works if we start in a good enough orbit (reasonably circular, close to target's orbital plane)
            {
                pass = HohmannTransfer(-0.5);
                if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (interceptAtTime) // Experimental
            {
                pass = InterceptTgtAtUT(interceptT, -0.5);
                if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (courseCorrection) // Experimental Works at least some times...
            {
                pass = CourseCorrection(-0.5);
                if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (moonReturn) // Works - but may give poor Pe, including potentially lithobreaking
            {
                pass = MoonReturn(-0.5);
                if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (matchVCA) // Experimental
            {
                pass = MatchVelocityAtCA(-0.5);
                if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (matchVNow) // Experimental
            {
                pass = MatchVelocityNow(-0.5);
                if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (planetaryXfer) // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
            {
                pass = PlanetaryXfer(-0.5);
                if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (launchMNC) callMNC();
            else if (executeNode) callK2D2();
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
