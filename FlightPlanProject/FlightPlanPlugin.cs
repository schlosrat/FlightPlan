using BepInEx;
using HarmonyLib;
using KSP.UI.Binding;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
// using SpaceWarp.API.Game;
// using SpaceWarp.API.Game.Extensions;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using UnityEngine;
using MuMech;
using KSP.Game;
// using KSP.Messages.PropertyWatchers;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.Sim;
using KSP.Map;
using BepInEx.Logging;
// using static UnityEngine.GraphicsBuffer;
// using KSP.Messages.PropertyWatchers;
// using static UnityEngine.ParticleSystem;
// using static UnityEngine.ParticleSystem.PlaybackState;
// using UnityEngine.UIElements;
// using static KSP.UI.Binding.Core.UIValue_ReadEnum_TextSet;
// using static UnityEngine.UIElements.StyleSheets.Dimension;
// using UnityEngine.Bindings;
// using MoonSharp.Interpreter.CodeAnalysis;

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

    static bool loaded = false;
    private bool interfaceEnabled = false;
    // private bool _isWindowOpen;
    private Rect _windowRect;
    private int windowWidth = Screen.width / 6; //384px on 1920x1080
    private int windowHeight = Screen.height / 3; //360px on 1920x1080
    private Rect closeBtnRect;

    // Button bools
    private bool circAp, circPe, circNow, newPe, newAp, newPeAp, newInc, matchPlanesA, matchPlanesD, hohmannT, interceptAtTime, courseCorrection, moonReturn, matchVCA, matchVNow, planetaryXfer;

    // Target Selection
    private enum TargetOptions
    {
        Kerbin,
        Mun,
        Minmus,
        Duna
    }
    private TargetOptions selectedTargetOption = TargetOptions.Mun;
    private readonly List<string> targetOptions = new List<string> { "Kerbin", "Mun", "Minmus", "Duna" };
    private bool selectingTargetOption = false;
    private static Vector2 scrollPositionTargetOptions;
    private bool applyTargetOption;

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
    private string targetIncStr  = "0";      // degrees
    private string interceptTStr = "100";    // s

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
    private string unitColorHex;
    private int spacingAfterHeader = -12;
    private int spacingAfterEntry = -5;
    private int spacingAfterSection = 5;

    // App bar button(s)
    private const string ToolbarFlightButtonID = "BTN-FlightPlanFlight";
    // private const string ToolbarOABButtonID = "BTN-FlightPlanOAB";

    // Hot Key config setting
    private string hotKey;

    public ManualLogSource logger;

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        // base.OnInitialized();

        // logger = BepInEx.Logging.Logger.CreateLogSource("FlightPlan");

        logger = Logger;
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
            fixedWidth = (int)(windowWidth / 2),
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

        // Try to get the currently active vessel, set its throttle to 100% and toggle on the landing gear
        //try
        //{
        //    var currentVessel = Vehicle.ActiveVesselVehicle;
        //    if (currentVessel != null)
        //    {
        //        currentVessel.SetMainThrottle(1.0f);
        //        currentVessel.SetGearState(true);
        //    }
        //}
        //catch (Exception e) {}

        // Fetch a configuration value or create a default one if it does not exist
        // var defaultValue = "LeftAlt P";
        // hotKey = Config.Bind<string>("Settings section", "Hot Key", defaultValue, "Keyboard shortcut key to launch mod").Value;

        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        // Logger.LogInfo($"Hot Key: {hotKey}");

        // activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        // targetPeR = double.Parse(targetPeAStr) + activeVessel.Orbit.referenceBody.radius;
        // targetApR = double.Parse(targetApAStr) + activeVessel.Orbit.referenceBody.radius;
    }

    private void ToggleButton(bool toggle)
    {
        interfaceEnabled = toggle;
        GameObject.Find("BTN-FlightPlan")?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(toggle);
    }

    void Awake()
    {
        _windowRect = new Rect((Screen.width * 0.7f) - (windowWidth / 2), (Screen.height / 2) - (windowHeight / 2), 0, 0);
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
        // activeVessel = Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        currentTarget = activeVessel.TargetObject;

        targetPeR  = double.Parse(targetPeAStr)  + activeVessel.Orbit.referenceBody.radius;
        targetApR  = double.Parse(targetApAStr)  + activeVessel.Orbit.referenceBody.radius;
        targetPeR1 = double.Parse(targetPeAStr1) + activeVessel.Orbit.referenceBody.radius;
        targetApR1 = double.Parse(targetApAStr1) + activeVessel.Orbit.referenceBody.radius;
        targetInc  = double.Parse(targetIncStr);

        // Set the UI
        if (interfaceEnabled)
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
        }
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
        game = GameManager.Instance.Game;
        activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        currentNode = (activeNodes.Count() > 0) ? activeNodes[0] : null;

        string tgtName;
        if (currentTarget == null)
            tgtName = "None";
        else
            tgtName = currentTarget.Name;
        DrawSectionHeader("Target", tgtName);

        //GUILayout.BeginHorizontal();
        //GUILayout.Label("New Target:");
        //GUILayout.FlexibleSpace();
        //if (!selectingTargetOption)
        //{
        //    if (GUILayout.Button(Enum.GetName(typeof(TargetOptions), selectedTargetOption)))
        //        selectingTargetOption = true;
        //}
        //else
        //{
        //    GUILayout.BeginVertical(GUI.skin.GetStyle("Box"), GUILayout.Width(windowWidth / 4));
        //    scrollPositionTargetOptions = GUILayout.BeginScrollView(scrollPositionTargetOptions, false, true, GUILayout.Height(70));

        //    foreach (string snapOption in Enum.GetNames(typeof(TargetOptions)).ToList())
        //    {
        //        if (GUILayout.Button(snapOption))
        //        {
        //            Enum.TryParse(snapOption, out selectedTargetOption);
        //            selectingTargetOption = false;
        //        }
        //    }

        //    GUILayout.EndScrollView();
        //    GUILayout.EndVertical();
        //}

        //applyTargetOption = GUILayout.Button("Select", GUILayout.Width(windowWidth / 5));
        //GUILayout.EndHorizontal();

        DrawSectionHeader("Ownship Maneuvers");
        // circAp = GUILayout.Button("Circularize at Ap", GUILayout.Width(windowWidth / 2));
        DrawButton("Circularize at Ap", ref circAp);
        // circPe = GUILayout.Button("Circularize at Pe", GUILayout.Width(windowWidth / 2));
        DrawButton("Circularize at Pe", ref circPe);
        // circNow = GUILayout.Button("Circularize Now", GUILayout.Width(windowWidth / 2));
        DrawButton("Circularize Now", ref circNow);

        // newPe = GUILayout.Button("New Pe", GUILayout.Width(windowWidth / 2));
        DrawButtonWithTextField("New Pe", ref newPe, ref targetPeAStr, "m");
        // targetPeAStr = "20000";
        targetPeR = double.Parse(targetPeAStr) + activeVessel.Orbit.referenceBody.radius;
        // newAp = GUILayout.Button("New Ap", GUILayout.Width(windowWidth / 2));
        DrawButtonWithTextField("New Ap", ref newAp, ref targetApAStr, "m");
        // targetApAStr = "250000";
        targetApR = double.Parse(targetApAStr) + activeVessel.Orbit.referenceBody.radius;
        // newPeAp = GUILayout.Button("New Pe & Ap", GUILayout.Width(windowWidth / 2));
        DrawButtonWithDualTextField("New Pe & Ap", ref newPeAp, ref targetPeAStr1, ref targetApAStr1);
        // targetPeAStr1 = "20000";
        // targetApAStr1 = "250000";
        targetPeR1 = double.Parse(targetPeAStr1) + activeVessel.Orbit.referenceBody.radius;
        targetApR1 = double.Parse(targetApAStr1) + activeVessel.Orbit.referenceBody.radius;
        // newInc = GUILayout.Button("New Inclination", GUILayout.Width(windowWidth / 2));
        DrawButtonWithTextField("New Inclination", ref newInc, ref targetIncStr, "°");
        // targetIncStr = "20";
        targetInc = double.Parse(targetIncStr);

        if (currentTarget != null)
        {
            DrawSectionHeader("Maneuvers Relative to Target");
            DrawButton("Match Planes at AN", ref matchPlanesA);
            DrawButton("Match Planes at DN", ref matchPlanesD);
            DrawButton("Hohmann Xfer", ref hohmannT);
            DrawButtonWithTextField("Intercept at Time", ref interceptAtTime, ref interceptTStr, "s");
            interceptT = double.Parse(interceptTStr);
            DrawButton("Course Correction", ref courseCorrection);
            DrawButton("Match Velocity @CA", ref matchVCA);
            DrawButton("Match Velocity Now", ref matchVNow);
            //matchPlanesA = GUILayout.Button("Match Planes at AN", GUILayout.Width(windowWidth / 2));
            //matchPlanesD = GUILayout.Button("Match Planes at DN", GUILayout.Width(windowWidth / 2));
            //hohmannT = GUILayout.Button("Hohmann Xfer", GUILayout.Width(windowWidth / 2));
            //interceptAtTime = GUILayout.Button("Intercept at Time", GUILayout.Width(windowWidth / 2));
            //courseCorrection = GUILayout.Button("Course Correction", GUILayout.Width(windowWidth / 2));
            //matchVCA = GUILayout.Button("Match Velocity @ CA", GUILayout.Width(windowWidth / 2));
            //matchVNow = GUILayout.Button("Match Velocity Now", GUILayout.Width(windowWidth / 2));
            if (currentTarget.Orbit.referenceBody.GlobalId == activeVessel.Orbit.referenceBody.Orbit.referenceBody.GlobalId)
            {
                DrawSectionHeader("Interplanetary Maneuvers");
                DrawButton("Interplanetary Transfer", ref planetaryXfer);
                // planetaryXfer = GUILayout.Button("Interplanetary Transfer", GUILayout.Width(windowWidth / 2));
            }
        }
        var referenceBody = activeVessel.Orbit.referenceBody;
        if (referenceBody.referenceBody != null)
        {
            DrawSectionHeader("Moon Specific Maneuvers");
            DrawButton("Moon Return", ref moonReturn);
            // moonReturn = GUILayout.Button("Moon Return", GUILayout.Width(windowWidth / 2));
        }

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
        ToggleButton(interfaceEnabled);
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
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
        GUILayout.Space(5);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawButton(string buttonStr, ref bool button)
    {
        GUILayout.BeginHorizontal();
        // GUILayout.Label(entryName, nameLabelStyle);
        button = GUILayout.Button(buttonStr, bigBtnStyle);
        GUILayout.FlexibleSpace();
        //textEntry = GUILayout.TextField(textEntry, textInputStyle);
        // GUILayout.Space(25);
        // GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }
    private void DrawButtonWithTextField(string entryName, ref bool button, ref string textEntry, string unit = "")
    {
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(entryName, smallBtnStyle);
        GUILayout.Space(10);
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
        GUILayout.Space(3);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.FlexibleSpace();
        // button = GUILayout.Button("Create Node", smallBtnStyle);
        // GUILayout.Space(5);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }
    private void DrawButtonWithDualTextField(string entryName, ref bool button, ref string textEntry1, ref string textEntry2, string unit = "")
    {
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(entryName, smallBtnStyle);
        GUILayout.Space(5);
        textEntry1 = GUILayout.TextField(textEntry1, textInputStyle);
        GUILayout.Space(5);
        textEntry2 = GUILayout.TextField(textEntry2, textInputStyle);
        GUILayout.Space(3);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.FlexibleSpace();
        // button = GUILayout.Button("Create Node", smallBtnStyle);
        // GUILayout.Space(5);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }


    private void handleButtons()
    {
        //if (currentNode == null)
        //{
        //    if (addNode)
        //    {
        //        // Add an empty maneuver node
        //        Logger.LogInfo("Adding New Node");

        //        // Define empty node data
        //        burnParams = Vector3d.zero;
        //        double UT = game.UniverseModel.UniversalTime;
        //        if (activeVessel.Orbit.eccentricity < 1)
        //        {
        //            UT += activeVessel.Orbit.TimeToAp;
        //        }

        //        // Create the nodeData structure
        //        ManeuverNodeData nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, false, game.UniverseModel.UniversalTime);

        //        // Populate the nodeData structure
        //        nodeData.BurnVector.x = 0;
        //        nodeData.BurnVector.y = 0;
        //        nodeData.BurnVector.z = 0;
        //        nodeData.Time = UT;

        //        // Add the new node to the vessel
        //        GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);

        //        // Update the map so the gizmo will be there
        //        MapCore mapCore = null;
        //        Game.Map.TryGetMapCore(out mapCore);

        //        mapCore.map3D.ManeuverManager.GetNodeDataForVessels();
        //        mapCore.map3D.ManeuverManager.UpdatePositionForGizmo(nodeData.NodeID);
        //        mapCore.map3D.ManeuverManager.UpdateAll();

        //        // Refresh stuff
        //        activeVessel.SimulationObject.ManeuverPlan.UpdateChangeOnNode(nodeData, burnParams);
        //        activeVessel.SimulationObject.ManeuverPlan.RefreshManeuverNodeState(0);

        //        // Set teh currentNode to be the node we just added
        //        currentNode = nodeData;
        //        // addNode = false;
        //    }
        //    else return;
        //}

        if (circAp || circPe || circNow|| newPe || newAp || newPeAp || newInc || matchPlanesA || matchPlanesD || hohmannT || interceptAtTime || courseCorrection || moonReturn || matchVCA || matchVNow || planetaryXfer)
        {
            Vector3d burnParams;
            double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;

            if (circAp) // Seems OK
            {
                Logger.LogInfo("Flight Plan: Circularize at Ap");
                var TimeToAp = activeVessel.Orbit.TimeToAp;
                var burnUT = UT + TimeToAp;
                burnParams = OrbitalManeuverCalculator.DeltaVToCircularize(activeVessel.Orbit, burnUT);
                Logger.LogInfo($"Flight Plan Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
            }
            else if (circPe) // Seems OK
            {
                Logger.LogInfo("Flight Plan: Circularize at Pe");
                var TimeToPe = activeVessel.Orbit.TimeToPe;
                var burnUT = UT + TimeToPe;
                burnParams = OrbitalManeuverCalculator.DeltaVToCircularize(activeVessel.Orbit, burnUT);
                Logger.LogInfo($"Flight Plan Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
            }
            else if (circNow) // Seems OK
            {
                Logger.LogInfo("Flight Plan: Circularize Now");
                burnParams = OrbitalManeuverCalculator.DeltaVToCircularize(activeVessel.Orbit, UT);
                Logger.LogInfo($"Flight Plan Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, UT);
            }
            else if (newPe) // Does not create node!
            {
                Logger.LogInfo("Flight Plan: Set New Pe");
                var TimeToAp = activeVessel.Orbit.TimeToAp;
                var burnUT = UT + TimeToAp;
                // double newPeR = activeVessel.Orbit.referenceBody.radius + 50000; // m
                // double newPeR = activeVessel.Orbit.Periapsis * 0.9;
                Logger.LogInfo($"Seeking Solution: targetPeR {targetPeR} m, currentPeR {activeVessel.Orbit.Periapsis} m");
                burnParams = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(activeVessel.Orbit, burnUT, targetPeR);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
            }
            else if (newAp) // Does not create node!
            {
                Logger.LogInfo("Flight Plan: Set New Ap");
                var TimeToPe = activeVessel.Orbit.TimeToPe;
                var burnUT = UT + TimeToPe;
                // double newApR = activeVessel.Orbit.referenceBody.radius + 250000; // m
                // double newApR = activeVessel.Orbit.Apoapsis * 1.5;
                Logger.LogInfo($"Seeking Solution: targetApR {targetApR} m, currentApR {activeVessel.Orbit.Apoapsis} m");
                burnParams = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(activeVessel.Orbit, burnUT, targetApR);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
            }
            else if (newPeAp) // Create bizare node!
            {
                Logger.LogInfo("Flight Plan: Set New Pe and Ap");
                // double newPeR = activeVessel.Orbit.referenceBody.radius + 50000; // m;
                // double newApR = activeVessel.Orbit.referenceBody.radius + 250000; // m;
                // double newPeR = activeVessel.Orbit.Periapsis * 0.9;
                // double newApR = activeVessel.Orbit.Apoapsis * 1.5;
                Logger.LogInfo($"Seeking Solution: targetPeR {targetPeR1} m, targetApR {targetApR1} m");
                burnParams = OrbitalManeuverCalculator.DeltaVToEllipticize(activeVessel.Orbit, UT, targetPeR1, targetApR1);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, UT);
            }
            else if (newInc) // Bizzare results, sometimes leaves SOI, does not seem to get to correct inc in any case.
            {
                Logger.LogInfo("Flight Plan: Set New Inclination");
                // double newInclination = 20;  // in degrees
                var TAN = activeVessel.Orbit.TimeOfAscendingNodeEquatorial(UT);
                var TDN = activeVessel.Orbit.TimeOfDescendingNodeEquatorial(UT);
                Logger.LogInfo($"Seeking Solution: targetInc {targetInc} °");
                burnParams = OrbitalManeuverCalculator.DeltaVToChangeInclination(activeVessel.Orbit, TAN, targetInc);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {TAN - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, TAN);
            }
            else if (matchPlanesA) // leaves SOI at inclination unlike that of target
            {
                Logger.LogInfo("Flight Plan: Match Planes at AN");
                double burnUT;
                burnParams = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(activeVessel.Orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
            }
            else if (matchPlanesD) // Seems OK
            {
                Logger.LogInfo("Flight Plan: Match Planes at DN");
                double burnUT;
                burnParams = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(activeVessel.Orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
            }
            else if (hohmannT) // Untested
            {
                Logger.LogInfo("Flight Plan: Hohmann Transfer");
                double burnUT;
                burnParams = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(activeVessel.Orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
            }
            else if (interceptAtTime) // Untested - adapted from call found in MechJebModuleScriptActionRendezvous.cs for "Get Closer"
                // Similar to code in MechJebModuleRendezvousGuidance.cs for "Get Closer" button code.
            {
                Logger.LogInfo("Flight Plan: Intercept at Time");
                var interceptUT = UT + interceptT;
                Logger.LogInfo($"Seeking Solution: interceptT {interceptT} s");
                burnParams = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(activeVessel.Orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, interceptUT, 10);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {interceptUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, interceptUT);
            }
            else if (courseCorrection) // Untested
            {
                Logger.LogInfo("Flight Plan: Course Correction");
                double burnUT;
                if (currentTarget.GetType() == typeof(CelestialBodyComponent)) // For a target that is a celestial
                {
                    Logger.LogInfo($"Seeking Solution for Celestial Target");
                    double finalPeR = currentTarget.Orbit.referenceBody.radius + 50000; // m (PeR at celestial target)                           
                    burnParams = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(activeVessel.Orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, currentTarget.Orbit.referenceBody, finalPeR, out burnUT);
                }
                else // For a tartget that is not a celestial
                {
                    Logger.LogInfo($"Seeking Solution for Non-Celestial Target");
                    double caDistance = 100; // m (closest approach to non-celestial target)
                    burnParams = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(activeVessel.Orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, caDistance, out burnUT);
                }
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
            }
            else if (moonReturn) // Does not create node
            {
                Logger.LogInfo("Flight Plan: Moon Return");
                var e = activeVessel.Orbit.eccentricity;
                if (e > 0.2)
                {
                    Logger.LogInfo($"Flight Plan: Moon Return Error: Starting Orbit Eccentrity Too Large");
                    Logger.LogError($"Flight Plan: Moon Return starting orbit eccentricty {e.ToString("F2")} is > 0.2");
                }
                else
                {
                    double burnUT;
                    double primaryRaidus = activeVessel.Orbit.referenceBody.Orbit.referenceBody.radius; // m
                    Logger.LogInfo($"Moon Return Attempting to Solve...");
                    burnParams = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(activeVessel.Orbit, UT, primaryRaidus, out burnUT);
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
                }
            }
            else if (matchVCA) // untested
            {
                Logger.LogInfo("Flight Plan: Match Velocity with Target at Closest Approach");
                double closestApproachTime = activeVessel.Orbit.NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT + 2); //+2 so that closestApproachTime is definitely > UT
                burnParams = OrbitalManeuverCalculator.DeltaVToMatchVelocities(activeVessel.Orbit, closestApproachTime, currentTarget.Orbit as PatchedConicsOrbit);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {closestApproachTime - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, closestApproachTime);
            }
            else if (matchVNow) // untested
            {
                Logger.LogInfo("Flight Plan: Match Velocity with Target at Closest Approach");
                // double closestApproachTime = activeVessel.Orbit.NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT + 2); //+2 so that closestApproachTime is definitely > UT
                burnParams = OrbitalManeuverCalculator.DeltaVToMatchVelocities(activeVessel.Orbit, UT, currentTarget.Orbit as PatchedConicsOrbit);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, UT);
            }
            else if (planetaryXfer) // untested
            {
                Logger.LogInfo("Flight Plan: Planetary Transfer");
                double burnUT;
                bool syncPhaseAngle = true;
                burnParams = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(activeVessel.Orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, syncPhaseAngle, out burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(activeVessel.Orbit, burnParams, burnUT);
            }

            //else if (snapToAp) // Snap the maneuver time to the next Ap
            //{
            //    currentNode.Time = game.UniverseModel.UniversalTime + game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeToAp;
            //}
            //else if (snapToPe) // Snap the maneuver time to the next Pe
            //{
            //    currentNode.Time = game.UniverseModel.UniversalTime + game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeToPe;
            //}
            //else if (snapToANe) // Snap the maneuver time to the AN relative to the equatorial plane
            //{
            //    Logger.LogInfo("Snapping Maneuver Time to TimeOfAscendingNodeEquatorial");
            //    var UT = game.UniverseModel.UniversalTime;
            //    var TAN = activeVessel.Orbit.TimeOfAscendingNodeEquatorial(UT);
            //    var ANTA = activeVessel.Orbit.AscendingNodeEquatorialTrueAnomaly();
            //    // var TAN = game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeOfAscendingNodeEquatorial(UT);
            //    currentNode.Time = TAN; // game.UniverseModel.UniversalTime + TAN;
            //    Logger.LogInfo($"UT: {UT}");
            //    Logger.LogInfo($"AscendingNodeEquatorialTrueAnomaly: {ANTA}");
            //    Logger.LogInfo($"TimeOfAscendingNodeEquatorial: {TAN}");
            //    // Logger.LogInfo($"UT + TimeOfAscendingNodeEquatorial: {TAN + UT}");
            //}
            //else if (snapToDNe) // Snap the maneuver time to the DN relative to the equatorial plane
            //{
            //    Logger.LogInfo("Snapping Maneuver Time to TimeOfDescendingNodeEquatorial");
            //    var UT = game.UniverseModel.UniversalTime;
            //    var TDN = activeVessel.Orbit.TimeOfDescendingNodeEquatorial(UT);
            //    var DNTA = activeVessel.Orbit.DescendingNodeEquatorialTrueAnomaly();
            //    // var TDN = game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeOfDescendingNodeEquatorial(UT);
            //    currentNode.Time = TDN; // game.UniverseModel.UniversalTime + TDN;
            //    Logger.LogInfo($"UT: {UT}");
            //    Logger.LogInfo($"DescendingNodeEquatorialTrueAnomaly: {DNTA}");
            //    Logger.LogInfo($"TimeOfDescendingNodeEquatorial: {TDN}");
            //    // Logger.LogInfo($"UT + TimeOfDescendingNodeEquatorial: {TDN + UT}");
            //}
            //else if (snapToANt) // Snap the maneuver time to the AN relative to selected target's orbit
            //{
            //    Logger.LogInfo("Snapping Maneuver Time to TimeOfAscendingNode");
            //    var UT = game.UniverseModel.UniversalTime;
            //    var TANt = activeVessel.Orbit.TimeOfAscendingNode(currentTarget.Orbit, UT);
            //    var ANTA = activeVessel.Orbit.AscendingNodeTrueAnomaly(currentTarget.Orbit);
            //    // var TANt = game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeOfAscendingNode(currentTarget.Orbit, UT);
            //    currentNode.Time = TANt; // game.UniverseModel.UniversalTime + TANt;
            //    Logger.LogInfo($"UT: {UT}");
            //    Logger.LogInfo($"AscendingNodeTrueAnomaly: {ANTA}");
            //    Logger.LogInfo($"TimeOfAscendingNode: {TANt}");
            //    // Logger.LogInfo($"UT + TimeOfAscendingNode: {TANt + UT}");
            //}
            //else if (snapToDNt) // Snap the maneuver time to the DN relative to selected target's orbit
            //{
            //    Logger.LogInfo("Snapping Maneuver Time to TimeOfDescendingNode");
            //    var UT = game.UniverseModel.UniversalTime;
            //    var TDNt = activeVessel.Orbit.TimeOfDescendingNode(currentTarget.Orbit, UT);
            //    var DNTA = activeVessel.Orbit.DescendingNodeTrueAnomaly(currentTarget.Orbit);
            //    // var TDNt = game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID).Orbit.TimeOfDescendingNode(currentTarget.Orbit, UT);
            //    currentNode.Time = TDNt; // game.UniverseModel.UniversalTime + TDNt;
            //    Logger.LogInfo($"UT: {UT}");
            //    Logger.LogInfo($"DescendingNodeTrueAnomaly: {DNTA}");
            //    Logger.LogInfo($"TimeOfDescendingNode: {TDNt}");
            //    // Logger.LogInfo($"UT + TimeOfDescendingNode: {TDNt + UT}");
            //}
            //activeVessel.SimulationObject.ManeuverPlan.UpdateChangeOnNode(currentNode, burnParams);
            //activeVessel.SimulationObject.ManeuverPlan.RefreshManeuverNodeState(0);
            //// game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().UpdateChangeOnNode(currentNode, burnParams);
            //// game.UniverseModel.FindVesselComponent(currentNode.RelatedSimID)?.SimulationObject.FindComponent<ManeuverPlanComponent>().RefreshManeuverNodeState(0);
        }
    }

    private IPatchedOrbit GetLastOrbit()
    {
        Logger.LogInfo("FlightPlan.GetLastOrbit()");
        List<ManeuverNodeData> patchList =
            Game.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.SimulationObject.GlobalId);

        Logger.LogMessage(patchList.Count);

        if (patchList.Count == 0)
        {
            Logger.LogMessage(activeVessel.Orbit);
            return activeVessel.Orbit;
        }
        Logger.LogMessage(patchList[patchList.Count - 1].ManeuverTrajectoryPatch);
        IPatchedOrbit orbit = patchList[patchList.Count - 1].ManeuverTrajectoryPatch;

        return orbit;
    }

    private void CreateManeuverNodeAtTA(PatchedConicsOrbit referencedOrbit, Vector3d burnVector, double TrueAnomalyRad)
    {
        Logger.LogInfo("FlightPlan.CreateManeuverNodeAtTA");
        //PatchedConicsOrbit referencedOrbit = GetLastOrbit() as PatchedConicsOrbit;
        //if (referencedOrbit == null)
        //{
        //    Logger.LogError("CreateManeuverNode: referencedOrbit is null!");
        //    return;
        //}

        double UT = referencedOrbit.GetUTforTrueAnomaly(TrueAnomalyRad, 0);

        CreateManeuverNodeAtUT(referencedOrbit, burnVector, UT);
    }

    private void CreateManeuverNodeAtUT(PatchedConicsOrbit referencedOrbit, Vector3d burnVector, double UT)
    {
        Logger.LogInfo("FlightPlan.CreateManeuverNodeAtUT");
        ManeuverNodeData nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, false, UT);

        //IPatchedOrbit orbit = referencedOrbit;

        //orbit.PatchStartTransition = PatchTransitionType.Maneuver;
        //orbit.PatchEndTransition = PatchTransitionType.Final;

        //nodeData.SetManeuverState((PatchedConicsOrbit)orbit);

        nodeData.BurnVector = burnVector;
        //Logger.LogInfo($"Flight Plan: BurnVector.x {nodeData.BurnVector.x}");
        //Logger.LogInfo($"Flight Plan: BurnVector.y {nodeData.BurnVector.y}");
        //Logger.LogInfo($"Flight Plan: BurnVector.z {nodeData.BurnVector.z}");
        //Logger.LogInfo($"Flight Plan: Burn Time    {nodeData.Time}");

        AddManeuverNode(nodeData);
    }

    private void AddManeuverNode(ManeuverNodeData nodeData)
    {
        Logger.LogInfo("FlightPlan.AddManeuverNode");
        //Logger.LogInfo($"Flight Plan: BurnVector.x {nodeData.BurnVector.x}");
        //Logger.LogInfo($"Flight Plan: BurnVector.y {nodeData.BurnVector.y}");
        //Logger.LogInfo($"Flight Plan: BurnVector.z {nodeData.BurnVector.z}");
        //Logger.LogInfo($"Flight Plan: Burn Time    {nodeData.Time}");

        GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);

        // Why not move this up to OnGUI?
        MapCore mapCore = null;
        Game.Map.TryGetMapCore(out mapCore);

        mapCore.map3D.ManeuverManager.GetNodeDataForVessels();
        mapCore.map3D.ManeuverManager.UpdatePositionForGizmo(nodeData.NodeID);
        mapCore.map3D.ManeuverManager.UpdateAll();
        // mapCore.map3D.ManeuverManager.RemoveAll();

        // activeVessel.SimulationObject.ManeuverPlan.UpdateChangeOnNode(nodeData, burnParams);
        // activeVessel.SimulationObject.ManeuverPlan.RefreshManeuverNodeState(0);
    }
}
