using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using UnityEngine;
using FlightPlan;
using SpaceWarp.Modules;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationInterplanetaryTransfer : Operation
    {
        private static readonly string _name = "transfer to another planet"; // Localizer.Format("#MechJeb_transfer_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        // [Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        public bool WaitForPhaseAngle = true;

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            // GUILayout.Label(Localizer.Format("#MechJeb_transfer_Label1"));                                           //Schedule the burn:
            // WaitForPhaseAngle = GUILayout.Toggle(WaitForPhaseAngle, Localizer.Format("#MechJeb_transfer_Label2"));   //at the next transfer window.
            // WaitForPhaseAngle = !GUILayout.Toggle(!WaitForPhaseAngle, Localizer.Format("#MechJeb_transfer_Label3")); //as soon as possible
            // FIX ME!
            // Need to ask user if they want to wait for the next transfer window or do it as soon as possible
            if (!WaitForPhaseAngle)
            {
                // GUILayout.Label("Using this mode voids your warranty"); // Localizer.Format("#MechJeb_transfer_Label4"), GuiUtils.yellowLabel); //Using this mode voids your warranty
            }
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double UT, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            // Check preconditions
            if (target is null) // was: !target.NormalTargetExists
                throw new OperationException("must select a target for the interplanetary transfer.");
            // Localizer.Format("#MechJeb_transfer_Exception1")); //"must select a target for the interplanetary transfer."

            if (o.referenceBody.referenceBody == null)
                throw new OperationException($"doesn't make sense to plot an interplanetary transfer from an orbit around {o.referenceBody.DisplayName.LocalizeRemoveGender()}");
                    // Localizer.Format("#MechJeb_transfer_Exception2",
                    // o.referenceBody.DisplayName.LocalizeRemoveGender())); //doesn't make sense to plot an interplanetary transfer from an orbit around <<1>>

            if (o.referenceBody.referenceBody != target.Orbit.referenceBody)
            {
                if (o.referenceBody == target.Orbit.referenceBody)
                    throw new OperationException($"use regular Hohmann transfer function to intercept another body orbiting {o.referenceBody.DisplayName.LocalizeRemoveGender()}");
                // Localizer.Format("#MechJeb_transfer_Exception3",
                // o.referenceBody.DisplayName.LocalizeRemoveGender())); //use regular Hohmann transfer function to intercept another body orbiting <<1>>
                throw new OperationException($"an interplanetary transfer from within {o.referenceBody.DisplayName.LocalizeRemoveGender()}'s sphere of influence must target a body that orbits {o.referenceBody.DisplayName.LocalizeRemoveGender()}'s parent, {o.referenceBody.referenceBody.DisplayName.LocalizeRemoveGender()}"); 
                    // Localizer.Format("#MechJeb_transfer_Exception4", o.referenceBody.DisplayName.LocalizeRemoveGender(),
                    // o.referenceBody.DisplayName.LocalizeRemoveGender(),
                    // o.referenceBody.referenceBody.DisplayName.LocalizeRemoveGender())); //"an interplanetary transfer from within "<<1>>"'s sphere of influence must target a body that orbits "<<2>>"'s parent, "<<3>>.
            }

            // Simple warnings
            if (o.referenceBody.Orbit.RelativeInclination(target.Orbit) > 30)
            {
                ErrorMessage = $"Warning: target's orbital plane is at a {o.RelativeInclination(target.Orbit).ToString("F0")}º angle to {o.referenceBody.DisplayName.LocalizeRemoveGender()}'s orbital plane (recommend at most 30º). Planned interplanetary transfer may not intercept target properly.";
                    // Localizer.Format("#MechJeb_transfer_errormsg1", o.RelativeInclination(target.TargetOrbit).ToString("F0"),
                    // o.referenceBody.DisplayName.LocalizeRemoveGender()); //"Warning: target's orbital plane is at a"<<1>>"º angle to "<<2>>"'s orbital plane (recommend at most 30º). Planned interplanetary transfer may not intercept target properly."
            }
            else
            {
                double relativeInclination = Vector3d.Angle(o.OrbitNormal(), o.referenceBody.Orbit.OrbitNormal());
                if (relativeInclination > 10)
                {
                    ErrorMessage = $"Warning: Recommend starting interplanetary transfers from {o.referenceBody.DisplayName.LocalizeRemoveGender()} from an orbit in the same plane as " +
                        $"{o.referenceBody.DisplayName.LocalizeRemoveGender()}'s orbit around {o.referenceBody.referenceBody.DisplayName.LocalizeRemoveGender()}. " +
                        $"Starting orbit around {o.referenceBody.DisplayName.LocalizeRemoveGender()} is inclined {relativeInclination.ToString("F1")}º with respect to " +
                        $"{o.referenceBody.DisplayName.LocalizeRemoveGender()}'s orbit around {o.referenceBody.referenceBody.DisplayName.LocalizeRemoveGender()} (recommend < 10º). " +
                        "Planned transfer may not intercept target properly.";
                        // Localizer.Format("#MechJeb_transfer_errormsg2", o.referenceBody.DisplayName.LocalizeRemoveGender(),
                        // o.referenceBody.DisplayName.LocalizeRemoveGender(), o.referenceBody.referenceBody.DisplayName.LocalizeRemoveGender(),
                        // o.referenceBody.DisplayName.LocalizeRemoveGender(), relativeInclination.ToString("F1"),
                        // o.referenceBody.DisplayName.LocalizeRemoveGender(),
                        // o.referenceBody.referenceBody.DisplayName.LocalizeRemoveGender()); //Warning: Recommend starting interplanetary transfers from  <<1>> from an orbit in the same plane as "<<2>>"'s orbit around "<<3>>". Starting orbit around "<<4>>" is inclined "<<5>>"º with respect to "<<6>>"'s orbit around "<<7>> " (recommend < 10º). Planned transfer may not intercept target properly."
                }
                else if (o.eccentricity > 0.2)
                {
                    ErrorMessage = $"Warning: Recommend starting interplanetary transfers from a near-circular orbit (eccentricity < 0.2). Planned transfer is starting from an orbit with eccentricity {o.eccentricity.ToString("F2")} and so may not intercept target properly.";
                        // Localizer.Format("#MechJeb_transfer_errormsg3",
                        // o.eccentricity.ToString("F2")); //Warning: Recommend starting interplanetary transfers from a near-circular orbit (eccentricity < 0.2). Planned transfer is starting from an orbit with eccentricity <<1>> and so may not intercept target properly.
                }
            }

            Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(o, UT, target.Orbit, WaitForPhaseAngle,
                out UT);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, UT) };
        }
    }
}
