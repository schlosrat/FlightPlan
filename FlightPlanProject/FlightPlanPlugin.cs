using BepInEx;
using HarmonyLib;
using KSP.UI.Binding;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using UnityEngine;
using MuMech;
using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.Sim;
using BepInEx.Logging;
using BepInEx.Bootstrap;
using ManeuverNodeController;
using NodeManager;
using BepInEx.Configuration;
using System.Reflection;
using FPUtilities;

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
    private bool circAp, circPe, circNow, newPe, newAp, newPeAp, newInc, matchPlanesA, matchPlanesD, hohmannT, interceptAtTime, courseCorrection, moonReturn, matchVCA, matchVNow, planetaryXfer;

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

    // Access control bool for launching MNC
    private bool MNCLoaded;
    PluginInfo MNC;
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
        
        Logger.LogInfo($"MNCLoaded = {MNCLoaded}");
        if (Chainloader.PluginInfos.TryGetValue(ManeuverNodeControllerMod.ModGuid, out MNC))
        {
            MNCLoaded = true;
            Logger.LogInfo("Maneuver Node Controller installed and available");
            Logger.LogInfo($"MNC = {MNC}");
        }
        else MNCLoaded = false;

        Logger.LogInfo($"MNCLoaded = {MNCLoaded}");

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

        DrawSectionHeader("Ownship Maneuvers");
        if (activeVessel.Orbit.eccentricity < 1)
        {
            DrawButton("Circularize at Ap", ref circAp);
        }
        DrawButton("Circularize at Pe", ref circPe);

        DrawButton("Circularize Now", ref circNow);

        DrawButtonWithTextField("New Pe", ref newPe, ref targetPeAStr, "m");
        if (double.TryParse(targetPeAStr, out targetPeR)) targetPeR += activeVessel.Orbit.referenceBody.radius;
        else targetPeR = 0;

        if (activeVessel.Orbit.eccentricity < 1)
        {
            DrawButtonWithTextField("New Ap", ref newAp, ref targetApAStr, "m");
            if (double.TryParse(targetApAStr, out targetApR)) targetApR += activeVessel.Orbit.referenceBody.radius;
            else targetApR = 0;

            DrawButtonWithDualTextField("New Pe & Ap", "New Ap & Pe", ref newPeAp, ref targetPeAStr1, ref targetApAStr1);
            if (double.TryParse(targetPeAStr1, out targetPeR1)) targetPeR1 += activeVessel.Orbit.referenceBody.radius;
            else targetPeR1 = 0;
            if (double.TryParse(targetApAStr1, out targetApR1)) targetApR1 += activeVessel.Orbit.referenceBody.radius;
            else targetApR1 = 0;
        }

        DrawButtonWithTextField("New Inclination", ref newInc, ref targetIncStr, "°");
        if (!double.TryParse(targetIncStr, out targetInc)) targetInc = 0;

        if (currentTarget != null)
        {
            // If the activeVessel and the currentTarget are both orbiting the same body
            if (currentTarget.Orbit.referenceBody.Name == activeVessel.Orbit.referenceBody.Name)
            {
                DrawSectionHeader("Maneuvers Relative to Target");
                DrawButton("Match Planes at AN", ref matchPlanesA);
                DrawButton("Match Planes at DN", ref matchPlanesD);

                DrawButton("Hohmann Xfer", ref hohmannT);

                if (experimental.Value)
                {
                    DrawButtonWithTextField("Intercept at Time", ref interceptAtTime, ref interceptTStr, "s");
                    try { interceptT = double.Parse(interceptTStr); }
                    catch { interceptT = 100; }
                    DrawButton("Course Correction", ref courseCorrection);
                    DrawButton("Match Velocity @CA", ref matchVCA);
                    DrawButton("Match Velocity Now", ref matchVNow);
                }
            }

            if (experimental.Value)
            {
                //if the currentTarget is a celestial body and it's in orbit around the same body that the activeVessel's parent body is orbiting
                if ((currentTarget.Name != activeVessel.Orbit.referenceBody.Name) && (currentTarget.Orbit.referenceBody.Name == activeVessel.Orbit.referenceBody.Orbit.referenceBody.Name))
                {
                    DrawSectionHeader("Interplanetary Maneuvers");
                    DrawButton("Interplanetary Transfer", ref planetaryXfer);
                }
            }
        }

        // If the activeVessle is at a moon (a celestial in orbit around another celestial that's not also a star)
        var referenceBody = activeVessel.Orbit.referenceBody;
        if (!referenceBody.referenceBody.IsStar && activeVessel.Orbit.eccentricity < 1)
        {
            DrawSectionHeader("Moon Specific Maneuvers");
            // DrawButton("Moon Return", ref moonReturn); // targetMRPeAAStr
            DrawButtonWithTextField("Moon Return", ref moonReturn, ref targetMRPeAStr, "m");
            if (double.TryParse(targetMRPeAStr, out targetMRPeR)) targetMRPeR += activeVessel.Orbit.referenceBody.radius;
            else targetMRPeR = 0;
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

    void BodySelectionGUI()
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
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void callMNC()
    {
        if (MNCLoaded && autoLaunchMNC.Value)
        {
            // Reflections method to attempt the same thing more cleanly
            var mncType = Type.GetType($"ManeuverNodeController.ManeuverNodeControllerMod, {ManeuverNodeControllerMod.ModGuid}");
            // Logger.LogDebug($"Type name: {mncType!.Name}");
            var instanceProperty = mncType!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            // Logger.LogDebug($"Property name: {instanceProperty!.Name}");
            var methodInfo = instanceProperty!.PropertyType.GetMethod("LaunchMNC");
            // Logger.LogDebug($"Method name: {methodInfo!.Name}");
            methodInfo!.Invoke(instanceProperty.GetValue(null), null);
        }
    }

    //private void CreateNodeAtUt(Vector3d burnVector, double UT, double burnDurationOffsetFactor = -0.5)
    //{
    //    if (NMLoaded)
    //    {
    //        // Reflections method to attempt the same thing more cleanly
    //        var nmType = Type.GetType($"NodeManager.NodeManagerPlugin, {NodeManagerPlugin.Instance.ModGuid}");
    //        Logger.LogDebug($"Type name: {nmType!.Name}");
    //        var instanceProperty = nmType!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
    //        Logger.LogDebug($"Property name: {instanceProperty!.Name}");
    //        var methodInfo = instanceProperty!.PropertyType.GetMethod("CreateManeuverNodeAtUT");
    //        Logger.LogDebug($"Method name: {methodInfo!.Name}");
    //        methodInfo!.Invoke(instanceProperty.GetValue(null), new object[] { burnVector, UT, burnDurationOffsetFactor });
    //    }
    //}

    //private void OpenMNCIfLoaded()
    //{
    //    ManeuverNodeControllerMod.Instance.LaunchMNC();
    //}

    private void handleButtons()
    {
        if (circAp || circPe || circNow|| newPe || newAp || newPeAp || newInc || matchPlanesA || matchPlanesD || hohmannT || interceptAtTime || courseCorrection || moonReturn || matchVCA || matchVNow || planetaryXfer)
        {
            Vector3d burnParams;
            double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
            // var orbit = GetLastOrbit() as PatchedConicsOrbit;
            var orbit = activeVessel.Orbit;

            if (circAp) // Working
            {
                Logger.LogDebug("Circularize at Ap");
                var TimeToAp = orbit.TimeToAp;
                var burnUT = UT + TimeToAp;
                var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT);

                status = Status.OK;
                statusText = "Ready to Circularize at Ap";
                statusTime = UT + statusPersistence.Value;

                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // TestPerturbedOrbit(orbit, burnUT, deltaV);

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
                    //if (burnParams.z < 0)
                    //{
                    //    burnParams.z *= -1;
                    //    Logger.LogDebug($"Solution Found: burnParams* [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT (* prograde flipped)");
                    //}
                    //else
                    //    Logger.LogDebug($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    //currentNode.BurnVector = burnParams;
                    //UpdateNode(currentNode);
                }
                else
                {
                    status = Status.ERROR;
                    statusText = "Circularize at Ap: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (circPe) // Working
            {
                Logger.LogDebug("Circularize at Pe");
                var TimeToPe = orbit.TimeToPe;
                var burnUT = UT + TimeToPe;
                var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT);
                
                status = Status.OK;
                statusText = "Ready to Circularize at Pe";
                statusTime = UT + statusPersistence.Value;

                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // TestPerturbedOrbit(orbit, burnUT, deltaV);

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
                    //if (burnParams.z > 0)
                    //{
                    //    burnParams.z *= -1;
                    //    burnParams.x *= -1;
                    //    Logger.LogDebug($"Solution Found: burnParams* [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT (* radial & prograde flipped)");
                    //}
                    //else
                    //    Logger.LogDebug($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    //currentNode.BurnVector = burnParams;
                    //UpdateNode(currentNode);
                }
                else
                {
                    status = Status.ERROR;
                    statusText = "Circularize at Pe: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (circNow) // Working
            {
                Logger.LogDebug("Circularize Now");
                var startTimeOffset = 60;
                var burnUT = UT + startTimeOffset;
                var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT);

                status = Status.OK;
                statusText = "Ready to Circularize Now"; // "Ready to Circularize Now"
                statusTime = UT + statusPersistence.Value;

                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // TestPerturbedOrbit(orbit, burnUT, deltaV);
                    // callMNC();

                    // Recalculate node based on the offset time
                    //var nodeTimeAdj = currentNode.BurnDuration / 2;
                    //var burnStartTime = currentNode.Time + nodeTimeAdj;
                    //Logger.LogDebug($"BurnDuration: {currentNode.BurnDuration}, Recalculating burn to be centered at {nodeTimeAdj + startTimeOffset} s from now ");
                    //deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnStartTime);
                    //burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnStartTime, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    ////var burnParamsT1 = orbit.DeltaVToManeuverNodeCoordinates(UT, deltaV);
                    ////var burnParamsT2 = orbit.DeltaVToManeuverNodeCoordinates(UT, activeVessel, deltaV);
                    ////Logger.LogDebug($"OG burnParams               [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    ////Logger.LogDebug($"Test: burnParamsT1          [{burnParamsT1.x}, {burnParamsT1.y}, {burnParamsT1.z}] m/s = {burnParamsT1.magnitude} m/s");
                    ////Logger.LogDebug($"Test: burnParamsT2          [{burnParamsT2.x}, {burnParamsT2.y}, {burnParamsT2.z}] m/s = {burnParamsT2.magnitude} m/s");
                    //burnParams.z *= -1;
                    //burnParams.x *= -1;
                    //Logger.LogDebug($"Solution Found: burnParams* [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT (* prograde and radial flipped)");
                    //currentNode.BurnVector = burnParams;
                    //UpdateNode(currentNode);
                }
                else
                {
                    status = Status.ERROR;
                    statusText = "Circularize Now: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (newPe) // Working
            {
                Logger.LogDebug("Set New Pe");
                Debug.Log("Set New Pe");
                var TimeToAp = orbit.TimeToAp;
                double burnUT, e;
                e = orbit.eccentricity;
                if (e < 1)
                    burnUT = UT + TimeToAp;
                else
                    burnUT = UT + 30;

                status = Status.OK;
                statusText = "Ready to Change Ap";
                statusTime = UT + statusPersistence.Value;
                
                Logger.LogDebug($"Seeking Solution: targetPeR {targetPeR} m, currentPeR {orbit.Periapsis} m, body.radius {orbit.referenceBody.radius} m");
                Debug.Log($"Seeking Solution: targetPeR {targetPeR} m, currentPeR {orbit.Periapsis} m, body.radius {orbit.referenceBody.radius} m");
                var deltaV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, burnUT, targetPeR);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // TestPerturbedOrbit(orbit, burnUT, deltaV);
                }
                else
                {
                    status = Status.ERROR;
                    statusText = "Set New Pe: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (newAp) // Working
            {
                Logger.LogDebug("Set New Ap");
                Debug.Log("Set New Ap");
                var TimeToPe = orbit.TimeToPe;
                var burnUT = UT + TimeToPe;

                status = Status.OK;
                statusText = "Ready to Change Ap";
                statusTime = UT + statusPersistence.Value;

                Logger.LogDebug($"Seeking Solution: targetApR {targetApR} m, currentApR {orbit.Apoapsis} m");
                var deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, burnUT, targetApR);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // TestPerturbedOrbit(orbit, burnUT, deltaV);
                }
                else
                {
                    status = Status.ERROR;
                    statusText = "Set New Ap: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (newPeAp) // Working: Not perfect, but pretty good results nevertheless
            {
                Logger.LogDebug("Set New Pe and Ap");

                status = Status.OK;
                statusText = "Experimental Ellipticize Ready"; // "Ready to Ellipticize";
                statusTime = UT + statusPersistence.Value;

                if (targetPeR1 > targetApR1)
                {
                    (targetPeR1, targetApR1) = (targetApR1, targetPeR1);
                    status = Status.WARNING;
                    statusText = "Pe Setting > Ap Setting";
                }

                Logger.LogDebug($"Seeking Solution: targetPeR {targetPeR1} m, targetApR {targetApR1} m, body.radius {orbit.referenceBody.radius} m");
                var burnUT = UT + 30;
                var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(orbit, burnUT, targetPeR1, targetApR1);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // TestPerturbedOrbit(orbit, burnUT, deltaV);
                    callMNC();
                }
                else
                {
                    status = Status.ERROR;
                    statusText = "Set New Pe and Ap: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (newInc) // Working
            {
                Logger.LogDebug("Set New Inclination");
                Logger.LogDebug($"Seeking Solution: targetInc {targetInc}°");
                double burnUT, TAN, TDN;
                Vector3d deltaV, deltaV1, deltaV2;

                status = Status.OK;
                statusText = "Ready to Change Inclination";
                statusTime = UT + statusPersistence.Value;

                if (orbit.eccentricity < 1) // Eliptical orbit: Pick cheapest deltaV between AN and DN
                {
                    TAN = orbit.TimeOfAscendingNodeEquatorial(UT);
                    TDN = orbit.TimeOfDescendingNodeEquatorial(UT);
                    deltaV1 = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, TAN, targetInc);
                    deltaV2 = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, TDN, targetInc);
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
                    burnUT = game.UniverseModel.UniversalTime + 30;
                    deltaV = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, burnUT, targetInc);
                }
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV2, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // TestPerturbedOrbit(orbit, burnUT, deltaV);
                }
                else
                {
                    status = Status.ERROR;
                    statusText = "Set New Inclination: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (matchPlanesA) // Working
            {
                Logger.LogDebug($"Match Planes  with {currentTarget.Name} at AN");
                double burnUT;
 
                status = Status.OK;
                statusText = $"Ready to Match Planes with {currentTarget.Name} at AN";
                statusTime = UT + statusPersistence.Value;

                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // TestPerturbedOrbit(orbit, burnUT, deltaV);
                }
                else
                {
                    status = Status.ERROR;
                    statusText = $"Match Planes with {currentTarget.Name} at AN: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (matchPlanesD) // Working
            {
                Logger.LogDebug($"Match Planes with {currentTarget.Name} at DN");
                double burnUT;

                status = Status.OK;
                statusText = $"Ready to Match Planes with {currentTarget.Name} at DN";
                statusTime = UT + statusPersistence.Value;

                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
                burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                if (deltaV != Vector3d.zero)
                {
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // TestPerturbedOrbit(orbit, burnUT, deltaV);
                }
                else
                {
                    status = Status.ERROR;
                    statusText = $"Match Planes with {currentTarget.Name} at DN: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (hohmannT) // Works if we start in a good enough orbit (reasonably circular, close to target's orbital plane)
            {
                Logger.LogDebug($"Hohmann Transfer to {currentTarget.Name}");
                Debug.Log("Hohmann Transfer");
                double burnUT;

                status = Status.WARNING;
                statusText = $"Ready to Transfer to {currentTarget.Name}";
                statusTime = UT + statusPersistence.Value;

                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    TestPerturbedOrbit(orbit, burnUT, deltaV);
                    callMNC();
                }
                else
                {
                    status = Status.ERROR;
                    statusText = $"Hohmann Transfer to {currentTarget.Name}: Solution Not Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (interceptAtTime) // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
            // Adapted from call found in MechJebModuleScriptActionRendezvous.cs for "Get Closer"
            // Similar to code in MechJebModuleRendezvousGuidance.cs for "Get Closer" button code.
            {
                Logger.LogDebug($"Intercept {currentTarget.Name} at Time");
                var burnUT = UT + 30;
                var interceptUT = UT + interceptT;
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
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {interceptUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    TestPerturbedOrbit(orbit, burnUT, deltaV);
                    callMNC();
                }
                else
                {
                    status = Status.ERROR;
                    statusText = $"Intercept {currentTarget.Name}: No Solution Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (courseCorrection) // Experimental Works at least some times...
            {
                Logger.LogDebug($"Course Correction for tragetory to {currentTarget.Name}");
                double burnUT;
                Vector3d deltaV;

                status = Status.WARNING;
                statusText = "Experimental Course Correction Ready"; // "Ready for Course Correction Burn";
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
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    TestPerturbedOrbit(orbit, burnUT, deltaV);
                    callMNC();
                }
                else
                {
                    status = Status.ERROR;
                    statusText = $"Course Correction for tragetory to {currentTarget.Name}: No Solution Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (moonReturn) // Works at least sometimes...
            {
                Logger.LogDebug("Moon Return");
                var e = orbit.eccentricity;

                status = Status.WARNING;
                statusText = $"Experimental Return from {orbit.referenceBody.Name} Ready"; // $"Ready to Return from {orbit.referenceBody.Name}";
                statusTime = UT + statusPersistence.Value;

                if (e > 0.2)
                {
                    status = Status.WARNING;
                    statusText = "Moon Return: Starting Orbit Eccentrity Too Large";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                    Logger.LogError($"Moon Return: Starting orbit eccentricty {e.ToString("F2")} is > 0.2");
                }
                else
                {
                    double burnUT;
                    // double primaryRaidus = orbit.referenceBody.Orbit.referenceBody.radius + 100000; // m
                    Logger.LogDebug($"Moon Return Attempting to Solve...");
                    var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(orbit, UT, targetMRPeR, out burnUT);
                    if (deltaV != Vector3d.zero)
                    {
                        burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                        Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                        Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                        NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                        TestPerturbedOrbit(orbit, burnUT, deltaV);
                        callMNC();
                    }
                    else
                    {
                        status = Status.ERROR;
                        statusText = "Moon Return: No Solution Found!";
                        statusTime = UT + statusPersistence.Value;
                        Logger.LogDebug(statusText);
                    }
                }
            }
            else if (matchVCA) // Experimental
            {
                Logger.LogDebug($"Match Velocity with {currentTarget.Name} at Closest Approach");

                status = Status.WARNING;
                statusText = $"Experimental Velocity Match with {currentTarget.Name} Ready"; // $"Ready to Match Velocity with {currentTarget.Name}";
                statusTime = UT + statusPersistence.Value;

                double closestApproachTime = orbit.NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT + 2); //+2 so that closestApproachTime is definitely > UT
                var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, closestApproachTime, currentTarget.Orbit as PatchedConicsOrbit);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(closestApproachTime, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, closestApproachTime);
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s {closestApproachTime - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {closestApproachTime - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, closestApproachTime, -0.5);
                    TestPerturbedOrbit(orbit, closestApproachTime, deltaV);
                    callMNC();
                }
                else
                {
                    status = Status.ERROR;
                    statusText = $"Match Velocity with {currentTarget.Name} at Closest Approach: No Solution Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (matchVNow) // Experimental
            {
                Logger.LogDebug($"Match Velocity with {currentTarget.Name} Now");
                var burnUT = UT + 30;

                status = Status.WARNING;
                statusText = $"Experimental Velocity Match with {currentTarget.Name} Ready"; // $"Ready to Match Velocity with {currentTarget.Name}";
                statusTime = UT + statusPersistence.Value;

                var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, burnUT, currentTarget.Orbit as PatchedConicsOrbit);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    TestPerturbedOrbit(orbit, burnUT, deltaV);
                    callMNC();
                }
                else
                {
                    status = Status.ERROR;
                    statusText = $"Match Velocity with {currentTarget.Name} Now: No Solution Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
            else if (planetaryXfer) // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
            {
                Logger.LogDebug($"Planetary Transfer to {currentTarget.Name}");
                double burnUT;
                bool syncPhaseAngle = true;

                status = Status.WARNING;
                statusText = $"Experimental Transfer to {currentTarget.Name} Ready"; // $"Ready to depart for {currentTarget.Name}";
                statusTime = UT + statusPersistence.Value;

                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, syncPhaseAngle, out burnUT);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    // burnParams.z *= -1;
                    Logger.LogDebug($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogDebug($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    TestPerturbedOrbit(orbit, burnUT, deltaV);
                    callMNC();
                }
                else
                {
                    status = Status.ERROR;
                    statusText = $"Planetary Transfer to {currentTarget.Name}: No Solution Found!";
                    statusTime = UT + statusPersistence.Value;
                    Logger.LogDebug(statusText);
                }
            }
        }
    }

    private void TestPerturbedOrbit(PatchedConicsOrbit o, double burnUT, Vector3d dV)
    {
        // This code compares the orbit info returned from a PerturbedOrbit orbit call with the
        // info for the orbit in the next patch. It should be called after creating a maneuver
        // node for the active vessel that applies the burn vector associated with the dV to
        // make sure that PerturbedOrbit is correctly predicting the effect of delta V on the
        // current orbit.

        List<ManeuverNodeData> patchList = 
            Game.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.SimulationObject.GlobalId);

        Logger.LogDebug($"TestPerturbedOrbit: patchList.Count = {patchList.Count}");

        if (patchList.Count == 0)
        {
            Logger.LogDebug($"TestPerturbedOrbit: No future patches to compare to.");
            return;
        }

        PatchedConicsOrbit hypotheticalOrbit = o.PerturbedOrbit(burnUT, dV);
        IPatchedOrbit nextOrbit = patchList[0].ManeuverTrajectoryPatch;

        Logger.LogDebug($"thisOrbit:{o}");
        Logger.LogDebug($"nextOrbit:{nextOrbit}");
        Logger.LogDebug($"hypotheticalOrbit:{hypotheticalOrbit}");
    }

    //private IEnumerator RefreshNodes(ManeuverPlanComponent maneuverPlanComponent)
    //{
    //    // yield return (object)new WaitForFixedUpdate();

    //    for (int i = 0; i < Nodes.Count; i++) // was i = SelectedNodeIndex
    //    {
    //        // Logger.LogDebug($"RefreshNodes: Updateing Node {i}");
    //        var node = Nodes[i];
    //        // maneuverPlanComponent.UpdateTimeOnNode(node, node.Time);
    //        maneuverPlanComponent.UpdateNodeDetails(node);
    //        //yield return (object)new WaitForFixedUpdate();
    //        //maneuverPlanComponent.RefreshManeuverNodeState(i);
    //    }

    //    for (int i = 0; i < Nodes.Count; i++) // was i = SelectedNodeIndex
    //    {
    //        // Logger.LogDebug($"RefreshNodes: Refreshing Node {i}");
    //        try { maneuverPlanComponent.RefreshManeuverNodeState(i); }
    //        catch (NullReferenceException e)
    //        {
    //            Logger.LogError($"RefreshNodes: Suppressed NRE for Node {i}: {e}");
    //            Logger.LogError($"RefreshNodes: Node {i}: {FPNodeControl.Nodes[i]}");
    //        }
    //    }

    //    yield return (object)new WaitForFixedUpdate();
    //    // NodeControl.RefreshManeuverNodes();
    //    // yield return (object)new WaitForFixedUpdate();

    //    for (int i = 0; i < Nodes.Count; i++) // was i = SelectedNodeIndex
    //    {
    //        // Logger.LogDebug($"RefreshNodes: Updateing Node {i}");
    //        var node = Nodes[i];
    //        // maneuverPlanComponent.UpdateTimeOnNode(node, node.Time);
    //        maneuverPlanComponent.UpdateNodeDetails(node);
    //        //yield return (object)new WaitForFixedUpdate();
    //        //maneuverPlanComponent.RefreshManeuverNodeState(i);
    //    }

    //    for (int i = 0; i < Nodes.Count; i++) // was i = SelectedNodeIndex
    //    {
    //        // Logger.LogDebug($"RefreshNodes: Refreshing Node {i}");
    //        try { maneuverPlanComponent.RefreshManeuverNodeState(i); }
    //        catch (NullReferenceException e)
    //        {
    //            Logger.LogError($"RefreshNodes: Suppressed NRE for Node {i}: {e}");
    //            Logger.LogError($"RefreshNodes: Node {i}: {Nodes[i]}");
    //        }
    //    }

    //    // yield return (object)new WaitForFixedUpdate();

    //    // NodeControl.RefreshManeuverNodes();
    //}

    //private IPatchedOrbit GetLastOrbit(bool silent = true)
    //{
    //    // Logger.LogDebug("GetLastOrbit");
    //    List<ManeuverNodeData> patchList =
    //        Game.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.SimulationObject.GlobalId); // GetNodesForVessel(kspVessel.GetGlobalIDActiveVessel())

    //    if (!silent)
    //        Logger.LogDebug($"GetLastOrbit: patchList.Count = {patchList.Count}");

    //    if (patchList.Count == 0)
    //    {
    //        if (!silent)
    //            Logger.LogDebug($"GetLastOrbit: last orbit is activeVessel.Orbit: {activeVessel.Orbit}");
    //        return activeVessel.Orbit;
    //    }
    //    IPatchedOrbit lastOrbit = patchList[patchList.Count - 1].ManeuverTrajectoryPatch;
    //    if (!silent)
    //    {
    //        Logger.LogDebug($"GetLastOrbit: ManeuverTrajectoryPatch = {patchList[patchList.Count - 1].ManeuverTrajectoryPatch}");
    //        Logger.LogDebug($"GetLastOrbit: last orbit is patch {patchList.Count - 1}: {lastOrbit}");
    //    }


    //    return lastOrbit;
    //}

    //private void CreateManeuverNodeAtTA(Vector3d burnVector, double TrueAnomalyRad, double burnDurationOffsetFactor = -0.5)
    //{
    //    // Logger.LogDebug("CreateManeuverNodeAtTA");
    //    PatchedConicsOrbit referencedOrbit = GetLastOrbit(false) as PatchedConicsOrbit;
    //    if (referencedOrbit == null)
    //    {
    //        Logger.LogError("CreateManeuverNode: referencedOrbit is null!");
    //        return;
    //    }

    //    double UT = referencedOrbit.GetUTforTrueAnomaly(TrueAnomalyRad, 0);

    //    CreateManeuverNodeAtUT(burnVector, UT, burnDurationOffsetFactor);
    //}

    //private void CreateManeuverNodeAtUT(Vector3d burnVector, double burnUT, double burnDurationOffsetFactor = -0.5)
    //{
    //    // Logger.LogDebug("CreateManeuverNodeAtUT");

    //    //PatchedConicsOrbit referencedOrbit = GetLastOrbit(false) as PatchedConicsOrbit;
    //    //if (referencedOrbit == null)
    //    //{
    //    //    Logger.LogError("CreateManeuverNode: referencedOrbit is null!");
    //    //    return;
    //    //}

    //    double UT = game.UniverseModel.UniversalTime;
    //    if (burnUT < UT + 1) // Don't set node to now or in the past
    //        burnUT = UT + 1;

    //    // KSPOrbitModule.IOrbit orbit = new OrbitWrapper(vesselAdapter.context, vesselAdapter.vessel.Orbiter.PatchedConicSolver.FindPatchContainingUT(ut) ?? vesselAdapter.vessel.Orbit);

    //    // Get the current list of nodes
    //    ManeuverPlanComponent activeVesselPlan = activeVessel?.SimulationObject?.FindComponent<ManeuverPlanComponent>();
    //    List<ManeuverNodeData> Nodes = new();
    //    if (activeVesselPlan != null)
    //    {
    //        Nodes = activeVesselPlan.GetNodes();
    //    }


    //    // Get the patch to put this node on
    //    ManeuverPlanSolver maneuverPlanSolver = activeVessel.Orbiter?.ManeuverPlanSolver;
    //    IPatchedOrbit orbit = activeVessel.Orbit;
    //    // maneuverPlanSolver.FindPatchContainingUt(UT, maneuverPlanSolver.ManeuverTrajectory, out orbit, out int _);
    //    // var selectedNode = -1;
    //    for (int i = 0; i < Nodes.Count - 1; i++)
    //    {
    //        if (burnUT > Nodes[i].Time && burnUT < Nodes[i + 1].Time)
    //        {
    //            orbit = Nodes[i + 1].ManeuverTrajectoryPatch;
    //            // selectedNode = i;
    //            Logger.LogDebug($"CreateManeuverNodeAtUT: Attaching node to Node[{i + 1}]'s ManeuverTrajectoryPatch");
    //        }
    //    }

    //    // Build the node data
    //    // ManeuverNodeData nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, false, UT);
    //    ManeuverNodeData nodeData;
    //    if (Nodes.Count == 0) // There are no nodes
    //    {
    //        nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, false, burnUT);
    //    }
    //    else
    //    {
    //        if (UT < Nodes[0].Time) // request time is before the first node
    //        {
    //            nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, false, burnUT);
    //            orbit.PatchEndTransition = PatchTransitionType.Maneuver;
    //        }
    //        else if (UT > Nodes[Nodes.Count - 1].Time) // requested time is after the last node
    //        {
    //            nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, true, burnUT);
    //            orbit.PatchEndTransition = PatchTransitionType.Final;
    //        }
    //        else // request time is between existing nodes
    //        {
    //            nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, true, burnUT);
    //            orbit.PatchEndTransition = PatchTransitionType.Maneuver;
    //        }
    //        orbit.PatchStartTransition = PatchTransitionType.EndThrust;

    //        nodeData.SetManeuverState((PatchedConicsOrbit)orbit);
    //    }

    //    //IPatchedOrbit orbit = referencedOrbit;

    //    //orbit.PatchStartTransition = PatchTransitionType.Maneuver;
    //    //orbit.PatchEndTransition = PatchTransitionType.Final;

    //    //nodeData.SetManeuverState((PatchedConicsOrbit)orbit);

    //    nodeData.BurnVector = burnVector;

    //    //Logger.LogDebug($"CreateManeuverNodeAtUT: BurnVector [{burnVector.x}, {burnVector.y}, {burnVector.z}] m/s");
    //    //Logger.LogDebug($"CreateManeuverNodeAtUT: BurnDuration {nodeData.BurnDuration} s");
    //    //Logger.LogDebug($"CreateManeuverNodeAtUT: Burn Time {nodeData.Time} s");

    //    AddManeuverNode(nodeData, burnDurationOffsetFactor);
    //}

    //private void AddManeuverNode(ManeuverNodeData nodeData, double burnDurationOffsetFactor)
    //{
    //    //Logger.LogDebug("AddManeuverNode");

    //    // Working this was we need to call maneuverPlan.AddNode & ManeuverPlanSolver.UpdateManeuverTrajectory
    //    ManeuverPlanComponent maneuverPlan;
    //    maneuverPlan = activeVessel.SimulationObject.ManeuverPlan;
    //    maneuverPlan.AddNode(nodeData, true);
    //    activeVessel.Orbiter.ManeuverPlanSolver.UpdateManeuverTrajectory();

    //    // For KSP2, We want the to start burns early to make them centered on the node
    //    var nodeTimeAdj = nodeData.BurnDuration * burnDurationOffsetFactor;

    //    Logger.LogDebug($"AddManeuverNode: BurnDuration {nodeData.BurnDuration} s");

    //    // Refersh the currentNode with what we've produced here in prep for calling UpdateNode
    //    currentNode = getCurrentNode();

    //    UpdateNode(nodeData, nodeTimeAdj);

    //    //Logger.LogDebug("AddManeuverNode Done");
    //}

    //private void AddManeuverNodeToVessel(ManeuverNodeData nodeData, double burnDurationOffsetFactor)
    //{
    //    //Logger.LogDebug("AddManeuverNode");

    //    // Working this was we only need to call Maneuvers.AddNodeToVessel
    //    GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);

    //    // For KSP2, We want the to start burns early to make them centered on the node
    //    var nodeTimeAdj = nodeData.BurnDuration * burnDurationOffsetFactor;

    //    Logger.LogDebug($"AddManeuverNode: BurnDuration {nodeData.BurnDuration} s");

    //    // Refersh the currentNode with what we've produced here in prep for calling UpdateNode
    //    currentNode = getCurrentNode();

    //    UpdateNode(nodeData, nodeTimeAdj);

    //    //Logger.LogDebug("AddManeuverNode Done");
    //}

    //private void UpdateNode(ManeuverNodeData nodeData, double nodeTimeAdj = 0) // was: return type IEnumerator
    //{
    //    MapCore mapCore = null;
    //    Game.Map.TryGetMapCore(out mapCore);
    //    var m3d = mapCore.map3D;
    //    var maneuverManager = m3d.ManeuverManager;
    //    IGGuid simID;
    //    SimulationObjectModel simObject;

    //    // Get the ManeuverPlanComponent for the active vessel
    //    var universeModel = game.UniverseModel;
    //    VesselComponent vesselComponent;
    //    ManeuverPlanComponent maneuverPlanComponent;
    //    if (currentNode != null)
    //    {
    //        simID = currentNode.RelatedSimID;
    //        simObject = universeModel.FindSimObject(simID);
    //    }
    //    else
    //    {
    //        // vc2 = activeVessel;
    //        vesselComponent = activeVessel;
    //        simObject = vesselComponent?.SimulationObject;
    //    }

    //    if (simObject != null)
    //    {
    //        maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();
    //    }
    //    else
    //    {
    //        simObject = universeModel.FindSimObject(simID);
    //        maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();
    //    }

    //    if (nodeTimeAdj != null) // was: 0
    //    {
    //        nodeData.Time += nodeTimeAdj;
    //        if (nodeData.Time < game.UniverseModel.UniversalTime + 1) // Don't set node in the past
    //            nodeData.Time = game.UniverseModel.UniversalTime + 1;
    //        maneuverPlanComponent.UpdateTimeOnNode(nodeData, nodeData.Time); // This may not be necessary?
    //    }

    //    // Wait a tick for things to get created
    //    // yield return new WaitForFixedUpdate();

    //    try { maneuverPlanComponent.RefreshManeuverNodeState(0); }
    //    catch (NullReferenceException e) { Logger.LogError($"UpdateNode: caught NRE on call to maneuverPlanComponent.RefreshManeuverNodeState(0): {e}"); }

    //    if (currentNode != null) // just don't do it... was: if (currentNode != null)
    //    {
    //        // Manage the maneuver on the map
    //        maneuverManager.RemoveAll();
    //        try { maneuverManager?.GetNodeDataForVessels(); }
    //        catch (Exception e) { Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels(): {e}"); }
    //        try { maneuverManager.UpdateAll(); }
    //        catch (Exception e) { Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll(): {e}"); }
    //        try { maneuverManager.UpdatePositionForGizmo(nodeData.NodeID); }
    //        catch (Exception e) { Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo(): {e}"); }
    //    }
    //}
}
