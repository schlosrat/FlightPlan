using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using KSP.Game;
using NodeManager;
using System.Reflection;

namespace FlightPlan;

public class FPOtherModsInterface
{
    private const string K2D2ModGuid = "com.github.cfloutier.k2d2";
    private const string MNCModGuid = "com.github.xyz3211.maneuver_node_controller";

    public static FPOtherModsInterface instance;

    private static readonly GameInstance Game = GameManager.Instance.Game;

    private ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("FlightPlanPlugin.OtherModsInterface");

    // Reflection access variables for launching MNC & K2-D2
    public static bool mncLoaded, k2d2Loaded, checkK2D2status  = false;
    private PluginInfo _mncInfo, _k2d2Info;
    private Version _mncMinVersion, _k2d2MinVersion;
    private int _mncVerCheck, _k2d2VerCheck;
    public static string k2d2Status;
    private Type K2D2Type, MNCType;
    private PropertyInfo K2D2PropertyInfo, MNCPropertyInfo;
    private MethodInfo K2D2GetStatusMethodInfo, K2D2FlyNodeMethodInfo, K2D2ToggleMethodInfo, MNCLaunchMNCMethodInfo;
    private object K2D2Instance, MNCInstance;

    private bool _launchMNC, _executeNode;

    public void CheckModsVersions()
    {
        Logger.LogInfo($"ManeuverNodeControllerMod.ModGuid = {MNCModGuid}");
        if (Chainloader.PluginInfos.TryGetValue(MNCModGuid, out _mncInfo))
        {
            mncLoaded = true;
            Logger.LogInfo("Maneuver Node Controller installed and available");
            Logger.LogInfo($"_mncInfo = {_mncInfo}");
            // mncVersion = _mncInfo.Metadata.Version;
            _mncMinVersion = new Version(0, 8, 3);
            _mncVerCheck = _mncInfo.Metadata.Version.CompareTo(_mncMinVersion);
            Logger.LogInfo($"_mncVerCheck = {_mncVerCheck}");

            // Reflections method to attempt the same thing more cleanly
            MNCType = Type.GetType($"ManeuverNodeController.ManeuverNodeControllerMod, {MNCModGuid}");
            MNCPropertyInfo = MNCType!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            MNCInstance = MNCPropertyInfo.GetValue(null);
            MNCLaunchMNCMethodInfo = MNCPropertyInfo!.PropertyType.GetMethod("LaunchMNC");
        }
        // else _mncLoaded = false;
        Logger.LogInfo($"_mncLoaded = {mncLoaded}");

        Logger.LogInfo($"K2D2_Plugin.ModGuid = {K2D2ModGuid}");
        if (Chainloader.PluginInfos.TryGetValue(K2D2ModGuid, out _k2d2Info))
        {
            _k2d2Info = Chainloader.PluginInfos[K2D2ModGuid];

            k2d2Loaded = true;
            Logger.LogInfo("K2-D2 installed and available");
            Logger.LogInfo($"K2D2 = {_k2d2Info}");
            _k2d2MinVersion = new Version(0, 8, 1);
            _k2d2VerCheck = _k2d2Info.Metadata.Version.CompareTo(_k2d2MinVersion);
            Logger.LogInfo($"_k2d2VerCheck = {_k2d2VerCheck}");
            string _toolTip;
            if (_k2d2VerCheck >= 0) 
                _toolTip = "Have K2-D2 Execute this node";
            else 
                _toolTip = "Launch K2-D2";

            var assembly_name = K2D2ModGuid;
            var is_new_version = _k2d2Info.Metadata.Version.CompareTo(new Version(1, 0, 0));
            if (is_new_version >= 0) 
                assembly_name = "K2D2";

            K2D2Type = Type.GetType($"K2D2.K2D2_Plugin, {assembly_name}");
            K2D2PropertyInfo = K2D2Type!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            K2D2Instance = K2D2PropertyInfo.GetValue(null);
            K2D2ToggleMethodInfo = K2D2PropertyInfo!.PropertyType.GetMethod("ToggleAppBarButton");
            K2D2FlyNodeMethodInfo = K2D2PropertyInfo!.PropertyType.GetMethod("FlyNode");
            K2D2GetStatusMethodInfo = K2D2PropertyInfo!.PropertyType.GetMethod("GetStatus");
        }
        // else _k2d2Loaded = false;
        Logger.LogInfo($"_k2d2Loaded = {k2d2Loaded}");

        instance = this;
    }

    public void CallMNC()
    {
        if (mncLoaded && _mncVerCheck >= 0)
        {
            MNCLaunchMNCMethodInfo!.Invoke(MNCPropertyInfo.GetValue(null), null);
        }
    }

    public void CallK2D2()
    {
        if (k2d2Loaded)
        {
            // Reflections method to attempt the same thing more cleanly
            if (_k2d2VerCheck < 0)
            {
                K2D2ToggleMethodInfo!.Invoke(K2D2PropertyInfo.GetValue(null), new object[] { true });
            }
            else
            {
                K2D2FlyNodeMethodInfo!.Invoke(K2D2PropertyInfo.GetValue(null), null);
                checkK2D2status = true;

                FPStatus.K2D2Status(FpUiController.ManeuverDescription, FlightPlanPlugin.Instance._currentNode.BurnDuration);
            }
        }
    }

    public void GetK2D2Status()
    {
        if (k2d2Loaded)
        {
            if (_k2d2VerCheck >= 0)
            {
                k2d2Status = (string)K2D2GetStatusMethodInfo!.Invoke(K2D2Instance, null);

                if (k2d2Status == "Done")
                {
                    if (FlightPlanPlugin.Instance._currentNode.Time < Game.UniverseModel.UniverseTime)
                    {
                        // NodeManagerPlugin.Instance.DeleteNodes(0);
                        // NodeManagerPlugin.Instance.DeleteNode(0);
                        NodeManagerPlugin.Instance.DeletePastNodes();
                    }
                    checkK2D2status = false;
                }
            }
        }
    }

    //public void OnGUI(ManeuverNodeData currentNode)
    //{
    //    GUILayout.BeginHorizontal();

    //    if (FPStyles.SquareButton("Make\nNode"))
    //        FlightPlanUI.Instance.MakeNode();

    //    if (mncLoaded && _mncVerCheck >= 0)
    //    {
    //        GUILayout.FlexibleSpace();
    //        if (FPStyles.SquareButton(FPStyles.MNCIcon))
    //            CallMNC();
    //    }

    //    if (k2d2Loaded && currentNode != null)
    //    {
    //        GUILayout.FlexibleSpace();
    //        if (FPStyles.SquareButton(FPStyles.K2D2BigIcon))
    //            CallK2D2();
    //    }
    //    GUILayout.EndHorizontal();

    //    if (checkK2D2status)
    //    {
    //        GetK2D2Status();
    //        GUILayout.BeginHorizontal();
    //        KTools.UI.UI_Tools.Label($"K2D2: {k2d2Status}");
    //        GUILayout.EndHorizontal();
    //    }
    //}
}