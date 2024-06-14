using FlightPlan;
using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using UnityEngine;
using FPUtilities;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationMoonReturn : Operation
    {
        private static readonly string _name = "return from a moon"; // Localizer.Format("#MechJeb_return_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public EditableDoubleMult MoonReturnAltitude = new EditableDoubleMult(100000, 1000);
        double MoonReturnAltitude;

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            // GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_return_label1"), MoonReturnAltitude, "km"); //Approximate final periapsis:
            // GUILayout.Label(Localizer.Format("#MechJeb_return_label2")); //Schedule the burn at the next return window.
            MoonReturnAltitude = FpUiController.TargetMRPeR_m;
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            if (o.referenceBody.referenceBody == null)
            {
                throw new OperationException($"{o.referenceBody.DisplayName.LocalizeRemoveGender()} is not orbiting another body you could return to."); // Localizer.Format("#MechJeb_return_Exception",
                    // o.referenceBody.DisplayName.LocalizeRemoveGender())); //<<1>> is not orbiting another body you could return to.
            }

            // fixed 30 second delay for hyperbolic orbits (this doesn't work for elliptical and i don't want to deal
            // with requests to "fix" it by surfacing it as a tweakable).
            double t0 = o.eccentricity >= 1 ? universalTime + 30 : universalTime;

            Debug.Log($"OperationMoonReturn.MakeNodesImpl: UT = {FPUtility.SecondsToTimeString(universalTime)}, MoonReturnAltitude = {MoonReturnAltitude}");
            (Vector3d dV, double ut) = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(o, t0,
                o.referenceBody.referenceBody.radius + MoonReturnAltitude);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
