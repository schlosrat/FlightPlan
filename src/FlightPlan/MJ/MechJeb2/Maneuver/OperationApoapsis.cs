using System.Collections.Generic;
using FlightPlan;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationApoapsis : Operation
    {
        private static readonly string _name = "change apoapsis"; // Localizer.Format("#MechJeb_Ap_title");
        public override         string GetName() => _name;
        
        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public readonly EditableDoubleMult NewApA = new EditableDoubleMult(200000, 1000);
        private double NewApA;

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.PERIAPSIS, TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE, TimeReference.EQ_DESCENDING,
            TimeReference.EQ_ASCENDING
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            // GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ap_label1"), NewApA, "km"); //New apoapsis:
            // _timeSelector.DoChooseTimeGUI();
            NewApA = FpUiController.TargetApR_m;
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            Vector3d deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(o, ut, NewApA + o.referenceBody.radius);

            return new List<ManeuverParameters> { new ManeuverParameters(deltaV, ut) };
        }
    }
}
