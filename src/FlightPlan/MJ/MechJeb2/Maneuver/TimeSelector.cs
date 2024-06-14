using System;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using UnityEngine;

namespace MuMech
{
    public enum TimeReference
    {
        COMPUTED, X_FROM_NOW, APOAPSIS, PERIAPSIS, ALTITUDE, EQ_ASCENDING, EQ_DESCENDING,
        REL_ASCENDING, REL_DESCENDING, CLOSEST_APPROACH,
        EQ_HIGHEST_AD, EQ_NEAREST_AD, REL_HIGHEST_AD, REL_NEAREST_AD
    }

    public class TimeSelector
    {
        private readonly string[] _timeRefNames;

        private double _universalTime;

        private readonly TimeReference[] _allowedTimeRef;

        // [Persistent(pass = (int)Pass.GLOBAL)]
        public int _currentTimeRef;

        public TimeReference TimeReference => _allowedTimeRef[_currentTimeRef];

        // Input parameters
        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.Global)]
        // public readonly EditableTime LeadTime = 0;
        public double LeadTime = 0;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.Global)]
        // public readonly EditableDoubleMult CircularizeAltitude = new EditableDoubleMult(150000, 1000);
        public double CircularizeAltitude;

        //"Warning: orbit is hyperbolic, so apoapsis doesn't exist."
        private static readonly string _maneuverException1 = "Warning: orbit is hyperbolic, so apoapsis doesn't exist."; // Localizer.Format("#MechJeb_Maneu_Exception1");

        //"Warning: no target selected."
        private static readonly string _maneuverException2 = "Warning: no target selected."; // Localizer.Format("#MechJeb_Maneu_Exception2");

        //"Warning: can't circularize at this altitude, since current orbit does not reach it."
        private static readonly string _maneuverException3 = "Warning: can't circularize at this altitude, since current orbit does not reach it."; // Localizer.Format("#MechJeb_Maneu_Exception3");

        //"Warning: equatorial ascending node doesn't exist."
        private static readonly string _maneuverException4 = "Warning: equatorial ascending node doesn't exist."; // Localizer.Format("#MechJeb_Maneu_Exception4");

        //"Warning: equatorial descending node doesn't exist."
        private static readonly string _maneuverException5 = "Warning: equatorial descending node doesn't exist."; // Localizer.Format("#MechJeb_Maneu_Exception5");

        //Warning: neither ascending nor descending node exists.
        private static readonly string _maneuverException6 = "Warning: neither ascending nor descending node exists."; // Localizer.Format("#MechJeb_Maneu_Exception6");

        //"Warning: neither ascending nor descending node exists."
        private static readonly string _maneuverException7 = "Warning: neither ascending nor descending node exists."; // Localizer.Format("#MechJeb_Maneu_Exception7");

        public TimeSelector(TimeReference[] allowedTimeRef)
        {
            this._allowedTimeRef = allowedTimeRef;
            _universalTime      = 0;
            _timeRefNames       = new string[allowedTimeRef.Length];
            for (int i = 0; i < allowedTimeRef.Length; ++i)
            {
                _timeRefNames[i] = allowedTimeRef[i] switch
                {
                    TimeReference.COMPUTED         => "at the optimum time", // Localizer.Format("#MechJeb_Maneu_TimeSelect1"),
                    TimeReference.APOAPSIS         => "at the next apoapsis", // Localizer.Format("#MechJeb_Maneu_TimeSelect2"),
                    TimeReference.CLOSEST_APPROACH => "at closest approach to target", // Localizer.Format("#MechJeb_Maneu_TimeSelect3"),
                    TimeReference.EQ_ASCENDING     => "at the equatorial AN", // Localizer.Format("#MechJeb_Maneu_TimeSelect4"),
                    TimeReference.EQ_DESCENDING    => "at the equatorial DN", // Localizer.Format("#MechJeb_Maneu_TimeSelect5"),
                    TimeReference.PERIAPSIS        => "at the next periapsis", // Localizer.Format("#MechJeb_Maneu_TimeSelect6"),
                    TimeReference.REL_ASCENDING    => "at the next AN with the target", // Localizer.Format("#MechJeb_Maneu_TimeSelect7"),
                    TimeReference.REL_DESCENDING   => "at the next DN with the target", // Localizer.Format("#MechJeb_Maneu_TimeSelect8"),
                    TimeReference.X_FROM_NOW       => "after a fixed time", // Localizer.Format("#MechJeb_Maneu_TimeSelect9"),
                    TimeReference.ALTITUDE         => "at an altitude", // Localizer.Format("#MechJeb_Maneu_TimeSelect10"),
                    TimeReference.EQ_NEAREST_AD    => "at the nearest equatorial AN/DN", // Localizer.Format("#MechJeb_Maneu_TimeSelect11"),
                    TimeReference.EQ_HIGHEST_AD    => "at the cheapest equatorial AN/DN", // Localizer.Format("#MechJeb_Maneu_TimeSelect12"),
                    TimeReference.REL_NEAREST_AD   => "at the nearest AN/DN with the target", // Localizer.Format("#MechJeb_Maneu_TimeSelect13"),
                    TimeReference.REL_HIGHEST_AD   => "at the cheapest AN/DN with the target", // Localizer.Format("#MechJeb_Maneu_TimeSelect14"),
                    _                              => _timeRefNames[i]
                };
            }
        }

        public void DoChooseTimeGUI()
        {
            // GUILayout.Label(Localizer.Format("#MechJeb_Maneu_STB")); //Schedule the burn
            // GUILayout.BeginHorizontal();
            // _currentTimeRef = GuiUtils.ComboBox.Box(_currentTimeRef, _timeRefNames, this);
            switch (TimeReference)
            {
                case TimeReference.X_FROM_NOW:
                    // GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_of"), LeadTime); //"of"
                    break;
                case TimeReference.ALTITUDE:
                    // GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_of"), CircularizeAltitude, "km"); //"of"
                    break;
            }

            GUILayout.EndHorizontal();
        }

        public double ComputeManeuverTime(PatchedConicsOrbit o, double ut, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            switch (_allowedTimeRef[_currentTimeRef])
            {
                case TimeReference.X_FROM_NOW:
                    ut += LeadTime;
                    break;

                case TimeReference.APOAPSIS:
                    if (!(o.eccentricity < 1))
                        throw new OperationException(_maneuverException1);
                    ut = o.NextApoapsisTime(ut);
                    break;

                case TimeReference.PERIAPSIS:
                    ut = o.NextPeriapsisTime(ut);
                    break;

                case TimeReference.CLOSEST_APPROACH:
                    if (target is null) // was: !target.NormalTargetExists
                        throw new OperationException(_maneuverException2);
                    ut = o.NextClosestApproachTime(target.Orbit, ut);
                    break;

                case TimeReference.ALTITUDE:
                    if (!(CircularizeAltitude > o.Periapsis) || (!(CircularizeAltitude < o.Apoapsis) && !(o.eccentricity >= 1)))
                        throw new OperationException(_maneuverException3);
                    ut = o.NextTimeOfRadius(ut, o.referenceBody.radius + CircularizeAltitude);
                    break;

                case TimeReference.EQ_ASCENDING:
                    if (!o.AscendingNodeEquatorialExists())
                        throw new OperationException(_maneuverException4);
                    ut = o.TimeOfAscendingNodeEquatorial(ut);
                    break;

                case TimeReference.EQ_DESCENDING:
                    if (!o.DescendingNodeEquatorialExists())
                        throw new OperationException(_maneuverException5);
                    ut = o.TimeOfDescendingNodeEquatorial(ut);
                    break;

                case TimeReference.EQ_NEAREST_AD:
                    if (o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists())
                        ut = Math.Min(o.TimeOfAscendingNodeEquatorial(ut), o.TimeOfDescendingNodeEquatorial(ut));
                    else if (o.AscendingNodeEquatorialExists())
                        ut = o.TimeOfAscendingNodeEquatorial(ut);
                    else if (o.DescendingNodeEquatorialExists())
                        ut = o.TimeOfDescendingNodeEquatorial(ut);
                    else
                        throw new OperationException(_maneuverException6);
                    break;

                case TimeReference.EQ_HIGHEST_AD:
                    if (o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists())
                        {
                            double anTime = o.TimeOfAscendingNodeEquatorial(ut);
                            double dnTime = o.TimeOfDescendingNodeEquatorial(ut);
                            ut = o.GetOrbitalVelocityAtUTZup (anTime).magnitude <= o.GetOrbitalVelocityAtUTZup(dnTime).magnitude
                                ? anTime
                                : dnTime;
                        }
                    else if (o.AscendingNodeEquatorialExists())
                            ut = o.TimeOfAscendingNodeEquatorial(ut);
                    else if (o.DescendingNodeEquatorialExists())
                        ut = o.TimeOfDescendingNodeEquatorial(ut);
                    else
                        throw new OperationException(_maneuverException7);

                    break;
            }

            _universalTime = ut;
            return _universalTime;
        }
    }
}
