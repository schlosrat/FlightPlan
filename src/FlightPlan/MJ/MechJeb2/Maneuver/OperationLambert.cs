using System.Collections.Generic;
using FlightPlan;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Networking.OnlineServices.Authentication.Models;
using KSP.Sim.impl;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationLambert : Operation
    {
        private static readonly string _name = "intercept target at chosen time"; // Localizer.Format("#MechJeb_intercept_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public EditableTime InterceptInterval = 3600;
        private double InterceptInterval = 3600;

        private static readonly TimeReference[] _timeReferences = { TimeReference.X_FROM_NOW };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            // GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_intercept_label"), InterceptInterval); //Time after burn to intercept target:
            InterceptInterval = FpUiController.TimeOffset_s; // FIX ME? Is this the right parameter to pull in?
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            if (target is null) // was: !target.NormalTargetExists
                throw new OperationException("must select a target to intercept."); // Localizer.Format("#MechJeb_intercept_Exception1")); //must select a target to intercept.
            if (o.referenceBody != target.Orbit.referenceBody)
                throw new OperationException("target must be in the same sphere of influence."); // Localizer.Format("#MechJeb_intercept_Exception2")); //target must be in the same sphere of influence.

            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            (Vector3d dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(o, ut, target.Orbit, ut + InterceptInterval);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
