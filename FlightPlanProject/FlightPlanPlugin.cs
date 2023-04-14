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
using KSP.Map;
using BepInEx.Logging;
using System.Collections;

namespace FlightPlan;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class FlightPlanPlugin : BaseSpaceWarpPlugin
{
    public static FlightPlanPlugin Instance { get; set; }

    // These are useful in case some other mod wants to add a dependency to this one
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    // Control click through to the game
    private bool gameInputState = true;
    // public readonly string vesselNameInput = "VesselRenamer.Input";
    public List<String> inputFields = new List<String>();

    static bool loaded = false;
    private bool interfaceEnabled = false;
    private bool GUIenabled = true;
    // private bool _isWindowOpen;
    private Rect _windowRect;
    private int windowWidth = Screen.width / 6; //384px on 1920x1080
    private int windowHeight = Screen.height / 3; //360px on 1920x1080
    private Rect closeBtnRect;

    // Button bools
    private bool circAp, circPe, circNow, newPe, newAp, newPeAp, newInc, matchPlanesA, matchPlanesD, hohmannT, interceptAtTime, courseCorrection, moonReturn, matchVCA, matchVNow, planetaryXfer;

    // Target Selection
    //private enum TargetOptions
    //{
    //    Kerbin,
    //    Mun,
    //    Minmus,
    //    Duna
    //}

    // Body selection.
    private string selectedBody = null;
    private List<string> bodies;
    private bool selectingBody = false;
    private static Vector2 scrollPositionBodies;

    //private TargetOptions selectedTargetOption = TargetOptions.Mun;
    //private readonly List<string> targetOptions = new List<string> { "Kerbin", "Mun", "Minmus", "Duna" };
    //private bool selectingTargetOption = false;
    //private static Vector2 scrollPositionTargetOptions;
    //private bool applyTargetOption;

    // mod-wide data
    private VesselComponent activeVessel;
    private SimulationObjectModel currentTarget;
    private ManeuverNodeData currentNode = null;
    List<ManeuverNodeData> activeNodes;
    private Vector3d burnParams;

    // Text Inputs
    private string targetPeAStr  =  "20000"; // m
    private string targetApAStr  = "250000"; // m
    private string targetPeAStr1 =  "20000"; // m
    private string targetApAStr1 = "250000"; // m
    private string targetIncStr  =      "0"; // degrees
    private string interceptTStr =    "100"; // s

    private double targetPeR  =  20000; // m
    private double targetApR  = 250000; // m
    private double targetPeR1 =  20000; // m
    private double targetApR1 = 250000; // m
    private double targetInc  = 0;      // degrees
    private double interceptT = 100;    // s

    // GUI layout and style stuff
    private GUIStyle errorStyle, warnStyle, progradeStyle, normalStyle, radialStyle, labelStyle;
    private GameInstance game;
    private GUIStyle horizontalDivider = new GUIStyle();
    private GUISkin _spaceWarpUISkin;
    private GUIStyle tgtBtnStyle;
    private GUIStyle ctrlBtnStyle;
    private GUIStyle bigBtnStyle;
    private GUIStyle smallBtnStyle;
    private GUIStyle mainWindowStyle;
    private GUIStyle textInputStyle;
    private GUIStyle sectionToggleStyle;
    private GUIStyle closeBtnStyle;
    private GUIStyle nameLabelStyle;
    private GUIStyle valueLabelStyle;
    private GUIStyle unitLabelStyle;
    private static GUIStyle boxStyle;
    private string unitColorHex;
    private int spacingAfterHeader = -12;
    private int spacingAfterEntry = -5;
    private int spacingAfterSection = 5;

    // App bar button(s)
    private const string ToolbarFlightButtonID = "BTN-FlightPlanFlight";
    // private const string ToolbarOABButtonID = "BTN-FlightPlanOAB";

    // Hot Key config setting
    private bool experimental;

    //public ManualLogSource logger;
    public new static ManualLogSource Logger { get; set; }

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();
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

        // Setup the list of input field names (most are the same as the entry string text displayed in the GUI window)
        inputFields.Add("New Pe");
        inputFields.Add("New Ap");
        inputFields.Add("New Pe & Ap");
        inputFields.Add("New Ap & Pe"); // kludgy name for the second input in a two input line
        inputFields.Add("New Inclination");
        inputFields.Add("Intercept at Time");
        inputFields.Add("Select Target");

