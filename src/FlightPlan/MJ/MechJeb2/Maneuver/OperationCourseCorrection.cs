using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using UnityEngine;
using FlightPlan;


namespace MuMech
{
    [UsedImplicitly]
    public class OperationCourseCorrection : Operation
    {
        private static readonly string _name = "fine tune closest approach to target"; // Localizer.Format("#MechJeb_approach_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public EditableDoubleMult CourseCorrectFinalPeA = new EditableDoubleMult(200000, 1000);
        private double CourseCorrectFinalPeA;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public EditableDoubleMult InterceptDistance = new EditableDoubleMult(200);
        private double InterceptDistance;

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            // if (target.Target is CelestialBody)
            //     GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_approach_label1"), CourseCorrectFinalPeA, "km"); //Approximate final periapsis
            // else
            //     GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_approach_label2"), InterceptDistance, "m"); //Closest approach distance
            // GUILayout.Label(Localizer.Format("#MechJeb_approach_label3")); //Schedule the burn to minimize the required ΔV.
            CourseCorrectFinalPeA = FpUiController.TargetPeR_m; // FIX ME!
            InterceptDistance = FpUiController.TargetPeR_m; // FIX ME!
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double ut, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            if (target is null) // was !target.NormalTargetExists
                throw new OperationException("must select a target for the course correction.");
                // Localizer.Format("#MechJeb_approach_Exception1")); //must select a target for the course correction.

            PatchedConicsOrbit correctionPatch = o;
            while (correctionPatch != null)
            {
                if (correctionPatch.referenceBody == target.Orbit.referenceBody) // was: target.TargetOrbit.referenceBody
                {
                    o  = correctionPatch;
                    ut = correctionPatch.StartUT;
                    break;
                }

                // FIX ME! WTF is this supposed to do?
                // correctionPatch = target.Core.vessel.GetNextPatch(correctionPatch);
            }

            if (correctionPatch == null || correctionPatch.referenceBody != target.Orbit.referenceBody) // was: target.TargetOrbit.referenceBody
                throw
                    new OperationException("target for course correction must be in the same sphere of influence");
                        // Localizer.Format("#MechJeb_approach_Exception2")); //"target for course correction must be in the same sphere of influence"

            if (o.NextClosestApproachTime(target.Orbit, ut) < ut + 1 ||
                o.NextClosestApproachDistance(target.Orbit, ut) > target.Orbit.semiMajorAxis * 0.2)
            {
                ErrorMessage = "Warning: orbit before course correction doesn't seem to approach target very closely. Planned course correction may be extreme. Recommend plotting an approximate intercept orbit and then plotting a course correction."; // Localizer.Format(
                    // "#MechJeb_Approach_errormsg"); //Warning: orbit before course correction doesn't seem to approach target very closely. Planned course correction may be extreme. Recommend plotting an approximate intercept orbit and then plotting a course correction.
            }

            var targetBody = target as CelestialBodyComponent;
            Vector3d dV = targetBody != null
                ? OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, ut, target.Orbit, targetBody,
                    targetBody.radius + CourseCorrectFinalPeA, out ut)
                : OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, ut, target.Orbit, InterceptDistance, out ut);


            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
