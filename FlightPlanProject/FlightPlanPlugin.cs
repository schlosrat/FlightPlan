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
// using ManeuverNodeController;
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

using FlightPlan.Tools;
using FlightPlan.UI;

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
    //public List<String> inputFields = new List<String>();

    // GUI stuff
    static bool loaded = false;
    private bool interfaceEnabled = false;
    private bool GUIenabled = true;
    private Rect windowRect = Rect.zero;
    private int windowWidth = 250; //384px on 1920x1080
  //  private int windowHeight = Screen.height / 4; //360px on 1920x1080


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
    private ConfigEntry<double> statusPersistence;
    private ConfigEntry<double> statusFadeTime;
    private ConfigEntry<bool> experimental;
    private ConfigEntry<bool> autoLaunchMNC;

    // Button bools
    private bool circAp, circPe, circNow, newPe, newAp, newPeAp, newInc, matchPlanesA, matchPlanesD, hohmannT, interceptAtTime, courseCorrection, moonReturn, matchVCA, matchVNow, planetaryXfer;

    // Dictionaries used for toggle button management to function like radio buttons. If no "radio buttons", then this can go.
    private Dictionary<string, bool> _toggles = new();
    private Dictionary<string, bool> _previousToggles = new();
    private readonly Dictionary<string, bool> _initialToggles = new()
    {
        { "Circularize",      false },
        { "SetNewAp",         false },
        { "SetNewPe",         false },
        { "Elipticize",       false },
        { "SetNewInc",        false },
        { "SetNewLAN",        false },
        { "MatchPlane",       false },
        { "MatchVelocity",    false },
        { "CourseCorrection", false },
        { "HohmannTransfer",  false },
        { "InterceptTgt",     false },
        { "MoonReturn",       false },
        { "PlanetaryXfer",    false }
    };

    // Time references for selectedOption
    private readonly Dictionary<string, string> TimeReference = new()
    {
        { "COMPUTED",          "At Optimum Time"               }, //at the optimum time
        { "APOAPSIS",          "At Next Apoapsis"              }, //"at the next apoapsis"
        { "CLOSEST_APPROACH",  "At Closest Approach to Target" }, //"at closest approach to target"
        { "EQ_ASCENDING",      "At Equatorial AN"              }, //"at the equatorial AN"
        { "EQ_DESCENDING",     "At Equatorial DN"              }, //"at the equatorial DN"
        { "PERIAPSIS",         "At Next Periapsis"             }, //"at the next periapsis"
        { "REL_ASCENDING",     "At Next AN with Target"        }, //"at the next AN with the target."
        { "REL_DESCENDING",    "At Next DN with Target"        }, //"at the next DN with the target."
        { "X_FROM_NOW",        "After a Fixed Time"            }, //"after a fixed time"
        { "ALTITUDE",          "At an Altitude"                }, //"at an altitude"
        { "EQ_NEAREST_AD",     "At Nearest Equatorial AN/DN"   }, //"at the nearest equatorial AN/DN"
        { "EQ_HIGHEST_AD",     "At Cheapest Equatorial AN/DN"  }, //"at the cheapest equatorial AN/DN"
        { "REL_NEAREST_AD",    "At Nearest AN/DN with Target"  }, //"at the nearest AN/DN with the target"
        { "REL_HIGHEST_AD",    "At Cheapest AN/DN with Target" } //"at the cheapest AN/DN with the target"
    };

    // Body selection.
    private bool selectingBody = false;
    private static Vector2 scrollPositionBodies;

    // Option selection
    private bool selectingOption = false;
    private static Vector2 scrollPositionOptions;
    private string selectedOption = null;
    private List<string> options;
    private double requestedBurnTime = 0;

    // mod-wide data
    private VesselComponent activeVessel;
    private SimulationObjectModel currentTarget;
    public ManeuverNodeData currentNode = null;
    List<ManeuverNodeData> activeNodes;

    // Radius Computed from Inputs
    private double targetPeR;
    private double targetApR;
    private double targetMRPeR;




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

        FPSettings.Init(SettingsPath);

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
        //inputFields.Add("New Pe");
        //inputFields.Add("New Ap");
        //inputFields.Add("New Pe & Ap");
        //inputFields.Add("New Ap & Pe"); // kludgy name for the second input in a two input line
        //inputFields.Add("New Inclination");
        //inputFields.Add("Intercept at Time");
        //inputFields.Add("Select Target");

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
    
        // Set the initial and Default values based on config parameters. These don't make sense to need live update, so there're here instead of useing the configParam.Value elsewhere
        statusText     = initialStatusText.Value;
       
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
  
    }

    void Update()
    {
        // Logger = base.Logger;
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            ToggleButton(!interfaceEnabled);
            Logger.LogInfo("UI toggled with hotkey");
        }
    }

    void save_rect_pos()
    {
        FPSettings.window_x_pos = (int)windowRect.xMin;
        FPSettings.window_y_pos = (int)windowRect.yMin;
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
            FlightPlan.UI.UIWindow.check_main_window_pos(ref windowRect);
            GUI.skin = Skins.ConsoleSkin;

            windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                windowRect,
                FillWindow,
                "<color=#696DFF>FLIGHT. PLAN</color>",
                FPStyles.window,
                GUILayout.Height(0),
                GUILayout.Width(windowWidth));

            save_rect_pos();
            // Draw the tool tip if needed
            ToolTipsManager.DrawToolTips();
            // check editor focus and unset Input if needed
            UI_Fields.CheckEditor();

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
        if ( TopButtons.IconButton(FPStyles.cross))
            CloseWindow();

        GUI.Label(new Rect(9, 2, 29, 29), FPStyles.icon, FPStyles.icons_label);
        if (selectingBody)
        {
            selectBodyUI();
            return;
        }

        // game = GameManager.Instance.Game;
        //activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        //currentNode = (activeNodes.Count() > 0) ? activeNodes[0] : null;
        FPUtility.RefreshActiveVesselAndCurrentManeuver();
        // FPNodeControl.RefreshManeuverNodes();
        currentNode = getCurrentNode();

        BodySelectionGUI();

        var referenceBody = activeVessel.Orbit.referenceBody;

        DrawSectionHeader("Ownship Maneuvers");

        UI_Tools.Label("Circularize");
        GUILayout.BeginHorizontal();
        if (activeVessel.Orbit.eccentricity < 1)
        {
            DrawButton("at Ap", ref circAp);
        }
        DrawButton("at Pe", ref circPe);

        DrawButton("Now", ref circNow);
        GUILayout.EndHorizontal();

        FPSettings.pe_altitude_km = DrawButtonWithTextField("New Pe", ref newPe, FPSettings.pe_altitude_km, "km");
        targetPeR = FPSettings.pe_altitude_km * 1000 + referenceBody.radius;

        if (activeVessel.Orbit.eccentricity < 1)
        {
            FPSettings.ap_altitude_km =  DrawButtonWithTextField("New Ap", ref newAp, FPSettings.ap_altitude_km, "km");
            targetApR = FPSettings.ap_altitude_km*1000 + referenceBody.radius;

            DrawButton("New Pe & Ap", ref newPeAp);
        }

        FPSettings.target_inc_deg = DrawButtonWithTextField("New Inclination", ref newInc, FPSettings.target_inc_deg, "°");

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
                        FPSettings.interceptT = DrawButtonWithTextField("Intercept at Time", ref interceptAtTime, FPSettings.interceptT, "s");
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

                var parentPlanet = referenceBody.Orbit.referenceBody;
                // DrawButton("Moon Return", ref moonReturn); // targetMRPeAAStr
                FPSettings.mr_altitude_km = DrawButtonWithTextField("Moon Return", ref moonReturn, FPSettings.mr_altitude_km, "km");
                targetMRPeR = FPSettings.mr_altitude_km * 1000 + parentPlanet.radius;
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

    private void CloseWindow()
    {
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
        interfaceEnabled = false;
        Logger.LogDebug("CloseWindow: Restoring Game Input on window close.");
        // game.Input.Flight.Enable();
        GameManager.Instance.Game.Input.Enable();
        ToggleButton(interfaceEnabled);

        UI_Fields.GameInputState = true;
    }

    

    void selectBodyUI()
    {
        //bodies = GameManager.Instance.Game.SpaceSimulation.GetBodyNameKeys().ToList();

        CelestialBodyComponent root_body = activeVessel.mainBody;
        while(root_body.referenceBody != null)
        {
            root_body = root_body.referenceBody;
        }

        void listSubBodies(CelestialBodyComponent body, int level)
        {
            foreach (CelestialBodyComponent sub in body.orbitingBodies)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(level * 30);
                if (UI_Tools.ListButton(sub.Name))
                {
                    selectingBody = false;
                    activeVessel.SetTargetByID(sub.GlobalId);
                    currentTarget = activeVessel.TargetObject;
                }
               
                GUILayout.EndHorizontal();
                listSubBodies(sub, level + 1);
            }
        }

      //  bodies = GameManager.Instance.Game.SpaceSimulation.GetAllObjectsWithComponent<CelestialBodyComponent>();

        GUILayout.BeginHorizontal();
        UI_Tools.Label("Select target ");
        if (UI_Tools.SmallButton("Cancel"))
        {
            selectingBody = false;
        }
        GUILayout.EndHorizontal();

        UI_Tools.Separator();

        //GUI.SetNextControlName("Select Target");
        scrollPositionBodies = UI_Tools.BeginScrollView(scrollPositionBodies, 300);

        listSubBodies(root_body, 0);

        GUILayout.EndScrollView();
    }

    private void BodySelectionGUI()
    {
        //string baseName = "Select Target";

        string tgtName;
        if (currentTarget == null)
            tgtName = "None";
        else
            tgtName = currentTarget.Name;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Target : ");

        if (UI_Tools.SmallButton(tgtName))
            selectingBody = true;
        
        GUILayout.EndHorizontal();
    }

    int spacingAfterHeader = 5;
    int spacingAfterEntry = 5;

    private void DrawSectionHeader(string sectionName, string value = "", GUIStyle valueStyle = null) // was (string sectionName, ref bool isPopout, string value = "")
    {
        if (valueStyle == null) valueStyle = FPStyles.label;
        GUILayout.BeginHorizontal();
        // Don't need popout buttons for ROC
        // isPopout = isPopout ? !CloseButton() : UI_Tools.SmallButton("⇖", popoutBtnStyle);

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

    private void DrawButton(string buttonStr, ref bool button)
    {
        button = UI_Tools.Button(buttonStr);
    }

    private double DrawButtonWithTextField(string entryName, ref bool button, double value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        button = UI_Tools.Button(entryName);
        GUILayout.Space(10);

        value = UI_Fields.DoubleField(entryName, value);

        GUILayout.Space(3);
        UI_Tools.Label(unit);
        GUILayout.EndHorizontal();

        GUILayout.Space(spacingAfterEntry);
        return value;
    }

    public OtherModsInterface other_mods = null;

    private void DrawGUIStatus(double UT)
    {
        // Indicate status of last GUI function
        float transparency = 1;
        if (UT > statusTime) transparency = (float)MuUtils.Clamp(1 - (UT - statusTime) / statusFadeTime.Value, 0, 1);

        var status_style = FPStyles.label;
        if (status == Status.VIRGIN)
            status_style = FPStyles.label;
        if (status == Status.OK)
            status_style = FPStyles.phase_ok;
        if (status == Status.WARNING)
            status_style = FPStyles.phase_warning;
        if (status == Status.ERROR)
            status_style = FPStyles.phase_error;

        UI_Tools.Separator();
        DrawSectionHeader("Status:", statusText, status_style);

        // Indication to User that its safe to type, or why vessel controls aren't working

        if (other_mods == null)
        {
            // init mode detection only when first needed
            other_mods = new OtherModsInterface();
            other_mods.CheckModsVersions();
        }

        other_mods.OnGUI(currentNode);
        GUILayout.Space(spacingAfterEntry);
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
        if (circAp || circPe || circNow|| newPe || newAp || newPeAp || newInc || matchPlanesA || matchPlanesD || hohmannT || interceptAtTime || courseCorrection || moonReturn || matchVCA || matchVNow || planetaryXfer )
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
                pass = Ellipticize(targetApR, targetPeR, - 0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newInc) // Working
            {
                pass = SetInclination(FPSettings.target_inc_deg, -0.5);
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
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (interceptAtTime) // Experimental
            {
                pass = InterceptTgtAtUT(FPSettings.interceptT, -0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (courseCorrection) // Experimental Works at least some times...
            {
                pass = CourseCorrection(-0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (moonReturn) // Works - but may give poor Pe, including potentially lithobreaking
            {
                pass = MoonReturn(-0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (matchVCA) // Experimental
            {
                pass = MatchVelocityAtCA(-0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (matchVNow) // Experimental
            {
                pass = MatchVelocityNow(-0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (planetaryXfer) // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
            {
                pass = PlanetaryXfer(-0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
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
