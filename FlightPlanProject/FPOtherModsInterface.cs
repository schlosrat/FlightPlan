using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
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

public class FPOtherModsInterface
{
    public static FPOtherModsInterface instance = null;
 
    ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("FlightPlanPlugin.OtherModsInterface");

    // Reflection access variables for launching MNC & K2-D2
    private bool _mncLoaded, _k2d2Loaded, _checkK2D2status  = false;
    private PluginInfo _mncInfo, _k2d2Info;
    private Version _mncMinVersion, _k2d2MinVersion;
    private int _mncVerCheck, _k2d2VerCheck;
    private string _k2d2Status;
    Type K2D2Type, MNCType;
    PropertyInfo K2D2PropertyInfo, MNCPropertyInfo;
    MethodInfo K2D2GetStatusMethodInfo, K2D2FlyNodeMethodInfo, K2D2ToggleMethodInfo, MNCLaunchMNCMethodInfo;
    object K2D2Instance, MNCInstance;
    Texture2D mncButtonTex, k2d2ButtonTex;
    GUIContent MNCButtonTexCon, K2D2ButtonTexCon;

    private bool _launchMNC, _executeNode;

    public void CheckModsVersions()
    {
        Logger.LogInfo($"ManeuverNodeControllerMod.ModGuid = {ManeuverNodeControllerMod.ModGuid}");
        if (Chainloader.PluginInfos.TryGetValue(ManeuverNodeControllerMod.ModGuid, out _mncInfo))
        {
            _mncLoaded = true;
            Logger.LogInfo("Maneuver Node Controller installed and available");
            Logger.LogInfo($"_mncInfo = {_mncInfo}");
            // mncVersion = _mncInfo.Metadata.Version;
            _mncMinVersion = new Version(0, 8, 3);
            _mncVerCheck = _mncInfo.Metadata.Version.CompareTo(_mncMinVersion);
            Logger.LogInfo($"_mncVerCheck = {_mncVerCheck}");

            // Get _mncInfo buton Icon
            mncButtonTex = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/mnc_icon_white_50.png");
            MNCButtonTexCon = new GUIContent(mncButtonTex, "Launch Maneuver Node Controller");

            // Reflections method to attempt the same thing more cleanly
            MNCType = Type.GetType($"ManeuverNodeController.ManeuverNodeControllerMod, {ManeuverNodeControllerMod.ModGuid}");
            MNCPropertyInfo = MNCType!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            MNCInstance = MNCPropertyInfo.GetValue(null);
            MNCLaunchMNCMethodInfo = MNCPropertyInfo!.PropertyType.GetMethod("LaunchMNC");
        }
        // else _mncLoaded = false;
        Logger.LogInfo($"_mncLoaded = {_mncLoaded}");

        Logger.LogInfo($"K2D2_Plugin.ModGuid = {K2D2_Plugin.ModGuid}");
        if (Chainloader.PluginInfos.TryGetValue(ManeuverNodeControllerMod.ModGuid, out _k2d2Info))
        {
            _k2d2Info = Chainloader.PluginInfos[K2D2_Plugin.ModGuid];

            _k2d2Loaded = true;
            Logger.LogInfo("K2-D2 installed and available");
            Logger.LogInfo($"K2D2 = {_k2d2Info}");
            _k2d2MinVersion = new Version(0, 8, 1);
            _k2d2VerCheck = _k2d2Info.Metadata.Version.CompareTo(_k2d2MinVersion);
            Logger.LogInfo($"_k2d2VerCheck = {_k2d2VerCheck}");
            string _toolTip;
            if (_k2d2VerCheck >= 0) _toolTip = "Have K2-D2 Execute this node";
            else _toolTip = "Launch K2-D2";

            // Get K2-D2 buton Icon
            k2d2ButtonTex = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.SpaceWarpMetadata.ModID}/images/k2d2_icon.png");
            K2D2ButtonTexCon = new GUIContent(k2d2ButtonTex, _toolTip);

            K2D2Type = Type.GetType($"K2D2.K2D2_Plugin, {K2D2_Plugin.ModGuid}");
            K2D2PropertyInfo = K2D2Type!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            K2D2Instance = K2D2PropertyInfo.GetValue(null);
            K2D2ToggleMethodInfo = K2D2PropertyInfo!.PropertyType.GetMethod("ToggleAppBarButton");
            K2D2FlyNodeMethodInfo = K2D2PropertyInfo!.PropertyType.GetMethod("FlyNode");
            K2D2GetStatusMethodInfo = K2D2PropertyInfo!.PropertyType.GetMethod("GetStatus");
        }
        // else _k2d2Loaded = false;
        Logger.LogInfo($"_k2d2Loaded = {_k2d2Loaded}");

        instance = this;
    }

    public void CallMNC()
    {
        if (_mncLoaded && _mncVerCheck >= 0)
        {
            MNCLaunchMNCMethodInfo!.Invoke(MNCPropertyInfo.GetValue(null), null);
        }
    }

    public void CallK2D2()
    {
        if (_k2d2Loaded)
        {
            // Reflections method to attempt the same thing more cleanly
            if (_k2d2VerCheck < 0)
            {
                K2D2ToggleMethodInfo!.Invoke(K2D2PropertyInfo.GetValue(null), new object[] { true });
            }
            else
            {
                K2D2FlyNodeMethodInfo!.Invoke(K2D2PropertyInfo.GetValue(null), null);
                _checkK2D2status = true;

                FPStatus.K2D2Status(FlightPlanUI.Instance.ManeuverDescription, FlightPlanPlugin.Instance._currentNode.BurnDuration);
            }
        }
    }

    private void GetK2D2Status()
    {
        if (_k2d2Loaded)
        {
            if (_k2d2VerCheck >= 0)
            {
                _k2d2Status = (string)K2D2GetStatusMethodInfo!.Invoke(K2D2Instance, null);

                if (_k2d2Status == "Done")
                {
                    if (FlightPlanPlugin.Instance._currentNode.Time < GameManager.Instance.Game.UniverseModel.UniversalTime)
                    {
                        NodeManagerPlugin.Instance.DeleteNodes(0);
                    }
                    _checkK2D2status = false;
                }
            }
        }
    }

    public void OnGUI(ManeuverNodeData currentNode)
    {
        GUILayout.BeginHorizontal();

        if (FPStyles.SquareButton("Make\nNode"))
            FlightPlanUI.Instance.MakeNode();

        if (_mncLoaded && _mncVerCheck >= 0)
        {
            GUILayout.FlexibleSpace();
            if (FPStyles.SquareButton(FPStyles.MNCIcon))
                CallMNC();
        }

        if (_k2d2Loaded && currentNode != null)
        {
            GUILayout.FlexibleSpace();
            if (FPStyles.SquareButton(FPStyles.K2D2BigIcon))
                CallK2D2();
        }
        GUILayout.EndHorizontal();

        if (_checkK2D2status)
        {
            GetK2D2Status();
            GUILayout.BeginHorizontal();
            FlightPlan.KTools.UI.UI_Tools.Label($"K2D2: {_k2d2Status}");
            GUILayout.EndHorizontal();
        }
    }
}