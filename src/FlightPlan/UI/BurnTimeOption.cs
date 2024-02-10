using KSP.Game;
using KSP.Sim.impl;
using MuMech;
using UnityEngine;

namespace FlightPlan;

internal class BurnTimeOption
{
    private static readonly GameInstance Game = GameManager.Instance.Game;

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
    public readonly static Dictionary<TimeReference, string> TextTimeRef = new()
    {
        { TimeReference.None,              ""                            },
        { TimeReference.COMPUTED,          "at optimum time"             },
        { TimeReference.APOAPSIS,          "at next apoapsis"            },
        { TimeReference.PERIAPSIS,         "at next periapsis"           },
        { TimeReference.CLOSEST_APPROACH,  "at closest approach"         },
        { TimeReference.EQ_ASCENDING,      "at equatorial AN"            },
        { TimeReference.EQ_DESCENDING,     "at equatorial DN"            },
        { TimeReference.REL_ASCENDING,     "at next AN with target"      },
        { TimeReference.REL_DESCENDING,    "at next DN with target"      },
        { TimeReference.X_FROM_NOW,        "after a fixed time"          },
        { TimeReference.ALTITUDE,          "at an altitude"              },
        { TimeReference.EQ_NEAREST_AD,     "at nearest Eq. AN/DN"        },
        { TimeReference.EQ_HIGHEST_AD,     "at cheapest Eq. AN/DN"       },
        { TimeReference.REL_NEAREST_AD,    "at nearest AN/DN w/Target"   },
        { TimeReference.REL_HIGHEST_AD,    "at cheapest AN/DN w/Target"  },
        { TimeReference.LIMITED_TIME,      "Limited Time"                },
        { TimeReference.PORKCHOP,          "Porkchop Selection"          },
        { TimeReference.NEXT_WINDOW,       "at the next transfer window" },
        { TimeReference.ASAP,              "as soon as possible"         }
    };

    // Inverse dictionary for Time references froom BurnTimeOption.selected
    public readonly static Dictionary<string, TimeReference> ValTimeRef = new()
    {
        { "",                            TimeReference.None              },
        { "at optimum time",             TimeReference.COMPUTED          },
        { "at next apoapsis",            TimeReference.APOAPSIS          },
        { "at next periapsis",           TimeReference.PERIAPSIS         },
        { "at closest approach",         TimeReference.CLOSEST_APPROACH  },
        { "at equatorial AN",            TimeReference.EQ_ASCENDING      },
        { "at equatorial DN",            TimeReference.EQ_DESCENDING     },
        { "at next AN with target",      TimeReference.REL_ASCENDING     },
        { "at next DN with target",      TimeReference.REL_DESCENDING    },
        { "after a fixed time",          TimeReference.X_FROM_NOW        },
        { "at an altitude",              TimeReference.ALTITUDE          },
        { "at nearest Eq. AN/DN",        TimeReference.EQ_NEAREST_AD     },
        { "at cheapest Eq. AN/DN",       TimeReference.EQ_HIGHEST_AD     },
        { "at nearest AN/DN w/Target",   TimeReference.REL_NEAREST_AD    },
        { "at cheapest AN/DN w/Target",  TimeReference.REL_HIGHEST_AD    },
        { "Limited Time",                TimeReference.LIMITED_TIME      },
        { "Porkchop Selection",          TimeReference.PORKCHOP          },
        { "at the next transfer window", TimeReference.NEXT_WINDOW       },
        { "as soon as possible",         TimeReference.ASAP              }
    };

    public List<TimeReference> Options = new List<TimeReference>();
    public static double RequestedBurnTime = 0;

    // Allow the user to pick an _option for the selected activity
    //public bool ListGUI()
    //{
    //  if (!isActive)
    //    return false;

    //  if (Options.Count == 0)
    //  {
    //    FlightPlanUI.TimeRef = TimeRef.None;
    //    isActive = false;
    //  }

