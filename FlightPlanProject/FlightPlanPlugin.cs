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

    // Control game input state while user has clicked into a TextField.
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
    public double statusTime = 0; // UT of last staus update
    public string statusText;
    public string maneuver;
    private string baseManeuver;

    // Config parameters
    private ConfigEntry<string> initialStatusText;
    private ConfigEntry<double> statusPersistence;
    private ConfigEntry<double> statusFadeTime;
    private ConfigEntry<bool> experimental;
    private ConfigEntry<bool> autoLaunchMNC;

    // Button bools
    private bool circularize, newPe, newAp, newPeAp, newInc, newLAN, newNodeLon, newSMA; // Ownship maneuvers (activity toggels)
    private bool matchPlane, hohmannXfer, interceptTgt, courseCorrection, matchVelocity; // Maneuvers relative to target (activity toggels)
    private bool moonReturn, planetaryXfer; // Specialized Moon/Planet relative maneuvers (activity toggels)

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
        { "SetNodeLongitude", false },
        { "SetNewSMA",        false },
        { "MatchPlane",       false },
        { "MatchVelocity",    false },
        { "CourseCorrection", false },
        { "HohmannTransfer",  false },
        { "InterceptTgt",     false },
        { "MoonReturn",       false },
        { "PlanetaryXfer",    false }
    };

    // Time references for selectedOption
    public readonly Dictionary<string, string> TimeReference = new()
    {
        { "COMPUTED",          "at optimum time"         }, //at the optimum time
        { "APOAPSIS",          "at next apoapsis"        }, //"at the next apoapsis"
        { "CLOSEST_APPROACH",  "at closest approach"     }, //"at closest approach to target"
        { "EQ_ASCENDING",      "at equatorial AN"        }, //"at the equatorial AN"
        { "EQ_DESCENDING",     "at equatorial DN"        }, //"at the equatorial DN"
        { "PERIAPSIS",         "at next periapsis"       }, //"at the next periapsis"
        { "REL_ASCENDING",     "at next AN with target"  }, //"at the next AN with the target."
        { "REL_DESCENDING",    "at next DN with target"  }, //"at the next DN with the target."
        { "X_FROM_NOW",        "after a fixed time"      }, //"after a fixed time"
        { "ALTITUDE",          "at an altitude"          }, //"at an altitude"
        { "EQ_NEAREST_AD",     "at nearest Eq. AN/DN"    }, //"at the nearest equatorial AN/DN"
        { "EQ_HIGHEST_AD",     "at cheapest Eq. AN/DN"   }, //"at the cheapest equatorial AN/DN"
        { "REL_NEAREST_AD",    "at nearest AN/DN w/Target"  }, //"at the nearest AN/DN with the target"
        { "REL_HIGHEST_AD",    "at cheapest AN/DN w/Target" } //"at the cheapest AN/DN with the target"
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
    private double targetSMA;
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

        GeneralSettings.Init(SettingsPath);

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

        // Initialize the toggle button dictionaries
        _toggles         = new Dictionary<string, bool>(_initialToggles);
        _previousToggles = new Dictionary<string, bool>(_initialToggles);

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
    }

    void save_rect_pos()
    {
        GeneralSettings.window_x_pos = (int)windowRect.xMin;
        GeneralSettings.window_y_pos = (int)windowRect.yMin;
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
            GUI.skin = GenericStyle.skin;

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
        if ( TopButtons.Button(GenericStyle.cross))
            CloseWindow();

        GUI.Label(new Rect(9, 2, 29, 29), GenericStyle.icon, GenericStyle.icons_label);
        
        if (selectingBody)
        {
            selectBodyUI();
            return;
        }
        if (selectingOption)
        {
            selectOptionUI();
            return;
        }

        var orbit = activeVessel.Orbit;
        var referenceBody = orbit.referenceBody;

        updateToggleButtons();

        // game = GameManager.Instance.Game;
        //activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        //currentNode = (activeNodes.Count() > 0) ? activeNodes[0] : null;
        FPUtility.RefreshActiveVesselAndCurrentManeuver();
        // FPNodeControl.RefreshManeuverNodes();
        currentNode = getCurrentNode();

        BodySelectionGUI();

        OptionSelectionGUI();

        // Initialize the available list of options. These get updated in setOptionsList
        options = new List<string> { "none" };

        DrawSectionHeader("Ownship Maneuvers");
        DrawToggleButton("Circularize", ref circularize);
        // GUILayout.EndHorizontal();

        FPSettings.pe_altitude_km = DrawToggleButtonWithTextField("New Pe", ref newPe, FPSettings.pe_altitude_km, "km");
        targetPeR = FPSettings.pe_altitude_km * 1000 + referenceBody.radius;

        if (orbit.eccentricity < 1)
        {
            FPSettings.ap_altitude_km = DrawToggleButtonWithTextField("New Ap", ref newAp, FPSettings.ap_altitude_km, "km");
            targetApR = FPSettings.ap_altitude_km * 1000 + referenceBody.radius;

            DrawToggleButton("New Pe & Ap", ref newPeAp);
        }

        FPSettings.target_inc_deg = DrawToggleButtonWithTextField("New Inclination", ref newInc, FPSettings.target_inc_deg, "°");

        if (experimental.Value)
        {
            FPSettings.target_lan_deg = DrawToggleButtonWithTextField("New LAN", ref newLAN, FPSettings.target_lan_deg, "°");

            // FPSettings.target_node_long_deg = DrawToggleButtonWithTextField("New Node Longitude", ref newNodeLon, FPSettings.target_node_long_deg, "°");
        }

        FPSettings.target_sma_km = DrawToggleButtonWithTextField("New SMA", ref newSMA, FPSettings.target_sma_km, "km");
        targetSMA = FPSettings.target_sma_km * 1000 + referenceBody.radius;

        if (currentTarget != null)
        {
            // If the activeVessel and the currentTarget are both orbiting the same body
            if (currentTarget.Orbit != null) // No maneuvers relative to a star
            {
                if (currentTarget.Orbit.referenceBody.Name == referenceBody.Name)
                {
                    DrawSectionHeader("Maneuvers Relative to Target");
                    DrawToggleButton("Match Planes", ref matchPlane);

                    DrawToggleButton("Hohmann Transfer", ref hohmannXfer);

                    DrawToggleButton("Course Correction", ref courseCorrection);

                    if (experimental.Value)
                    {
                        FPSettings.interceptT = DrawToggleButtonWithTextField("Intercept", ref interceptTgt, FPSettings.interceptT, "s");

                        DrawToggleButton("Match Velocity", ref matchVelocity);
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
                            DrawToggleButton("Interplanetary Transfer", ref planetaryXfer);
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
                DrawSectionHeader("Moon Specific Maneuvers");

                var parentPlanet = referenceBody.Orbit.referenceBody;
                FPSettings.mr_altitude_km = DrawToggleButtonWithTextField("Moon Return", ref moonReturn, FPSettings.mr_altitude_km, "km");
                targetMRPeR = FPSettings.mr_altitude_km * 1000 + parentPlanet.radius;
            }
        }

        // If the selected option is to do an activity "at an altitude", then present an input field for the altitude to use
        if (selectedOption == TimeReference["ALTITUDE"])
        {
            FPSettings.altitude_km = DrawLabelWithTextField("Maneuver Altitude", FPSettings.altitude_km, "km");
        }
        if (selectedOption == TimeReference["X_FROM_NOW"])
        {
            FPSettings.timeOffset = DrawLabelWithTextField("Time From Now", FPSettings.timeOffset, "s");
        }

        // Using the selected activity configure the valid options list
        setOptionsList();

        var UT = game.UniverseModel.UniversalTime;
        // if (statusText == "Virgin") statusTime = UT;
        if (currentNode == null && status != Status.VIRGIN)
        {
            status = Status.OK;
            statusText = "";
        }
        DrawGUIStatus(UT);

        // If the selected option is to do an activity "at an altitude", then make sure the altitude is possible for the orbit
        if (selectedOption == TimeReference["ALTITUDE"])
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

        maneuver = $"{baseManeuver} {selectedOption}";

        setBurnTime();

        // handleButtons();

        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    // This method should be called after getting the selectedOption for desired maneuver time/effect
    private void setBurnTime()
    {
        // Set the requested burn time based on the selected timing option
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        if (selectedOption == TimeReference["COMPUTED"])
            requestedBurnTime = -1; // for optimal time the burn time is computed and returned from the OrbitalManeuverCalculator method called.
        else if (selectedOption == TimeReference["APOAPSIS"])
            requestedBurnTime = orbit.NextApoapsisTime(UT);
        else if (selectedOption == TimeReference["PERIAPSIS"])
            requestedBurnTime = orbit.NextPeriapsisTime(UT);
        else if (selectedOption == TimeReference["CLOSEST_APPROACH"])
            requestedBurnTime = orbit.NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT + 2); // +2 so that closestApproachTime is definitely > UT
        else if (selectedOption == TimeReference["EQ_ASCENDING"])
            requestedBurnTime = orbit.TimeOfAscendingNodeEquatorial(UT);
        else if (selectedOption == TimeReference["EQ_DESCENDING"])
            requestedBurnTime = orbit.TimeOfDescendingNodeEquatorial(UT);
        else if (selectedOption == TimeReference["REL_ASCENDING"])
            requestedBurnTime = orbit.TimeOfAscendingNode(currentTarget.Orbit, UT); // like built in TimeOfAN(currentTarget.Orbit, UT), but with check to prevent time in the past
        else if (selectedOption == TimeReference["REL_DESCENDING"])
            requestedBurnTime = orbit.TimeOfDescendingNode(currentTarget.Orbit, UT); // like built in TimeOfDN(currentTarget.Orbit, UT), but with check to prevent time in the past
        else if (selectedOption == TimeReference["X_FROM_NOW"])
            requestedBurnTime = UT + FPSettings.timeOffset;
        else if (selectedOption == TimeReference["ALTITUDE"])
            requestedBurnTime = orbit.NextTimeOfRadius(UT, FPSettings.altitude_km * 1000);
        else if (selectedOption == TimeReference["EQ_NEAREST_AD"])
            requestedBurnTime = Math.Min(orbit.TimeOfAscendingNodeEquatorial(UT), orbit.TimeOfDescendingNodeEquatorial(UT));
        else if (selectedOption == TimeReference["EQ_HIGHEST_AD"])
        {
            var timeAN = orbit.TimeOfAscendingNodeEquatorial(UT);
            var timeDN = orbit.TimeOfDescendingNodeEquatorial(UT);
            var ANRadius = orbit.Radius(timeAN);
            var DNRadius = orbit.Radius(timeDN);
            if (ANRadius > DNRadius) requestedBurnTime = timeAN;
            else requestedBurnTime = timeDN;
        }
        else if (selectedOption == TimeReference["REL_NEAREST_AD"])
            requestedBurnTime = Math.Min(orbit.TimeOfAscendingNode(currentTarget.Orbit, UT), orbit.TimeOfDescendingNode(currentTarget.Orbit, UT));
        else if (selectedOption == TimeReference["REL_HIGHEST_AD"])
        {
            var timeAN = orbit.TimeOfAscendingNode(currentTarget.Orbit, UT);
            var timeDN = orbit.TimeOfDescendingNode(currentTarget.Orbit, UT);
            var ANRadius = orbit.Radius(timeAN);
            var DNRadius = orbit.Radius(timeDN);
            if (ANRadius > DNRadius) requestedBurnTime = timeAN;
            else requestedBurnTime = timeDN;
        }
    }

    // This method sould be called at the top of FillWindow to enable toggle buttons to work like radio buttons
    private void updateToggleButtons()
    {
        // Make toggle buttons behave like radio buttons
        int numChecked = _toggles.Count(item => item.Value); // how many are selected now (could be 0, 1, or 2)
        int oldNumChecked = _previousToggles.Count(item => item.Value); // how many were selected before (could be 0 or 1)
        if (numChecked == 0)
        {
            if (oldNumChecked > 0) // if the selected action has been deselected
                _previousToggles = new Dictionary<string, bool>(_initialToggles);
        }
        else if (numChecked == 1)
        {
            if (oldNumChecked == 0) // We gone from none selected to 1 selected
            {
                var selected = _toggles.FirstOrDefault(item => item.Value).Key;
                _previousToggles[selected] = true; // record the new selection in the previous list
            }
            else if (oldNumChecked == 1) // they should be the same, let's check
            {
                var oldSelected = _previousToggles.FirstOrDefault(item => item.Value).Key;
                var newSelected = _toggles.FirstOrDefault(item => item.Value).Key;
                if (oldSelected != newSelected)
                {
                    Logger.LogWarning($"Selection Mismatch: Previously {oldSelected} was selected, but now {newSelected} is selected. Correcting previous list.");
                    _previousToggles[oldSelected] = false; // update the previous list to deselect the previous selection
                    _previousToggles[newSelected] = true;  // update the previous list to select the new selection
                }
            }
            else // We shouldn't get here, but if there's more than one thing selected in the previous list and only one in the current list then fix it
            {
                _previousToggles = new Dictionary<string, bool>(_initialToggles);
                var newSelected = _toggles.FirstOrDefault(item => item.Value).Key;
                _previousToggles[newSelected] = true;
            }
        }
        else if (numChecked == 2)
        {
            if (oldNumChecked == 0) // This should not happen, report it and clear everything
            {
                Logger.LogError($"Selection Mismatch: Two or more things selected with zero previously. Resetting all.");
                _toggles = new Dictionary<string, bool>(_initialToggles);
                _previousToggles = new Dictionary<string, bool>(_initialToggles);
                clearToggleStates();
            }
            else if (oldNumChecked == 1) // We've selected something new
            {
                var oldSelected = _previousToggles.FirstOrDefault(item => item.Value).Key;
                _toggles[oldSelected] = false; // deselect the previous selection
                setToggleState(oldSelected, false);
                var newSelected = _toggles.FirstOrDefault(item => item.Value).Key;
                _previousToggles[newSelected] = true; // update the previous list to select the new selection
            }
            else // We shouldn't get here, but if there's more than one thing selected in the previous list and two in the current list then clear everything
            {
                Logger.LogError($"Selection Mismatch: Two or more things selected with two or more previously. Resetting all.");
                _toggles = new Dictionary<string, bool>(_initialToggles);
                _previousToggles = new Dictionary<string, bool>(_initialToggles);
                clearToggleStates();
            }
        }
        else // We should not be able to get here! Deselect everything...
        {
            Logger.LogError($"Selection Mismatch: More than two things selected! Resetting all.");
            _toggles = new Dictionary<string, bool>(_initialToggles);
            _previousToggles = new Dictionary<string, bool>(_initialToggles);
            clearToggleStates();
        }

        // Make toggle buttons behave like radio buttons
        numChecked = _toggles.Count(item => item.Value); // how many are selected now (could be 0, 1)
        oldNumChecked = _previousToggles.Count(item => item.Value); // how many were selected before (should match numChecked)
        string selectedAction = null;
        if (numChecked == 1)
            selectedAction = _toggles.FirstOrDefault(item => item.Value).Key;
        else if (numChecked > 1)
        {
            Logger.LogError($"Selection Mismatch: Two or more things selected after update. Resetting all.");
            _toggles = new Dictionary<string, bool>(_initialToggles);
            _previousToggles = new Dictionary<string, bool>(_initialToggles);
            clearToggleStates();
        }
    }

    // This method is called by updateToggleButtons to set the sate of a toggle button
    private void setToggleState(string key, bool value)
    {
        if (key == "Circularize")
            circularize = value;
        if (key == "SetNewAp")
            newAp = value;
        if (key == "SetNewPe")
            newPe = value;
        if (key == "Elipticize")
            newPeAp = value;
        if (key == "SetNewInc")
            newInc = value;
        if (key == "SetNewLAN")
            newLAN = value;
        if (key == "SetNodeLongitude")
            newNodeLon = value;
        if (key == "SetSMA")
            newSMA = value;
        if (key == "MatchPlane")
            matchPlane = value;
        if (key == "MatchVelocity")
            matchVelocity = value;
        if (key == "CourseCorrection")
            courseCorrection = value;
        if (key == "HohmannTransfer")
            hohmannXfer = value;
        if (key == "InterceptTgt")
            interceptTgt = value;
        if (key == "MoonReturn")
            moonReturn = value;
        if (key == "PlanetaryXfer")
            planetaryXfer = value;
    }

    // This method is called by updateToggleButtons to clear the state of all toggle buttons
    private void clearToggleStates()
    {
        circularize = false;
        newAp = false;
        newPe = false;
        newPeAp = false;
        newInc = false;
        newLAN = false;
        newNodeLon = false;
        newSMA = false;
        matchPlane = false;
        matchVelocity = false;
        courseCorrection = false;
        hohmannXfer = false;
        interceptTgt = false;
        moonReturn = false;
        planetaryXfer = false;
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

        // bodies = GameManager.Instance.Game.SpaceSimulation.GetAllObjectsWithComponent<CelestialBodyComponent>();

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

    // Allow the user to pick an option for the selected activity
    void selectOptionUI()
    {
        GUILayout.BeginHorizontal();
        UI_Tools.Label("Burn Time Option ");
        if (UI_Tools.SmallButton("Cancel"))
        {
            selectingOption = false;
        }
        GUILayout.EndHorizontal();

        UI_Tools.Separator();

        //GUI.SetNextControlName("Select Target");
        scrollPositionOptions = UI_Tools.BeginScrollView(scrollPositionOptions, 300);

        foreach (string option in options)
        {
            GUILayout.BeginHorizontal();
             if (UI_Tools.ListButton(option))
            {
                selectedOption = option;
                selectingOption = false;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    // Control display of the Option Picker UI
    private void OptionSelectionGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Burn : ");

        if (UI_Tools.SmallButton(selectedOption))
            selectingOption = true;

        GUILayout.EndHorizontal();
    }

    int spacingAfterHeader = 5;
    int spacingAfterEntry = 5;

    private void DrawSectionHeader(string sectionName, string value = "", float labelWidth = -1, GUIStyle valueStyle = null) // was (string sectionName, ref bool isPopout, string value = "")
    {
        if (valueStyle == null) valueStyle = GenericStyle.label;
        GUILayout.BeginHorizontal();
        // Don't need popout buttons for ROC
        // isPopout = isPopout ? !CloseButton() : UI_Tools.SmallButton("⇖", popoutBtnStyle);

        if (labelWidth < 0)
            GUILayout.Label($"<b>{sectionName}</b> ");
        else
            GUILayout.Label($"<b>{sectionName}</b> ", GUILayout.Width(labelWidth));
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
        button = UI_Tools.SmallButton(buttonStr);
    }

    private void DrawToggleButton(string runString, ref bool button, string stopString = "")
    {
        if (stopString.Length < 1)
            stopString = runString;
        button = UI_Tools.SmallToggleButton(button, runString, stopString);
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

    private double DrawToggleButtonWithTextField(string runString, ref bool button, double value, string unit = "", string stopString = "")
    {
        GUILayout.BeginHorizontal();
        if (stopString.Length < 1)
            stopString = runString;
        button = UI_Tools.SmallToggleButton(button, runString, stopString);
        GUILayout.Space(10);

        value = UI_Fields.DoubleField(runString, value);

        GUILayout.Space(3);
        UI_Tools.Label(unit);
        GUILayout.EndHorizontal();

        GUILayout.Space(spacingAfterEntry);
        return value;
    }

    public FPOtherModsInterface other_mods = null;

    private void DrawGUIStatus(double UT)
    {
        // Indicate status of last GUI function
        float transparency = 1;
        if (UT > statusTime) transparency = (float)MuUtils.Clamp(1 - (UT - statusTime) / statusFadeTime.Value, 0, 1);

        var status_style = FPStyles.status;
        //if (status == Status.VIRGIN)
        //    status_style = FPStyles.label;  
        if (status == Status.OK)
            status_style.normal.textColor = new Color(0, 1, 0, transparency); // FPStyles.phase_ok;
        if (status == Status.WARNING)
            status_style.normal.textColor = new Color(1, 1, 0, transparency); // FPStyles.phase_warning;
        if (status == Status.ERROR)
            status_style.normal.textColor = new Color(1, 0, 0, transparency); // FPStyles.phase_error;

        UI_Tools.Separator();
        DrawSectionHeader("Status:", statusText, 60, status_style);

        // Indication to User that its safe to type, or why vessel controls aren't working

        if (other_mods == null)
        {
            // init mode detection only when first needed
            other_mods = new FPOtherModsInterface();
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
    public bool Circularize(double burnUT, double burnOffsetFactor = -0.5)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"Circularize {selectedOption}");
        //var startTimeOffset = 60;
        //var burnUT = UT + startTimeOffset;
        var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT);

        status = Status.OK;
        statusText = $"Ready to Circularize {selectedOption}"; // "Ready to Circularize Now"
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

    public bool SetNewPe(double burnUT, double newPe, double burnOffsetFactor = -0.5)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNewPe {selectedOption}");
        // Debug.Log("Set New Pe");
        //var TimeToAp = orbit.TimeToAp;
        //double burnUT, e;
        //e = orbit.eccentricity;
        //if (e < 1)
        //    burnUT = UT + TimeToAp;
        //else
        //    burnUT = UT + 30;

        status = Status.OK;
        statusText = $"Ready to Change Pe {selectedOption}";
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

    public bool SetNewAp(double burnUT, double newAp, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNewAp {selectedOption}");
        // Debug.Log("Set New Ap");
        //var TimeToPe = orbit.TimeToPe;
        //var burnUT = UT + TimeToPe;

        status = Status.OK;
        statusText = $"Ready to Change Ap {selectedOption}";
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

    public bool Ellipticize(double burnUT, double newAp, double newPe, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"Ellipticize: Set New Pe and Ap {selectedOption}");

        status = Status.OK;
        statusText = $"Ready to Ellipticize {selectedOption}";
        statusTime = UT + statusPersistence.Value;

        if (newPe > newAp)
        {
            (newPe, newAp) = (newAp, newPe);
            status = Status.WARNING;
            statusText = "Pe Setting > Ap Setting";
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
            status = Status.ERROR;
            statusText = "Set New Pe and Ap: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool SetInclination(double burnUT, double inclination, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetInclination: Set New Inclination {inclination}° {selectedOption}");
        // double burnUT, TAN, TDN;
        Vector3d deltaV;

        status = Status.OK;
        statusText = $"Ready to Change Inclination {selectedOption}";
        statusTime = UT + statusPersistence.Value;

        deltaV = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, burnUT, inclination);
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
    
    public bool SetNewLAN(double burnUT, double newLANvalue, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNewLAN: Set New LAN {newLANvalue}° {selectedOption}");
        // Debug.Log("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = UT + 30;

        status = Status.WARNING;
        statusText = $"Experimental LAN Change {selectedOption}";
        statusTime = UT + statusPersistence.Value;

        Logger.LogDebug($"Seeking Solution: newLANvalue {newLANvalue}°");
        var deltaV = OrbitalManeuverCalculator.DeltaVToShiftLAN(orbit, burnUT, newLANvalue);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = "Set New LAN: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool SetNodeLongitude(double burnUT, double newNodeLongValue, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNodeLongitude: Set Node Longitude {newNodeLongValue}° {selectedOption}");
        // Debug.Log("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = UT + 30;

        status = Status.WARNING;
        statusText = $"Experimental Node Longitude Change {selectedOption}";
        statusTime = UT + statusPersistence.Value;

        Logger.LogDebug($"Seeking Solution: newNodeLongValue {newNodeLongValue}°");
        var deltaV = OrbitalManeuverCalculator.DeltaVToShiftNodeLongitude(orbit, burnUT, newNodeLongValue);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = "Shift Node Longitude: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool SetNewSMA(double burnUT, double newSMA, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"SetNewSMA {selectedOption}");
        // Debug.Log("Set New Ap");
        // var TimeToPe = orbit.TimeToPe;
        // var burnUT = UT + 30;

        status = Status.OK;
        statusText = $"Ready to Change SMA Change {selectedOption}";
        statusTime = UT + statusPersistence.Value;

        Logger.LogDebug($"Seeking Solution: newSMA {newSMA} m");
        var deltaV = OrbitalManeuverCalculator.DeltaVForSemiMajorAxis(orbit, burnUT, newSMA);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
            return true;
        }
        else
        {
            status = Status.ERROR;
            statusText = "Set New SMA: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }
    
    public bool MatchPlanes(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MatchPlanes: Match Planes with {currentTarget.Name} {selectedOption}");
        double burnUTout = UT + 1;

        status = Status.OK;
        statusText = $"Ready to Match Planes with {currentTarget.Name} {selectedOption}";
        statusTime = UT + statusPersistence.Value;

        Vector3d deltaV = Vector3d.zero;
        if (selectedOption == TimeReference["REL_ASCENDING"])
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
        else if (selectedOption == TimeReference["REL_DESCENDING"])
            deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
        else if (selectedOption == TimeReference["REL_NEAREST_AD"])
        {
            if (orbit.TimeOfAscendingNode(currentTarget.Orbit as PatchedConicsOrbit, UT) < orbit.TimeOfDescendingNode(currentTarget.Orbit as PatchedConicsOrbit, UT))
                deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
            else
                deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUTout);
        }
        else if (selectedOption == TimeReference["REL_HIGHEST_AD"])
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
            status = Status.ERROR;
            statusText = $"Match Planes with {currentTarget.Name} at AN: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool HohmannTransfer(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"HohmannTransfer: Hohmann Transfer to {currentTarget.Name} {selectedOption}");
        // Debug.Log("Hohmann Transfer");
        double burnUTout;
        Vector3d deltaV;

        status = Status.WARNING;
        statusText = $"Ready to Transfer to {currentTarget.Name}?";
        statusTime = UT + statusPersistence.Value;

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
            status = Status.ERROR;
            statusText = $"Hohmann Transfer to {currentTarget.Name}: Solution Not Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
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

        Logger.LogDebug($"InterceptTgt: Intercept {currentTarget.Name} {selectedOption}");
        // var burnUT = UT + 30;
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

    public bool CourseCorrection(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"CourseCorrection: Course Correction burn to improve trajectory to {currentTarget.Name} {selectedOption}");
        double burnUTout;
        Vector3d deltaV;

        status = Status.OK;
        statusText = "Course Correction Ready"; // "Ready for Course Correction Burn";
        statusTime = UT + statusPersistence.Value;

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
            status = Status.ERROR;
            statusText = $"Course Correction for tragetory to {currentTarget.Name}: No Solution Found!";
            statusTime = UT + statusPersistence.Value;
            Logger.LogDebug(statusText);
            return false;
        }
    }

    public bool MoonReturn(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MoonReturn: Return from {orbit.referenceBody.Name} {selectedOption}");
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
                status = Status.ERROR;
                statusText = "Moon Return: No Solution Found!";
                statusTime = UT + statusPersistence.Value;
                Logger.LogDebug(statusText);
                return false;
            }
        }
    }

    public bool MatchVelocity(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"MatchVelocity: Match Velocity with {currentTarget.Name} {selectedOption}");

        status = Status.WARNING;
        statusText = $"Experimental Velocity Match with {currentTarget.Name} Ready"; // $"Ready to Match Velocity with {currentTarget.Name}";
        statusTime = UT + statusPersistence.Value;

        // double closestApproachTime = orbit.NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT + 2); //+2 so that closestApproachTime is definitely > UT
        var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, burnUT, currentTarget.Orbit as PatchedConicsOrbit);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUT, burnOffsetFactor);
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

    public bool PlanetaryXfer(double burnUT, double burnOffsetFactor)
    {
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var orbit = activeVessel.Orbit;

        Logger.LogDebug($"PlanetaryXfer: Transfer to {currentTarget.Name} {selectedOption}");
        double burnUTout, burnUT2;
        bool syncPhaseAngle = true;

        status = Status.WARNING;
        statusText = $"Experimental Transfer to {currentTarget.Name} Ready"; // $"Ready to depart for {currentTarget.Name}";
        statusTime = UT + statusPersistence.Value;

        var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, syncPhaseAngle, out burnUTout);
        var deltaV2 = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryLambertTransferEjection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, out burnUT2);
        if (deltaV != Vector3d.zero)
        {
            CreateManeuverNode(deltaV, burnUTout, burnOffsetFactor);
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

    // This method sets up the options list based on the selected activity. This method also configures the _toggles dictionary to record the setting of the "radio buttons"
    // for comparison to the _previousToggles dictionary.
    private void setOptionsList()
    {
        if (circularize || newPe || newAp || newPeAp || newInc || newLAN || newNodeLon || newSMA || matchPlane || hohmannXfer || courseCorrection || interceptTgt || matchVelocity || moonReturn || planetaryXfer )
        {
            if (options.Contains("none"))
                options.Remove("none");
            if (circularize)
            {
                _toggles["Circularize"] = true;
                if (activeVessel.Orbit.eccentricity < 1) options.Add(TimeReference["APOAPSIS"]); //"At Next Apoapsis"
                options.Add(TimeReference["PERIAPSIS"]); //"At Next Periapsis"
                options.Add(TimeReference["ALTITUDE"]); //"At An Altittude"
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                baseManeuver = "Circularizing";
            }
            if (newPe)
            {
                _toggles["SetNewPe"] = true;
                if (activeVessel.Orbit.eccentricity < 1) options.Add(TimeReference["APOAPSIS"]); //"At Next Apoapsis"
                options.Add(TimeReference["PERIAPSIS"]); //"At Next Apoapsis"
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                options.Add(TimeReference["ALTITUDE"]); //"At An Altittude"
                baseManeuver = "Setting new Pe";
            }
            if (newAp)
            {
                _toggles["SetNewAp"] = true;
                options.Add(TimeReference["PERIAPSIS"]); //"At Next Periapsis"
                if (activeVessel.Orbit.eccentricity < 1) options.Add(TimeReference["APOAPSIS"]); //"At Next Apoapsis"
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                options.Add(TimeReference["ALTITUDE"]); //"At An Altittude"
                baseManeuver = "Setting new Ap";
            }
            if (newPeAp)
            {
                _toggles["Elipticize"] = true;
                if (activeVessel.Orbit.eccentricity < 1) options.Add(TimeReference["APOAPSIS"]); //"At Next Apoapsis"
                options.Add(TimeReference["PERIAPSIS"]); //"At Next Periapsis"
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                options.Add(TimeReference["ALTITUDE"]); //"At An Altittude"
                options.Add(TimeReference["EQ_ASCENDING"]); //"At Equatorial AN"
                options.Add(TimeReference["EQ_DESCENDING"]); //"At Equatorial DN"
                baseManeuver = "Elipticizing";
            }
            if (newLAN)
            {
                _toggles["SetNewLAN"] = true;
                if (activeVessel.Orbit.eccentricity < 1) options.Add(TimeReference["APOAPSIS"]); //"At Next Apoapsis"
                options.Add(TimeReference["PERIAPSIS"]); //"At Next Periapsis"
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                baseManeuver = "Setting new LAN";
            }
            if (newNodeLon)
            {
                _toggles["SetNodeLongitude"] = true;
                if (activeVessel.Orbit.eccentricity < 1) options.Add(TimeReference["APOAPSIS"]); //"At Next Apoapsis"
                options.Add(TimeReference["PERIAPSIS"]); //"At Next Periapsis"
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                baseManeuver = "Shifting Node LongitudeN";
            }
            if (newSMA)
            {
                _toggles["SetNewSMA"] = true;
                if (activeVessel.Orbit.eccentricity < 1) options.Add(TimeReference["APOAPSIS"]); //"At Next Apoapsis"
                options.Add(TimeReference["PERIAPSIS"]); //"At Next Periapsis"
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                baseManeuver = "Setting new SMA";
            }
            if (newInc)
            {
                _toggles["SetNewInc"] = true;
                options.Add(TimeReference["EQ_HIGHEST_AD"]); //"At Cheapest eq AN/DN"
                options.Add(TimeReference["EQ_NEAREST_AD"]); //"At Nearest eq AN/DN"
                options.Add(TimeReference["EQ_ASCENDING"]); //"At Equatorial AN"
                options.Add(TimeReference["EQ_DESCENDING"]); //"At Equatorial DN"
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                baseManeuver = "Setting new inclination";
            }
            if (matchPlane)
            {
                _toggles["MatchPlane"] = true;
                options.Add(TimeReference["REL_HIGHEST_AD"]); //"At Cheapest AN/DN With Target"
                options.Add(TimeReference["REL_NEAREST_AD"]); //"At Nearest AN/DN With Target"
                options.Add(TimeReference["REL_ASCENDING"]); //"At Next AN With Target"
                options.Add(TimeReference["REL_DESCENDING"]); //"At Next DN With Target"
                baseManeuver = "Matching planes";
            }
            if (hohmannXfer)
            {
                _toggles["HohmannTransfer"] = true;
                options.Add(TimeReference["COMPUTED"]); //"At Optimal Time"
                //options.Add(TimeReference["APOAPSIS"]); //"At Apoapsis"
                //options.Add(TimeReference["PERIAPSIS"]); //"At Periapsis"
                //options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                //options.Add(TimeReference["ALTITUDE"]); //"At An Altitude"
                //options.Add(TimeReference["EQ_ASCENDING"]); //"At Equatorial AN"
                //options.Add(TimeReference["EQ_DESCENDING"]); //"At Equatorial DN"
                //options.Add(TimeReference["REL_ASCENDING"]); //"At Next AN With Target"
                //options.Add(TimeReference["REL_DESCENDING"]); //"At Next DN With Target"
                //options.Add(TimeReference["EQ_NEAREST_AD"]); //"At Nearest AN/DN With Target"
                //options.Add(TimeReference["EQ_HIGHEST_AD"]); //"At Cheapest An/DN With Target"
                //options.Add(TimeReference["CLOSEST_APPROACH"]); //"At Closest Approach"
                baseManeuver = "Performaing Homann transfer";
            }
            if (interceptTgt)
            {
                _toggles["InterceptTgt"] = true;
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                //options.Add(TimeReference["CLOSEST_APPROACH"]); //"At Closest Approach"
                //options.Add(TimeReference["COMPUTED"]); //"At Optimal Time"
                //options.Add(TimeReference["APOAPSIS"]); //"At Apoapsis"
                //options.Add(TimeReference["PERIAPSIS"]); //"At Periapsis"
                //options.Add(TimeReference["ALTITUDE"]); //"At An Altitude"
                //options.Add(TimeReference["EQ_ASCENDING"]); //"At Equatorial AN"
                //options.Add(TimeReference["EQ_DESCENDING"]); //"At Equatorial DN"
                //options.Add(TimeReference["REL_ASCENDING"]); //"At Next AN With Target"
                //options.Add(TimeReference["REL_DESCENDING"]); //"At Next DN With Target"
                //options.Add(TimeReference["EQ_NEAREST_AD"]); //"At Nearest AN/DN With Target"
                //options.Add(TimeReference["EQ_HIGHEST_AD"]); //"At Cheapest An/DN With Target"
                baseManeuver = "Intercepting";
            }
            if (courseCorrection)
            {
                _toggles["CourseCorrection"] = true;
                options.Add(TimeReference["COMPUTED"]); //"At Optimal Time"
                baseManeuver = "Performaing course correction";
            }
            if (moonReturn)
            {
                _toggles["MoonReturn"] = true;
                options.Add(TimeReference["COMPUTED"]); //"At Optimal Time"
                baseManeuver = "Performaing moon return";
            }
            if (matchVelocity)
            {
                _toggles["MatchVelocity"] = true;
                options.Add(TimeReference["CLOSEST_APPROACH"]); //"At Closest Approach"
                options.Add(TimeReference["X_FROM_NOW"]); //"After Fixed Time"
                baseManeuver = "Matching velocity";
            }
            if (planetaryXfer)
            {
                _toggles["PlanetaryXfer"] = true;
                options.Add(TimeReference["COMPUTED"]); //"At Optimal Time"
                baseManeuver = "Performaing planetary transfer";
            }
            if (!options.Contains(selectedOption))
                selectedOption = options[0];
        }
    }

    public void MakeNode()
    {
        if (circularize|| newPe || newAp || newPeAp || newInc || newLAN || newNodeLon || newSMA || matchPlane || hohmannXfer || courseCorrection || interceptTgt || matchVelocity || moonReturn || planetaryXfer )
        {
            bool pass;

            if (circularize) // Working
            {
                pass = Circularize(requestedBurnTime, -0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newPe) // Working
            {
                pass = SetNewPe(requestedBurnTime, targetPeR, - 0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newAp) // Working
            {
                pass = SetNewAp(requestedBurnTime, targetApR, - 0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newPeAp) // Working: Not perfect, but pretty good results nevertheless
            {
                pass = Ellipticize(requestedBurnTime, targetApR, targetPeR, - 0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newInc) // Working
            {
                pass = SetInclination(requestedBurnTime, FPSettings.target_inc_deg, -0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (newLAN) // Untested
            {
                pass = SetNewLAN(requestedBurnTime, FPSettings.target_lan_deg, -0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (newNodeLon) // Untested
            {
                pass = SetNodeLongitude(requestedBurnTime, FPSettings.target_node_long_deg, -0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (newSMA) // Untested
            {
                pass = SetNewSMA(requestedBurnTime, targetSMA, -0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (matchPlane) // Working
            {
                pass = MatchPlanes(requestedBurnTime, -0.5);
                // if (pass && autoLaunchMNC.Value) callMNC();
            }
            else if (hohmannXfer) // Works if we start in a good enough orbit (reasonably circular, close to target's orbital plane)
            {
                pass = HohmannTransfer(requestedBurnTime, - 0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (interceptTgt) // Experimental
            {
                pass = InterceptTgt(requestedBurnTime, FPSettings.interceptT, -0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (courseCorrection) // Experimental Works at least some times...
            {
                pass = CourseCorrection(requestedBurnTime, -0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (moonReturn) // Works - but may give poor Pe, including potentially lithobreaking
            {
                pass = MoonReturn(requestedBurnTime, -0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (matchVelocity) // Experimental
            {
                pass = MatchVelocity(requestedBurnTime, -0.5);
                if (pass && autoLaunchMNC.Value) other_mods.callMNC();
            }
            else if (planetaryXfer) // Experimental - also not working at all. Places node at wrong time, often on the wrong side of mainbody (lowering when should be raising and vice versa)
            {
                pass = PlanetaryXfer(requestedBurnTime, -0.5);
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
