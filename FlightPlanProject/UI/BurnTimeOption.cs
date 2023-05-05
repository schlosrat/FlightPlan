using KSP.Game;
using KSP.Sim.impl;
using MuMech;
using UnityEngine;
using FlightPlan.KTools.UI;

namespace FlightPlan;

internal class BurnTimeOption
{
    public static BurnTimeOption _instance;
    public static BurnTimeOption Instance { get => _instance; }

    public BurnTimeOption()
    {
        _instance = this;
    }

    private bool is_active = false;
    // Option selection
    private Vector2 scrollPositionOptions;

    // Time references for BurnTimeOption.selected
    public readonly static Dictionary<TimeRef, string> text_time_ref = new()
    {
        { TimeRef.None,              ""                         },
        { TimeRef.COMPUTED,          "at optimum time"          }, //at the optimum time
        { TimeRef.APOAPSIS,          "at next apoapsis"         }, //"at the next apoapsis"
        { TimeRef.PERIAPSIS,         "at next periapsis"        }, //"at the next periapsis"
        { TimeRef.CLOSEST_APPROACH,  "at closest approach"      }, //"at closest approach to target"
        { TimeRef.EQ_ASCENDING,      "at equatorial AN"         }, //"at the equatorial AN"
        { TimeRef.EQ_DESCENDING,     "at equatorial DN"         }, //"at the equatorial DN"
        { TimeRef.REL_ASCENDING,     "at next AN with target"   }, //"at the next AN with the target."
        { TimeRef.REL_DESCENDING,    "at next DN with target"   }, //"at the next DN with the target."
        { TimeRef.X_FROM_NOW,        "after a fixed time"       }, //"after a fixed time"
        { TimeRef.ALTITUDE,          "at an altitude"           }, //"at an altitude"
        { TimeRef.EQ_NEAREST_AD,     "at nearest Eq. AN/DN"     }, //"at the nearest equatorial AN/DN"
        { TimeRef.EQ_HIGHEST_AD,     "at cheapest Eq. AN/DN"    }, //"at the cheapest equatorial AN/DN"
        { TimeRef.REL_NEAREST_AD,    "at nearest AN/DN w/Target"  }, //"at the nearest AN/DN with the target"
        { TimeRef.REL_HIGHEST_AD,    "at cheapest AN/DN w/Target" } //"at the cheapest AN/DN with the target"
    };

    public List<TimeRef> options = new List<TimeRef>();
    public static double requestedBurnTime = 0;