    //  GUILayout.BeginHorizontal();
    //  UI_Tools.Label("Burn Time Option ");
    //  if (UI_Tools.SmallButton("Cancel"))
    //  {
    //    isActive = false;
    //  }
    //  GUILayout.EndHorizontal();

    //  UI_Tools.Separator();

    //  //GUI.SetNextControlName("Select Target");
    //  scrollPositionOptions = UI_Tools.BeginScrollView(scrollPositionOptions, 300);

    //  foreach (TimeRef _option in Options)
    //  {
    //    GUILayout.BeginHorizontal();
    //    if (UI_Tools.ListButton(TextTimeRef[_option]))
    //    {
    //      FlightPlanUI.TimeRef = _option;
    //      isActive = false;
    //    }
    //    GUILayout.EndHorizontal();
    //  }

    //  GUILayout.EndScrollView();

    //  return true;
    //}

    // Control display of the Option Picker UI
    //public void OptionSelectionGUI()
    //{
    //  GUILayout.BeginHorizontal();
    //  GUILayout.Label("Burn : ");


    //  if (UI_Tools.SmallButton(TextTimeRef[FlightPlanUI.TimeRef]))
    //    isActive = Options.Count > 0;

    //  GUILayout.EndHorizontal();
    //}

    // This method should be called after getting the BurnTimeOption.selected for desired maneuver time/effect
    public void SetBurnTime()
    {
        // Set the requested burn time based on the selected timing _option
        double UT = Game.UniverseModel.UniverseTime;
        FlightPlanPlugin Plugin = FlightPlanPlugin.Instance;
        PatchedConicsOrbit Orbit = Plugin._activeVessel.Orbit;

        SimulationObjectModel currentTarget = Plugin._currentTarget;
        PatchedConicsOrbit tgtOrbit = null;
        if (currentTarget != null)
        {
            if (currentTarget.IsPart)
            {
                tgtOrbit = currentTarget.Part.PartOwner.SimulationObject.Orbit as PatchedConicsOrbit;
            }
            else if (currentTarget.IsVessel || currentTarget.IsCelestialBody)
            {
                tgtOrbit = currentTarget.Orbit as PatchedConicsOrbit;
            }
        }

        switch (FpUiController.TimeRef)
        {
            case TimeReference.None:

                break;
            case TimeReference.COMPUTED:
                RequestedBurnTime = -1; // for optimal time the burn time is computed and returned from the OrbitalManeuverCalculator method called.
                break;
            case TimeReference.APOAPSIS:
                RequestedBurnTime = Orbit.NextApoapsisTime(UT);
                break;

            case TimeReference.PERIAPSIS:
                RequestedBurnTime = Orbit.NextPeriapsisTime(UT);
                break;
            case TimeReference.CLOSEST_APPROACH:
                if (tgtOrbit != null)
                    RequestedBurnTime = Orbit.NextClosestApproachTime(tgtOrbit, UT + 2); // +2 so that closestApproachTime is definitely > _UT
                else
                    FpUiController.TimeRef = TimeReference.None;
                break;
            case TimeReference.EQ_ASCENDING:
                RequestedBurnTime = Orbit.TimeOfAscendingNodeEquatorial(UT);
                break;
            case TimeReference.EQ_DESCENDING:
                RequestedBurnTime = Orbit.TimeOfDescendingNodeEquatorial(UT);
                break;
            case TimeReference.REL_ASCENDING:
                if (Plugin._currentTarget != null)
                    RequestedBurnTime = Orbit.TimeOfAscendingNode(tgtOrbit, UT); // like built in TimeOfAN(_currentTarget.Orbit, _UT), but with check to prevent time in the past
                else
                    FpUiController.TimeRef = TimeReference.None;
                break;
            case TimeReference.REL_DESCENDING:
                if (tgtOrbit != null)
                    RequestedBurnTime = Orbit.TimeOfDescendingNode(tgtOrbit, UT); // like built in TimeOfDN(_currentTarget.Orbit, _UT), but with check to prevent time in the past
                else
                    FpUiController.TimeRef = TimeReference.None;
                break;
            case TimeReference.X_FROM_NOW:
                RequestedBurnTime = UT + FpUiController.TimeOffset_s; // FPSettings.TimeOffset
                break;
            case TimeReference.ALTITUDE:
                RequestedBurnTime = Orbit.NextTimeOfRadius(UT, FpUiController.Altitude_km * 1000 + Orbit.referenceBody.radius); // FPSettings.Altitude_km.
                break;
            case TimeReference.EQ_NEAREST_AD:
                RequestedBurnTime = Math.Min(Orbit.TimeOfAscendingNodeEquatorial(UT), Orbit.TimeOfDescendingNodeEquatorial(UT));
                break;
            case TimeReference.EQ_HIGHEST_AD:
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
            case TimeReference.REL_NEAREST_AD:
                if (tgtOrbit != null)
                    RequestedBurnTime = Math.Min(Orbit.TimeOfAscendingNode(tgtOrbit, UT), Orbit.TimeOfDescendingNode(tgtOrbit, UT));
                else
                    FpUiController.TimeRef = TimeReference.None;
                break;
            case TimeReference.REL_HIGHEST_AD:
                {
                    if (tgtOrbit != null)
                    {
                        double _timeAN = Orbit.TimeOfAscendingNode(tgtOrbit, UT);
                        double _timeDN = Orbit.TimeOfDescendingNode(tgtOrbit, UT);
                        double _ascendingNodeRadius = Orbit.Radius(_timeAN);
                        double _descendingNodeRadius = Orbit.Radius(_timeDN);
                        if (_ascendingNodeRadius > _descendingNodeRadius)
                            RequestedBurnTime = _timeAN;
                        else
                            RequestedBurnTime = _timeDN;
                    }
                    else
                        FpUiController.TimeRef = TimeReference.None;
                }
                break;
            default:
                break;
        }
    }



