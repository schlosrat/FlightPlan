using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using FlightPlan;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationCircularize : Operation
    {
        private static readonly string _name = "circularize"; // Localizer.Format("#MechJeb_Maneu_circularize_title");
        public override         string GetName() => _name;

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) => _timeSelector.DoChooseTimeGUI(); // was: MechJebModuleTargetController target

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            Vector3d deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(o, ut);

            return new List<ManeuverParameters> { new ManeuverParameters(deltaV, ut) };
        }
    }
}
