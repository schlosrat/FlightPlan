﻿using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationKillRelVel : Operation
    {
        private static readonly string _name = "match velocities with target"; // Localizer.Format("#MechJeb_match_v_title");
        public override         string GetName() => _name;

        private static readonly TimeReference[] _timeReferences = { TimeReference.CLOSEST_APPROACH, TimeReference.X_FROM_NOW };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) => _timeSelector.DoChooseTimeGUI(); // was: MechJebModuleTargetController target

        // FIX ME!
        // This manuevuer doesn't make sense unless the target is a vessel. We will need something akin to MechJebModuleTargetController so that we can
        // have different types of targets in a single common structure for this to work.
        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            if (target is null) // was: !target.NormalTargetExists
                throw new OperationException("must select a target to match velocities with."); // Localizer.Format("#MechJeb_match_v_Exception1")); //must select a target to match velocities with.
            if (o.referenceBody != target.Orbit.referenceBody)
                throw new OperationException("target must be in the same sphere of influence."); // Localizer.Format("#MechJeb_match_v_Exception2")); //target must be in the same sphere of influence.

            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(o, ut, target.Orbit);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
