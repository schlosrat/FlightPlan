# Flight Plan
![Flight Plan GUI](https://i.imgur.com/nAqnh60.png)
Plan your (Space) Flight! Fly your Plan! Handy tools to help you set up maneuver nodes that will get you where you want to be.
Making spaceflight planning easier for Kerbal Space Program 2 one mission at a time.

NOTE: This mod draws heavily on some core [MechJeb2](https://github.com/MuMech/MechJeb2) code that has been adapted to work in KSP2, and would not be possible without the kind and generous contributions of Sarbian and the MechJeb development team! It is not the intent to replicate all MechJeb2 features and functions in this mod, but merely to make some handy maneuver planning tools available for KSP2 players. While you may be able to create some useful nodes with this mod, you'll still need to execute them accurately! Also, understanding some basic mission planning will be very usful for those employing the tools in this toolbox.

## Compatibility
* Tested with Kerbal Space Program 2 v0.1.1.0.21572 & SpaceWarp 1.1.3
* Requires SpaceWarp 1.0.1

## Features
### Display Current Target Selection
* Drop Down Menu for easy target selection from list of celestial objects
### Create Maneuver Nodes for Own Ship Maneuvers
* Circularize at Ap
* Circularize at Pe
* New Pe (for user specified value) - planned for next Ap
* New Ap (for user specified value) - planned for next Pe
* New Inclination (for user specified value) - if e < 0: Planned for cheapest AN/DN, otherwise planned for ASAP
### Create Maneuver Nodes for Maneuvers Relative to the Selected Target (only available if a target is selected)
* Match planes with Target at AN
* Match Planes with Target at DN 
* Hohmann Transfer to Target

## Planned Improvement
### Create Maneuver Nodes for Own Ship Maneuvers
* Circularize Now
* New Pe & Ap (for user specified values) - planned now
### Create Maneuver Nodes for Maneuvers Relative to the Selected Target (only available if a target is selected)
* Intercept Target (for user specified time from now)
* Course Correction (requires being on an intercept trajectory)
* Match Velocity at Closest Approach (requires being on an intercept trajectory)
* Match Velocity Now
### Create Interplanetary Transfer Maneuver Nodes (only available if a planet is the selected target)
* Interplanetary Transfer
### Create Moon Specific Maneuver Nodes (only available when in orbit about a moon)
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

## Installation
1. Download and extract SpaceWarp into your game folder.
1. Download and extract this mod into the game folder. If done correctly, you should have the following folder structure: <KSP Folder>/BepInEx/plugins/flight_plan.
