using System.Collections.Generic;
using FlightPlan;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationLongitude : Operation
    {
        private static readonly string _name = "change surface longitude of apsis"; // Localizer.Format("#MechJeb_la_title");
        public override         string GetName() => _name;

        private double targetLongitude = 0.0;

        private static readonly TimeReference[] _timeReferences = { TimeReference.APOAPSIS, TimeReference.PERIAPSIS };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            _timeSelector.DoChooseTimeGUI();
            // GUILayout.Label(Localizer.Format("#MechJeb_la_label")); //New Surface Longitude after one orbit:
            // target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);
            
            // FIX ME! There is not input/control for surface longitude of apsis
            // targetLongitude = FpUiController.TargetLongitude_deg;
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            Vector3d dV = OrbitalManeuverCalculator.DeltaVToShiftNodeLongitude(o, ut, targetLongitude);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
