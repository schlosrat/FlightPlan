/*
 * This Software was obtained from the MechJeb2 project (https://github.com/MuMech/MechJeb2) on 3/25/23
 * and was further modified as needed for compatibility with KSP2 and/or for incorporation into the
 * FlightPlan project (https://github.com/schlosrat/FlightPlan)
 * 
 * This work is relaesed under the same license(s) inherited from the originating version.
 */

using FlightPlan;
using FPUtilities;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using MechJebLib.Primitives;
using System.Runtime.CompilerServices;
using UnityEngine;
// using static MuMech.KSPOrbitModule;

namespace MuMech
{
    public static class OrbitExtensions
    {
        public static double RelativeDistance(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            return (b.WorldBCIPositionAtUT(UT) - a.WorldBCIPositionAtUT(UT)).magnitude;
        }

        public static double RelativeSpeed(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            Vector3d relVel = a.WorldOrbitalVelocityAtUT(UT) - b.WorldOrbitalVelocityAtUT(UT);
            Vector3d relPos = a.WorldBCIPositionAtUT(UT) - b.WorldBCIPositionAtUT(UT);
            return Vector3d.Dot(relVel, relPos.normalized);
        }

        /// <summary>
        /// Get the orbital velocity at a given time in left handed world coordinates.  This value will rotate
        /// due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="UT">Universal Time</param>
        /// <returns>World Velocity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d WorldOrbitalVelocityAtUT(this PatchedConicsOrbit o, double UT) // KS2: OrbitalVelocity // was: SwappedOrbitalVelocityAtUT
        {
            // return o.getOrbitalVelocityAtUT(UT).xzy
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetOrbitalVelocityAtUTZup(UT).SwapYAndZ); // from KS2
        }

