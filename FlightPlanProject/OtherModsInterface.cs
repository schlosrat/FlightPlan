

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using FlightPlan.UI;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

// CF : this direct dependency cause the dll to be needed during build
// it is not really needed, we can easyly hardcode the mode names
// during the introduction of K2D2 UI it cause me many trouble in naming
using K2D2;
using KSP.Sim.Maneuver;
using ManeuverNodeController;
using SpaceWarp.API.Assets;
using System.Reflection;
using UnityEngine;
using NodeManager;
using KSP.Game;

namespace FlightPlan;

public class OtherModsInterface
{
    ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("FlightPlanPlugin.OtherModsInterface");

    // Reflection access variables for launching MNC & K2-D2
    private bool MNCLoaded, K2D2Loaded, checkK2D2status  = false;
    private PluginInfo MNC, K2D2_info;
    private Version mncMinVersion, k2d2MinVersion;
    private int mncVerCheck, k2d2VerCheck;
    private string k2d2Status;
    Type k2d2Type, mncType;
    PropertyInfo k2d2PropertyInfo, mncPropertyInfo;
    MethodInfo k2d2GetStatusMethodInfo, k2d2FlyNodeMethodInfo, k2d2ToggleMethodInfo, mncLaunchMNCMethodInfo;
    object k2d2Instance, mncInstance;
    Texture2D mnc_button_tex, k2d2_button_tex;
    GUIContent mnc_button_tex_con, k2d2_button_tex_con;

    private bool launchMNC, executeNode;

    public void CheckModsVersions()
    {
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

            // Get MNC buton icon
            mnc_button_tex = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/mnc_icon.png");
            mnc_button_tex_con = new GUIContent(mnc_button_tex, "Launch Maneuver Node Controller");

            // Reflections method to attempt the same thing more cleanly
            mncType = Type.GetType($"ManeuverNodeController.ManeuverNodeControllerMod, {ManeuverNodeControllerMod.ModGuid}");
            mncPropertyInfo = mncType!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            mncInstance = mncPropertyInfo.GetValue(null);
            mncLaunchMNCMethodInfo = mncPropertyInfo!.PropertyType.GetMethod("LaunchMNC");
        }
        // else MNCLoaded = false;
        Logger.LogInfo($"MNCLoaded = {MNCLoaded}");

        Logger.LogInfo($"K2D2_Plugin.ModGuid = {K2D2_Plugin.ModGuid}");
        if (Chainloader.PluginInfos.TryGetValue(ManeuverNodeControllerMod.ModGuid, out K2D2_info))
        {
            K2D2_info = Chainloader.PluginInfos[K2D2_Plugin.ModGuid];

            K2D2Loaded = true;
            Logger.LogInfo("K2-D2 installed and available");
            Logger.LogInfo($"K2D2 = {K2D2_info}");
            k2d2MinVersion = new Version(0, 8, 1);
            k2d2VerCheck = K2D2_info.Metadata.Version.CompareTo(k2d2MinVersion);
            Logger.LogInfo($"k2d2VerCheck = {k2d2VerCheck}");
            string tooltip;
            if (k2d2VerCheck >= 0) tooltip = "Have K2-D2 Execute this node";
            else tooltip = "Launch K2-D2";

            // Get K2-D2 buton icon
            k2d2_button_tex = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/k2d2_icon.png");
            k2d2_button_tex_con = new GUIContent(k2d2_button_tex, tooltip);

            k2d2Type = Type.GetType($"K2D2.K2D2_Plugin, {K2D2_Plugin.ModGuid}");
            k2d2PropertyInfo = k2d2Type!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            k2d2Instance = k2d2PropertyInfo.GetValue(null);
            k2d2ToggleMethodInfo = k2d2PropertyInfo!.PropertyType.GetMethod("ToggleAppBarButton");
            k2d2FlyNodeMethodInfo = k2d2PropertyInfo!.PropertyType.GetMethod("FlyNode");
            k2d2GetStatusMethodInfo = k2d2PropertyInfo!.PropertyType.GetMethod("GetStatus");
        }
        // else K2D2Loaded = false;
        Logger.LogInfo($"K2D2Loaded = {K2D2Loaded}");
    }

    public void callMNC()
    {
        if (MNCLoaded && mncVerCheck >= 0)
        {
            mncLaunchMNCMethodInfo!.Invoke(mncPropertyInfo.GetValue(null), null);
        }
    }

    public void callK2D2()
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
                // Extend the status time to encompass the maneuver
                FlightPlanPlugin.Instance.statusTime = FlightPlanPlugin.Instance.currentNode.Time + FlightPlanPlugin.Instance.currentNode.BurnDuration;
                FlightPlanPlugin.Instance.statusText = FlightPlanPlugin.Instance.maneuver;
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
                    if (FlightPlanPlugin.Instance.currentNode.Time < GameManager.Instance.Game.UniverseModel.UniversalTime)
                    {
                        NodeManagerPlugin.Instance.DeleteNodes(0);
                    }
                    checkK2D2status = false;
                }
            }
        }
    }

    public void OnGUI(ManeuverNodeData currentNode)
    {
        GUILayout.BeginHorizontal();
        if (FlightPlan.UI.UI_Tools.BigButton("Make\nNode"))
            FlightPlanPlugin.Instance.MakeNode();

        if (MNCLoaded && mncVerCheck >= 0)
        {
            GUILayout.FlexibleSpace();
            if (FlightPlan.UI.UI_Tools.BigIconButton(FPStyles.mnc_icon))
                callMNC();
        }

        if (K2D2Loaded && currentNode != null)
        {
            GUILayout.FlexibleSpace();
            if (FlightPlan.UI.UI_Tools.BigIconButton(FPStyles.k2d2_big_icon))
                callK2D2();
        }
        GUILayout.EndHorizontal();

        if (checkK2D2status)
        {
            getK2D2Status();
            GUILayout.BeginHorizontal();
            FlightPlan.UI.UI_Tools.Label($"K2D2: {k2d2Status}");
            GUILayout.EndHorizontal();
        }
    }
}