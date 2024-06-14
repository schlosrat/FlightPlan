using System.Collections.Generic;
using FlightPlan;
using JetBrains.Annotations;
using KSP.Localization;
using KSP.Sim.impl;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationResonantOrbit : Operation
    {
        private static readonly string _name = "resonant orbit"; // Localizer.Format("#MechJeb_resonant_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public EditableInt ResonanceNumerator = 2;
        private short ResonanceNumerator = 2;

        [UsedImplicitly]
        // [Persistent(pass = (int)Pass.GLOBAL)]
        // public EditableInt ResonanceDenominator = 3;
        private short ResonanceDenominator = 3;

        private readonly TimeSelector _timeSelector =
            new TimeSelector(new[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE });

        public OperationResonantOrbit()
        {
            _timeSelector = new TimeSelector(new[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE });
        }

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            //GUILayout.Label(Localizer.Format("#MechJeb_resonant_label1",
            //    ResonanceNumerator.Val + "/" + ResonanceDenominator.Val)); //"Change your orbital period to <<1>> of your current orbital period"
            //GUILayout.BeginHorizontal();
            //GUILayout.Label(Localizer.Format("#MechJeb_resonant_label2"), GUILayout.ExpandWidth(true)); //New orbital period ratio :
            //ResonanceNumerator.Text = GUILayout.TextField(ResonanceNumerator.Text, GUILayout.Width(30));
            //GUILayout.Label("/", GUILayout.ExpandWidth(false));
            //ResonanceDenominator.Text = GUILayout.TextField(ResonanceDenominator.Text, GUILayout.Width(30));
            //GUILayout.EndHorizontal();
            
            // FIX ME!
            // ResonanceNumerator = FpUiController.ResonanceNumerator;
            // ResonanceDenominator = FpUiController.ResonanceDenominator;
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            Vector3d dV = OrbitalManeuverCalculator.DeltaVToResonantOrbit(o, ut, (double)ResonanceNumerator / ResonanceDenominator);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
