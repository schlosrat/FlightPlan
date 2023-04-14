# Flight Plan
![Flight Plan GUI](https://i.imgur.com/UI0BWGY.png)

Plan your (Space) Flight! Fly your Plan! Handy tools to help you set up maneuver nodes that will get you where you want to be.
Making spaceflight planning easier for Kerbal Space Program 2 one mission at a time.

**NOTE:** This mod draws heavily on some core [MechJeb2](https://github.com/MuMech/MechJeb2) code that has been adapted to work in KSP2, and would not be possible without the kind and generous contributions of Sarbian and the MechJeb development team! It is not the intent to replicate all MechJeb2 features and functions in this mod, but merely to make some handy maneuver planning tools available for KSP2 players. While you may be able to create some useful nodes with this mod, you'll still need to execute them accurately! Also, understanding some basic mission planning will be very usful for those employing the tools in this toolbox.

## Compatibility
* Tested with Kerbal Space Program 2 v0.1.2.0.22258 & SpaceWarp 1.1.3
* Requires SpaceWarp 1.0.1

## Installation
1. Download and extract SpaceWarp into your game folder. If you've installed the game via Steam, then this is probably here: *C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program 2*. If you complete this step correctly you'll have a **BepInEx** subfolder in that directory along with the following files (in addition to what was there before): **changelog.txt, doorstop_config.ini, winhttp.dll**
1. Download and extract this mod into the game folder. The mod's ZIP file contains a single BepInEx folder. You can drag that right onto your KSP2 folder to install the mod. If done correctly, you should have the following folder structure within your KSP2 game folder: *KSP2GameFolder*/**BepInEx/plugins/flight_plan**.

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

**NOTE:** At this time *Flight Plan has no capability to execute nodes* - it just helps you plan them. Getting a *Good* result in the status does not mean your craft is pointed in the right direction or is otherwise ready to execute the node, but rather that the node is ready for you!

## UI Screens
In addition to the basic UI screen above the UI will automatically asjust to offer capabilities relevant to the current orbit and selected target.
### Selecting a Celestial Target with the Drop Down Menu
![Flight Plan Target Selection GUI](https://i.imgur.com/NyhCARt.png)

### With a Local Object Selected (selected target is orbiting the same body your vessel is)
![Flight Plan Target Selected GUI - Moon](https://i.imgur.com/givkRgG.png)

## Configuration Parameters
![Flight Plan Configuration Parameters](https://i.imgur.com/8LRSdtU.png)

This mod includes a number of user configurable parameters which can be accessed through the *SpaceWarp* configuration screen. Press Alt + M to pull up the SpaceWarp Mod dialog, and select the **Open Configuration Manager** button at the bottom to display the list of installed mods with configurable settings. Clicking on the Flight Plan entry will display the ionterface shown above. There are tool tip strings which describe what each setting does.
Using the configuration parameters you can change a variety of things such as how long a staus message sticks around before it start to fade, and also how long it will take to fade.
**NOTE:** Changing settings for mods requires the game to be exited and restarted for the new settings to take effect.

## Planned Improvement
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

### Circularize at P3 for Munar Fly By
![Flight Plan: Circularize at Next Pe Example - Munar Flyby](https://i.imgur.com/gFuZRau.png)

### Hohmann Transfer to Mun (from non-coplanar orbit: Inclined 20 degrees from target plane)
![Flight Plan: Hohmann Transfer Example](https://i.imgur.com/iliH2bY.png)

