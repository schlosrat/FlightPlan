using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using static MechJebLib.Utils.Statics;
using FlightPlan;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationEllipticize : Operation
    {
        private static readonly string _name = "change both Pe and Ap"; // Localizer.Format("#MechJeb_both_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public EditableDoubleMult NewApA = new EditableDoubleMult(200000, 1000);
        private double NewApA;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public EditableDoubleMult NewPeA = new EditableDoubleMult(100000, 1000);
        private double NewPeA;

        private static readonly TimeReference[] _timeReferences = { TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            // GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_both_label1"), NewPeA, "km"); //New periapsis:
            // GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_both_label2"), NewApA, "km"); //New apoapsis:
            // _timeSelector.DoChooseTimeGUI();
            NewApA = FpUiController.TargetApR_m; // FIX ME!
            NewPeA = FpUiController.TargetPeR_m; // FIX ME!
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            string burnAltitude = (o.Radius(ut) - o.referenceBody.radius).ToSI() + "m";
            if (o.referenceBody.radius + NewPeA > o.Radius(ut))
            {
                throw new OperationException($"new periapsis cannot be higher than the altitude of the burn {burnAltitude}"); //  Localizer.Format("#MechJeb_both_Exception1",
                    // burnAltitude)); //new periapsis cannot be higher than the altitude of the burn (<<1>>)
            }

            if (o.referenceBody.radius + NewApA < o.Radius(ut))
            {
                throw new OperationException($"new apoapsis cannot be lower than the altitude of the burn {burnAltitude}"); // Localizer.Format("#MechJeb_both_Exception2") + "(" + burnAltitude +
                                             // ")"); //new apoapsis cannot be lower than the altitude of the burn
            }

            if (NewPeA < -o.referenceBody.radius)
            {
                throw new OperationException($"new periapsis cannot be lower than minus the radius of {o.referenceBody.DisplayName.LocalizeRemoveGender()}");
                                             // Localizer.Format("#MechJeb_both_Exception3", o.referenceBody.DisplayName.LocalizeRemoveGender()) + "(-" +
                                             // o.referenceBody.radius.ToSI(3) + "m)"); //"new periapsis cannot be lower than minus the radius of <<1>>"
            }

            double newPeR = NewPeA + o.referenceBody.radius;
            double newApR = NewApA + o.referenceBody.radius;
            Vector3d deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(o, ut, newPeR, newApR);

            return new List<ManeuverParameters> { new ManeuverParameters(deltaV, ut) };
        }
    }
}
