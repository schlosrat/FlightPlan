

using System.Reflection;
using UnityEngine;
using KSP.Sim.Maneuver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using K2D2;
using ManeuverNodeController;

using FlightPlan.UI;

namespace FlightPlan;

public class OtherMods
{
    ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ManeuverNodeController.Utility");

    // Reflection access variables for launching MNC & K2-D2
    private bool MNCLoaded, K2D2Loaded, checkK2D2status  = false;
    private PluginInfo MNC, K2D2;
    private Version mncMinVersion, k2d2MinVersion;
    private int mncVerCheck, k2d2VerCheck;
    private string k2d2Status;
    Type k2d2Type, mncType;
    PropertyInfo k2d2PropertyInfo, mncPropertyInfo;
    MethodInfo k2d2GetStatusMethodInfo, k2d2FlyNodeMethodInfo, k2d2ToggleMethodInfo, mncLaunchMNCMethodInfo;
    object k2d2Instance, mncInstance;

    private bool launchMNC, executeNode;


    public void check()
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
                    // if (currentNode.Time < Game.UniverseModel.UniversalTime)
                    // {
                    //     NodeManagerPlugin.Instance.DeleteNodes(0);
                    // }
                    checkK2D2status = false;
                }
            }
        }
    }

    public void OnGUI(ManeuverNodeData currentNode)
    {
        if (MNCLoaded && mncVerCheck >= 0)
        {
            if (FlightPlan.UI.UI_Tools.SmallButton("MNC"))
                callMNC();
        }
        GUILayout.Space(10);
        if (K2D2Loaded && currentNode != null)
        {
            if (FlightPlan.UI.UI_Tools.SmallButton("K2D2"))
                callK2D2();
        }
        GUILayout.EndHorizontal();
        if (checkK2D2status)
        {
            getK2D2Status();
            GUILayout.BeginHorizontal();
            FlightPlan.UI.UI_Tools.Label($"K2D2: {k2d2Status}");
            // GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}