        // logger = BepInEx.Logging.Logger.CreateLogSource("FlightPlan");

        // logger = Logger;
        Logger.LogInfo("Loaded");
        if (loaded)
        {
            Destroy(this);
        }
        loaded = true;

        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);

        // Instance = this;

        _spaceWarpUISkin = Skins.ConsoleSkin;

        boxStyle = new GUIStyle(_spaceWarpUISkin.box); // GUI.skin.GetStyle("Box");

        mainWindowStyle = new GUIStyle(_spaceWarpUISkin.window)
        {
            padding = new RectOffset(8, 8, 20, 8),
            contentOffset = new Vector2(0, -22),
            fixedWidth = windowWidth
        };

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

        sectionToggleStyle = new GUIStyle(_spaceWarpUISkin.toggle)
        {
            padding = new RectOffset(14, 0, 3, 3)
        };

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
        unitColorHex = ColorUtility.ToHtmlStringRGBA(unitLabelStyle.normal.textColor);

        closeBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            fontSize = 12
        };

        closeBtnRect = new Rect(windowWidth - 23, 6, 16, 16);

        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            "Flight Plan",
            "BTN-FlightPlan",
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            ToggleButton);

        // Register OAB AppBar Button
        //Appbar.RegisterOABAppButton(
        //    "BTN-FlightPlan",
        //    ToolbarOABButtonID,
        //    AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
        //    isOpen =>
        //    {
        //        _isWindowOpen = isOpen;
        //        GameObject.Find(ToolbarOABButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(isOpen);
        //    }
        //);

        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(FlightPlanPlugin).Assembly);

        // Fetch a configuration value or create a default one if it does not exist
        var defaultValue = false;
        experimental = Config.Bind<bool>("Settings section", "Experimental Features", defaultValue, "Enable/Disable experimental features for testing - Warrantee Void if Enabled!").Value;

        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        Logger.LogInfo($"Experimental Features: {experimental}");

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
        // activeVessel = Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
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
                // Logger.LogInfo($"[Flight Plan]: Disabling Game Input: Focused Item '{GUI.GetNameOfFocusedControl()}'");
                gameInputState = false;
                GameManager.Instance.Game.Input.Disable();
            }
            else if (!gameInputState && !inputFields.Contains(GUI.GetNameOfFocusedControl()))
            {
                // Logger.LogInfo($"[Flight Plan]: Enabling Game Input: FYI, Focused Item '{GUI.GetNameOfFocusedControl()}'");
                gameInputState = true;
                GameManager.Instance.Game.Input.Enable();
            }
            if (selectingBody)
            {
                // Do something here to disable mouse wheel control of zoom in and out.
                // Intent: allow player to scroll in the scroll view without causing the game to zoom in and out
                GameManager.Instance._game.MouseManager.enabled = false;
            }
            else
            {
                // Do something here to re-enable mouse wheel control of zoom in and out.
                GameManager.Instance._game.MouseManager.enabled = true;
            }
        }
        else
        {
            if (!gameInputState)
            {
                // Logger.LogInfo($"[Flight Plan]: Enabling Game Input due to GUI disabled: FYI, Focused Item '{GUI.GetNameOfFocusedControl()}'");
                gameInputState = true;
                // game.Input.Flight.Enable();
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

        labelStyle = warnStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        errorStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        errorStyle.normal.textColor = Color.red;
        warnStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        warnStyle.normal.textColor = Color.yellow;
        progradeStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        progradeStyle.normal.textColor = Color.yellow;
        normalStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        normalStyle.normal.textColor = Color.magenta;
        radialStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        radialStyle.normal.textColor = Color.cyan;
        horizontalDivider.fixedHeight = 2;
        horizontalDivider.margin = new RectOffset(0, 0, 4, 4);
        // game = GameManager.Instance.Game;
        //activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        //currentNode = (activeNodes.Count() > 0) ? activeNodes[0] : null;
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

        if (experimental)
            DrawButton("Circularize Now", ref circNow);

        DrawButtonWithTextField("New Pe", ref newPe, ref targetPeAStr, "m");
        try { targetPeR = double.Parse(targetPeAStr) + activeVessel.Orbit.referenceBody.radius; }
        catch { targetPeR = 0; }

        if (activeVessel.Orbit.eccentricity < 1)
        {
            DrawButtonWithTextField("New Ap", ref newAp, ref targetApAStr, "m");
            try { targetApR = double.Parse(targetApAStr) + activeVessel.Orbit.referenceBody.radius; }
            catch { targetApR = 0; }

            if (experimental)
            {
                DrawButtonWithDualTextField("New Pe & Ap", "New Ap & Pe", ref newPeAp, ref targetPeAStr1, ref targetApAStr1);
                try { targetPeR1 = double.Parse(targetPeAStr1) + activeVessel.Orbit.referenceBody.radius; }
                catch { targetPeR1 = 0; };
                try { targetApR1 = double.Parse(targetApAStr1) + activeVessel.Orbit.referenceBody.radius; }
                catch { targetApR1 = 0; }
            }
        }

        DrawButtonWithTextField("New Inclination", ref newInc, ref targetIncStr, "°");
        try { targetInc = double.Parse(targetIncStr); }
        catch { targetInc = 0; }

        if (currentTarget != null)
        {
            // If the activeVessel and the currentTarget are both orbiting the same body
            if (currentTarget.Orbit.referenceBody.Name == activeVessel.Orbit.referenceBody.Name)
            {
                DrawSectionHeader("Maneuvers Relative to Target");
                DrawButton("Match Planes at AN", ref matchPlanesA);
                DrawButton("Match Planes at DN", ref matchPlanesD);

                DrawButton("Hohmann Xfer", ref hohmannT);

                if (experimental)
                {
                    DrawButtonWithTextField("Intercept at Time", ref interceptAtTime, ref interceptTStr, "s");
                    try { interceptT = double.Parse(interceptTStr); }
                    catch { interceptT = 100; }
                    DrawButton("Course Correction", ref courseCorrection);
                    DrawButton("Match Velocity @CA", ref matchVCA);
                    DrawButton("Match Velocity Now", ref matchVNow);
                }

            }

            if (experimental)
            {
                //if the currentTarget is a celestial body and it's in orbit around the same body that the activeVessel's parent body is orbiting
                if ((currentTarget.Name != activeVessel.Orbit.referenceBody.Name) && (currentTarget.Orbit.referenceBody.Name == activeVessel.Orbit.referenceBody.Orbit.referenceBody.Name))
                {
                    DrawSectionHeader("Interplanetary Maneuvers");
                    DrawButton("Interplanetary Transfer", ref planetaryXfer);
                }
            }
        }
        if (experimental)
        {
            // If the activeVessle is at a moon (a celestial in orbit around another celestial that's not also a star)
            var referenceBody = activeVessel.Orbit.referenceBody;
            if (!referenceBody.referenceBody.IsStar && activeVessel.Orbit.eccentricity < 1)
            {
                DrawSectionHeader("Moon Specific Maneuvers");
                DrawButton("Moon Return", ref moonReturn);
            }
        }


        // Indication to User that its safe to type, or why vessel controls aren't working
        GUILayout.BeginHorizontal();
        string inputStateString = gameInputState ? "Enabled" : "Disabled";
        GUILayout.Label($"Game Input: {inputStateString}");
        GUILayout.EndHorizontal();

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
        Logger.LogInfo("[Flight Plan]: Restoring game.Input.Flight on window close.");
        // game.Input.Flight.Enable();
        GameManager.Instance.Game.Input.Enable();
        ToggleButton(interfaceEnabled);
    }

    void BodySelectionGUI()
    {
        bodies = GameManager.Instance.Game.SpaceSimulation.GetBodyNameKeys().ToList();
        string baseName = "Select Target";
        GUILayout.BeginHorizontal();
        GUILayout.Label("Target Celestial Body: ", GUILayout.Width((float)(windowWidth*0.6)));
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

    private void DrawSectionHeader(string sectionName, string value = "") // was (string sectionName, ref bool isPopout, string value = "")
    {
        GUILayout.BeginHorizontal();
        // Don't need popout buttons for ROC
        // isPopout = isPopout ? !CloseButton() : GUILayout.Button("⇖", popoutBtnStyle);

        GUILayout.Label($"<b>{sectionName}</b>");
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, valueLabelStyle);
        GUILayout.Space(5);
        // GUILayout.Label("", unitLabelStyle);
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
        GUILayout.BeginHorizontal();
        GUILayout.Label(entryName, nameLabelStyle);
        GUILayout.FlexibleSpace();
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
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
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(entryName, smallBtnStyle);
        GUILayout.Space(10);
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
        GUILayout.Space(3);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawButtonWithDualTextField(string entryName1, string entryName2, ref bool button, ref string textEntry1, ref string textEntry2, string unit = "")
    {
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(entryName1, smallBtnStyle);
        GUILayout.Space(5);
        GUI.SetNextControlName(entryName1);
        textEntry1 = GUILayout.TextField(textEntry1, textInputStyle);
        GUILayout.Space(5);
        GUI.SetNextControlName(entryName2);
        textEntry2 = GUILayout.TextField(textEntry2, textInputStyle);
        GUILayout.Space(3);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

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
                Logger.LogInfo("Circularize at Ap");
                var TimeToAp = orbit.TimeToAp; // activeVessel.Orbit.TimeToAp;
                var burnUT = UT + TimeToAp;
                var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    if (burnParams.z < 0)
                    {
                        burnParams.z *= -1;
                        Logger.LogInfo($"Solution Found: burnParams* [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT (* prograde flipped)");
                    }
                    else
                        Logger.LogInfo($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // currentNode = getCurrentNode();
                    var nodeTimeAdj = -currentNode.BurnDuration / 2;
                    var burnStartTime = currentNode.Time + nodeTimeAdj;
                    Logger.LogInfo($"BurnDuration: {currentNode.BurnDuration}, Adjusting start of burn by {nodeTimeAdj}s");
                    deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnStartTime); // activeVessel.Orbit
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnStartTime, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    if (burnParams.z < 0)
                    {
                        burnParams.z *= -1;
                        Logger.LogInfo($"Solution Found: burnParams* [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT (* prograde flipped)");
                    }
                    else
                        Logger.LogInfo($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    currentNode.BurnVector = burnParams;
                    // currentNode.Time = burnStartTime;
                    UpdateNode(currentNode);
                }
                else
                    Logger.LogInfo("Circularize at Ap: Solution Not Found!");
            }
            else if (circPe) // Working
            {
                Logger.LogInfo("Circularize at Pe");
                var TimeToPe = orbit.TimeToPe; // activeVessel.Orbit
                var burnUT = UT + TimeToPe;
                var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    if (burnParams.z > 0)
                    {
                        burnParams.z *= -1;
                        Logger.LogInfo($"Solution Found: burnParams* [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT (* prograde flipped)");
                    }
                    else
                        Logger.LogInfo($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // currentNode = getCurrentNode();
                    var nodeTimeAdj = -currentNode.BurnDuration / 2;
                    var burnStartTime = currentNode.Time + nodeTimeAdj;
                    Logger.LogInfo($"BurnDuration: {currentNode.BurnDuration}, Adjusting start of burn by {nodeTimeAdj}s");
                    deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnStartTime); // activeVessel.Orbit
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnStartTime, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    if (burnParams.z > 0)
                    {
                        burnParams.z *= -1;
                        burnParams.x *= -1;
                        Logger.LogInfo($"Solution Found: burnParams* [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT (* radial & prograde flipped)");
                    }
                    else
                        Logger.LogInfo($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    currentNode.BurnVector = burnParams;
                    // currentNode.Time = burnStartTime;
                    UpdateNode(currentNode);
                }
                else
                    Logger.LogInfo("Circularize at Pe: Solution Not Found!");
            }
            else if (circNow) // Not Working - Getting burn component in normal direction and we shouldn't be
            {
                Logger.LogInfo("Circularize Now");
                var startTimeOffset = 60;
                var burnUT = UT + startTimeOffset;
                var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnUT); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    burnParams.z *= -1;
                    burnParams.x *= -1;
                    Logger.LogInfo($"Solution Found: burnParams* [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT (* prograde flipped)");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    // currentNode = getCurrentNode();
                    var nodeTimeAdj = currentNode.BurnDuration / 2;
                    var burnStartTime = currentNode.Time + nodeTimeAdj;
                    Logger.LogInfo($"BurnDuration: {currentNode.BurnDuration}, Recalculating burn to be centered at {nodeTimeAdj + startTimeOffset} s from now ");
                    deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, burnStartTime); // activeVessel.Orbit
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnStartTime, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    burnParams.z *= -1;
                    burnParams.x *= -1;
                    currentNode.BurnVector = burnParams;
                    // currentNode.Time = burnStartTime;
                    UpdateNode(currentNode);
                }
                else
                    Logger.LogInfo("Circularize Now: Solution Not Found!");
            }
            else if (newPe) // Working
            {
                Logger.LogInfo("Set New Pe");
                Debug.Log("Set New Pe");
                var TimeToAp = orbit.TimeToAp; // activeVessel.Orbit
                double burnUT, e;
                e = orbit.eccentricity; // activeVessel.Orbit
                if (e < 1)
                    burnUT = UT + TimeToAp;
                else
                    burnUT = UT + 30;
                Logger.LogInfo($"Seeking Solution: targetPeR {targetPeR} m, currentPeR {orbit.Periapsis} m, body.radius {orbit.referenceBody.radius} m"); // activeVessel.Orbit
                Debug.Log($"Seeking Solution: targetPeR {targetPeR} m, currentPeR {orbit.Periapsis} m, body.radius {orbit.referenceBody.radius} m"); // activeVessel.Orbit
                var deltaV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, burnUT, targetPeR); // activeVessel.Orbit
                // var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(activeVessel.Orbit, burnUT, targetPeR, activeVessel.Orbit.Apoapsis);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    if ((e >= 1) && (targetPeR < orbit.Periapsis)) // activeVessel.Orbit
                    {
                        burnParams.z *= -1; // Why do we need this?
                        Logger.LogInfo($"Solution Found: burnParams* [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT (* prograde flipped)");
                        CreateManeuverNodeAtUT(burnParams, burnUT, 0);
                    }
                    else
                    {
                        Logger.LogInfo($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                        CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    }
                }
                else
                    Logger.LogInfo("Set New Pe: Solution Not Found!");
            }
            else if (newAp) // Working
            {
                Logger.LogInfo("Set New Ap");
                Debug.Log("Set New Ap");
                var TimeToPe = orbit.TimeToPe; // activeVessel.Orbit
                var burnUT = UT + TimeToPe;
                Logger.LogInfo($"Seeking Solution: targetApR {targetApR} m, currentApR {orbit.Apoapsis} m"); // activeVessel.Orbit
                Debug.Log($"Seeking Solution: targetApR {targetApR} m, currentApR {orbit.Apoapsis} m"); // activeVessel.Orbit
                var deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, burnUT, targetApR); // activeVessel.Orbit
                // var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(activeVessel.Orbit, burnUT, activeVessel.Orbit.Periapsis, targetApR);
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    // burnParams.z *= -1; // Why do we need this?
                    Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo("Set New Ap: Solution Not Found!");
            }
            else if (newPeAp) // Sorta almost works, but this is a kludge!
            {
                Logger.LogInfo("Set New Pe and Ap");
                if (targetPeR1 > targetApR1)
                    (targetPeR1, targetApR1) = (targetApR1, targetPeR1);
                Logger.LogInfo($"Seeking Solution: targetPeR {targetPeR1} m, targetApR {targetApR1} m, body.radius {orbit.referenceBody.radius} m"); // activeVessel.Orbit
                var burnUT = UT + 30;
                var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(orbit, burnUT, targetPeR1, targetApR1); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    // Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                    // burnParams.z *= -1; // Why do we need this?
                    // burnParams.y = 0; // Why do we need this?
                    Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo("Set New Pe and Ap: Solution Not Found!");
            }
            else if (newInc) // Working except for slight error in application of burnUT!
            {
                Logger.LogInfo("Set New Inclination");
                Logger.LogInfo($"Seeking Solution: targetInc {targetInc}°");
                double burnUT, TAN, TDN;
                Vector3d deltaV, deltaV1, deltaV2;
                if (orbit.eccentricity < 1) // Eliptical orbit: Pick cheapest deltaV between AN and DN // activeVessel.Orbit
                {
                    TAN = orbit.TimeOfAscendingNodeEquatorial(UT); // activeVessel.Orbit
                    TDN = orbit.TimeOfDescendingNodeEquatorial(UT); // activeVessel.Orbit
                    deltaV1 = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, TAN, targetInc); // activeVessel.Orbit
                    deltaV2 = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, TDN, targetInc); // activeVessel.Orbit
                    if (deltaV1.magnitude < deltaV2.magnitude)
                    {
                        Logger.LogInfo($"Selecting maneuver at Ascending Node");
                        burnUT = TAN;
                        deltaV = deltaV1;
                    }
                    else
                    {
                        Logger.LogInfo($"Selecting maneuver at Descending Node");
                        burnUT = TDN;
                        deltaV = deltaV2;
                    }
                    Logger.LogInfo($"deltaV1 (AN) [{deltaV1.x}, {deltaV1.y}, {deltaV1.z}] = {deltaV1.magnitude} m/s {TAN - UT} s from UT");
                    Logger.LogInfo($"deltaV2 (DN) [{deltaV2.x}, {deltaV2.y}, {deltaV2.z}] = {deltaV2.magnitude} m/s {TDN - UT} s from UT");
                }
                else // parabolic or hyperbolic orbit: Do it now!
                {
                    burnUT = game.UniverseModel.UniversalTime + 30;
                    deltaV = OrbitalManeuverCalculator.DeltaVToChangeInclination(orbit, burnUT, targetInc); // activeVessel.Orbit
                }
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV2, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV      [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams  [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo("Set New Inclination: Solution Not Found!");
            }
            else if (matchPlanesA) // Working except for slight error in application of burnUT!
            {
                Logger.LogInfo($"Match Planes  with {currentTarget.Name} at AN");
                double burnUT;
                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    deltaV.z *= -1; // why?
                    Logger.LogInfo($"Solution Found: deltaV*    [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT (prograde flipped)");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo($"Match Planes with {currentTarget.Name} at AN: Solution Not Found!");
            }
            else if (matchPlanesD) // Working except for slight error in application of burnUT!
            {
                Logger.LogInfo($"Match Planes with {currentTarget.Name} at DN");
                double burnUT;
                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT); // activeVessel.Orbit
                burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo($"Match Planes with {currentTarget.Name} at DN: Solution Not Found!");
            }
            else if (hohmannT) // Works if we start in a good enough orbit (reasonably circular, close to target's orbital plane)
            {
                Logger.LogInfo($"Hohmann Transfer to {currentTarget.Name}");
                Debug.Log("Hohmann Transfer");
                double burnUT;
                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo($"Hohmann Transfer to {currentTarget.Name}: Solution Not Found!");
            }
            else if (interceptAtTime) // Not Working
            // Adapted from call found in MechJebModuleScriptActionRendezvous.cs for "Get Closer"
            // Similar to code in MechJebModuleRendezvousGuidance.cs for "Get Closer" button code.
            {
                Logger.LogInfo($"Intercept {currentTarget.Name} at Time");
                var burnUT = UT + 30;
                var interceptUT = UT + interceptT;
                double offsetDistance;
                Logger.LogInfo($"Seeking Solution: interceptT {interceptT} s");
                if (currentTarget.IsCelestialBody) // For a target that is a celestial
                    offsetDistance = currentTarget.Orbit.referenceBody.radius + 50000;
                else
                    offsetDistance = 100;
                var deltaV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, burnUT, currentTarget.Orbit as PatchedConicsOrbit, interceptUT, offsetDistance); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {interceptUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo($"Intercept {currentTarget.Name} at Time: No Solution Found!");
            }
            else if (courseCorrection) // Works at least some times...
            {
                Logger.LogInfo($"Course Correction for tragetory to {currentTarget.Name}");
                double burnUT;
                Vector3d deltaV;
                if (currentTarget.IsCelestialBody) // For a target that is a celestial
                {
                    Logger.LogInfo($"Seeking Solution for Celestial Target");
                    double finalPeR = currentTarget.CelestialBody.radius + 50000; // m (PeR at celestial target)
                    deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, currentTarget.Orbit.referenceBody, finalPeR, out burnUT); // activeVessel.Orbit
                }
                else // For a tartget that is not a celestial
                {
                    Logger.LogInfo($"Seeking Solution for Non-Celestial Target");
                    double caDistance = 100; // m (closest approach to non-celestial target)
                    deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, caDistance, out burnUT); // activeVessel.Orbit
                }
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo($"Course Correction for tragetory to {currentTarget.Name}: No Solution Found!");
            }
            else if (moonReturn) // Works at least sometimes...
            {
                Logger.LogInfo("Moon Return");
                var e = orbit.eccentricity; // activeVessel.Orbit
                if (e > 0.2)
                {
                    Logger.LogInfo($"Moon Return Error: Starting Orbit Eccentrity Too Large");
                    Logger.LogError($"Moon Return starting orbit eccentricty {e.ToString("F2")} is > 0.2");
                }
                else
                {
                    double burnUT;
                    double primaryRaidus = orbit.referenceBody.Orbit.referenceBody.radius + 100000; // m // activeVessel.Orbit
                    Logger.LogInfo($"Moon Return Attempting to Solve...");
                    var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(orbit, UT, primaryRaidus, out burnUT); // activeVessel.Orbit
                    if (deltaV != Vector3d.zero)
                    {
                        burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                        // burnParams.z *= -1;
                        Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                        Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                        CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                    }
                    else
                        Logger.LogInfo("Moon Return: No Solution Found!");
                }
            }
            else if (matchVCA) // untested
            {
                Logger.LogInfo($"Match Velocity with {currentTarget.Name} at Closest Approach");
                double closestApproachTime = orbit.NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT + 2); //+2 so that closestApproachTime is definitely > UT // activeVessel.Orbit
                var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, closestApproachTime, currentTarget.Orbit as PatchedConicsOrbit); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(closestApproachTime, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, closestApproachTime); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s {closestApproachTime - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {closestApproachTime - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, closestApproachTime, -0.5);
                }
                else
                    Logger.LogInfo($"Match Velocity with {currentTarget.Name} at Closest Approach: No Solution Found!");
            }
            else if (matchVNow) // untested
            {
                Logger.LogInfo($"Match Velocity with {currentTarget.Name} Now");
                var burnUT = UT + 30;
                var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, burnUT, currentTarget.Orbit as PatchedConicsOrbit); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo($"Match Velocity with {currentTarget.Name} Now: No Solution Found!");
            }
            else if (planetaryXfer) // Not Working. At minimum, this will not work until DeltaVToChangeApoapsis works, which is failing on calls to solve with BrentRoot
            {
                Logger.LogInfo($"Planetary Transfer to {currentTarget.Name}");
                double burnUT;
                bool syncPhaseAngle = true;
                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, syncPhaseAngle, out burnUT); // activeVessel.Orbit
                if (deltaV != Vector3d.zero)
                {
                    burnParams = orbit.DeltaVToManeuverNodeCoordinates(burnUT, deltaV); // OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT); // activeVessel.Orbit
                    // burnParams.z *= -1;
                    Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s {burnUT - UT} s from UT");
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s  = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT, -0.5);
                }
                else
                    Logger.LogInfo($"Planetary Transfer to {currentTarget.Name}: No Solution Found!");
            }
        }
    }

    private IPatchedOrbit GetLastOrbit(bool silent = true)
    {
        // Logger.LogInfo("GetLastOrbit");
        List<ManeuverNodeData> patchList =
            Game.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.SimulationObject.GlobalId);

        if (!silent)
            Logger.LogMessage($"GetLastOrbit: patchList.Count = {patchList.Count}");

        if (patchList.Count == 0)
        {
            if (!silent)
                Logger.LogMessage($"GetLastOrbit: last orbit is activeVessel.Orbit: {activeVessel.Orbit}");
            return activeVessel.Orbit;
        }
        // Logger.LogMessage($"GetLastOrbit: ManeuverTrajectoryPatch = {patchList[patchList.Count - 1].ManeuverTrajectoryPatch}");
        IPatchedOrbit lastOrbit = patchList[patchList.Count - 1].ManeuverTrajectoryPatch;
        if (!silent)
            Logger.LogMessage($"GetLastOrbit: last orbit is patch {patchList.Count - 1}: {lastOrbit}");

        return lastOrbit;
    }

    private void CreateManeuverNodeAtTA(Vector3d burnVector, double TrueAnomalyRad, double burnDurationOffsetFactor = -0.5)
    {
        // Logger.LogInfo("CreateManeuverNodeAtTA");
        PatchedConicsOrbit referencedOrbit = GetLastOrbit(false) as PatchedConicsOrbit;
        if (referencedOrbit == null)
        {
            Logger.LogError("CreateManeuverNode: referencedOrbit is null!");
            return;
        }

        double UT = referencedOrbit.GetUTforTrueAnomaly(TrueAnomalyRad, 0);

        CreateManeuverNodeAtUT(burnVector, UT, burnDurationOffsetFactor);
    }

    private void CreateManeuverNodeAtUT(Vector3d burnVector, double UT, double burnDurationOffsetFactor = -0.5)
    {
        // Logger.LogInfo("CreateManeuverNodeAtUT");

        //PatchedConicsOrbit referencedOrbit = GetLastOrbit(false) as PatchedConicsOrbit;
        //if (referencedOrbit == null)
        //{
        //    Logger.LogError("CreateManeuverNode: referencedOrbit is null!");
        //    return;
        //}

        if (UT < game.UniverseModel.UniversalTime + 1) // Don't set node to now or in the past
            UT = game.UniverseModel.UniversalTime + 1;
        
        ManeuverNodeData nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, false, UT);

        //IPatchedOrbit orbit = referencedOrbit;

        //orbit.PatchStartTransition = PatchTransitionType.Maneuver;
        //orbit.PatchEndTransition = PatchTransitionType.Final;

        //nodeData.SetManeuverState((PatchedConicsOrbit)orbit);

        nodeData.BurnVector = burnVector;

        //Logger.LogInfo($"CreateManeuverNodeAtUT: BurnVector [{burnVector.x}, {burnVector.y}, {burnVector.z}] m/s");
        //Logger.LogInfo($"CreateManeuverNodeAtUT: BurnDuration {nodeData.BurnDuration} s");
        //Logger.LogInfo($"CreateManeuverNodeAtUT: Burn Time {nodeData.Time} s");

        AddManeuverNode(nodeData, burnDurationOffsetFactor);
    }

    private void AddManeuverNode(ManeuverNodeData nodeData, double burnDurationOffsetFactor)
    {
        //Logger.LogInfo("AddManeuverNode");

        // Add the node to the vessel's orbit
        GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);

        // For KSP2, We want the to start burns early to make them centered on the node
        var nodeTimeAdj = nodeData.BurnDuration * burnDurationOffsetFactor;

        //Logger.LogInfo($"AddManeuverNode: BurnVector   [{nodeData.BurnVector.x}, {nodeData.BurnVector.y}, {nodeData.BurnVector.z}] m/s");
        Logger.LogInfo($"AddManeuverNode: BurnDuration {nodeData.BurnDuration} s");
        //Logger.LogInfo($"AddManeuverNode: Burn Time    {nodeData.Time}");

        // Refersh the currentNode with what we've produced here in prep for calling UpdateNode
        currentNode = getCurrentNode();

        StartCoroutine(UpdateNode(nodeData, nodeTimeAdj));

        //Logger.LogInfo("AddManeuverNode Done");
    }

    private IEnumerator UpdateNode(ManeuverNodeData nodeData, double nodeTimeAdj = 0)
    {
        MapCore mapCore = null;
        Game.Map.TryGetMapCore(out mapCore);
        var m3d = mapCore.map3D;
        var maneuverManager = m3d.ManeuverManager;
        IGGuid simID;
        SimulationObjectModel simObject;

        // Get the ManeuverPlanComponent for the active vessel
        var universeModel = game.UniverseModel;
        VesselComponent vesselComponent;
        ManeuverPlanComponent maneuverPlanComponent;
        if (currentNode != null)
        {
            simID = currentNode.RelatedSimID;
            // vc1 = universeModel.FindVesselComponent(simID);
            // vesselComponent = vc1;
            simObject = universeModel.FindSimObject(simID);
        }
        else
        {
            // vc2 = activeVessel;
            vesselComponent = activeVessel;
            simObject = vesselComponent?.SimulationObject;
        }

        if (simObject != null)
        {
            maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();
        }
        else
        {
            simObject = universeModel.FindSimObject(simID);
            maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();
        }

        if (nodeTimeAdj != 0)
        {
            nodeData.Time += nodeTimeAdj;
            if (nodeData.Time < game.UniverseModel.UniversalTime + 1) // Don't set node in the past
                nodeData.Time = game.UniverseModel.UniversalTime + 1;
            maneuverPlanComponent.UpdateTimeOnNode(nodeData, nodeData.Time); // This may not be necessary?
        }

        // Wait a tick for things to get created
        yield return new WaitForFixedUpdate();

        try { maneuverPlanComponent.RefreshManeuverNodeState(0); } // Occasionally getting NREs here...
        catch (NullReferenceException e) { Logger.LogError($"UpdateNode: caught NRE on call to maneuverPlanComponent.RefreshManeuverNodeState(0): {e}"); }

        // Wait a tick for things to get created
        yield return new WaitForFixedUpdate();

        // Manage the maneuver on the map
        maneuverManager.RemoveAll();
        try { maneuverManager?.GetNodeDataForVessels(); }
        catch (Exception e) { Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels(): {e}"); }
        try { maneuverManager.UpdateAll(); }
        catch (Exception e) { Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll(): {e}"); }
        try { maneuverManager.UpdatePositionForGizmo(nodeData.NodeID); }
        catch (Exception e) { Logger.LogError($"UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo(): {e}"); }

        // Set the currentNode  to the node we just created and added to the vessel
        // currentNode = nodeData;
    }
}
