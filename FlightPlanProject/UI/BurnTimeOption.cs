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

    private bool isActive = false;
    // Option selection
    private Vector2 scrollPositionOptions;

    // Time references for BurnTimeOption.selected
    public readonly static Dictionary<TimeRef, string> TextTimeRef = new()
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

    public List<TimeRef> Options = new List<TimeRef>();
    public static double RequestedBurnTime = 0;

    // Allow the user to pick an _option for the selected activity
    public bool ListGUI()
    {
        if (!isActive)
            return false;

        if (Options.Count == 0)
        {
            FlightPlanUI.TimeRef = TimeRef.None;
            isActive = false;
        }

        GUILayout.BeginHorizontal();
        UI_Tools.Label("Burn Time Option ");
        if (UI_Tools.SmallButton("Cancel"))
        {
            isActive = false;
        }
        GUILayout.EndHorizontal();

        UI_Tools.Separator();

        //GUI.SetNextControlName("Select Target");
        scrollPositionOptions = UI_Tools.BeginScrollView(scrollPositionOptions, 300);

        foreach (TimeRef _option in Options)
        {
            GUILayout.BeginHorizontal();
            if (UI_Tools.ListButton(TextTimeRef[_option]))
            {
                FlightPlanUI.TimeRef = _option;
                isActive = false;
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


        if (UI_Tools.SmallButton(TextTimeRef[ FlightPlanUI.TimeRef ]))
            isActive = Options.Count > 0;

        GUILayout.EndHorizontal();
    }

    // This method should be called after getting the BurnTimeOption.selected for desired maneuver time/effect
    public void SetBurnTime()
    {
        // Set the requested burn time based on the selected timing _option
        double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        FlightPlanPlugin Plugin = FlightPlanPlugin.Instance;
        PatchedConicsOrbit Orbit = Plugin._activeVessel.Orbit;

        switch (FlightPlanUI.TimeRef)
        {
            case TimeRef.None:

                break;
            case TimeRef.COMPUTED:
                RequestedBurnTime = -1; // for optimal time the burn time is computed and returned from the OrbitalManeuverCalculator method called.

                break;
            case TimeRef.APOAPSIS:
                RequestedBurnTime = Orbit.NextApoapsisTime(UT);
                break;

            case TimeRef.PERIAPSIS:
                RequestedBurnTime = Orbit.NextPeriapsisTime(UT);
                break;
            case TimeRef.CLOSEST_APPROACH:
                if (Plugin._currentTarget != null)
                    RequestedBurnTime = Orbit.NextClosestApproachTime(Plugin._currentTarget.Orbit as PatchedConicsOrbit, UT + 2); // +2 so that closestApproachTime is definitely > _UT
                else
                    FlightPlanUI.TimeRef = TimeRef.None;
                break;
            case TimeRef.EQ_ASCENDING:
                RequestedBurnTime = Orbit.TimeOfAscendingNodeEquatorial(UT);
                break;
            case TimeRef.EQ_DESCENDING:
                RequestedBurnTime = Orbit.TimeOfDescendingNodeEquatorial(UT);
                break;
            case TimeRef.REL_ASCENDING:
                if (Plugin._currentTarget != null)
                    RequestedBurnTime = Orbit.TimeOfAscendingNode(Plugin._currentTarget.Orbit, UT); // like built in TimeOfAN(_currentTarget.Orbit, _UT), but with check to prevent time in the past
                else
                    FlightPlanUI.TimeRef = TimeRef.None;
                break;
            case TimeRef.REL_DESCENDING:
                if (Plugin._currentTarget != null)
                    RequestedBurnTime = Orbit.TimeOfDescendingNode(Plugin._currentTarget.Orbit, UT); // like built in TimeOfDN(_currentTarget.Orbit, _UT), but with check to prevent time in the past
                else
                    FlightPlanUI.TimeRef = TimeRef.None;
                break;
            case TimeRef.X_FROM_NOW:
                RequestedBurnTime = UT + FPSettings.TimeOffset;
                break;
            case TimeRef.ALTITUDE:
                RequestedBurnTime = Orbit.NextTimeOfRadius(UT, FPSettings.Altitude_km * 1000);
                break;
            case TimeRef.EQ_NEAREST_AD:
                RequestedBurnTime = Math.Min(Orbit.TimeOfAscendingNodeEquatorial(UT), Orbit.TimeOfDescendingNodeEquatorial(UT));
                break;
            case TimeRef.EQ_HIGHEST_AD:
                {
                    double _timeAN = Orbit.TimeOfAscendingNodeEquatorial(UT);
                    double _timeDN = Orbit.TimeOfDescendingNodeEquatorial(UT);
                    double _ascendingNodeRadius = Orbit.Radius(_timeAN);
                    double _descendingNodeRadius = Orbit.Radius(_timeDN);
                    if (_ascendingNodeRadius > _descendingNodeRadius)
                        RequestedBurnTime = _timeAN;
                    else
                        RequestedBurnTime = _timeDN;
                }
                break;
            case TimeRef.REL_NEAREST_AD:
                if (Plugin._currentTarget != null)
                    RequestedBurnTime = Math.Min(Orbit.TimeOfAscendingNode(Plugin._currentTarget.Orbit, UT), Orbit.TimeOfDescendingNode(Plugin._currentTarget.Orbit, UT));
                else
                    FlightPlanUI.TimeRef = TimeRef.None;
                break;
            case TimeRef.REL_HIGHEST_AD:
                {
                    if (Plugin._currentTarget.Orbit != null)
                    {
                        double _timeAN = Orbit.TimeOfAscendingNode(Plugin._currentTarget.Orbit, UT);
                        double _timeDN = Orbit.TimeOfDescendingNode(Plugin._currentTarget.Orbit, UT);
                        double _ascendingNodeRadius = Orbit.Radius(_timeAN);
                        double _descendingNodeRadius = Orbit.Radius(_timeDN);
                        if (_ascendingNodeRadius > _descendingNodeRadius)
                            RequestedBurnTime = _timeAN;
                        else
                            RequestedBurnTime = _timeDN;
                    }
                    else
                        FlightPlanUI.TimeRef = TimeRef.None;
                }
                break;
            default:
                break;
        }
    }

    

    // This method sets up the Options list based on the selected activity. This method also configures the _toggles dictionary to record the setting of the "radio buttons"
    // for comparison to the _previousToggles dictionary.
    public string SetOptionsList(ManeuverType type)
    {
        Options.Clear();
        
        VesselComponent ActiveVessel = FlightPlanPlugin.Instance._activeVessel;

        string ManeuverTypeDesc = "";

        switch (type)
        {
            case ManeuverType.None:
                ManeuverTypeDesc = "None";
                break;
            case ManeuverType.circularize:
                if (ActiveVessel.Orbit.eccentricity < 1)
                    Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                Options.Add(TimeRef.ALTITUDE); //"At An Altittude"
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                ManeuverTypeDesc = "Circularizing";
                break;
            case ManeuverType.newPe:
                if (ActiveVessel.Orbit.eccentricity < 1)
                    Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
                Options.Add(TimeRef.ALTITUDE); //"At An Altittude"

                ManeuverTypeDesc = "Setting new Pe";
                break;
            case ManeuverType.newAp:
                Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                if (ActiveVessel.Orbit.eccentricity < 1)
                    Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
                Options.Add(TimeRef.ALTITUDE); //"At An Altittude"

                ManeuverTypeDesc = "Setting new Ap";
                break;
            case ManeuverType.newPeAp:
                if (ActiveVessel.Orbit.eccentricity < 1)
                    Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
                Options.Add(TimeRef.ALTITUDE); //"At An Altittude"
                Options.Add(TimeRef.EQ_ASCENDING); //"At Equatorial AN"
                Options.Add(TimeRef.EQ_DESCENDING); //"At Equatorial DN"

                ManeuverTypeDesc = "Elipticizing";
                break;
            case ManeuverType.newInc:
                Options.Add(TimeRef.EQ_HIGHEST_AD); //"At Cheapest eq AN/DN"
                Options.Add(TimeRef.EQ_NEAREST_AD); //"At Nearest eq AN/DN"
                Options.Add(TimeRef.EQ_ASCENDING); //"At Equatorial AN"
                Options.Add(TimeRef.EQ_DESCENDING); //"At Equatorial DN"
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                ManeuverTypeDesc = "Setting new inclination";
                break;
            case ManeuverType.newLAN:
                if (ActiveVessel.Orbit.eccentricity < 1)
                    Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                ManeuverTypeDesc = "Setting new LAN";
                break;
            case ManeuverType.newNodeLon:
                if (ActiveVessel.Orbit.eccentricity < 1)
                    Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                ManeuverTypeDesc = "Shifting Node LongitudeN";
                break;
            case ManeuverType.newSMA:
                if (ActiveVessel.Orbit.eccentricity < 1)
                    Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
                Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                ManeuverTypeDesc = "Setting new SMA";
                break;
            case ManeuverType.matchPlane:
                Options.Add(TimeRef.REL_HIGHEST_AD); //"At Cheapest AN/DN With Target"
                Options.Add(TimeRef.REL_NEAREST_AD); //"At Nearest AN/DN With Target"
                Options.Add(TimeRef.REL_ASCENDING); //"At Next AN With Target"
                Options.Add(TimeRef.REL_DESCENDING); //"At Next DN With Target"

                ManeuverTypeDesc = "Matching planes";
                break;
            case ManeuverType.hohmannXfer:
                Options.Add(TimeRef.COMPUTED); //"At Optimal Time"

                ManeuverTypeDesc = "Performing Homann transfer";
                break;
            case ManeuverType.courseCorrection:
                Options.Add(TimeRef.COMPUTED); //"At Optimal Time"

                ManeuverTypeDesc = "Performaing course correction";
                break;
            case ManeuverType.interceptTgt:
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                ManeuverTypeDesc = "Intercepting";
                break;
            case ManeuverType.matchVelocity:
                Options.Add(TimeRef.CLOSEST_APPROACH); //"At Closest Approach"
                Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

                ManeuverTypeDesc = "Matching velocity";
                break;
            case ManeuverType.moonReturn:
                Options.Add(TimeRef.COMPUTED); //"At Optimal Time"

                ManeuverTypeDesc = "Performaing moon return";
                break;
            case ManeuverType.planetaryXfer:
                Options.Add(TimeRef.COMPUTED); //"At Optimal Time"

                ManeuverTypeDesc = "Performing planetary transfer";
                break;
            case ManeuverType.fixAp:
                Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"

                ManeuverTypeDesc = "Setting new Ap";
                break;
            case ManeuverType.fixPe:
                Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"

                ManeuverTypeDesc = "Setting new Pe";
                break;
            default:
                break;
        }

        if (Options.Count < 1)
            Options.Add(TimeRef.None);

        if (!Options.Contains(FlightPlanUI.TimeRef))
            FlightPlanUI.TimeRef = Options[0];

        return ManeuverTypeDesc;
    }


    public static string TimeRefDesc
    {
        get
        {
            return TextTimeRef[FlightPlanUI.TimeRef];
        }    
    }
}
