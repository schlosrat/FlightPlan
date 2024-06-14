using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using UnityEngine;
using FlightPlan;
using FPUtilities;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationGeneric : Operation
    {
    	private static readonly string _name = "bi-impulsive (Hohmann) transfer to target"; // Localizer.Format("#MechJeb_Hohm_title"); } //bi-impulsive (Hohmann) transfer to target
        public override         string GetName()  => _name;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Capture = true;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        public bool PlanCapture = true;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Rendezvous = true;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public EditableDouble LagTime = 0;
        public double LagTime = 0;

        // [Persistent(pass = (int)Pass.Global)]
        // public EditableTime MinDepartureUT = 0;
        private double MinDepartureUT = 0;

        // [Persistent(pass = (int)Pass.Global)]
        // public EditableTime MaxDepartureUT = 0;
        private double MaxDepartureUT = 0;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Coplanar;

        [UsedImplicitly]
        public bool InterceptOnly;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.Global)]
        // public EditableDouble PeriodOffset = 0;
        private double PeriodOffset = 0;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.Global)]
        public bool SimpleTransfer;

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.COMPUTED, TimeReference.PERIAPSIS, TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE,
            TimeReference.EQ_DESCENDING, TimeReference.EQ_ASCENDING, TimeReference.REL_NEAREST_AD, TimeReference.REL_ASCENDING,
            TimeReference.REL_DESCENDING, TimeReference.CLOSEST_APPROACH
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);
        
        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            Capture = false;
            // !GUILayout.Toggle(!Capture, Localizer.Format("#MechJeb_Hohm_intercept_only")); //no capture burn (impact/flyby)
            if (Capture)
                PlanCapture = true; // GUILayout.Toggle(PlanCapture, "Plan insertion burn");
            Coplanar = true; //  GUILayout.Toggle(Coplanar, Localizer.Format("#MechJeb_Hohm_simpleTransfer")); //coplanar maneuver
            // GUILayout.BeginHorizontal();
            // if (GUILayout.Toggle(Rendezvous, "Rendezvous"))
                Rendezvous = true;
            // if (GUILayout.Toggle(!Rendezvous, "Transfer"))
                Rendezvous = false;
            // GUILayout.EndHorizontal();
            if (Rendezvous)
                // GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Hohm_Label1"), LagTime, "sec"); //fractional target period offset
                LagTime = 0.0;
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            double ut;

            if (target is null) // was: !target.NormalTargetExists
                throw new OperationException("must select a target for the bi-impulsive transfer."); // Localizer.Format("#MechJeb_Hohm_Exception1")); //must select a target for the bi-impulsive transfer.

            if (o.referenceBody != target.Orbit.referenceBody)
                throw
                    new OperationException("target for bi-impulsive transfer must be in the same sphere of influence.");
                        // Localizer.Format("#MechJeb_Hohm_Exception2")); //target for bi-impulsive transfer must be in the same sphere of influence.

            if (target is CelestialBodyComponent && Capture && PlanCapture)
                ErrorMessage =
                    "Insertion burn to a celestial with an SOI is not supported by this maneuver.  A Transfer-to-Moon maneuver needs to be written to properly support this case.";

            Vector3d dV;

            PatchedConicsOrbit targetOrbit = target.Orbit;

            double lagTime = Rendezvous ? LagTime : 0;

            bool fixedTime = false;

            if (_timeSelector.TimeReference != TimeReference.COMPUTED)
            {
                bool anExists = o.AscendingNodeExists(target.Orbit);
                bool dnExists = o.DescendingNodeExists(target.Orbit);

                if (_timeSelector.TimeReference == TimeReference.REL_ASCENDING && !anExists)
                    throw new OperationException("ascending node with target doesn't exist."); // Localizer.Format("#MechJeb_Hohm_Exception3")); //ascending node with target doesn't exist.

                if (_timeSelector.TimeReference == TimeReference.REL_DESCENDING && !dnExists)
                    throw new OperationException("descending node with target doesn't exist."); // Localizer.Format("#MechJeb_Hohm_Exception4")); //descending node with target doesn't exist.

                if (_timeSelector.TimeReference == TimeReference.REL_NEAREST_AD && !(anExists || dnExists))
                    throw new OperationException("neither ascending nor descending node with target exists.");
                        // Localizer.Format("#MechJeb_Hohm_Exception5")); //neither ascending nor descending node with target exists.

                universalTime = _timeSelector.ComputeManeuverTime(o, universalTime, target);
                fixedTime     = true;
            }

            FlightPlanPlugin.Logger.LogDebug($"OperationGeneric.MakeNodesImpl: Calling DeltaVAndTimeForHohmannTransfer");
            FlightPlanPlugin.Logger.LogDebug($"OperationGeneric.MakeNodesImpl: UT = {FPUtility.SecondsToTimeString(universalTime)}, lagTime = {lagTime}, fixedTime = {fixedTime}, Coplanar = {Coplanar}, Rendezvous = {Rendezvous}, Capture = {Capture}");
            (Vector3d dV1, double ut1, Vector3d dV2, double ut2) =
                OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, targetOrbit, universalTime, lagTime, fixedTime, Coplanar, Rendezvous,
                    Capture);

            if (Capture && PlanCapture)
                return new List<ManeuverParameters> { new ManeuverParameters(dV1, ut1), new ManeuverParameters(dV2, ut2) };
            return new List<ManeuverParameters> { new ManeuverParameters(dV1, ut1) };
        }
    }
}
