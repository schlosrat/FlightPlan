using KSP.Sim.impl;
// using Smooth.Pools;
using UtilScripts;

namespace MuMech
{
    public partial class ReentrySimulation
    {
        // FloatCurve (Unity Animation curve) are not thread safe so we need a local copy of the curves for the thread
        public class SimCurves
        {
            private static readonly Pool<SimCurves> _simcurvesPool = new Pool<SimCurves>(CreateSimCurve, ResetSimCurve);

            private SimCurves()
            {
            }

            private static SimCurves CreateSimCurve() => new SimCurves();

            public void Release() => _simcurvesPool.ReleaseInstance(this);

            private static void ResetSimCurve(SimCurves obj)
            {
            }

            public static SimCurves Borrow(CelestialBodyComponent newBody)
            {
                SimCurves curve = _simcurvesPool.FetchInstance();
                curve.Setup(newBody);
                return curve;
            }

            private void Setup(CelestialBodyComponent newBody)
            {
                // No point in copying those again if we already have them loaded
                if (!_loaded)
                {
                    _dragCurveCd         = new FloatCurve(PhysicsSettings.DragCurveCd.Curve.keys);
                    _dragCurveCdPower    = new FloatCurve(PhysicsSettings.DragCurveCdPower.Curve.keys);
                    _dragCurveMultiplier = new FloatCurve(PhysicsSettings.DragCurveMultiplier.Curve.keys);

                    _dragCurveSurface = new FloatCurve(PhysicsSettings.SurfaceCurves.DragCurveSurface.Curve.keys);
                    _dragCurveTail    = new FloatCurve(PhysicsSettings.SurfaceCurves.DragCurveTail.Curve.keys);
                    _dragCurveTip     = new FloatCurve(PhysicsSettings.SurfaceCurves.DragCurveTip.Curve.keys);

                    _liftCurve     = new FloatCurve(PhysicsSettings.BodyLiftLiftingSurfaceCurve.liftCurve.Curve.keys);
                    LiftMachCurve = new FloatCurve(PhysicsSettings.BodyLiftLiftingSurfaceCurve.liftMachCurve.Curve.keys);
                    _dragCurve     = new FloatCurve(PhysicsSettings.BodyLiftLiftingSurfaceCurve.dragCurve.Curve.keys);
                    _dragMachCurve = new FloatCurve(PhysicsSettings.BodyLiftLiftingSurfaceCurve.dragMachCurve.Curve.keys);

                    DragCurvePseudoReynolds = new FloatCurve(PhysicsSettings.DragCurvePseudoReynolds.Curve.keys);

                    SpaceTemperature = PhysicsSettings.SpaceTemperature;
                    _loaded          = true;
                }

                if (newBody != _body)
                {
                    _body                             = newBody;
                    AtmospherePressureCurve           = new FloatCurve(newBody.atmospherePressureCurve.Curve.keys);
                    // AtmosphereTemperatureSunMultCurve = new FloatCurve(newBody.atmosphereTemperatureSunMultCurve.Curve.keys);
                    // LatitudeTemperatureBiasCurve      = new FloatCurve(newBody.latitudeTemperatureBiasCurve.Curve.keys);
                    // LatitudeTemperatureSunMultCurve   = new FloatCurve(newBody.latitudeTemperatureSunMultCurve.Curve.keys);
                    AtmosphereTemperatureCurve        = new FloatCurve(newBody.BodyAltitudeTemperatureCurve.Curve.keys); // was atmosphereTemperatureCurve
                    // AxialTemperatureSunMultCurve      = new FloatCurve(newBody.axialTemperatureSunMultCurve.Curve.keys);
                }
            }

            private bool _loaded;

            private CelestialBodyComponent _body;

            private FloatCurve _liftCurve                        { get; set; }
            public  FloatCurve LiftMachCurve                     { get; private set; }
            private FloatCurve _dragCurve                        { get; set; }
            private FloatCurve _dragMachCurve                    { get; set; }
            private FloatCurve _dragCurveTail                    { get; set; }
            private FloatCurve _dragCurveSurface                 { get; set; }
            private FloatCurve _dragCurveTip                     { get; set; }
            private FloatCurve _dragCurveCd                      { get; set; }
            private FloatCurve _dragCurveCdPower                 { get; set; }
            private FloatCurve _dragCurveMultiplier              { get; set; }
            public  FloatCurve AtmospherePressureCurve           { get; private set; }
            public  FloatCurve AtmosphereTemperatureSunMultCurve { get; private set; }
            public  FloatCurve LatitudeTemperatureBiasCurve      { get; private set; }
            public  FloatCurve LatitudeTemperatureSunMultCurve   { get; private set; }
            public  FloatCurve AxialTemperatureSunMultCurve      { get; private set; }
            public  FloatCurve AtmosphereTemperatureCurve        { get; private set; }
            public  FloatCurve DragCurvePseudoReynolds           { get; private set; }

            public double SpaceTemperature { get; private set; }

            public void CopyTo(List<DragCube> dest) // was: DragCubeList
            {
                // FIX ME!
                //dest.DragCurveCd         = _dragCurveCd;
                //dest.DragCurveCdPower    = _dragCurveCdPower;
                //dest.DragCurveMultiplier = _dragCurveMultiplier;

                //dest.BodyLiftCurve.liftCurve     = _liftCurve;
                //dest.BodyLiftCurve.dragCurve     = _dragCurve;
                //dest.BodyLiftCurve.dragMachCurve = _dragMachCurve;
                //dest.BodyLiftCurve.liftMachCurve = LiftMachCurve;

                //dest.SurfaceCurves.dragCurveMultiplier = _dragCurveMultiplier;
                //dest.SurfaceCurves.dragCurveSurface    = _dragCurveSurface;
                //dest.SurfaceCurves.dragCurveTail       = _dragCurveTail;
                //dest.SurfaceCurves.dragCurveTip        = _dragCurveTip;
            }
        }
    }
}