    // This method sets up the Options list based on the selected activity. This method also configures the _toggles dictionary to record the setting of the "radio buttons"
    // for comparison to the _previousToggles dictionary.
    //public string SetOptionsList(ManeuverType type)
    //{
    //  Options.Clear();

    //  VesselComponent ActiveVessel = FlightPlanPlugin.Instance._activeVessel;

    //  string ManeuverTypeDesc = "";

    //  switch (type)
    //  {
    //    case ManeuverType.None:
    //      ManeuverTypeDesc = "None";
    //      break;
    //    case ManeuverType.circularize:
    //      if (ActiveVessel.Orbit.eccentricity < 1)
    //        Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
    //      Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
    //      Options.Add(TimeRef.ALTITUDE); //"At An Altittude"
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

    //      ManeuverTypeDesc = "Circularizing";
    //      break;
    //    case ManeuverType.newPe:
    //      if (ActiveVessel.Orbit.eccentricity < 1)
    //        Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
    //      Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
    //      Options.Add(TimeRef.ALTITUDE); //"At An Altittude"

    //      ManeuverTypeDesc = "Setting new Pe";
    //      break;
    //    case ManeuverType.newAp:
    //      Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
    //      if (ActiveVessel.Orbit.eccentricity < 1)
    //        Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
    //      Options.Add(TimeRef.ALTITUDE); //"At An Altittude"

    //      ManeuverTypeDesc = "Setting new Ap";
    //      break;
    //    case ManeuverType.newPeAp:
    //      if (ActiveVessel.Orbit.eccentricity < 1)
    //        Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
    //      Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"
    //      Options.Add(TimeRef.ALTITUDE); //"At An Altittude"
    //      Options.Add(TimeRef.EQ_ASCENDING); //"At Equatorial AN"
    //      Options.Add(TimeRef.EQ_DESCENDING); //"At Equatorial DN"

    //      ManeuverTypeDesc = "Elipticizing";
    //      break;
    //    case ManeuverType.newInc:
    //      Options.Add(TimeRef.EQ_HIGHEST_AD); //"At Cheapest eq AN/DN"
    //      Options.Add(TimeRef.EQ_NEAREST_AD); //"At Nearest eq AN/DN"
    //      Options.Add(TimeRef.EQ_ASCENDING); //"At Equatorial AN"
    //      Options.Add(TimeRef.EQ_DESCENDING); //"At Equatorial DN"
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

