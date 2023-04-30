# Flight Plan
![Flight Plan GUI](https://i.imgur.com/xaRyMGU.png)

Plan your (Space) Flight! Fly your Plan! Handy tools to help you set up maneuver nodes that will get you where you want to be.
Making spaceflight planning easier for Kerbal Space Program 2 one mission at a time.

**NOTE:** This mod draws heavily on some core [MechJeb2](https://github.com/MuMech/MechJeb2) code that has been adapted to work in KSP2, and would not be possible without the kind and generous contributions of Sarbian and the MechJeb development team! It is not the intent to replicate all MechJeb2 features and functions in this mod, but merely to make some handy maneuver planning tools available for KSP2 players. While you may be able to create some useful nodes with this mod, you'll still need to execute them accurately! Also, understanding some basic mission planning will be very usful for those employing the tools in this toolbox.

**Note:** Version 0.8.0 has received significant updates and improvements in the GUI from [cfloutier](https://github.com/cfloutier) who richly deserves the credit for those parts. His contributions have dramatically improved the quality of the user interface and make the mod not only more modern and visually pleasing, but also easier and more fun to use.

## Compatibility
* Tested with Kerbal Space Program 2 v0.1.2.0.22258 & SpaceWarp 1.1.3
* Requires [SpaceWarp](https://spacedock.info/mod/3277/Space%20Warp%20+%20BepInEx) 1.0.1+
* Requires [Node Manager](https://spacedock.info/mod/3366/Node%20Manager) 0.5.3+
* Optional, but highly recommended: [K2-D2](https://spacedock.info/mod/3325/K2-D2) 0.8.1+. See capabilities described below.
* Optional, but highly recommended: [Maneuver Node Controller](https://spacedock.info/mod/3270/Maneuver%20Node%20Controller) 0.8.3+. See capabilities described below.

## Links
* [Space Dock](https://spacedock.info/mod/3359/Flight%20Plan)
* [Forum](https://forum.kerbalspaceprogram.com/index.php?/topic/216393-flight-plan/)
* [Must Have Mods Video](https://youtu.be/zaXk8t07KW4)

## Installation
1. Download and extract the **BepInEx mod loader with SpaceWarp** 1.0.1 or later (see link above) into your game folder and run the game, then close it. If you've done this before, you can skip this step. If you've installed the game via Steam, then this is probably here: `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program 2`. If you complete this step correctly you'll have a **BepInEx** subfolder in that directory along with the following files (in addition to what was there before): **changelog.txt, doorstop_config.ini, winhttp.dll**
1. Install **Node Manager** 0.5.3 or later (see link above). From the NodeManager-x.x.x.zip file copy the `BepInEx` folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\node_manager`.
1. Download and extract this mod into the game folder. From the FlightPlan-x.x.x.zip file copy the `BepInEx` folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\flight_plan`.
1. *Optional*: Download and install **K2-D2**, your friendly KSP Astromech ready to perform precision node execution for you! (see link above). From the K2D2_vx.x.x.zip file copy the BepInEx folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\K2D2`.
1. *Optional*: Download and install **Maneuver Node Controller** to assist you with finetuning your maneuver nodes! (see link above). From the ManeuverNodeController-x.x.x.zip file copy the BepInEx folder on top of your game's install folder. If done correctly, you should have the following folder structure within your KSP2 game folder: `...\Kerbal Space Program 2\BepInEx\plugins\maneuver_node_controller`.

## Features
### Easy Celestial Target Selection
![Flight Plan Celestial Target Selection Menu](https://i.imgur.com/7b7Muph.png)
* Menu for easy selection of *Celestial Targets*. Planets and their moons are graphically organized with moons indented below the planet they orbit. This capability augments the game's built-in target selection for easier selection of distant celestial bodies.
### Burn Time Option Selection
![Flight Plan Burn Time Option Menu](https://i.imgur.com/bYgeCMo.png)
* Automatically populated menu for available burn time options consistent with the selected maneuver type and the current situation. The options available for a **New Inclination** maneuver are shown above. When a maneuver type is selected, if the previously selected burn time option is not a valid option for that maneuver type and your current situation, then a default will be populated. If the displayed maneuver time option is not what you need simply click the option to display a menu of available options to customize your maneuver.
### Ownship Maneuvers
![Flight Plan Main GUI](https://i.imgur.com/N4OKm4f.png)
* **Circularize**
* **New Pe** (user specified value - km)
* **New Ap** (user specified value - km)
* **New Pe & Ap** (uses inputs for **New Pe** and **New Ap** above changing both in one maneuver)
* **New Inclination** (user specified value - degrees)
* **New LAN** (user specified value - degrees)
### Maneuvers Relative to the Selected Target (only available if a target is selected)
* **Match Planes** with Target
* **Hohmann Transfer** to Target
* **Course Correction** (requires being on an intercept trajectory)
### Moon Specific Maneuvers (only available when in orbit about a moon)
* **Moon Return** (user specified target Pe (km) for arrival back at the parent planet)
### Display Status of Last Command
* Normal/Good results are shown in **Green** indicating a maneuver node was generated and it's ready for you to execute it. *Don't forget to get your craft pointed in the right direction first!*
* Warnings and Cautions are shown in **Yellow** indicating a node was generated, but you should inspect it carefully first and may need to modify it.
* Failures are shown in **Red** indicating no node has been generated with some clue as to why.
### Game Input Enable/Disable Status
* To prevent things you type in a user input field from passing through to the game and affecting things in odd ways, the game input is automatically disabled when you click inside a *text input field*. This will cause the game to not respond to your mouse or to anything you type, although you can freely type what you need into the input field. Typing a "." as part of a decimal number will not increase your time warp setting, and using the 1 and 2 keys on your number pad will not mute the game or the music. To restore full functionality for keyboard and mouse inputs simply click anywhere else other than the text input field. Closing the Flight Plan GUI will also have this effect.
### Integration with K2-D2, v0.8.1+
* *If* K2-D2 is installed, then a K2D2 Icon button will be presented in the lower right part of the GUI whenever there is an executable maneuver node. If the version of K2-D2 is 0.8.0 or *earlier*, then pressing Flight Plan's K2-D2 button will bring up the K2-D2 GUI to assist with the precision execution of the planned maneuver. If K2-D2 0.8.1 or *later* is installed, then pressing the K2-D2 button will cause K2-D2 to **execute** the next maneuver node - this doesn't bring up the K2-D2 GUI, but if you have it up you'll be able to watch it as it executes the node.
### Integration with Maneuver Node Controller, v0.8.3+
* *If* Maneuver Node Controller (v0.8.3 or later) is installed then Flight Plan will present an MNC Icon button in the lower right corner of the GUI (to the right of the **Make Node** button, and left of the K2D2 button if present). Pressing that button will bring up the Maneuver Node Controller GUI. *If* the *Launch Maneuver Node Controller* configuration setting is Enabled, then when you activate an *experimental* node creation function the Maneuver Node Controller mod will automatically be brought up if it is not already up. This can make it easier to evaluate and adjust nodes constructed using experimental functions (those listed under Planned Improvement below)

## UI Screens
The Flight Plan GUI will always display all Ownship maneuvers available in the current orbital situation. These are maneuvers which don't require a target and so are relative to your current vessel's orbit alone. Each *Maneuver Type* may be customized by the making a selection from the Burn options menu (right below the Celestial Target Selection Menu). For example, selecting **Circularize** as the Maneuver Type will result in having Burn Time options for *at the next Ap*, *at the next Pe*, *at an altitude*, and *after a fixed time*. In the case of the latter two options these will cause an additional input field to be presented where you can specify the **Maneuver Altitude** or **Time From Now**.

![Flight Plan GUI Examples](https://i.imgur.com/sJKFA12.png)

Similarly, selecting **Match Planes** will give Burn Time options for *at the cheapest AN/DN w/Target*, *at the nearest AN/DN w/Target*, *at the next AN w/Target*, and *at the next DN w/Target*. NOTE: in the Match Planes example above the Make Node button has been pressed and Flight Plan is showing a status indicating the node is ready. In this example, with K2-D2 installed, the K2-D2 Astromech Icon is displayed in the lower right indicating that K2-D2 is ready to help you fly the node.

In addition to the basic UI screens above, the UI will automatically adjust to offer capabilities relevant to the current orbit and selected target. Some options such as **Interplanetary Transfer** (above far right example) are only available if the *Experimental Features* option has been selected in the Flight Plan configuration options menu.

## Configuration Parameters
![Flight Plan Configuration Parameters](https://i.imgur.com/wltT7P0.png)

This mod includes a number of user configurable parameters which can be accessed through the *SpaceWarp* configuration screen. Press **Alt + M** to pull up the SpaceWarp Mod dialog, and select the **Open Configuration Manager** button at the bottom to display the list of installed mods with configurable settings. Clicking on the Flight Plan entry will display the ionterface shown above. There are tool tip strings which describe what each setting does.
Using the configuration parameters you can change a variety of things such as how long a status message sticks around before it start to fade, and also how long it will take to fade.

**NOTE:** The following settings are dynamically managed and may be usted by the user while the game is running. All others will require exiting and restart to take effect.
* Experimental Features: Enable/Disable
* Launch Maneuver Node Controller: Enable/Disable
* Status Fade Time: Seconds
* Status Hold Time: Seconds

## Planned Improvement / Experimental Functions
![Flight Plan Future GUI](https://i.imgur.com/zPLPAsx.png)

**Work In Progress** developmental features may be enabled by switching on the *Experimental Features* in the mod's configuration screen. You do not need to restart the game for this setting to take effect, and it will allow you to play with some broken toys if you like. As these featuers mature and become realiable enough to use they will be moved up into the main feature set avaialble without turning on the *Experimental Features* setting.
### Maneuvers Relative to the Selected Target (only available if a target is selected)
* **Intercept** Target
* **Match Velocity**
### Interplanetary Transfer Maneuvers (only available if a planet is the selected target)
* **Interplanetary Transfer**

## Example: Take a Trip to Minmus
### Step 1: Match Planes
Here we're stating out in a nicely equatorial Low Kerbin Orbit. As we want to go to Minmus, the first step is to get into a new circular orbit that's co-planar with the target. We can see that the necessary plane change maneuver has been planned and is ready to execute.
![Flight Plan: Match Planes with Minmus 1](https://i.imgur.com/1Se5ET3.png)

Here we can see that K2-D2 has been activated. The Flight Plan status has been updated to show that we're executing the planned maneuver, and K2-D2 is reporting its status indicating the vessel is turning to point in the right direction for the planned burn.
![Flight Plan: Match Planes with Minmus 2](https://i.imgur.com/eHeOimz.png)

Here we can see K2-D2's status indicates we're warping to the burn. The Flight Plan status is unchanged.
![Flight Plan: Match Planes with Minmus 3](https://i.imgur.com/j1kp06a.png)

Here we can see K2-D2's status indicates we're executing the burn. The Flight Plan status is unchanged.
![Flight Plan: Match Planes with Minmus 4](https://i.imgur.com/SmpwCSx.png)

Here we can see the plane change burn is done, the old node has been deleted, and we're now in a coplanar orbit with the target: Minums.
![Flight Plan: Match Planes with Minmus 5](https://i.imgur.com/qeKaGaS.png)

### Step 2: Hohmann Transfer
Now that we're in a coplanar orbit with our target we're ready to plan a Hohmann Transfer. Note that Flight Plan generated Hohmann Transfer maneuvers are not always spot on but will get you close. For this reason Flight Plan will bring up the Maneuver Node Controller mod if it's installed any time it produces an Hohmann Transfer. You may need to make minor adjustments to the prograde burn component or the node time, but should find that it's easy to get the transfer orbit you need with only a few clicks and no need to manually tweak the node. This example produced a good initial orbit that only required a few m/s more prograde delta-v and a slightly earlier burn time to get the result shown below.
![Flight Plan: Match Planes with Minmus 6](https://i.imgur.com/pFHp7Du.png)

Here we can see K2-D2 has been commanded to execute the node and we're warping to the starting point for the burn.
![Flight Plan: Match Planes with Minmus 7](https://i.imgur.com/sb9e2cu.png)

Here we can see K2-D2 executing the transfer burn.
![Flight Plan: Match Planes with Minmus 8](https://i.imgur.com/ErbYeRw.png)

**Step 3: Course Correction**
Sometimes in the game, as in life, things don't go quite as planned. What if you overshot the planned burn slightly as shown below? This can easily happen when executing a burn manually, and may also happen in some isolated cases when executing an automated burn. 
![Flight Plan: Match Planes with Minmus 8a](https://i.imgur.com/dHYzrHN.png)

Here we can see a Course Correction burn has been planned. Like the Hohmann Transfer option, this option will also bring up the Maneuver Node Controller mod so you can fine tune things to make sure you've got the exact node you want. In this case very small prograde adjustments were made to get a good Pe at the Minmus flyby encounter.
![Flight Plan: Match Planes with Minmus 9](https://i.imgur.com/rsZUbjG.png)

Here we can see K2-D2 performing a flawless Course Correction burn to get us back on track and headed for the encounter we want.
![Flight Plan: Match Planes with Minmus 10](https://i.imgur.com/EKoN3BR.png)

**Step 4: Arrival at Minmus**
Here we can see that we've arrived inside the Minmus SOI, and are on track for a nearly equatorial flyby with a nice low Pe in a prograde orbit. What if we'd like to have an inclined orbit when we get to Minmus? Easy! Use Flight Plan to set up a a New Inclination at a burn time 30 seconds from now (this offset ensures we'll have sufficient time to point in the direction we need before the start of the burn).
![Flight Plan: Match Planes with Minmus 11](https://i.imgur.com/TwGmGpb.png)

**Step 5: Capture!** 
Here we can see we're in a 60 degree inclined flyby orbit and we've got a Circularization burn planned for the periapsis of the Minmus encounter.
![Flight Plan: Match Planes with Minmus 12](https://i.imgur.com/mAkr1MG.png)

Here we are approaching Pe in our Minmus Flyby with a Circularization burn planned to put us into a 60 degree inclined low circular orbit about Minmus. Perfect for picking a landing spot almost anywhere we may want to go. In this view you can also see the Maneuver Node Controller's GUI showing that our planned maneuver will place us in the orbit we want.
![Flight Plan: Match Planes with Minmus 13](https://i.imgur.com/OUeWhYu.png)

## Older Examples
The follwing images show more details illustrating Flight Plans fetures and capabilities. Althoguh the GUI has since been updated, these images still show relevant performance characteristics for the nodes you can generate.
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

This mod is primarily meant as a direct aid to the player but can also be used as a service provider to other mods which can call functions in this one without needing to recreate all these functions in the base mod. Creating maneuver nodes from a KSP2 mod is not necessarily an intuitive thing to code, so having this as a resource you can call may save you time developing those capabilities internally to your mod.

## Orbital Maneuver Node Capabilities

**NOTE 1:** All of the orbital maneuver node creation methods in Flight Plan take a (double) burnUT, and an optional (double) burnOffsetFactor parameter. This factor is uesd with the node's burn duration (as estimated by the game) to allow you to offset the start time of the node. By default in KSP2 nodes begin at the time you've created them for unlike in KSP1 where they would bracket the requested start time. This optional parameter allows you to plan maneuver nodes that will start earlier so that the applied Delta-v occurs centered on the intended time.

**NOTE 2:** All of the orbital maneuver node creation methods in Flight Plan return a boolean value which is true if the node creation was successful and false otherwise.

* **Circularize(burnUT, burnOffsetFactor)**: Calling this method will create a maneuver node at the specified time for the active vessel to circularize the vessel's orbit at that point. This method can be used to plan a circularization burn at the next Apoapsis, Periapsis, or any other time that suits your needs during the orbit.
* **SetNewPe(burnUT, newPeR, burnOffsetFactor)**: Calling this method will create a maneuver node at the specified time to set the vessel's Periapsis (measured from center of body, not altitude above serface).
* **SetNewAp(burnUT, newApR, burnOffsetFactor)**: Calling this method will create a maneuver node at the specified time to set the vessel's Apoapsis (measured from center of body, not altitude above serface).
* **Ellipticize(burnUT, newApR, newPeR, burnOffsetFactor)**: Calling this method will create a maneuver node at the specified time to set the vessel's Apoapsis and Periapsis (both measured from center of body, not altitude above serface).
* **SetInclination(burnUT, newIncDeg, burnOffsetFactor)**: Calling this method will create a maneuver node at the specified time to set the vessel's orbital inclination (in degrees).
* **SetLAN(burnUT, newLANDeg, burnOffsetFactor)**: Calling this method will create a maneuver node at the specified time to set the vessel's longitude of ascending node (in degrees).
* **MatchPlanes(burnUT, burnOffsetFactor)**: Calling this method will create a maneuver node at the specified time to set the vessel's orbit inclination to match that of the currently selected target.
* **HohmannTransfer(burnUT, burnOffsetFactor)**: Calling this method will create a maneuver node at the *optimal time* for a Hohmann Transfer to the currently selected target at the next available window. *NOTE*: The supplied burnUT parameter presently has no effect.
* **InterceptTgt(burnUT, deltaUT, burnOffsetFactor)**: Calling this method will create a maneuver node at the specified time to intercept the currently selected target at the a time of deltaUT from now.
* **CourseCorrection(burnUT, burnOffsetFactor)**: Calling this method will create a maneuver node at the *optimal time* to finetune the trajectory to intercept the currently selected target. This method may be useful after executing a Hohman Transfer or Moon Return maneuver node. Calling it prior to node execution is suboptimal and not advised. *NOTE*: The supplied burnUT parameter presently has no effect.
* **MoonReturn(burnUT, targetPe, burnOffsetFactor)**: Calling this method will create a maneuver node at the *optimal time* for a Hohmann Transfer to return the active vessel from a moon to the planet the moon is orbiting in a transfer orbit that give the targetPe when arriving at the parent planet. *NOTE*: The supplied burnUT parameter presently has no effect.
* **MatchVelocity(burnUT, burnOffsetFactor)**: Calling this method will create a maneuver node at the specified time to match velocity with the currently selected target.
* **PlanetaryXfer(burnUT, burnOffsetFactor)**: Calling this method will create a maneuver node at the *optimal time* for a Hohmann Transfer to the currently selected target planet during the next available transfer window. *NOTE*: The supplied burnUT parameter presently has no effect.

## Orbital Time Prognistication Capabilities (KSP2's plus those available from Flight Plan)
These methods can be accessed either directly (in the case of KSP2 native parameters) or by calling Flight Plan to get them. They are useful to determine the burnUT you may wish to use in a call to one of Flight Plans's maneuver node creation methods above.

* Time_to_Ap - Basic KSP2 parameter available from the active vessel's Orbit object.
* Time_to_Pe - Basic KSP2 parameter available from the active vessel's Orbit object.
* Radius(UT) - Basic KSP2 parameter available from the active vessel's Orbit object. Use in conjunction with AN/DN time predictors to get the *cheapest* node (hgihest node).
* **NextApoapsisTime(UT)**: Returns the time of the next Ap occuring *after* UT. Useful for finding an Ap further in the future
* **NextPeriapsisTime(UT)**: Returns the time of the next Pe occuring *after* UT. Useful for finding a Pe further in the future
* **NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT)**: Returns the time of the next closest approach *after* UT between the active vessel and the target specified.
* TimeOfAscendingNodeEquatorial(UT): Returns the time of the next *Equatorial* AN *after* UT for the active vessel. This is the point where the active vessel's orbit moves North through the plane of the ecliptic for the parent body the vessel is in orbit about.
* TimeOfDescendingNodeEquatorial(UT): Returns the time of the next *Equatorial* DN *after* UT for the active vessel. This is the point where the active vessel's orbit moves South through the plane of the ecliptic for the parent body the vessel is in orbit about.
* TimeOfAscendingNode(currentTarget.Orbit, UT): Returns the time of the next *target relative* AN *after* UT for the active vessel. This is the point where the active vessel's orbit moves North through the plane of the target's orbit for targets in orbit about the same parent body the vessel is orbiting.
* TimeOfDescendingNode(currentTarget.Orbit, UT): Returns the time of the next *target relative* DN *after* UT for the active vessel. This is the point where the active vessel's orbit moves South through the plane of the target's orbit for targets in orbit about the same parent body the vessel is orbiting.
* NextTimeOfRadius(UT, Radius): This is the next time *after* UT at which the active vessel will have the radius specified. Returns -1 in the event that the specified radius is not possible given the current orbit.

To use this mod from your mod you will need to do one of the following:

## Hard Dependency via Nuget Package
If core capabilities in your mod will rely on calling Flight Plan methods, then setting up a **hard dependency** like this is the way to go, plus it's actually easier to develop your mod this way. There are two ways to set up your mod for development with Flight Plan as a hard dependency, and this is the easiest of the two, so is the recommended way. Fundamentally, it works just like what you're already doing to reference BepInEx and SpaceWarp.

* The **advantage** to this way is coding will be easier for you! Just call FlightPlanPlugin.Instance.*method_name()* for any public method in Flight Plan!
* The **disadvantage** to this way is you've got a *hard dependency* and your mod will not even start up unless both Flight Plan and its hard dependency of Node Manager are installed. You may want to ship a copy of Flight Plan and Node Manager with your mod (put both the flight_plan and node_manager plugin folders with your mod's plugin folder into the BepInEx/plugins folder before zipping it up). There may be a way to do this with CKAN in some automated fashion. This guide will be updated with those details, or a link to them, at some point.

### Step 1: Update your csproj file
In your csproj file you may already have an ItemGroup where BepInEx, HarmonyX, and SpaceWarp are added as PackageReferene includes. If so, all you need to do is add PackageReferences for FlightPlan and NodeManager as sown below.

```xml
    <ItemGroup>
        <PackageReference Include="BepInEx.Analyzers" Version="1.*" PrivateAssets="all" />
        <PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.0" PrivateAssets="all" />
        <PackageReference Include="BepInEx.Core" Version="5.*" />
        <PackageReference Include="BepInEx.PluginInfoProps" Version="2.*" />
        <PackageReference Include="HarmonyX" Version="2.10.1" />
        <PackageReference Include="SpaceWarp" Version="1.1.1" />
        <PackageReference Include="FlightPlan" Version="0.7.3" />
        <PackageReference Include="NodeManager" Version="0.5.3" />
        <PackageReference Include="UnityEngine.Modules" Version="2020.3.33" IncludeAssets="compile" />
    </ItemGroup>
```

### Step 2: Configure Namespace and Add Dependency
Bring in the FlightPlan namespace in the class you want to call it from, and add both Flight Plan and Node Manager as a BepInDependencies. You won't need the NodeManger namespace unless you plan to also call Node Manager directly.

```cs
    using FlightPlan;
    
    namespace MyCoolModsNameSpace;
    
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
    [BepInDependency(FlightPlanPlugin.ModGuid, FlightPlanPlugin.ModVer)]
    [BepInDependency(NodeManagerPlugin.ModGuid, NodeManagerPlugin.ModVer)]
    public class MyCoolModName : BaseSpaceWarpPlugin
```

### Step 3: Call Flight Plan Methods from your Mod
You can now call any of Node Manager's public methods directly and easily from your code. Here are some examples:

```cs
    pass = FlightPlanPlugin.Instance.Circularize(burnUT, burnOffsetFactor) // double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.SetNewPe(burnUT, newPeR, burnOffsetFactor); // double, double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.SetNewAp(burnUT, newApR, burnOffsetFactor); // double, double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.Ellipticize(burnUT, newApR, newPeR, burnOffsetFactor); // double, double, double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.SetInclination(burnUT, newIncDeg, burnOffsetFactor); // double, double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.MatchPlanes(burnUT, burnOffsetFactor); // double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.HohmannTransfer(burnUT, burnOffsetFactor); // double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.InterceptTgt(burnUT, deltaUT, burnOffsetFactor); // double, double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.CourseCorrection(burnUT, burnOffsetFactor); // double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.MoonReturn(burnUT, burnOffsetFactor); // double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.MatchVelocity(burnUT, burnOffsetFactor); // double, double (default = -0.5)
    pass = FlightPlanPlugin.Instance.PlanetaryXfer(burnUT, burnOffsetFactor); // double, double (default = -0.5)
```

### Step 4: Profit!

## Hard Dependency via Local Copy of Flight Plan DLL
This way works like the method above with a few minor differences in your csproj and what you need to do in your mod's development folder.

### Step 1: Configure Assemblies
Add a copy of the flight_plan.dll and node_manager.dll to your mod's list of Assemblies. Generally, this means two things. First, put copies of the flight_plan.dll and node_manager.dll in a folder where your mod can find them. You may want to put them in the same folder you have Assembly-CSharp.dll. Secondly, add them to your csproj file similarly to how you're already referencing Assembly-CSharp.dll. Your mod will need to have access to them, and know where to find them, when you compile your mod. At run time your mod will be accessing the flight_plan.dll from the flight_plan plugins folder where Flight Plan is installed, so you don't need to distribute the Flight Plan or Node Manager DLLs with your mod, but both of those mods will need to be installed in the players game for your to be able to access Flight Plan.

In your csproj file locate the ItemGroup where you have local References defined. There will be at least one for Assembly-CSharp.dll. You'll need to add one for Flight Plan and one for Node Manager like this.

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
        <Reference Include="node_manager">
            <HintPath>..\external_dlls\node_manager.dll</HintPath>
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
This is the way to go if optional capabilities in your mod will rely on functions in this Flight Plan, and you don't want your mod to have a hard dependency on Flight Plan.

* The **advantage** to this way is that your mod's users don't need to have Flight Plan installed if they prefer not to have it, and you can distribute your mod without needing a hard dependency on Flight Plan - meaning you mod can launch and run yor mod without Flight Plan or its dependency Node Manager, although there may be some capabilities that aren't available to your users if they choose to pass on installing Flight Plan and Node Manager.
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

### Step 3: Check for Flight Plan
Somewhere in your mod you need to check to make sure Flight Plan is loaded before you use it (e.g., OnInitialized()). You don't need this with a hard dependency, but it's essential for a soft one.

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
This is where things get really different for you compared to what's needed to call Flight Plan methods using a hard dependency. For a soft dependency to work you're going to need to create a reflection calling method for each of the Flight Plan methods that you would like to call from your mod. Here's an example of one for calling Flight Plan's SetNewPe method which will pass it the new Pe you would like to have, and optionally a burn time offset. Note: Using a burn time time offset of -0.5 will cause the maneuver node to be centered on the nominal time for the burn (next Ap in this case).

```cs
    private void SetNewPe(double burnUT, double newPeR, double burnOffsetFactor = -0.5)
    {
        if (FPLoaded)
        {
            // Reflections method to call Node Manager methods from your mod
            var nmType = Type.GetType($"FlightPlan.FlightPlanPlugin, {FlightPlanPlugin.ModGuid}");
            Logger.LogDebug($"Type name: {nmType!.Name}");
            var instanceProperty = nmType!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            Logger.LogDebug($"Property name: {instanceProperty!.Name}");
            var methodInfo = instanceProperty!.PropertyType.GetMethod("SetNewPe");
            Logger.LogDebug($"Method name: {methodInfo!.Name}");
            methodInfo!.Invoke(instanceProperty.GetValue(null), new object[] { burnUT, newPeR, burnOffsetFactor });
        }
    }
```

This example includes some (optional) debug logging that may be helpful if you are having trouble with the reflection calling method. You can safely remove those once it's working to your satisfaction.

### Step 5: Call Reflection Method
Call your reflection method wherever you need to invoke the corresponding Node Manager method.

```cs
    SetNewPe(newPeR, -0.5);
```

### Step 6: Profit!