    // Allow the user to pick an option for the selected activity
    public bool listGui()
    {
        if (!is_active)
            return false;

        if (options.Count == 0)
        {
            FlightPlanUI.time_ref = TimeRef.None;
            is_active = false;
        }

        GUILayout.BeginHorizontal();
        UI_Tools.Label("Burn Time Option ");
        if (UI_Tools.SmallButton("Cancel"))
        {
            is_active = false;
        }
        GUILayout.EndHorizontal();

        UI_Tools.Separator();

        //GUI.SetNextControlName("Select Target");
        scrollPositionOptions = UI_Tools.BeginScrollView(scrollPositionOptions, 300);

        foreach (var option in options)
        {
            GUILayout.BeginHorizontal();
            if (UI_Tools.ListButton(text_time_ref[option]))
            {
                FlightPlanUI.time_ref = option;
                is_active = false;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        return true;
    }

    // Control display of the Option Picker UI
    public void OptionSelectionGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Burn : ");


        if (UI_Tools.SmallButton(text_time_ref[ FlightPlanUI.time_ref ]))
            is_active = options.Count > 0;

        GUILayout.EndHorizontal();
    }

    // This method should be called after getting the BurnTimeOption.selected for desired maneuver time/effect
    public void setBurnTime()
    {
        // Set the requested burn time based on the selected timing option
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        var plugin = FlightPlanPlugin.Instance;
        PatchedConicsOrbit orbit = plugin.activeVessel.Orbit;

        switch (FlightPlanUI.time_ref)
        {
            case TimeRef.None:

                break;
            case TimeRef.COMPUTED:
                requestedBurnTime = -1; // for optimal time the burn time is computed and returned from the OrbitalManeuverCalculator method called.

                break;
            case TimeRef.APOAPSIS:
                requestedBurnTime = orbit.NextApoapsisTime(UT);
                break;

            case TimeRef.PERIAPSIS:
                requestedBurnTime = orbit.NextPeriapsisTime(UT);
                break;
            case TimeRef.CLOSEST_APPROACH:
                requestedBurnTime = orbit.NextClosestApproachTime(plugin.currentTarget.Orbit as PatchedConicsOrbit, UT + 2); // +2 so that closestApproachTime is definitely > UT
                break;
            case TimeRef.EQ_ASCENDING:
                requestedBurnTime = orbit.TimeOfAscendingNodeEquatorial(UT);
                break;
            case TimeRef.EQ_DESCENDING:
                requestedBurnTime = orbit.TimeOfDescendingNodeEquatorial(UT);
                break;
            case TimeRef.REL_ASCENDING:
                requestedBurnTime = orbit.TimeOfAscendingNode(plugin.currentTarget.Orbit, UT); // like built in TimeOfAN(currentTarget.Orbit, UT), but with check to prevent time in the past
                break;
            case TimeRef.REL_DESCENDING:
                requestedBurnTime = orbit.TimeOfDescendingNode(plugin.currentTarget.Orbit, UT); // like built in TimeOfDN(currentTarget.Orbit, UT), but with check to prevent time in the past
                break;
            case TimeRef.X_FROM_NOW:
                requestedBurnTime = UT + FPSettings.timeOffset;
                break;
            case TimeRef.ALTITUDE:
                requestedBurnTime = orbit.NextTimeOfRadius(UT, FPSettings.altitude_km * 1000);
                break;
            case TimeRef.EQ_NEAREST_AD:
                requestedBurnTime = Math.Min(orbit.TimeOfAscendingNodeEquatorial(UT), orbit.TimeOfDescendingNodeEquatorial(UT));
                break;
            case TimeRef.EQ_HIGHEST_AD:
                {
                    var timeAN = orbit.TimeOfAscendingNodeEquatorial(UT);
                    var timeDN = orbit.TimeOfDescendingNodeEquatorial(UT);
                    var ANRadius = orbit.Radius(timeAN);
                    var DNRadius = orbit.Radius(timeDN);
                    if (ANRadius > DNRadius)
                        requestedBurnTime = timeAN;
                    else
                        requestedBurnTime = timeDN;
                }
                break;
            case TimeRef.REL_NEAREST_AD:
                requestedBurnTime = Math.Min(orbit.TimeOfAscendingNode(plugin.currentTarget.Orbit, UT), orbit.TimeOfDescendingNode(plugin.currentTarget.Orbit, UT));
                break;
            case TimeRef.REL_HIGHEST_AD:
                {
                    var timeAN = orbit.TimeOfAscendingNode(plugin.currentTarget.Orbit, UT);
                    var timeDN = orbit.TimeOfDescendingNode(plugin.currentTarget.Orbit, UT);
                    var ANRadius = orbit.Radius(timeAN);
                    var DNRadius = orbit.Radius(timeDN);
                    if (ANRadius > DNRadius)
                        requestedBurnTime = timeAN;
                    else
                        requestedBurnTime = timeDN;
                }
                break;
            default:
                break;
        }
    }

    

    // This method sets up the options list based on the selected activity. This method also configures the _toggles dictionary to record the setting of the "radio buttons"
    // for comparison to the _previousToggles dictionary.
    public string setOptionsList(ManeuverType type)
    {
        options.Clear();
        
        var activeVessel = FlightPlanPlugin.Instance.activeVessel;

        string maneuver_type_desc = "";


        switch (type)
        {
            case ManeuverType.None:
                maneuver_type_desc = "None";
                break;
            case ManeuverType.circularize:
                if (activeVessel.Orbit.eccentricity < 1)
                    options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"


                options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                options.Add(TimeRef.ALTITUDE); //"At An Altittude"
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                maneuver_type_desc = "Circularizing";
                break;
            case ManeuverType.newPe:
                if (activeVessel.Orbit.eccentricity < 1)
                    options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                options.Add(TimeRef.PERIAPSIS); //"At Next Apoapsis"
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
                options.Add(TimeRef.ALTITUDE); //"At An Altittude"

                maneuver_type_desc = "Setting new Pe";
                break;
            case ManeuverType.newAp:
                options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                if (activeVessel.Orbit.eccentricity < 1)
                    options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
                options.Add(TimeRef.ALTITUDE); //"At An Altittude"

                maneuver_type_desc = "Setting new Ap";
                break;
            case ManeuverType.newPeAp:
                if (activeVessel.Orbit.eccentricity < 1)
                    options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
                options.Add(TimeRef.ALTITUDE); //"At An Altittude"
                options.Add(TimeRef.EQ_ASCENDING); //"At Equatorial AN"
                options.Add(TimeRef.EQ_DESCENDING); //"At Equatorial DN"

                maneuver_type_desc = "Elipticizing";
                break;
            case ManeuverType.newInc:
                options.Add(TimeRef.EQ_HIGHEST_AD); //"At Cheapest eq AN/DN"
                options.Add(TimeRef.EQ_NEAREST_AD); //"At Nearest eq AN/DN"
                options.Add(TimeRef.EQ_ASCENDING); //"At Equatorial AN"
                options.Add(TimeRef.EQ_DESCENDING); //"At Equatorial DN"
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                maneuver_type_desc = "Setting new inclination";
                break;
            case ManeuverType.newLAN:
                if (activeVessel.Orbit.eccentricity < 1)
                    options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                maneuver_type_desc = "Setting new LAN";
                break;
            case ManeuverType.newNodeLon:
                if (activeVessel.Orbit.eccentricity < 1)
                    options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                maneuver_type_desc = "Shifting Node LongitudeN";
                break;
            case ManeuverType.newSMA:
                if (activeVessel.Orbit.eccentricity < 1)
                    options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                maneuver_type_desc = "Setting new SMA";
                break;
            case ManeuverType.matchPlane:
                options.Add(TimeRef.REL_HIGHEST_AD); //"At Cheapest AN/DN With Target"
                options.Add(TimeRef.REL_NEAREST_AD); //"At Nearest AN/DN With Target"
                options.Add(TimeRef.REL_ASCENDING); //"At Next AN With Target"
                options.Add(TimeRef.REL_DESCENDING); //"At Next DN With Target"

                maneuver_type_desc = "Matching planes";
                break;
            case ManeuverType.hohmannXfer:
                options.Add(TimeRef.COMPUTED); //"At Optimal Time"

                maneuver_type_desc = "Performing Homann transfer";
                break;
            case ManeuverType.courseCorrection:
                options.Add(TimeRef.COMPUTED); //"At Optimal Time"

                maneuver_type_desc = "Performaing course correction";
                break;
            case ManeuverType.interceptTgt:
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                maneuver_type_desc = "Intercepting";
                break;
            case ManeuverType.matchVelocity:
                options.Add(TimeRef.CLOSEST_APPROACH); //"At Closest Approach"
                options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
                maneuver_type_desc = "Matching velocity";
                break;
            case ManeuverType.moonReturn:
                options.Add(TimeRef.COMPUTED); //"At Optimal Time"
                maneuver_type_desc = "Performaing moon return";
                break;
            case ManeuverType.planetaryXfer:
                options.Add(TimeRef.COMPUTED); //"At Optimal Time"
                maneuver_type_desc = "Performing planetary transfer";
                break;
            default:
                break;
        }

        if (!options.Contains(FlightPlanUI.time_ref))
            FlightPlanUI.time_ref = options[0];

        return maneuver_type_desc;
    }


    public static string TimeRefDesc
    {
        get
        {
            return text_time_ref[FlightPlanUI.time_ref];
        }    
    }
}