    //      ManeuverTypeDesc = "Setting new inclination";
    //      break;
    //    case ManeuverType.newLAN:
    //      if (ActiveVessel.Orbit.eccentricity < 1)
    //        Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
    //      Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

    //      ManeuverTypeDesc = "Setting new LAN";
    //      break;
    //    case ManeuverType.newNodeLon:
    //      if (ActiveVessel.Orbit.eccentricity < 1)
    //        Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
    //      Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

    //      ManeuverTypeDesc = "Shifting Node LongitudeN";
    //      break;
    //    case ManeuverType.newSMA:
    //      if (ActiveVessel.Orbit.eccentricity < 1)
    //        Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"
    //      Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

    //      ManeuverTypeDesc = "Setting new SMA";
    //      break;
    //    case ManeuverType.matchPlane:
    //      Options.Add(TimeRef.REL_HIGHEST_AD); //"At Cheapest AN/DN With Target"
    //      Options.Add(TimeRef.REL_NEAREST_AD); //"At Nearest AN/DN With Target"
    //      Options.Add(TimeRef.REL_ASCENDING); //"At Next AN With Target"
    //      Options.Add(TimeRef.REL_DESCENDING); //"At Next DN With Target"

    //      ManeuverTypeDesc = "Matching planes";
    //      break;
    //    case ManeuverType.hohmannXfer:
    //      Options.Add(TimeRef.COMPUTED); //"At Optimal Time"

    //      ManeuverTypeDesc = "Performing Hohmann transfer";
    //      break;
    //    case ManeuverType.courseCorrection:
    //      Options.Add(TimeRef.COMPUTED); //"At Optimal Time"

    //      ManeuverTypeDesc = "Performaing course correction";
    //      break;
    //    case ManeuverType.interceptTgt:
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

    //      ManeuverTypeDesc = "Intercepting";
    //      break;
    //    case ManeuverType.matchVelocity:
    //      Options.Add(TimeRef.CLOSEST_APPROACH); //"At Closest Approach"
    //      Options.Add(TimeRef.X_FROM_NOW); //"After Fixed Time"

    //      ManeuverTypeDesc = "Matching velocity";
    //      break;
    //    case ManeuverType.moonReturn:
    //      Options.Add(TimeRef.COMPUTED); //"At Optimal Time"

    //      ManeuverTypeDesc = "Performaing moon return";
    //      break;
    //    case ManeuverType.planetaryXfer:
    //      Options.Add(TimeRef.COMPUTED); //"At Optimal Time"

    //      ManeuverTypeDesc = "Performing planetary transfer";
    //      break;
    //    case ManeuverType.advancedPlanetaryXfer:
    //      Options.Add(TimeRef.PORKCHOP); //"Porkchop Selection"
    //      Options.Add(TimeRef.LIMITED_TIME); //"Limited Time"

    //      ManeuverTypeDesc = "Performing advanced planetary transfer";
    //      break;
    //    case ManeuverType.fixAp:
    //      Options.Add(TimeRef.PERIAPSIS); //"At Next Periapsis"

    //      ManeuverTypeDesc = "Setting new Ap";
    //      break;
    //    case ManeuverType.fixPe:
    //      Options.Add(TimeRef.APOAPSIS); //"At Next Apoapsis"

    //      ManeuverTypeDesc = "Setting new Pe";
    //      break;
    //    default:
    //      break;
    //  }

    //  if (Options.Count < 1)
    //    Options.Add(TimeRef.None);

    //  if (!Options.Contains(FlightPlanUI.TimeRef))
    //    FlightPlanUI.TimeRef = Options[0];

    //  return ManeuverTypeDesc;
    //}


    public static string TimeRefDesc
    {
        get
        {
            return TextTimeRef[FpUiController.TimeRef];
        }
    }
}