        /// <summary>
        /// Get the body centered inertial position at a given time in left handed world coordinates.  This value
        /// will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="UT">Universal Time</param>
        /// <returns>BCI World Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d WorldBCIPositionAtUT(this PatchedConicsOrbit o, double UT) // KS2: RelativePosition // was: SwappedRelativePositionAtUT
        {
            // return o.getRelativePositionAtUT(UT).xzy
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetRelativePositionAtUTZup(UT).SwapYAndZ); // From KS2
        }

        /// <summary>
        /// Get the world space position at a given time in left handed world coordinates.  This value
        /// will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="UT">Universal Time</param>
        /// <returns>World Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d WorldPositionAtUT(this PatchedConicsOrbit o, double UT) // was: SwappedAbsolutePositionAtUT
        {
            // return o.referenceBody.Position.localPosition + o.WorldBCIPositionAtUT(UT);
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.referenceBody.Position.localPosition + o.GetRelativePositionAtUTZup(UT).SwapYAndZ); // from KS2
        }

        /// <summary>
        /// Get the orbital velocity at a given time in right handed coordinates.  This value will rotate
        /// due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="UT">Universal Time</param>
        /// <returns>World Velocity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V3 RightHandedOrbitalVelocityAtUT(this PatchedConicsOrbit o, double UT)
        {
            // return o.getOrbitalVelocityAtUT(UT).ToV3();
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetOrbitalVelocityAtUTZup(UT)).ToV3(); // from KS2
        }

        /// <summary>
        /// Get the body centered inertial position at a given time in right handed coordinates.  This value
        /// will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="UT">Universal Time</param>
        /// <returns>BCI World Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V3 RightHandedBCIPositionAtUT(this PatchedConicsOrbit o, double UT)
        {
            // return o.getRelativePositionAtUT(UT).ToV3();
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetRelativePositionAtUTZup(UT)).ToV3(); // from KS2
        }

        /// <summary>
        /// Get both position and velocity state vectors at a given time in right handed coordinates.  This value
        /// will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="ut">Universal Time</param>
        /// <returns>BCI World Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (V3 pos, V3 vel) RightHandedStateVectorsAtUT(this PatchedConicsOrbit o, double ut)
        {
            o.GetOrbitalStateVectorsAtUT(ut, out Vector3d pos, out Vector3d vel);
            return (pos.ToV3(), vel.ToV3());
        }

        // ReferenceFrame (from KS2)
        //public static ITransformFrame ReferenceFrame(this PatchedConicsOrbit o)
        //{
        //    return o.ReferenceFrame;
        //}

        // ReferenceBody (from KS2)
        //public static KSPOrbitModule.IBody ReferenceBody(this PatchedConicsOrbit o)
        //{
        //    return new BodyWrapper(context, o.ReferenceBody);
        //}

        // ReferenceBody FP Shortcut
        //public static CelestialBodyComponent ReferenceBody(this PatchedConicsOrbit o)
        //{
        //    return o.ReferenceBody;
        //}

        // GlobalPosition (from KS2)
        //public static Position GlobalPosition(this PatchedConicsOrbit o, double UT)
        //{
        //    return new Position(ReferenceFrame, o.GetRelativePositionAtUTZup(UT).SwapYAndZ);
        //}

        // GlobalVelocity (from KS2)
        //public static VelocityAtPosition GlobalVelocity(this PatchedConicsOrbit o, double UT)
        //{
        //    return new VelocityAtPosition(new Velocity(ReferenceFrame.motionFrame, o.GetOrbitalVelocityAtUTZup(UT).SwapYAndZ), GlobalPosition(UT)); ;
        //}

        //normalized vector perpendicular to the orbital plane
        //convention: as you look down along the orbit normal, the satellite revolves counterclockwise
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d OrbitNormal(this PatchedConicsOrbit o) // KS2: OrbitNormal // was: SwappedOrbitNormal
        {
            // return -o.GetOrbitNormal().xzy.normalized
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, -o.GetRelativeOrbitNormal().SwapYAndZ).normalized; // From KS2
        }

        //normalized vector perpendicular to the orbital plane
        //convention: as you look down along the orbit normal, the satellite revolves counterclockwise
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d OrbitNormal(this IKeplerOrbit o) // KS2: OrbitNormal // was: SwappedOrbitNormal
        {
            // return -o.GetOrbitNormal().xzy.normalized
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, -o.GetRelativeOrbitNormal().SwapYAndZ).normalized; // From KS2
        }

        //normalized vector along the orbital velocity
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Prograde(this PatchedConicsOrbit o, double UT) // Agrees with KS2
        {
            return o.WorldOrbitalVelocityAtUT(UT).normalized;
        }

        //normalized vector pointing radially outward from the planet
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Up(this PatchedConicsOrbit o, double UT) // Agrees with KS2
        {
            return o.WorldBCIPositionAtUT(UT).normalized;
        }

        //normalized vector pointing radially outward and perpendicular to prograde
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d RadialPlus(this PatchedConicsOrbit o, double UT) // Agrees with KS2
        {
            return Vector3d.Exclude(o.Prograde(UT), o.Up(UT)).normalized;
        }

        //another name for the orbit normal; this form makes it look like the other directions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d NormalPlus(this PatchedConicsOrbit o, double UT)
        {
            // return o.OrbitNormal();
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetRelativeOrbitNormal().SwapYAndZ.normalized); // From KS2
        }

        //normalized vector parallel to the planet's surface, and pointing in the same general direction as the orbital velocity
        //(parallel to an ideally spherical planet's surface, anyway)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Horizontal(this PatchedConicsOrbit o, double UT) // Agrees with KS2
        {
            return Vector3d.Exclude(o.Up(UT), o.Prograde(UT)).normalized;
        }

        //horizontal component of the velocity vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d HorizontalVelocity(this PatchedConicsOrbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), o.WorldOrbitalVelocityAtUT(UT));
        }

        //vertical component of the velocity vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d VerticalVelocity(this PatchedConicsOrbit o, double UT)
        {
            return Vector3d.Dot(o.Up(UT), o.WorldOrbitalVelocityAtUT(UT)) * o.Up(UT);
        }

        //normalized vector parallel to the planet's surface and pointing in the northward direction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d North(this PatchedConicsOrbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), o.referenceBody.transform.up.vector * (float)o.referenceBody.radius - o.WorldBCIPositionAtUT(UT))
                .normalized;
        }

        //normalized vector parallel to the planet's surface and pointing in the eastward direction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d East(this PatchedConicsOrbit o, double UT)
        {
            return Vector3d.Cross(o.Up(UT), o.North(UT));
        }

        //distance from the center of the planet
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Radius(this PatchedConicsOrbit o, double UT) // Agrees with KS2
        {
            return o.WorldBCIPositionAtUT(UT).magnitude;
        }

        //returns a new PatchedConicsOrbit object that represents the result of applying a given dV to o at UT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PatchedConicsOrbit PerturbedOrbit(this PatchedConicsOrbit o, double UT, Vector3d dV)
        {
            // return MuUtils.OrbitFromStateVectors(o.WorldPositionAtUT(UT), o.WorldOrbitalVelocityAtUT(UT) + dV, o.referenceBody, UT);
            return o.CreateOrbit(o.WorldBCIPositionAtUT(UT), o.WorldOrbitalVelocityAtUT(UT) + dV, UT); // From KS2
            // Actual KS2 returns: ReferenceBody.CreateOrbit(RelativePosition(ut), OrbitalVelocity(ut) + dV, ut);
        }

        // Adapted from KS2
        public static PatchedConicsOrbit CreateOrbit(this PatchedConicsOrbit o, Vector3d position, Vector3d velocity, double UT)
        {
            PatchedConicsOrbit orbit = new(o.referenceBody.universeModel);

            // Actual KS2 returns: orbit.UpdateFromStateVectors(new Position(body.SimulationObject.transform.celestialFrame, position), new Velocity(body.SimulationObject.transform.celestialFrame.motionFrame, velocity), body, ut);
            orbit.UpdateFromStateVectors(new Position(o.referenceBody.SimulationObject.transform.celestialFrame, position), new Velocity(o.referenceBody.SimulationObject.transform.celestialFrame.motionFrame, velocity), o.referenceBody, UT);

            return orbit;
        }

        // returns a new orbit that is identical to the current one (although the epoch will change)
        // (i tried many different APIs in the orbit class, but the GetOrbitalStateVectors/UpdateFromStateVectors route was the only one that worked)
        public static PatchedConicsOrbit Clone(this PatchedConicsOrbit o, double UT = double.NegativeInfinity)
        {
            Vector3d pos, vel;

            // hack up a dynamic default value to the current time
            if (UT == double.NegativeInfinity)
                UT = GameManager.Instance.Game.UniverseModel.UniversalTime;

            PatchedConicsOrbit newOrbit = new PatchedConicsOrbit(GameManager.Instance.Game.UniverseModel);
            o.GetOrbitalStateVectorsAtUT(UT, out pos, out vel);
            Position position = new Position(o.referenceBody.SimulationObject.transform.celestialFrame, (pos - o.referenceBody.Position.localPosition).SwapYAndZ);
            Velocity velocity = new Velocity(o.referenceBody.SimulationObject.transform.celestialFrame.motionFrame, vel.SwapYAndZ); // OrbitExtensions.SwapYZ(vel)
            newOrbit.UpdateFromStateVectors(position, velocity, o.referenceBody, UT);

            return newOrbit;
        }

        // calculate the next patch, which makes patchEndTransition be valid
        public static PatchedConicsOrbit CalculateNextOrbit(this PatchedConicsOrbit o, double UT = double.NegativeInfinity)
        {
            // PatchedConicSolver.SolverParameters solverParameters = new PatchedConicSolver.SolverParameters();

            // hack up a dynamic default value to the current time
            if (UT == double.NegativeInfinity)
                UT = GameManager.Instance.Game.UniverseModel.UniversalTime;

            o.StartUT = UT;
            o.EndUT   = o.eccentricity >= 1.0 ? o.period : UT + o.period;
            // PatchedConicsOrbit nextOrbit = new PatchedConicsOrbit(GameManager.Instance.Game.UniverseModel);
            // PatchedConics.CalculatePatch(o, nextOrbit, UT, solverParameters, null); // Maybe this need to be CalculatePatchConicList, or CalculatePatchList ???
            // Assuming nextOrbit can be obtained from o.NextPatch
            PatchedConicsOrbit nextOrbit = o.NextPatch as PatchedConicsOrbit;

            return nextOrbit;
        }

        // This does not allocate a new orbit object and the caller should call new PatchedConicsOrbit if/when required
        public static void MutatedOrbit(this PatchedConicsOrbit o, double periodOffset = double.NegativeInfinity)
        {
            double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;

            if (periodOffset.IsFinite())
            {
                Vector3d pos, vel;
                o.GetOrbitalStateVectorsAtUT(UT + o.period * periodOffset, out pos, out vel);
                Position position = new Position(o.referenceBody.SimulationObject.transform.celestialFrame, (pos - o.referenceBody.Position.localPosition).SwapYAndZ);
                Velocity velocity = new Velocity(o.referenceBody.SimulationObject.transform.celestialFrame.motionFrame, vel.SwapYAndZ);
                o.UpdateFromStateVectors(position, velocity, o.referenceBody, UT);
            }
        }

        //mean motion is rate of increase of the mean anomaly
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MeanMotion(this PatchedConicsOrbit o)
        {
            if (o.eccentricity > 1)
            {
                return Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));
            }

            // The above formula is wrong when using the RealSolarSystem mod, which messes with orbital periods.
            // This simpler formula should be foolproof for elliptical orbits:
            return 2 * Math.PI / o.period;
        }

        //distance between two orbiting objects at a given time
        // TODO: Explore using the built-in KPS2 orbit function for ClosestApproachDistance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Separation(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            return (a.WorldPositionAtUT(UT) - b.WorldPositionAtUT(UT)).magnitude;
        }

        //Time during a's next orbit at which object a comes nearest to object b.
        //If a is hyperbolic, the examined interval is the next 100 units of mean anomaly.
        //This is quite a large segment of the hyperbolic arc. However, for extremely high
        //hyperbolic eccentricity it may not find the actual closest approach.
        // TODO: Explore using the built-in KSP2 orbit functions for closestEncounterBody, closestEncounterLevel,
        // or closestEncounterPatch
        public static double NextClosestApproachTime(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            double closestApproachTime = UT;
            double closestApproachDistance = double.MaxValue;
            double minTime = UT;
            double interval = a.period;
            if (a.eccentricity > 1)
            {
                interval = 100 / a.MeanMotion(); //this should be an interval of time that covers a large chunk of the hyperbolic arc
            }

            double maxTime = UT + interval;
            const int numDivisions = 20;

            for (int iter = 0; iter < 8; iter++)
            {
                double dt = (maxTime - minTime) / numDivisions;
                for (int i = 0; i < numDivisions; i++)
                {
                    double t = minTime + i * dt;
                    double distance = a.Separation(b, t);
                    if (distance < closestApproachDistance)
                    {
                        closestApproachDistance = distance;
                        closestApproachTime     = t;
                    }
                }

                minTime = MuUtils.Clamp(closestApproachTime - dt, UT, UT + interval);
                maxTime = MuUtils.Clamp(closestApproachTime + dt, UT, UT + interval);
            }

            return closestApproachTime;
        }

        //Distance between a and b at the closest approach found by NextClosestApproachTime
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NextClosestApproachDistance(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            return a.Separation(b, a.NextClosestApproachTime(b, UT));
        }

        //The mean anomaly of the orbit.
        //For elliptical orbits, the value return is always between 0 and 2pi
        //For hyperbolic orbits, the value can be any number.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MeanAnomalyAtUT(this PatchedConicsOrbit o, double UT)
        {
            // We use ObtAtEpoch and not meanAnomalyAtEpoch because somehow meanAnomalyAtEpoch
            // can be wrong when using the RealSolarSystem mod. ObtAtEpoch is always correct.
            double ret = (o.ObTAtEpoch + (UT - o.epoch)) * o.MeanMotion();
            if (o.eccentricity < 1) ret = MuUtils.ClampRadiansTwoPi(ret);
            return ret;
        }

        //The next time at which the orbiting object will reach the given mean anomaly.
        //For elliptical orbits, this will be a time between UT and UT + o.period
        //For hyperbolic orbits, this can be any time, including a time in the past, if
        //the given mean anomaly occurred in the past
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double UTAtMeanAnomaly(this PatchedConicsOrbit o, double meanAnomaly, double UT)
        {
            double currentMeanAnomaly = o.MeanAnomalyAtUT(UT);
            double meanDifference = meanAnomaly - currentMeanAnomaly;
            if (o.eccentricity < 1) meanDifference = MuUtils.ClampRadiansTwoPi(meanDifference);
            return UT + meanDifference / o.MeanMotion();
        }

        //The next time at which the orbiting object will be at periapsis.
        //For elliptical orbits, this will be between UT and UT + o.period.
        //For hyperbolic orbits, this can be any time, including a time in the past,
        //if the periapsis is in the past.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NextPeriapsisTime(this PatchedConicsOrbit o, double UT)
        {
            if (o.eccentricity < 1)
            {
                return o.TimeOfTrueAnomaly(0, UT);
            }

            return UT - o.MeanAnomalyAtUT(UT) / o.MeanMotion();
        }

        //Returns the next time at which the orbiting object will be at apoapsis.
        //For elliptical orbits, this is a time between UT and UT + period.
        //For hyperbolic orbits, this throws an ArgumentException.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NextApoapsisTime(this PatchedConicsOrbit o, double UT)
        {
            if (o.eccentricity < 1)
            {
                return o.TimeOfTrueAnomaly(Math.PI, UT);
                // SAVEFORNOW return o.TimeOfTrueAnomaly(180, UT);
            }

            throw new ArgumentException("OrbitExtensions.NextApoapsisTime cannot be called on hyperbolic orbits");
        }

        //Gives the true anomaly (in a's orbit) at which a crosses its ascending node
        //with b's orbit.
        //The returned value is always between 0 and 2 * PI.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AscendingNodeTrueAnomaly(this PatchedConicsOrbit a, IKeplerOrbit b)  // was orbit as type for b
        {
            Vector3d vectorToAN = Vector3d.Cross(a.OrbitNormal(), b.OrbitNormal());
            return a.TrueAnomalyFromVector(vectorToAN);
        }

        //Gives the true anomaly (in a's orbit) at which a crosses its descending node
        //with b's orbit.
        //The returned value is always between 0 and 2 * PI.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DescendingNodeTrueAnomaly(this PatchedConicsOrbit a, IKeplerOrbit b) // was orbit as type for b
        {
            return MuUtils.ClampRadiansTwoPi(a.AscendingNodeTrueAnomaly(b) + Math.PI);
            // SAVEFORNOW return MuUtils.ClampDegrees360(a.AscendingNodeTrueAnomaly(b) + 180.0);
        }

        //Gives the true anomaly at which o crosses the equator going northwards, if o is east-moving,
        //or southwards, if o is west-moving.
        //The returned value is always between 0 and 2 * PI.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AscendingNodeEquatorialTrueAnomaly(this PatchedConicsOrbit o)
        {
            Vector3d vectorToAN = Vector3d.Cross(o.referenceBody.transform.up.vector, o.OrbitNormal());
            return o.TrueAnomalyFromVector(vectorToAN);
        }

        //Gives the true anomaly at which o crosses the equator going southwards, if o is east-moving,
        //or northwards, if o is west-moving.
        //The returned value is always between 0 and 2 * PI.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DescendingNodeEquatorialTrueAnomaly(this PatchedConicsOrbit o)
        {
            return MuUtils.ClampRadiansTwoPi(o.AscendingNodeEquatorialTrueAnomaly() + Math.PI);
            // SAVEFORNOW return MuUtils.ClampDegrees360(o.AscendingNodeEquatorialTrueAnomaly() + 180);
        }

        //For hyperbolic orbits, the true anomaly only takes on values in the range
        // -M < true anomaly < +M for some M. This function computes M.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MaximumTrueAnomaly(this PatchedConicsOrbit o)
        {
            if (o.eccentricity < 1) return Math.PI;
            return Math.Acos(-1 / o.eccentricity);
            // SAVEFORNOW if (o.eccentricity < 1) return 180;
            // SAVEFORNOW return UtilMath.Rad2Deg * Math.Acos(-1 / o.eccentricity);
        }

        //Returns whether a has an ascending node with b. This can be false
        //if a is hyperbolic and the would-be ascending node is within the opening
        //angle of the hyperbola.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AscendingNodeExists(this PatchedConicsOrbit a, PatchedConicsOrbit b)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(a.AscendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
        }

        //Returns whether a has a descending node with b. This can be false
        //if a is hyperbolic and the would-be descending node is within the opening
        //angle of the hyperbola.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DescendingNodeExists(this PatchedConicsOrbit a, PatchedConicsOrbit b)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(a.DescendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
        }

        //Returns whether o has an ascending node with the equator. This can be false
        //if o is hyperbolic and the would-be ascending node is within the opening
        //angle of the hyperbola.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AscendingNodeEquatorialExists(this PatchedConicsOrbit o)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(o.AscendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
        }

        //Returns whether o has a descending node with the equator. This can be false
        //if o is hyperbolic and the would-be descending node is within the opening
        //angle of the hyperbola.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DescendingNodeEquatorialExists(this PatchedConicsOrbit o)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(o.DescendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
        }

        //Returns the vector from the primary to the orbiting body at periapsis
        //Better than using PatchedConicsOrbit.eccVec because that is zero for circular orbits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d WorldBCIPositionAtPeriapsis(this PatchedConicsOrbit o) // was: SwappedRelativePositionAtPeriapsis
        {
            Vector3d vectorToAN = Quaternion.AngleAxis(-(float)o.longitudeOfAscendingNode, o.ReferenceFrame.up.vector) * o.ReferenceFrame.right.vector;
            Vector3d vectorToPe = Quaternion.AngleAxis((float)o.argumentOfPeriapsis, o.OrbitNormal()) * vectorToAN;
            return o.Periapsis * vectorToPe;
        }

        //Returns the vector from the primary to the orbiting body at apoapsis
        //Better than using -Orbit.eccVec because that is zero for circular orbits
        public static Vector3d WorldBCIPositionAtApoapsis(this PatchedConicsOrbit o) // replaced by: WorldBCIPositionAtApoapsis
        {
            Vector3d vectorToAN = Quaternion.AngleAxis(-(float)o.longitudeOfAscendingNode, o.ReferenceFrame.up.vector) * o.ReferenceFrame.right.vector;
            Vector3d vectorToPe = Quaternion.AngleAxis((float)o.argumentOfPeriapsis, o.OrbitNormal()) * vectorToAN;
            Vector3d ret = -o.Apoapsis * vectorToPe;
            if (double.IsNaN(ret.x))
            {
                FlightPlanPlugin.Logger.LogError("OrbitExtensions.WorldBCIPositionAtApoapsis got a NaN result!");
                FlightPlanPlugin.Logger.LogError("o.LAN = " + o.longitudeOfAscendingNode);
                FlightPlanPlugin.Logger.LogError("o.inclination = " + o.inclination);
                FlightPlanPlugin.Logger.LogError("o.argumentOfPeriapsis = " + o.argumentOfPeriapsis);
                FlightPlanPlugin.Logger.LogError("o.OrbitNormal() = " + o.OrbitNormal());
            }
            return ret;
        }

        //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

        //Converts a direction, specified by a Vector3d, into a true anomaly.
        //The vector is projected into the orbital plane and then the true anomaly is
        //computed as the angle this vector makes with the vector pointing to the periapsis.
        //The returned value is always between 0 and 2*PI.
        public static double TrueAnomalyFromVector(this PatchedConicsOrbit o, Vector3d vec)
        {
            Vector3d oNormal = o.OrbitNormal();
            Vector3d projected = Vector3d.Exclude(oNormal, vec);
            Vector3d vectorToPe = o.WorldBCIPositionAtPeriapsis();
            double angleFromPe = Vector3d.Angle(vectorToPe, projected);

            //If the vector points to the infalling part of the orbit then we need to do 360 minus the
            //angle from Pe to get the true anomaly. Test this by taking the cross product of the
            //orbit normal and vector to the periapsis. This gives a vector that points to center of the
            //outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
            //during the infalling part of the orbit.
            if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(oNormal, vectorToPe))) < 90)
            {
                return angleFromPe * UtilMath.Deg2Rad;
                // SAVEFORNOW return angleFromPe;
            }

            return (360 - angleFromPe) * UtilMath.Deg2Rad;
            // SAVEFORNOW return 360 - angleFromPe;
        }

        //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

        //Originally by Zool, revised by The_Duck
        //Converts a true anomaly into an eccentric anomaly.
        //For elliptical orbits this returns a value between 0 and 2pi
        //For hyperbolic orbits the returned value can be any number.
        //NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
        //past the true anomaly of the asymptote) then an ArgumentException is thrown
        public static double GetEccentricAnomalyAtTrueAnomaly(this PatchedConicsOrbit o, double trueAnomalyRad)
        {
            double e = o.eccentricity;
            trueAnomalyRad = MuUtils.ClampRadiansTwoPi(trueAnomalyRad);
            // SAVEFORNOW trueAnomaly = MuUtils.ClampDegrees360(trueAnomaly);
            // SAVEFORNOW trueAnomaly = trueAnomaly * (UtilMath.Deg2Rad);

            if (e < 1) //elliptical orbits
            {
                double cosE = (e + Math.Cos(trueAnomalyRad)) / (1 + e * Math.Cos(trueAnomalyRad));
                double sinE = Math.Sqrt(1 - cosE * cosE);
                if (trueAnomalyRad > Math.PI) sinE *= -1;

                return MuUtils.ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
            }

            //hyperbolic orbits
            double coshE = (e + Math.Cos(trueAnomalyRad)) / (1 + e * Math.Cos(trueAnomalyRad));
            if (coshE < 1)
                throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomalyRad +
                                            " radians is not attained by orbit with eccentricity " + o.eccentricity);

            double E = MuUtils.Acosh(coshE);
            if (trueAnomalyRad > Math.PI) E *= -1;

            return E;
        }

        //Originally by Zool, revised by The_Duck
        //Converts an eccentric anomaly into a mean anomaly.
        //For an elliptical orbit, the returned value is between 0 and 2pi
        //For a hyperbolic orbit, the returned value is any number
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetMeanAnomalyAtEccentricAnomaly(this PatchedConicsOrbit o, double E)
        {
            double e = o.eccentricity;
            if (e < 1) //elliptical orbits
            {
                return MuUtils.ClampRadiansTwoPi(E - e * Math.Sin(E));
            }

            //hyperbolic orbits
            return e * Math.Sinh(E) - E;
        }

        //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

        //Converts a true anomaly into a mean anomaly (via the intermediate step of the eccentric anomaly)
        //For elliptical orbits, the output is between 0 and 2pi
        //For hyperbolic orbits, the output can be any number
        //NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
        //past the true anomaly of the asymptote) then an ArgumentException is thrown
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetMeanAnomalyAtTrueAnomaly(this PatchedConicsOrbit o, double trueAnomaly)
        {
            return o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly));
        }

        //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

        //NOTE: this function can throw an ArgumentException, if o is a hyperbolic orbit with an eccentricity
        //large enough that it never attains the given true anomaly
        public static double TimeOfTrueAnomaly(this PatchedConicsOrbit o, double trueAnomalyRad, double UT)
        {
            //var eccAnom = o.GetEccentricAnomalyAtTrueAnomaly(trueAnomalyRad);
            //var meanAnom = o.GetMeanAnomalyAtEccentricAnomaly(eccAnom);
            //var utAtmeanAnom = o.UTAtMeanAnomaly(meanAnom, UT);
            //FlightPlanPlugin.Logger.LogDebug($"TimeOfTrueAnomaly: trueAnomalyRad {trueAnomalyRad} = {trueAnomalyRad * UtilMath.Rad2Deg}°");
            //FlightPlanPlugin.Logger.LogDebug($"TimeOfTrueAnomaly: GetEccentricAnomalyAtTrueAnomaly {eccAnom} = {eccAnom*UtilMath.Rad2Deg}°");
            //FlightPlanPlugin.Logger.LogDebug($"TimeOfTrueAnomaly: GetMeanAnomalyAtEccentricAnomaly {meanAnom} = {meanAnom * UtilMath.Rad2Deg}°");
            //FlightPlanPlugin.Logger.LogDebug($"TimeOfTrueAnomaly: utAtmeanAnom {utAtmeanAnom} = {FPUtility.SecondsToTimeString(utAtmeanAnom)}");
            return o.UTAtMeanAnomaly(o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomalyRad)), UT);
        }

        //Returns the next time at which a will cross its ascending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if
        //the ascending node is in the past.
        //NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "ascending node"
        //occurs at a true anomaly that a does not actually ever attain
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TimeOfAscendingNode(this PatchedConicsOrbit a, IKeplerOrbit b, double UT)
        {
            // return a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);
            var UTAN = a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);
            if (UTAN < UT) UTAN += a.period;
            return UTAN;
        }

        //Returns the next time at which a will cross its descending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if
        //the descending node is in the past.
        //NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "descending node"
        //occurs at a true anomaly that a does not actually ever attain
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TimeOfDescendingNode(this PatchedConicsOrbit a, IKeplerOrbit b, double UT)
        {
            //return a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);
            var UTDN = a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);
            if (UTDN < UT) UTDN += a.period;
            return UTDN;
        }

        //Returns the next time at which the orbiting object will cross the equator
        //moving northward, if o is east-moving, or southward, if o is west-moving.
        //For elliptical orbits this is a time between UT and UT + o.period.
        //For hyperbolic orbits this can by any time, including a time in the past if the
        //ascending node is in the past.
        //NOTE: this function will throw an ArgumentException if o is a hyperbolic orbit and the
        //"ascending node" occurs at a true anomaly that o does not actually ever attain.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TimeOfAscendingNodeEquatorial(this PatchedConicsOrbit o, double UT)
        {
            // FlightPlanPlugin.Logger.LogDebug($"TimeOfAscendingNodeEquatorial: AscendingNodeEquatorialTrueAnomaly = {o.AscendingNodeEquatorialTrueAnomaly()} Rad");
            // FlightPlanPlugin.Logger.LogDebug($"TimeOfAscendingNodeEquatorial: TimeOfTrueAnomaly = {FPUtility.SecondsToTimeString(o.TimeOfTrueAnomaly(o.AscendingNodeEquatorialTrueAnomaly(), UT))}");
            return o.TimeOfTrueAnomaly(o.AscendingNodeEquatorialTrueAnomaly(), UT);
        }

        //Returns the next time at which the orbiting object will cross the equator
        //moving southward, if o is east-moving, or northward, if o is west-moving.
        //For elliptical orbits this is a time between UT and UT + o.period.
        //For hyperbolic orbits this can by any time, including a time in the past if the
        //descending node is in the past.
        //NOTE: this function will throw an ArgumentException if o is a hyperbolic orbit and the
        //"descending node" occurs at a true anomaly that o does not actually ever attain.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TimeOfDescendingNodeEquatorial(this PatchedConicsOrbit o, double UT)
        {
            // FlightPlanPlugin.Logger.LogDebug($"TimeOfDescendingNodeEquatorial: DescendingNodeEquatorialTrueAnomaly = {o.DescendingNodeEquatorialTrueAnomaly()} Rad");
            // FlightPlanPlugin.Logger.LogDebug($"TimeOfAscendingNodeEquatorial: TimeOfTrueAnomaly = {FPUtility.SecondsToTimeString(o.TimeOfTrueAnomaly(o.AscendingNodeEquatorialTrueAnomaly(), UT))}");
            return o.TimeOfTrueAnomaly(o.DescendingNodeEquatorialTrueAnomaly(), UT);
        }

        //Computes the period of the phase angle between orbiting objects a and b.
        //This only really makes sense for approximately circular orbits in similar planes.
        //For noncircular orbits the time variation of the phase angle is only "quasiperiodic"
        //and for high eccentricities and/or large relative inclinations, the relative motion is
        //not really periodic at all.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SynodicPeriod(this PatchedConicsOrbit a, PatchedConicsOrbit b)
        {
            int sign = Vector3d.Dot(a.OrbitNormal(), b.OrbitNormal()) > 0 ? 1 : -1; //detect relative retrograde motion
            return Math.Abs(1.0 / (1.0 / a.period - sign * 1.0 / b.period)); //period after which the phase angle repeats
        }

        //Computes the phase angle between two orbiting objects.
        //This only makes sense if a.referenceBody == b.referenceBody.
        public static double PhaseAngle(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            Vector3d normalA = a.OrbitNormal();
            Vector3d posA = a.WorldBCIPositionAtUT(UT);
            Vector3d projectedB = Vector3d.Exclude(normalA, b.WorldBCIPositionAtUT(UT));
            double angle = Vector3d.Angle(posA, projectedB);
            if (Vector3d.Dot(Vector3d.Cross(normalA, posA), projectedB) < 0)
            {
                angle = 360 - angle;
            }

            return angle;
        }

        public static double Transfer(this PatchedConicsOrbit currentOrbit, PatchedConicsOrbit targetOrbit, out double time)
        {
            // GameInstance game = GameManager.Instance.Game;
            // SimulationObjectModel target = Plugin._currentTarget; // game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;
            CelestialBodyComponent cur = currentOrbit.referenceBody; // game.ViewController.GetActiveVehicle(true)?.GetSimVessel().orbit.referenceBody;

            double ellipseA, transfer;

            // This deals with if we're at a moon and backing thing off so that cur would be the planet about which this moon is orbitting
            //while (cur.Orbit.referenceBody.Name != Plugin._currentTarget.Orbit.referenceBody.Name)
            //{
            //    cur = cur.Orbit.referenceBody;
            //}

            // IKeplerOrbit targetOrbit = Plugin._currentTarget.Orbit;
            // IKeplerOrbit currentOrbit = cur.Orbit;

            ellipseA = (targetOrbit.semiMajorAxis + currentOrbit.semiMajorAxis) / 2;
            time = Mathf.PI * Mathf.Sqrt((float)((ellipseA) * (ellipseA) * (ellipseA)) / ((float)targetOrbit.referenceBody.gravParameter));

            transfer = 180 - ((time / targetOrbit.period) * 360);
            while (transfer < -180) { transfer += 360; }
            return Math.Round(transfer, 1);
        }

        //Computes the angle between two orbital planes. This will be a number between 0 and 180
        //Note that in the convention used two objects orbiting in the same plane but in
        //opposite directions have a relative inclination of 180 degrees.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RelativeInclination(this PatchedConicsOrbit a, PatchedConicsOrbit b)
        {
            return Math.Abs(Vector3d.Angle(a.OrbitNormal(), b.OrbitNormal()));
        }

        //Finds the next time at which the orbiting object will achieve a given radius
        //from the center of the primary.
        //If the given radius is impossible for this orbit, an ArgumentException is thrown.
        //For elliptical orbits this will be a time between UT and UT + period
        //For hyperbolic orbits this can be any time. If the given radius will be achieved
        //in the future then the next time at which that radius will be achieved will be returned.
        //If the given radius was only achieved in the past, then there are no guarantees
        //about which of the two times in the past will be returned.
        public static double NextTimeOfRadius(this PatchedConicsOrbit o, double UT, double radius)
        {
            if (radius < o.Periapsis || (o.eccentricity < 1 && radius > o.Apoapsis))
                throw new ArgumentException("OrbitExtensions.NextTimeOfRadius: given radius of " + radius + " is never achieved: o.Periapsis = " + o.Periapsis +
                                            " and o.Apoapsis = " + o.Apoapsis);

            double trueAnomalyRad1 = o.TrueAnomalyAtRadius(radius);
            double trueAnomalyRad2 = 2 * Math.PI - trueAnomalyRad1;
            // SAVEFORNOW double trueAnomalyDeg1 = UtilMath.Rad2Deg * o.TrueAnomalyAtRadius(radius);
            // SAVEFORNOW double trueAnomalyDeg2 = 360 - trueAnomaly1;
            double time1 = o.TimeOfTrueAnomaly(trueAnomalyRad1, UT);
            double time2 = o.TimeOfTrueAnomaly(trueAnomalyRad2, UT);
            if (time2 < time1 && time2 > UT) return time2;
            return time1;
        }

        public static Vector3d DeltaVToManeuverNodeCoordinates(this PatchedConicsOrbit o, double UT, Vector3d dV)
        {
            return new Vector3d(Vector3d.Dot(o.RadialPlus(UT), dV),
                Vector3d.Dot(o.NormalPlus(UT), dV), // was -o.NormalPlus(UT). This aligns with KS2
                Vector3d.Dot(o.Prograde(UT), dV));
        }

        public static Vector3d BurnVecToDv(this PatchedConicsOrbit o, double UT, Vector3d burnVec)
        {
            return burnVec.x * o.RadialPlus(UT) + burnVec.y * o.NormalPlus(UT) + burnVec.z * o.Prograde(UT);
        }


        // Return the orbit of the parent body orbiting the sun
        public static PatchedConicsOrbit TopParentOrbit(this PatchedConicsOrbit orbit)
        {
            PatchedConicsOrbit result = orbit;
            while (result.referenceBody != result.referenceBody.GetRelevantStar())
            {
                result = result.referenceBody.Orbit;
            }

            return result;
        }

        public static string MuString(this PatchedConicsOrbit o)
        {
            return "PeriapsisArl:" + o.PeriapsisArl + " ApoapsisArl:" + o.ApoapsisArl + " SMA:" + o.semiMajorAxis + " ECC:" + o.eccentricity + " INC:" + o.inclination + " LAN:" + o.longitudeOfAscendingNode + " ArgP:" + o.argumentOfPeriapsis + " TA:" + o.TrueAnomaly*UtilMath.Rad2Deg;
        }

        public static double SuicideBurnCountdown(PatchedConicsOrbit orbit, VesselComponent vessel) // was: (Orbit orbit, VesselState vesselState, Vessel vessel)
        {
            if (vessel.mainBody == null) return 0;
            if (orbit.PeriapsisArl > 0) return double.PositiveInfinity;

            double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
            double angleFromHorizontal = 90 - Vector3d.Angle(-vessel.SurfaceVelocity.vector, vessel.Orbit.Up(UT));
            angleFromHorizontal = MuUtils.Clamp(angleFromHorizontal, 0, 90);
            double sine = Math.Sin(angleFromHorizontal * UtilMath.Deg2Rad);
            double g = vessel.graviticAcceleration.magnitude;
            double T = 0 ; // THIS IS WRONG! THIS JUST ALLOWS THE CODE TO COMPILE. FIX ME! was: vesselState.limitedMaxThrustAccel;

            double effectiveDecel = 0.5 * (-2 * g * sine + Math.Sqrt((2 * g * sine) * (2 * g * sine) + 4 * (T * T - g * g)));
            double decelTime = vessel.SrfSpeedMagnitude / effectiveDecel;

            Vector3d estimatedLandingSite = vessel.CenterOfMass.localPosition + 0.5 * decelTime * vessel.SurfaceVelocity.vector;
            Position Position = new(orbit.coordinateSystem, estimatedLandingSite);
            double terrainRadius = vessel.mainBody.radius + vessel.mainBody.GetAltitudeFromRadius(Position); // was: vessel.mainBody.TerrainAltitude(estimatedLandingSite)
            double impactTime = 0;
            try
            {
                impactTime = orbit.NextTimeOfRadius(UT, terrainRadius); // was: vesselState.time
            }
            catch (ArgumentException)
            {
                return 0;
            }

            return impactTime - decelTime / 2 - UT; // was: - vesselState.time;
        }
    }
}
