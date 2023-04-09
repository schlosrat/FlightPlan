# Flight Plan
![Flight Plan GUI](https://i.imgur.com/nAqnh60.png)
Plan your (Space) Flight! Fly your Plan! Handy tools to help you set up maneuver nodes that will get you where you want to be.
Making spaceflight planning easier for Kerbal Space Program 2 one mission at a time.

NOTE: This mod draws heavily on some core [MechJeb2](https://github.com/MuMech/MechJeb2) code that has been adapted to work in KSP2, and would not be possible without the kind and generous contributions of Sarbian and the MechJeb development team! It is not the intent to replicate all MechJeb2 features and functions in this mod, but merely to make some handy maneuver planning tools available for KSP2 players. While you may be able to create some useful nodes with this mod, you'll still need to execute them accurately! Also, understanding some basic mission planning will be very usful for those employing the tools in this toolbox.

## Compatibility
* Tested with Kerbal Space Program 2 v0.1.1.0.21572
* Requires SpaceWarp 1.0.1
## Features
### Display Current Target Selection
### Create Maneuver Nodes for Own Ship Maneuvers
* Circularize at Ap
* Circularize at Pe
* Circularize Now
* New Pe (for user specified value) - planned for next Ap
* New Ap (for user specified value) - planned for next Pe
* New Pe & Ap (for user specified values) - planned now
* New Inclination (for user specified value) - Planned for next equatorial AN
### Create Maneuver Nodes for Maneuvers Relative to the Selected Target (only available if a target is selected)
* Match planes at AN
* Match Planed at DN
* Hohmann Transfer to Target
* Intercept Target (for user specified time from now)
* Course Correction (requires being on an intercept trajectory)
* Match Velocity at Closest Approach (requires being on an intercept trajectory)
* Match Velocity Now
### Create Interplanetary Transfer Maneuver Nodes (only available if a planet is the selected target)
* Interplanetary Transfer
### Create Moon Specific Maneuver Nodes (only available when in orbit about a moon)
* Return from a Moon

![Flight Plan: Circularize at Next Ap](https://i.imgur.com/3dQ6LBS.png)
![Flight Plan: Circularize at Next Pe](https://i.imgur.com/by0kbUF.png)

## Planned Improvements
To see what improvements and new features are planned for this mod, you can visit the Issues page on the project's GitHub.
## Installation
1. Download and extract SpaceWarp into your game folder.
1. Download and extract this mod into the game folder. If done correctly, you should have the following folder structure: <KSP Folder>/BepInEx/plugins/flight_plan.
