# Flight Plan
![Flight Plan GUI](https://i.imgur.com/UI0BWGY.png)

Plan your (Space) Flight! Fly your Plan! Handy tools to help you set up maneuver nodes that will get you where you want to be.
Making spaceflight planning easier for Kerbal Space Program 2 one mission at a time.

**NOTE:** This mod draws heavily on some core [MechJeb2](https://github.com/MuMech/MechJeb2) code that has been adapted to work in KSP2, and would not be possible without the kind and generous contributions of Sarbian and the MechJeb development team! It is not the intent to replicate all MechJeb2 features and functions in this mod, but merely to make some handy maneuver planning tools available for KSP2 players. While you may be able to create some useful nodes with this mod, you'll still need to execute them accurately! Also, understanding some basic mission planning will be very usful for those employing the tools in this toolbox.

## Compatibility
* Tested with Kerbal Space Program 2 v0.1.2.0.22258 & SpaceWarp 1.1.3
* Requires [SpaceWarp 1.0.1+](https://spacedock.info/mod/3277/Space%20Warp%20+%20BepInEx)
* Requires [Node Manager 0.5.2+](https://spacedock.info/mod/3366/Node%20Manager)
* Optional, but highly recommended: [Maneuver Node Controller](https://spacedock.info/mod/3270/Maneuver%20Node%20Controller). See capabilites described below.

## Links
* [Space Dock](https://spacedock.info/mod/3359/Flight%20Plan)
* [Forum](https://forum.kerbalspaceprogram.com/index.php?/topic/216393-flight-plan/)
* [Must Have Mods Video](https://youtu.be/zaXk8t07KW4)

## Installation
1. Download and extract BepInEx mod loader with SpaceWarp 1.0.1 or later (see link above) into your game folder and run the game, then close it. If you've done this before, you can skip this step. If you've installed the game via Steam, then this is probably here: `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program 2`. If you complete this step correctly you'll have a **BepInEx** subfolder in that directory along with the following files (in addition to what was there before): **changelog.txt, doorstop_config.ini, winhttp.dll**
1. Install Node Manager 0.5.2 or later (see link above). From the NodeManager-x.x.x.zip file copy the `BepInEx` folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\node_manager`.
1. Download and extract this mod into the game folder. From the FlightPlan-x.x.x.zip file copy the `BepInEx` folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\flight_plan`.

## Features
### Display Current Target Selection
* Drop Down Menu for easy selection of *Celestial Targets*
### Own Ship Maneuvers
* Circularize at Ap
* Circularize at Pe
* New Pe (user specified value) - planned for next Ap
* New Ap (user specified value) - planned for next Pe
* New Inclination (user specified value) - if e < 0: Planned for cheapest AN/DN, otherwise planned for ASAP
### Maneuvers Relative to the Selected Target (only available if a target is selected)
* Match planes with Target at AN
* Match Planes with Target at DN 
* Hohmann Transfer to Target
### Display Status of Last Command
* Normal/Good results are shown in **Green** indicating a maneuver node was generated and it's ready for you to execute it. *Don't forget to get your craft pointed in the right direction first!*
* Warnings and Cautions are shown in **Yellow** indicating a node was generated, but you should inspect it carefully first and may need to modify it.
* Failures are shown in **Red** indicating no node has been generated with some clue as to why.
### Game Input Enable/Disable Status
* To prevent the things you type in a user input field from passing through to the game and affecting things in odd ways, the game input is automatically disabled when you click inside a *text input field*. This will cause the game to not respond to your mouse or to anything you type, although you can freely type what you need into the input field. Typing a "." as part of a decimal number will not increase your time warp setting, and using the 1 and 2 keys on your number pad will not mute the game or the music. To restore full functionality for keyboard and mouse inputs simply click anywhere else other than the text input field. Closing the Flight Plan GUI will also have this effect.
### Integration with Maneuver Node Controller, v0.8.3+
* *If* Maneuver Node Controller (v0.8.3 or later) is installed, and *if* the Launch Maneuver Node Controller configuration setting is Enabled, then when you activate an *experimental* node creation function the Maneuver Node Controller mod will automatically be brought up if it is not already up. This can make it easier to evaluate and adjust nodes constructed using experimental functions (those listed under Planned Improvement below)

**NOTE:** At this time *Flight Plan has no capability to execute nodes* - it just helps you plan them. Getting a *Good* result in the status does not mean your craft is pointed in the right direction or is otherwise ready to execute the node, but rather that the node is ready for you!

## UI Screens
In addition to the basic UI screen above the UI will automatically asjust to offer capabilities relevant to the current orbit and selected target.
### Selecting a Celestial Target with the Drop Down Menu
![Flight Plan Target Selection GUI](https://i.imgur.com/NyhCARt.png)

### With a Local Object Selected (selected target is orbiting the same body your vessel is)
![Flight Plan Target Selected GUI - Moon](https://i.imgur.com/givkRgG.png)

## Configuration Parameters
![Flight Plan Configuration Parameters](https://i.imgur.com/BcJsRLP.png)

This mod includes a number of user configurable parameters which can be accessed through the *SpaceWarp* configuration screen. Press Alt + M to pull up the SpaceWarp Mod dialog, and select the **Open Configuration Manager** button at the bottom to display the list of installed mods with configurable settings. Clicking on the Flight Plan entry will display the ionterface shown above. There are tool tip strings which describe what each setting does.
Using the configuration parameters you can change a variety of things such as how long a status message sticks around before it start to fade, and also how long it will take to fade.

**NOTE:** The following settings are dyanically managed and may be usted by the user while the game is running. All others will require exiting and restart to take effect.
* Experimental Features: Enable/Disable
* Launch Maneuver Node Controller: Enable/Disable
* Status Fade Time: Seconds
* Status Hold Time: Seconds

## Planned Improvement / Experimental Functions
![Flight Plan Future GUI](https://i.imgur.com/nAqnh60.png)

Work In Progress developmental features may be enabled by switching on the Experimental Features in the mod's configuration screen. You will need to restart the game for this setting to take effect, but it will allow you to play with some broken toys if you like. As these featuers mature and become realiable enough to use they will be moved up into the main feature set avaialble without turning on the Experimental Features setting.
### Own Ship Maneuvers
* Circularize Now
* New Pe & Ap (for user specified values) - Burn ASAP
### Maneuvers Relative to the Selected Target (only available if a target is selected)
* Intercept Target (at user specified time from now)
* Course Correction (requires being on an intercept trajectory)
* Match Velocity at Closest Approach (requires being on an intercept trajectory)
* Match Velocity Now
### Interplanetary Transfer Maneuvers (only available if a planet is the selected target)
* Interplanetary Transfer
### Moon Specific Maneuvers (only available when in orbit about a moon)
* Return from a Moon

## Example Images
### Circularize at the Next Ap
![Flight Plan: Circularize at Next Ap Example](https://i.imgur.com/by0kbUF.png)

### Burn Timing Detail (burn brackets effective point: Ap)
![Flight Plan: Circularize at Next Ap Burn Detail](https://i.imgur.com/pDkXeBM.png)

### Circularize at the Next Pe
![Flight Plan: Circularize at Next Pe Example](https://i.imgur.com/3dQ6LBS.png)

### Set New Pe
![Flight Plan: Set New Pe Example](https://i.imgur.com/QhFGbxf.png)

### Set New Ap
![Flight Plan: Set New Pe Example](https://i.imgur.com/mz8dkXo.png)

### Set Inclination to 0
![Flight Plan: Set Inclination to 0 Example](https://i.imgur.com/LXN40KN.png)

### Match Planes with Minmus at AN (from 20 deg inclined orbit)
![Flight Plan: Match Planes with Target at AN Example](https://i.imgur.com/tJ1muhG.png)

### Match Planes with Minmus at DN (from 20 deg inclined orbit)
![Flight Plan: Match Planes with Target at DN Example](https://i.imgur.com/G4D3tiF.png)

### Hohmann Transfer to Minmus (from coplanar orbit)
![Flight Plan: Hohmann Transfer Example](https://i.imgur.com/tIH5hkD.png)

### Hohmann Transfer to Mun (from coplanar orbit)
![Flight Plan: Hohmann Transfer Example](https://i.imgur.com/ymKTLyT.png)

### Set Inclination on Munar Flyby
![Flight Plan: Set Incination to 87 in Hyperbolic Orbit](https://i.imgur.com/nBFNtm5.png)

### Circularize at Pe for Munar Fly By
![Flight Plan: Circularize at Next Pe Example - Munar Flyby](https://i.imgur.com/gFuZRau.png)

### Hohmann Transfer to Mun (from non-coplanar orbit: Inclined 20 degrees from target plane)
![Flight Plan: Hohmann Transfer Example](https://i.imgur.com/iliH2bY.png)

# Flight Plan as a Library
Flight Plan's orbital maneuver node creation methods are public, and so may be called from other mods. In this sense, Flight Plan can be accessed like a library whether the Flight Plan GUI is visible or not. 

This mod is primarily meant as a service provider to other mods, which can call functions in this one without needing to recreate all these functions in the base mod. Creating maneuver nodes from a KSP2 mod is not necessarily an intuitive thing to code, so having this as a resource you can call may save you time developing those capabilities internally to your mod.

## Orbital Maneuver Node Capabilities

* **CircularizeAtAP(burnOffsetFactor)**: Calling this method will create a maneuver node at the next Ap for the active vessel to circularize the vessel's orbit at that point. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the Ap by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **CircularizeAtAP(burnOffsetFactor)**: Calling this method will create a maneuver node at the next Pe for the active vessel to circularize the vessel's orbit at that point. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the Pe by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **CircularizeNow(burnOffsetFactor)**: Calling this method will create a maneuver node to circularize the vessel's orbit at a time aproximately 30 seconds from now. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the nominal time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **SetNewPe(newPe, burnOffsetFactor)**: Calling this method will create a maneuver node to set the vessel's Pe at the next Ap. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the Ap by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **SetNewAp(newAp, burnOffsetFactor)**: Calling this method will create a maneuver node to set the vessel's Ap at the next Pe. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the Pe by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **Ellipticize(newAp, newPe, burnOffsetFactor)**: Calling this method will create a maneuver node to set the vessel's Ap and Pe at a time aproximately 30 seconds from now. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the nominal time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **SetInclination(inclination, burnOffsetFactor)**: Calling this method will create a maneuver node to set the vessel's orbit inclination (in degrees) at either the next Ascending Node or Descending Node. Both options are evaluated and the one requireing the least Delta-v is automatically selected. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the AN/DN time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **MatchPlanesAtAN(burnOffsetFactor)**: Calling this method will create a maneuver node to set the vessel's orbit inclination to match that of the currently selected target at either the next Ascending Node for the target. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the AN time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **MatchPlanesAtDN(burnOffsetFactor)**: Calling this method will create a maneuver node to set the vessel's orbit inclination to match that of the currently selected target at either the next Descending Node for the target. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the DN time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **HohmannTransfer(burnOffsetFactor)**: Calling this method will create a maneuver node for a Hohmann Transfer to the currently selected target at the next available window. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the nominal time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **InterceptTgtAtUT(interceptTime, burnOffsetFactor)**: Calling this method will create a maneuver node to intercept the currently selected target at the a time of interceptTime from now. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the nominal time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **CourseCorrection()**: Calling this method will create a maneuver node to finetime the trajectory to intercept the currently selected target. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the nominal time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **MoonReturn(burnOffsetFactor)**: Calling this method will create a maneuver node for a HohmannTransfer to return the active vessel from a moon to the planet the moon is orbiting. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the nominal time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **MatchVelocityAtCA(burnOffsetFactor)**: Calling this method will create a maneuver node to match velocity with the currently selected target at the point of closest approach. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the nominal time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **MatchVelocityNow(burnOffsetFactor)**: Calling this method will create a maneuver node to match velocity with the currently selected target at a time aproximately 20s from now. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the nominal time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).
* **PlanetaryXfer(burnOffsetFactor)**: Calling this method will create a maneuver node for a Hohmann Transfer to the currently selected target planet at the next available window. The (optionally) specified (double) burnOffsetFactor controls an offset to the burn time scaled by the node's burn duration. If the burnOffsetFactor is not specified, a default value of -0.5 will be used resulting in the burn time starting before the nominal time by 1/2 of the node's burn duration (as estimated by the game for the active vessel).

To use this mod from your mod you will need to do one of the following:

## Hard Dependency via Nuget Package
If core capabilities in your mod will rely on calling Flight Plan methods, then setting up a **hard dependency** like this is the way to go, plus it's actually easier to develop your mod this way. There are two ways to set up your mod for development with Node Manager as a hard dependency, and this is the easiest of the two, so is the recommended way. Fundamentally, it works just like what you're already doing to reference BepInEx and SpaceWarp.

* The **advantage** to this way is coding will be easier for you! Just call FlightPlanPlugin.Instance.*method_name()* for any public method in Flight Plan!
* The **disadvantage** to this way is you've got a *hard dependency* and your mod will not even start up unless Flight Plan is installed. You may want to ship a copy of Flight Plan with your mod (put both the flight_plan plugin folder and your mod's plugin folder into the BepInEx/plugins folder before zipping it up). There may be a way to do this with CKAN in some automated fashion. This guide will be updated with those details, or a link to them, at some point.

### Step 1: Update your csproj file
In your csproj file you probably already have an ItemGroup where BepInEx, HarmonyX, and SpaceWarp are added as PackageReferene includes. All you need to do is add another PackageReference for FlightPlan like the one sown below.

```xml
    <ItemGroup>
        <PackageReference Include="BepInEx.Analyzers" Version="1.*" PrivateAssets="all" />
        <PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.0" PrivateAssets="all" />
        <PackageReference Include="BepInEx.Core" Version="5.*" />
        <PackageReference Include="BepInEx.PluginInfoProps" Version="2.*" />
        <PackageReference Include="HarmonyX" Version="2.10.1" />
        <PackageReference Include="SpaceWarp" Version="1.1.1" />
        <PackageReference Include="FlightPlan" Version="0.7.3" />
        <PackageReference Include="UnityEngine.Modules" Version="2020.3.33" IncludeAssets="compile" />
    </ItemGroup>
```

### Step 2: Configure Namespace and Add Dependency
Bring in the NodeManger namespace in the class you want to call it from, and add Node Manager as a BepInDependency.

```cs
    using FlightPlan;
    
    namespace MyCoolModsNameSpace;
    
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
    [BepInDependency(FlightPlanPlugin.ModGuid, FlightPlanPlugin.ModVer)]
    public class MyCoolModName : BaseSpaceWarpPlugin
```

### Step 3: Call Flight Plan Methods from your Mod
You can now call any of Node Manager's public methods directly and easily from your code. Here are some examples:

```cs
    pass = NodeManagerPlugin.Instance.AddNode(burnUT) // double
    pass = NodeManagerPlugin.Instance.CreateManeuverNodeAtTA(burnVector, TrueAnomalyRad, burnDurationOffsetFactor); // Vector3d, double, double (default = -0.5)
    pass = NodeManagerPlugin.Instance.CreateManeuverNodeAtUT(burnVector, burnUT, burnDurationOffsetFactor); // Vector3d, double, double (default = -0.5)
    NodeManagerPlugin.Instance.DeleteNodes(SelectedNodeIndex); // int
    NodeManagerPlugin.Instance.RefreshActiveVesselAndCurrentManeuver();
    NodeManagerPlugin.Instance.RefreshManeuverNodes();
    NodeManagerPlugin.Instance.RefreshNodes();
    NodeManagerPlugin.Instance.SpitNode(SelectedNodeIndex, isError); // int, bool
    NodeManagerPlugin.Instance.SpitNode(node, isError); // ManeuverNodeData, bool
```

### Step 4: Profit!

## Hard Dependency via Local Copy of Flight Plan DLL
This way works like the method above with a few minor differences in your csproj and what you need to do in your mod's development folder.

### Step 1: Configure Assemblies
Add a copy of the node_manager.dll to your mod's list of Assemblies. Generally, this means two things. First, put the flight_plan.dll in a folder where your mod can find it. You may want to put it in the same folder you have Assembly-CSharp.dll. Secondly, add it to your csproj file similarly to how you're already referencing Assembly-CSharp.dll. Your mod will need to have access to it, and know where to find it, when you compile your mod. At run time your mod will be accessing the node_manager.dll from the node_manager plugins folder where Node Manager is installed, so you don't need to distribute the Node Manager DLL with your mod, but it will need to be installed in the players game for you to be able to access it.

In your csproj file locate the ItemGroup where you have local References defined. There will be at least one for Assembly-CSharp.dll. You'll need to add another one for Node Manager like this.

```xml
    <ItemGroup>
        <Reference Include="Assembly-CSharp">
            <HintPath>..\external_dlls\Assembly-CSharp.dll</HintPath>
            <Publicize>true</Publicize>
            <Private>false</Private>
        </Reference>
        <Reference Include="flight_plan">
            <HintPath>..\external_dlls\flight_plan.dll</HintPath>
            <Private>false</Private>
        </Reference>
    </ItemGroup>
```

### Step 2: Configure Namespace and Add Dependency
See Step 2 above as there is no difference.

### Step 3: Call Node Manager Methods from your Mod
See Step 3 above as there is no difference.

### Step 4: Profit!

## Soft Dependency
This is the way to go if optional capabilities in your mod will rely on functions in this one, and you don't want to for the user to have a hard dependency on Flight Plan.

* The **advantage** to this way is that your mod's users don't need to have Flight Plan installed if they prefer not to have it, and you can distribute your mod without needed a hard dependency on Flight Plan - meaning you mod can launch and run without Flight Plan, although there may be some capabilities that aren't available to your users if they choose to pass on installing Flight Plan.
* The **disadvantage** to this way is you'll need to do a bit more coding in your mod to be able to call Flight Plan's methods from your mod.

### Step 1: configure Assemblies
This is the same as for Hard Dependency above as you'll not be able to compile without this.

## Step 2: Configure Namespace and Variables
Bring in the NodeManger namespace and create some variables in the class you want to call it from. This is almost the same as above.

```cs
   using FlightPlan;
   
   private bool FPLoaded;
   PluginInfo FP;
```

### Step 3: Check for Node Manager
Somewhere in your mod you need to check to make sure Node Manager is loaded before you use it (e.g., OnInitialized()). You don't need this with a hard dependency, but it's essential for a soft one.

```cs
    if (Chainloader.PluginInfos.TryGetValue(FlightPlanPlugin.ModGuid, out FP))
    {
        FPLoaded = true;
        Logger.LogInfo("Flight Plan installed and available");
        Logger.LogInfo($"FP = {FP}");
    }
    else FPLoaded = false;
```

### Step 4: Create Reflection Method
This is where things get really different for you compared to what's needed to call Node Manager methods using a hard dependency. For a soft dependency to work you're going to need to create a reflection calling method for each of the Flight Plan methods that you would like to call from your mod. Here's an example of one for calling Flight Plan's CreateManeuverNodeAtUT method which will pass it the burn vector you want, the time to schedule the burn and optionally a time offset. Using a time offset of -0.5 will cause the maneuver node to be centered on the time you supply rather than starting on the time.

```cs
    private void CreateNodeAtUt(Vector3d burnVector, double UT, double burnDurationOffsetFactor = -0.5)
    {
        if (FPLoaded)
        {
            // Reflections method to call Node Manager methods from your mod
            var nmType = Type.GetType($"FlightPlan.FlightPlanPlugin, {FlightPlanPlugin.ModGuid}");
            Logger.LogDebug($"Type name: {nmType!.Name}");
            var instanceProperty = nmType!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            Logger.LogDebug($"Property name: {instanceProperty!.Name}");
            var methodInfo = instanceProperty!.PropertyType.GetMethod("CreateManeuverNodeAtUT");
            Logger.LogDebug($"Method name: {methodInfo!.Name}");
            methodInfo!.Invoke(instanceProperty.GetValue(null), new object[] { burnVector, UT, burnDurationOffsetFactor });
        }
    }
```

This example include some (optional) debug logging that may be helpful if you are having trouble with the reflection calling method. You can safely remove those once it's working to your satisfaction.

### Step 5: Call Reflection Method
Call your reflection method wherever you need to invoke the corresponding Node Manager method.

```cs
    CreateNodeAtUt(burnVector, burnUT, -0.5);
```

### Step 6: Profit!
