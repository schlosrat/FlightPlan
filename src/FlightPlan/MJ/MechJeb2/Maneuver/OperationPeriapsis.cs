using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using static MechJebLib.Utils.Statics;
using FlightPlan;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationPeriapsis : Operation
    {
        private static readonly string _name = "change periapsis"; // Localizer.Format("#MechJeb_Pe_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public readonly EditableDoubleMult NewPeA = new EditableDoubleMult(100000, 1000);
        private double NewPeA;

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            // GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Pe_label"), NewPeA, "km"); //New periapsis:
            // _timeSelector.DoChooseTimeGUI();
            NewPeA = FpUiController.TargetPeR_m; // FIX ME!
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (NewPeA < -o.referenceBody.radius)
            {
                throw new OperationException($"new periapsis cannot be lower than minus the radius of {o.referenceBody.DisplayName.LocalizeRemoveGender()}");
                    // Localizer.Format("#MechJeb_Pe_Exception2", o.referenceBody.DisplayName.LocalizeRemoveGender()) + "(-" +
                    // o.referenceBody.radius.ToSI(3) + "m)"); //new periapsis cannot be lower than minus the radius of <<1>>
            }

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangePeriapsis(o, ut, NewPeA + o.referenceBody.radius), ut)
            };
        }
    }
}
