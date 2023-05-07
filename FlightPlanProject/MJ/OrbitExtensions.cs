/*
 * This Software was obtained from the MechJeb2 project (https://github.com/MuMech/MechJeb2) on 3/25/23
 * and was further modified as needed for compatibility with KSP2 and/or for incorporation into the
 * FlightPlan project (https://github.com/schlosrat/FlightPlan)
 * 
 * This work is relaesed under the same license(s) inherited from the originating version.
 */

using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using FlightPlan;
//using UnityEngine;
//using static VehiclePhysics.TelemetryTemplateBase;
//using static MuMech.KSPOrbitModule;

namespace MuMech
{
    public static class OrbitExtensions
    {
        //can probably be replaced with Vector3d.xzy?
        public static Vector3d SwapYZ(Vector3d v)
        {
            return v.SwapYAndZ; // It's built into KSP2 now. No need to call Reorder (unless it's faster)
            // return v.Reorder(132);
        }

        //
        // These "Swapped" functions translate preexisting Orbit class functions into world
        // SPACE. For some reason, Orbit class functions seem to use a coordinate system
        // in which the Y and Z coordinates are swapped.
        //
        public static Vector3d SwappedOrbitalVelocityAtUT(this PatchedConicsOrbit o, double UT) // KS2: OrbitalVelocity
        {
            // Vector3d GetOrbitalVelocityAtUTZup(double UT) => this.GetOrbitalVelocityAtObTZup(this.GetObtAtUT(UT));
            // Vector3d GetOrbitalVelocityAtObTZup(double obT) => this.GetOrbitalVelocityAtTrueAnomaly(this.TrueAnomalyAtObT(obT));
            // return SwapYZ(o.GetOrbitalVelocityAtUTZup(UT)); // was: o.getOrbitalVelocityAtUT(UT)
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetOrbitalVelocityAtUTZup(UT).SwapYAndZ);
        }

        //Position relative to the primary
        public static Vector3d SwappedRelativePositionAtUT(this PatchedConicsOrbit o, double UT) // KS2: RelativePosition
        {
            // Vector3d GetRelativePositionAtUT(double UT) => this.GetRelativePositionAtObTZup(this.GetObtAtUT(UT)).SwapYAndZ;
            // Vector3d GetRelativePositionAtObTZup(double obT) => this.GetRelativePositionFromTrueAnomalyZup(this.GetTrueAnomaly(this.SolveEccentricAnomaly(obT * this.meanMotion, this.OrbitalElements.Eccentricity)));
            // return SwapYZ(o.GetRelativePositionAtUT(UT)); // was: o.getRelativePositionAtUT(UT)
            // From KontrolSystem2:
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetRelativePositionAtUTZup(UT).SwapYAndZ);
        }

        //Position in world SPACE
        public static Vector3d SwappedAbsolutePositionAtUT(this PatchedConicsOrbit o, double UT)
        {
            // was: o.ReferenceBody.Position -> o.ReferenceBody.Position.localPosition
            // return o.ReferenceBody.Position.localPosition + o.SwappedRelativePositionAtUT(UT);
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.referenceBody.Position.localPosition + o.GetRelativePositionAtUTZup(UT).SwapYAndZ);
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
        //convention: as you look down along the Orbit normal, the satellite revolves counterclockwise
        public static Vector3d SwappedOrbitNormal(this PatchedConicsOrbit o) // KS2: OrbitNormal
        {
            // Vector3d GetRelativeOrbitNormal() => this.universeModel.Zup.WorldToLocal(this.orbitFrame.Z);
            // return -SwapYZ(o.GetRelativeOrbitNormal()).normalized; // was: o.GetOrbitNormal()
            // From KontrolSystem2:
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, -o.GetRelativeOrbitNormal().SwapYAndZ);
        }

        //normalized vector perpendicular to the orbital plane
        //convention: as you look down along the Orbit normal, the satellite revolves counterclockwise
        public static Vector3d SwappedOrbitNormal(this IKeplerOrbit o) // KS2: OrbitNormal
        {
            // Vector3d GetRelativeOrbitNormal() => this.universeModel.Zup.WorldToLocal(this.orbitFrame.Z);
            // return -SwapYZ(o.GetRelativeOrbitNormal()).normalized; // was: o.GetOrbitNormal()
            // From KontrolSystem2:
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, -o.GetRelativeOrbitNormal().SwapYAndZ);
        }

        //normalized vector along the orbital velocity
        public static Vector3d Prograde(this PatchedConicsOrbit o, double UT)
        {
            // Agrees with KS2
            return o.SwappedOrbitalVelocityAtUT(UT).normalized;
        }

        //normalized vector pointing radially outward from the planet
        public static Vector3d Up(this PatchedConicsOrbit o, double UT)
        {
            // Agrees with KS2
            return o.SwappedRelativePositionAtUT(UT).normalized;
        }

        //normalized vector pointing radially outward and perpendicular to prograde
        public static Vector3d RadialPlus(this PatchedConicsOrbit o, double UT)
        {
            // Agrees with KS2
            return Vector3d.Exclude(o.Prograde(UT), o.Up(UT)).normalized;
        }

        //another name for the Orbit normal; this form makes it look like the other directions
        public static Vector3d NormalPlus(this PatchedConicsOrbit o, double UT)
        {
            // From KS2:
            return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetRelativeOrbitNormal().SwapYAndZ.normalized);
        }

        //normalized vector parallel to the planet's surface, and pointing in the same general direction as the orbital velocity
        //(parallel to an ideally spherical planet's surface, anyway)
        public static Vector3d Horizontal(this PatchedConicsOrbit o, double UT)
        {
            // Agrees with KS2
            return Vector3d.Exclude(o.Up(UT), o.Prograde(UT)).normalized;
        }

        //horizontal component of the velocity vector
        public static Vector3d HorizontalVelocity(this PatchedConicsOrbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), o.SwappedOrbitalVelocityAtUT(UT));
        }

        //vertical component of the velocity vector
        public static Vector3d VerticalVelocity(this PatchedConicsOrbit o, double UT)
        {
            return Vector3d.Dot(o.Up(UT), o.SwappedOrbitalVelocityAtUT(UT)) * o.Up(UT);
        }

        //normalized vector parallel to the planet's surface and pointing in the northward direction
        public static Vector3d North(this PatchedConicsOrbit o, double UT)
        {
            // was: o.ReferenceBody.Radius -> o.ReferenceBody.radius
            // was: o.ReferenceBody.transform.up -> o.ReferenceBody.transform.up.vector
            return Vector3d.Exclude(o.Up(UT), (o.referenceBody.transform.up.vector * (float)o.referenceBody.radius) - o.SwappedRelativePositionAtUT(UT)).normalized;
        }

        //normalized vector parallel to the planet's surface and pointing in the eastward direction
        public static Vector3d East(this PatchedConicsOrbit o, double UT)
        {
            return Vector3d.Cross(o.Up(UT), o.North(UT));
        }

        //distance from the center of the planet
        public static double Radius(this PatchedConicsOrbit o, double UT)
        {
            // Agrees with KS2
            return o.SwappedRelativePositionAtUT(UT).magnitude;
        }

        //returns a new PatchedConicsOrbit object that represents the result of applying a given dV to o at UT
        public static PatchedConicsOrbit PerturbedOrbit(this PatchedConicsOrbit o, double UT, Vector3d dV)
        {
            // Position Position = new(o.coordinateSystem, OrbitExtensions.SwapYZ(o.SwappedAbsolutePositionAtUT(UT) - o.ReferenceBody.Position.localPosition));
            // Position pos = new(o.coordinateSystem, o.SwappedAbsolutePositionAtUT(UT));
            // Velocity vel = new(o.ReferenceBody.celestialMotionFrame, o.SwappedOrbitalVelocityAtUT(UT) + dV);
            // return MuUtils.OrbitFromStateVectors(o.SwappedAbsolutePositionAtUT(UT), o.SwappedOrbitalVelocityAtUT(UT) + dV, o.coordinateSystem, o.ReferenceBody, UT);

            // From KS2
            // Actual KS2 returns: ReferenceBody.CreateOrbit(RelativePosition(ut), OrbitalVelocity(ut) + dV, ut);
            return o.CreateOrbit(o.SwappedRelativePositionAtUT(UT), o.SwappedOrbitalVelocityAtUT(UT) + dV, UT);

            // KS2 RelativePosition(ut) matches code in SwappedRelativePositionAtUT(UT)
            // KS2 OrbitalVelocity(ut) matches code in SwappedOrbitalVelocityAtUT(UT)

            // If CreateOrbit were embedded here:
            //Vector3d pos = o.SwappedRelativePositionAtUT(UT); // o.ReferenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetRelativePositionAtUTZup(UT).SwapYAndZ)
            //Vector3d vel = o.SwappedOrbitalVelocityAtUT(UT) + dV; // o.ReferenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, o.GetOrbitalVelocityAtUTZup(UT).SwapYAndZ);
            //PatchedConicsOrbit Orbit = new PatchedConicsOrbit(o.ReferenceBody.universeModel);
            //Orbit.UpdateFromStateVectors(new Position(o.ReferenceBody.SimulationObject.transform.celestialFrame, pos), new Velocity(o.ReferenceBody.SimulationObject.transform.celestialFrame.motionFrame, vel), o.ReferenceBody, UT);

            //return Orbit;

            // MJ version is one line:
            // return MuUtils.OrbitFromStateVectors(o.SwappedAbsolutePositionAtUT(UT), o.SwappedOrbitalVelocityAtUT(UT) + dV, o.ReferenceBody, UT);

            // MuUtils.OrbitFromStateVectors calls the KSP2 method UpdateFromStateVectors. The KSP1 version took pos and vel as Vector3d, but the
            // KSP2 version requires these to be type Position and Velocity.
            // pos = o.SwappedAbsolutePositionAtUT(UT);
            // vel = o.SwappedOrbitalVelocityAtUT(UT) + dV;
            // The MJ version of OrbitFromStateVectors peroforms a SwapYZ on (pos - body Position), and a SwapYZ on vel before passing them into
            // the KSP1 version of UpdateFromStateVectors.
            // ret.UpdateFromStateVectors(OrbitExtensions.SwapYZ(pos - body.Position), OrbitExtensions.SwapYZ(vel), body, UT);
        }

        // Adapted from KS2
        public static PatchedConicsOrbit CreateOrbit(this PatchedConicsOrbit o, Vector3d position, Vector3d velocity, double UT)
        {
            PatchedConicsOrbit orbit = new(o.referenceBody.universeModel);

            // Actual KS2 returns: Orbit.UpdateFromStateVectors(new Position(body.SimulationObject.transform.celestialFrame, Position), new Velocity(body.SimulationObject.transform.celestialFrame.motionFrame, velocity), body, ut);
            orbit.UpdateFromStateVectors(new Position(o.referenceBody.SimulationObject.transform.celestialFrame, position), new Velocity(o.referenceBody.SimulationObject.transform.celestialFrame.motionFrame, velocity), o.referenceBody, UT);

            return orbit;
        }

        // returns a new Orbit that is identical to the current one (although the epoch will change)
        // (i tried many different APIs in the Orbit class, but the GetOrbitalStateVectors/UpdateFromStateVectors route was the only one that worked)
        public static PatchedConicsOrbit Clone(this PatchedConicsOrbit o, double UT = Double.NegativeInfinity)
        {
            Vector3d pos, vel;

            // hack up a dynamic default value to the current time
            if (UT == Double.NegativeInfinity)
                UT = GameManager.Instance.Game.UniverseModel.UniversalTime;

            PatchedConicsOrbit newOrbit = new PatchedConicsOrbit(GameManager.Instance.Game.UniverseModel);
            o.GetOrbitalStateVectorsAtUT(UT, out pos, out vel);
            Position position = new Position(o.referenceBody.SimulationObject.transform.celestialFrame, (pos - o.referenceBody.Position.localPosition).SwapYAndZ); //  OrbitExtensions.SwapYZ(pos - o.ReferenceBody.Position.localPosition)
            Velocity velocity = new Velocity(o.referenceBody.SimulationObject.transform.celestialFrame.motionFrame, vel.SwapYAndZ); // OrbitExtensions.SwapYZ(vel)
            newOrbit.UpdateFromStateVectors(position, velocity, o.referenceBody, UT);

            return newOrbit;
        }

        // calculate the next patch, which makes patchEndTransition be valid
        public static PatchedConicsOrbit CalculateNextOrbit(this PatchedConicsOrbit o, double UT = Double.NegativeInfinity)
        {
            // PatchedConicSolver.SolverParameters solverParameters = new PatchedConicSolver.SolverParameters();

            // hack up a dynamic default value to the current time
            if (UT == Double.NegativeInfinity)
                UT = GameManager.Instance.Game.UniverseModel.UniversalTime;

            o.StartUT = UT;
            o.EndUT = o.eccentricity >= 1.0 ? o.period : UT + o.period;
            // PatchedConicsOrbit nextOrbit = new PatchedConicsOrbit(GameManager.Instance.Game.UniverseModel);
            // PatchedConics.CalculatePatch(o, nextOrbit, UT, solverParameters, null); // Maybe this need to be CalculatePatchConicList, or CalculatePatchList ???
            // Assuming nextOrbit can be obtained from o.NextPatch
            PatchedConicsOrbit nextOrbit = o.NextPatch as PatchedConicsOrbit;

            return nextOrbit;
        }

        // This does not allocate a new Orbit object and the caller should call new PatchedConicsOrbit if/when required
        public static void MutatedOrbit(this PatchedConicsOrbit o, double periodOffset = Double.NegativeInfinity)
        {
            double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;

            if (periodOffset.IsFinite())
            {
                Vector3d pos, vel;
                o.GetOrbitalStateVectorsAtUT(UT + o.period * periodOffset, out pos, out vel);
                Position position = new Position(o.referenceBody.SimulationObject.transform.celestialFrame, (pos - o.referenceBody.Position.localPosition).SwapYAndZ); // OrbitExtensions.SwapYZ(pos - o.ReferenceBody.Position.localPosition)
                Velocity velocity = new Velocity(o.referenceBody.SimulationObject.transform.celestialFrame.motionFrame, vel.SwapYAndZ); // OrbitExtensions.SwapYZ(vel)
                o.UpdateFromStateVectors(position, velocity, o.referenceBody, UT);
            }
        }

        //mean motion is rate of increase of the mean anomaly
        public static double MeanMotion(this PatchedConicsOrbit o)
        {
            if (o.eccentricity > 1)
            {
                return Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));
            }
            else
            {
                // The above formula is wrong when using the RealSolarSystem mod, which messes with orbital periods.
                // This simpler formula should be foolproof for elliptical orbits:
                return 2 * Math.PI / o.period;
            }
        }

        //distance between two orbiting objects at a given time
        public static double Separation(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            return (a.SwappedAbsolutePositionAtUT(UT) - b.SwappedAbsolutePositionAtUT(UT)).magnitude;
        }

        //Time during a's next Orbit at which object a comes nearest to object b.
        //If a is hyperbolic, the examined interval is the next 100 units of mean anomaly.
        //This is quite a large segment of the hyperbolic arc. However, for extremely high
        //hyperbolic eccentricity it may not find the actual closest approach.
        public static double NextClosestApproachTime(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            double closestApproachTime = UT;
            double closestApproachDistance = Double.MaxValue;
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
                        closestApproachTime = t;
                    }
                }
                minTime = MuUtils.Clamp(closestApproachTime - dt, UT, UT + interval);
                maxTime = MuUtils.Clamp(closestApproachTime + dt, UT, UT + interval);
            }

            return closestApproachTime;
        }

        //Distance between a and b at the closest approach found by NextClosestApproachTime
        public static double NextClosestApproachDistance(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            return a.Separation(b, a.NextClosestApproachTime(b, UT));
        }

        //The mean anomaly of the Orbit.
        //For elliptical orbits, the value return is always between 0 and 2pi
        //For hyperbolic orbits, the value can be any number.
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
        public static double NextPeriapsisTime(this PatchedConicsOrbit o, double UT)
        {
            if (o.eccentricity < 1)
            {
                return o.TimeOfTrueAnomaly(0, UT);
            }
            else
            {
                return UT - o.MeanAnomalyAtUT(UT) / o.MeanMotion();
            }
        }

        //Returns the next time at which the orbiting object will be at apoapsis.
        //For elliptical orbits, this is a time between UT and UT + period.
        //For hyperbolic orbits, this throws an ArgumentException.
        public static double NextApoapsisTime(this PatchedConicsOrbit o, double UT)
        {
            if (o.eccentricity < 1)
            {
                return o.TimeOfTrueAnomaly(180, UT);
            }
            else
            {
                throw new ArgumentException("OrbitExtensions.NextApoapsisTime cannot be called on hyperbolic orbits");
            }
        }

        //Gives the true anomaly (in a's Orbit) at which a crosses its ascending node
        //with b's Orbit.
        //The returned value is always between 0 and 2 * PI.
        public static double AscendingNodeTrueAnomaly(this PatchedConicsOrbit a, IKeplerOrbit b)  // was Orbit as type for b
        {
            Vector3d vectorToAN = Vector3d.Cross(a.SwappedOrbitNormal(), b.SwappedOrbitNormal());
            return a.TrueAnomalyFromVector(vectorToAN);
        }

        //Gives the true anomaly (in a's Orbit) at which a crosses its descending node
        //with b's Orbit.
        //The returned value is always between 0 and 2 * PI.
        public static double DescendingNodeTrueAnomaly(this PatchedConicsOrbit a, IKeplerOrbit b) // was Orbit as type for b
        {
            return MuUtils.ClampDegrees360(a.AscendingNodeTrueAnomaly(b) + 180.0);
        }

        //Gives the true anomaly at which o crosses the equator going northwards, if o is east-moving,
        //or southwards, if o is west-moving.
        //The returned value is always between 0 and 2 * PI.
        public static double AscendingNodeEquatorialTrueAnomaly(this PatchedConicsOrbit o)
        {
            // was: o.ReferenceBody.transform.up -> o.ReferenceBody.transform.up.vector
            Vector3d vectorToAN = Vector3d.Cross(o.referenceBody.transform.up.vector, o.SwappedOrbitNormal());
            //Vector3d an = Vector3d.Cross(Vector3d.forward, a.orbitFrame.Z);
            //if (Math.Abs(an.sqrMagnitude) < double.Epsilon)
            //    an = Vector3d.right;
            return o.TrueAnomalyFromVector(vectorToAN);
        }

        //Gives the true anomaly at which o crosses the equator going southwards, if o is east-moving,
        //or northwards, if o is west-moving.
        //The returned value is always between 0 and 2 * PI.
        public static double DescendingNodeEquatorialTrueAnomaly(this PatchedConicsOrbit o)
        {
            return MuUtils.ClampDegrees360(o.AscendingNodeEquatorialTrueAnomaly() + 180);
        }

        //For hyperbolic orbits, the true anomaly only takes on values in the range
        // -M < true anomaly < +M for some M. This function computes M.
        public static double MaximumTrueAnomaly(this PatchedConicsOrbit o)
        {
            if (o.eccentricity < 1) return 180;
            else return UtilMath.Rad2Deg * Math.Acos(-1 / o.eccentricity);
        }

        //Returns whether a has an ascending node with b. This can be false
        //if a is hyperbolic and the would-be ascending node is within the opening
        //angle of the hyperbola.
        public static bool AscendingNodeExists(this PatchedConicsOrbit a, PatchedConicsOrbit b)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(a.AscendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
        }

        //Returns whether a has a descending node with b. This can be false
        //if a is hyperbolic and the would-be descending node is within the opening
        //angle of the hyperbola.
        public static bool DescendingNodeExists(this PatchedConicsOrbit a, PatchedConicsOrbit b)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(a.DescendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
        }

        //Returns whether o has an ascending node with the equator. This can be false
        //if o is hyperbolic and the would-be ascending node is within the opening
        //angle of the hyperbola.
        public static bool AscendingNodeEquatorialExists(this PatchedConicsOrbit o)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(o.AscendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
        }

        //Returns whether o has a descending node with the equator. This can be false
        //if o is hyperbolic and the would-be descending node is within the opening
        //angle of the hyperbola.
        public static bool DescendingNodeEquatorialExists(this PatchedConicsOrbit o)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(o.DescendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
        }

        //Returns the vector from the primary to the orbiting body at periapsis
        //Better than using PatchedConicsOrbit.eccVec because that is zero for circular orbits
        public static Vector3d SwappedRelativePositionAtPeriapsis(this PatchedConicsOrbit o)
        {
            // was: o.PeR -> o.Periapsis
            // was: o.LAN -> longitudeOfAscendingNode
            // was: Planetarium.up -> o.ReferenceFrame.up.vector
            // was: Planetarium.right -> o.ReferenceFrame.right.vector
            // was: Quaternion -> QuaternionD
            Vector3d vectorToAN = QuaternionD.AngleAxis(-(float)o.longitudeOfAscendingNode, o.ReferenceFrame.up.vector) * o.ReferenceFrame.right.vector;
            Vector3d vectorToPe = QuaternionD.AngleAxis((float)o.argumentOfPeriapsis, o.SwappedOrbitNormal()) * vectorToAN;
            return o.Periapsis * vectorToPe;
        }

        //Returns the vector from the primary to the orbiting body at apoapsis
        //Better than using -PatchedConicsOrbit.eccVec because that is zero for circular orbits
        public static Vector3d SwappedRelativePositionAtApoapsis(this PatchedConicsOrbit o)
        {
            // was: o.Apr -> o.Apoapsis
            // was: o.LAN -> longitudeOfAscendingNode
            // was: Planetarium.up -> o.ReferenceFrame.up.vector
            // was: Planetarium.right -> o.ReferenceFrame.right.vector
            // was: Quaternion -> QuaternionD
            Vector3d vectorToAN = QuaternionD.AngleAxis(-(float)o.longitudeOfAscendingNode, o.ReferenceFrame.up.vector) * o.ReferenceFrame.right.vector;
            Vector3d vectorToPe = QuaternionD.AngleAxis((float)o.argumentOfPeriapsis, o.SwappedOrbitNormal()) * vectorToAN;
            Vector3d ret = -o.Apoapsis * vectorToPe;
            if (double.IsNaN(ret.x))
            {
                FlightPlanPlugin.Logger.LogError("OrbitExtensions.SwappedRelativePositionAtApoapsis got a NaN result!");
                FlightPlanPlugin.Logger.LogError("o.LAN = " + o.longitudeOfAscendingNode);
                FlightPlanPlugin.Logger.LogError("o.inclination = " + o.inclination);
                FlightPlanPlugin.Logger.LogError("o.argumentOfPeriapsis = " + o.argumentOfPeriapsis);
                FlightPlanPlugin.Logger.LogError("o.GetRelativeOrbitNormal() = " + o.SwappedOrbitNormal());
            }
            return ret;
        }

        //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

        //Converts a direction, specified by a Vector3d, into a true anomaly.
        //The vector is projected into the orbital plane and then the true anomaly is
        //computed as the angle this vector makes with the vector pointing to the periapsis.
        //The returned value is always between 0 and 360.
        public static double TrueAnomalyFromVector(this PatchedConicsOrbit o, Vector3d vec)
        {
            Vector3d oNormal = o.SwappedOrbitNormal();
            Vector3d projected = Vector3d.Exclude(oNormal, vec);
            Vector3d vectorToPe = o.SwappedRelativePositionAtPeriapsis();
            double angleFromPe = Vector3d.Angle(vectorToPe, projected);

            //If the vector points to the infalling part of the Orbit then we need to do 360 minus the
            //angle from Pe to get the true anomaly. Test this by taking the the Cross product of the
            //Orbit normal and vector to the periapsis. This gives a vector that points to center of the
            //outgoing side of the Orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
            //during the infalling part of the Orbit.
            if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(oNormal, vectorToPe))) < 90)
            {
                return angleFromPe;
            }
            else
            {
                return 360 - angleFromPe;
            }
        }

        //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency


        //Originally by Zool, revised by The_Duck
        //Converts a true anomaly into an eccentric anomaly.
        //For elliptical orbits this returns a value between 0 and 2pi
        //For hyperbolic orbits the returned value can be any number.
        //NOTE: For a hyperbolic Orbit, if a true anomaly is requested that does not exist (a true anomaly
        //past the true anomaly of the asymptote) then an ArgumentException is thrown
        public static double GetEccentricAnomalyAtTrueAnomaly(this PatchedConicsOrbit o, double trueAnomaly)
        {
            double e = o.eccentricity;
            trueAnomaly = MuUtils.ClampDegrees360(trueAnomaly);
            trueAnomaly = trueAnomaly * (UtilMath.Deg2Rad);

            if (e < 1) //elliptical orbits
            {
                double cosE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                double sinE = Math.Sqrt(1 - (cosE * cosE));
                if (trueAnomaly > Math.PI) sinE *= -1;

                return MuUtils.ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
            }
            else  //hyperbolic orbits
            {
                double coshE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                if (coshE < 1) throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomaly + " radians is not attained by Orbit with eccentricity " + o.eccentricity);

                double E = MuUtils.Acosh(coshE);
                if (trueAnomaly > Math.PI) E *= -1;

                return E;
            }
        }

        //Originally by Zool, revised by The_Duck
        //Converts an eccentric anomaly into a mean anomaly.
        //For an elliptical Orbit, the returned value is between 0 and 2pi
        //For a hyperbolic Orbit, the returned value is any number
        public static double GetMeanAnomalyAtEccentricAnomaly(this PatchedConicsOrbit o, double E)
        {
            double e = o.eccentricity;
            if (e < 1) //elliptical orbits
            {
                return MuUtils.ClampRadiansTwoPi(E - (e * Math.Sin(E)));
            }
            else //hyperbolic orbits
            {
                return (e * Math.Sinh(E)) - E;
            }
        }

        //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

        //Converts a true anomaly into a mean anomaly (via the intermediate step of the eccentric anomaly)
        //For elliptical orbits, the output is between 0 and 2pi
        //For hyperbolic orbits, the output can be any number
        //NOTE: For a hyperbolic Orbit, if a true anomaly is requested that does not exist (a true anomaly
        //past the true anomaly of the asymptote) then an ArgumentException is thrown
        public static double GetMeanAnomalyAtTrueAnomaly(this PatchedConicsOrbit o, double trueAnomaly)
        {
            return o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly));
        }

        //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

        //NOTE: this function can throw an ArgumentException, if o is a hyperbolic Orbit with an eccentricity
        //large enough that it never attains the given true anomaly
        public static double TimeOfTrueAnomaly(this PatchedConicsOrbit o, double trueAnomaly, double UT)
        {
            //FlightPlanPlugin.Logger.LogDebug($"OrbitExtensions: trueAnomaly: {trueAnomaly}° = {trueAnomaly*UtilMath.Deg2Rad} radians");
            return o.GetUTforTrueAnomaly(trueAnomaly*UtilMath.Deg2Rad, o.period);
            //return o.UTAtMeanAnomaly(o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)), UT);
        }

        //Returns the next time at which a will Cross its ascending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if
        //the ascending node is in the past.
        //NOTE: this function will throw an ArgumentException if a is a hyperbolic Orbit and the "ascending node"
        //occurs at a true anomaly that a does not actually ever attain
        public static double TimeOfAscendingNode(this PatchedConicsOrbit a, IKeplerOrbit b, double UT)
        {
            var UTAN = a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);
            if (UTAN < UT) UTAN += a.period;
            return UTAN;
        }

        //Returns the next time at which a will Cross its descending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if
        //the descending node is in the past.
        //NOTE: this function will throw an ArgumentException if a is a hyperbolic Orbit and the "descending node"
        //occurs at a true anomaly that a does not actually ever attain
        public static double TimeOfDescendingNode(this PatchedConicsOrbit a, IKeplerOrbit b, double UT)
        {
            var UTDN = a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);
            if (UTDN < UT) UTDN += a.period;
            return UTDN;
        }

        //Returns the next time at which the orbiting object will Cross the equator
        //moving northward, if o is east-moving, or southward, if o is west-moving.
        //For elliptical orbits this is a time between UT and UT + o.period.
        //For hyperbolic orbits this can by any time, including a time in the past if the
        //ascending node is in the past.
        //NOTE: this function will throw an ArgumentException if o is a hyperbolic Orbit and the
        //"ascending node" occurs at a true anomaly that o does not actually ever attain.
        public static double TimeOfAscendingNodeEquatorial(this PatchedConicsOrbit o, double UT)
        {
            return o.TimeOfTrueAnomaly(o.AscendingNodeEquatorialTrueAnomaly(), UT);
        }

        //Returns the next time at which the orbiting object will Cross the equator
        //moving southward, if o is east-moving, or northward, if o is west-moving.
        //For elliptical orbits this is a time between UT and UT + o.period.
        //For hyperbolic orbits this can by any time, including a time in the past if the
        //descending node is in the past.
        //NOTE: this function will throw an ArgumentException if o is a hyperbolic Orbit and the
        //"descending node" occurs at a true anomaly that o does not actually ever attain.
        public static double TimeOfDescendingNodeEquatorial(this PatchedConicsOrbit o, double UT)
        {
            return o.TimeOfTrueAnomaly(o.DescendingNodeEquatorialTrueAnomaly(), UT);
        }

        //Computes the period of the phase angle between orbiting objects a and b.
        //This only really makes sense for approximately circular orbits in similar planes.
        //For noncircular orbits the time variation of the phase angle is only "quasiperiodic"
        //and for high eccentricities and/or large relative inclinations, the relative motion is
        //not really periodic at all.
        public static double SynodicPeriod(this PatchedConicsOrbit a, PatchedConicsOrbit b)
        {
            int sign = (Vector3d.Dot(a.SwappedOrbitNormal(), b.SwappedOrbitNormal()) > 0 ? 1 : -1); //detect relative retrograde motion
            return Math.Abs(1.0 / (1.0 / a.period - sign * 1.0 / b.period)); //period after which the phase angle repeats
        }

        //Computes the phase angle between two orbiting objects.
        //This only makes sence if a.ReferenceBody == b.ReferenceBody.
        public static double PhaseAngle(this PatchedConicsOrbit a, PatchedConicsOrbit b, double UT)
        {
            Vector3d normalA = a.SwappedOrbitNormal();
            Vector3d posA = a.SwappedRelativePositionAtUT(UT);
            Vector3d projectedB = Vector3d.Exclude(normalA, b.SwappedRelativePositionAtUT(UT));
            double angle = Vector3d.Angle(posA, projectedB);
            if (Vector3d.Dot(Vector3d.Cross(normalA, posA), projectedB) < 0)
            {
                angle = 360 - angle;
            }
            return angle;
        }

        //Computes the angle between two orbital planes. This will be a number between 0 and 180
        //Note that in the convention used two objects orbiting in the same plane but in
        //opposite directions have a relative inclination of 180 degrees.
        public static double RelativeInclination(this PatchedConicsOrbit a, PatchedConicsOrbit b)
        {
            return Math.Abs(Vector3d.Angle(a.SwappedOrbitNormal(), b.SwappedOrbitNormal()));
        }

        //Finds the next time at which the orbiting object will achieve a given radius
        //from the center of the primary.
        //If the given radius is impossible for this Orbit, an ArgumentException is thrown.
        //For elliptical orbits this will be a time between UT and UT + period
        //For hyperbolic orbits this can be any time. If the given radius will be achieved
        //in the future then the next time at which that radius will be achieved will be returned.
        //If the given radius was only achieved in the past, then there are no guarantees
        //about which of the two times in the past will be returned.
        public static double NextTimeOfRadius(this PatchedConicsOrbit o, double UT, double radius)
        {
            // was o.PeR -> o.Periapsis
            // was o.ApR -> o.Apoapsis
            if (radius < o.Periapsis || (o.eccentricity < 1 && radius > o.Apoapsis)) throw new ArgumentException("OrbitExtensions.NextTimeOfRadius: given radius of " + radius + " is never achieved: o.Periapsis = " + o.Periapsis + " and o.Apoapsis = " + o.Apoapsis);

            double trueAnomaly1 = UtilMath.Rad2Deg * o.TrueAnomalyAtRadius(radius);
            double trueAnomaly2 = 360 - trueAnomaly1;
            double time1 = o.TimeOfTrueAnomaly(trueAnomaly1, UT);
            double time2 = o.TimeOfTrueAnomaly(trueAnomaly2, UT);
            if (time2 < time1 && time2 > UT) return time2;
            else return time1;
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

        // Sadly, this does not work... Maybe with more work it could? It doesn't actually use UT, so to test it you need to get close to
        // the burn point you want, then call both this one and the one above and compare those results to eachother and whatever kludge
        // (flip prograde, flip radial, etc.) to see if this get's you to the answer the kludge provides without needing the kludge.
        //public static Vector3d DeltaVToManeuverNodeCoordinates(this PatchedConicsOrbit o, double UT, VesselComponent vessel, Vector3d dV)
        //{
        //    var frame = vessel.transform.coordinateSystem;
        //    Vector deltaV = new(o.coordinateSystem, dV);
        //    Vector3d burnVec = frame.ToLocalVector(deltaV);
        //    return burnVec;
        //}

        // Return the Orbit of the parent body orbiting the sun
        //public static PatchedConicsOrbit TopParentOrbit(this PatchedConicsOrbit Orbit)
        //{
        //    PatchedConicsOrbit result = Orbit;
        //    while (result.ReferenceBody != Planetarium.fetch.Sun)
        //    {
        //        result = result.ReferenceBody.Orbit; // was: result.ReferenceBody.Orbit
        //    }
        //    return result;
        //}

        public static String MuString(this PatchedConicsOrbit o)
        {
            // was: o.PeR -> Periapsis, should this be PeriapsisArl?
            // was: o.ApR -> Apoapsis, should this be ApoapsisArl?
            // was: o.LAN -> longitudeOfAscendingNode
            // was: o.trueAnomaly -> TrueAnomaly
            return "PeriapsisArl:" + o.PeriapsisArl + " ApoapsisArl:" + o.ApoapsisArl + " SMA:" + o.semiMajorAxis + " ECC:" + o.eccentricity + " INC:" + o.inclination + " LAN:" + o.longitudeOfAscendingNode + " ArgP:" + o.argumentOfPeriapsis + " TA:" + o.TrueAnomaly;
        }

        // used to be SuicideBurnCountdown(Orbit Orbit, VesselState vesselState, Vessel vessel)
        //public static double SuicideBurnCountdown(PatchedConicsOrbit Orbit, VesselComponent vessel)
        //{
        //    if (vessel.mainBody == null) return 0;
        //    if (Orbit.PeriapsisArl > 0) return Double.PositiveInfinity;

        //    double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
        //    double angleFromHorizontal = 90 - Vector3d.Angle(-vessel.SurfaceVelocity.vector, vessel.Orbit.Up(UT));
        //    angleFromHorizontal = MuUtils.Clamp(angleFromHorizontal, 0, 90);
        //    double sine = Math.Sin(angleFromHorizontal * UtilMath.Deg2Rad);
        //    double g = vessel.graviticAcceleration.magnitude;
        //    double T = vesselState.limitedMaxThrustAccel;

        //    double effectiveDecel = 0.5 * (-2 * g * sine + Math.Sqrt((2 * g * sine) * (2 * g * sine) + 4 * (T * T - g * g)));
        //    double decelTime = vessel.SrfSpeedMagnitude / effectiveDecel;

        //    Vector3d estimatedLandingSite = vessel.CenterOfMass.localPosition + 0.5 * decelTime * vessel.SurfaceVelocity.vector;
        //    Position Position = new(Orbit.coordinateSystem, estimatedLandingSite);
        //    double terrainRadius = vessel.mainBody.radius + vessel.mainBody.GetAltitudeFromRadius(Position); // was: vessel.mainBody.TerrainAltitude(estimatedLandingSite)
        //    double impactTime = 0;
        //    try
        //    {
        //        impactTime = Orbit.NextTimeOfRadius(vesselState.time, terrainRadius);
        //    }
        //    catch (ArgumentException)
        //    {
        //        return 0;
        //    }
        //    return impactTime - decelTime / 2 - vesselState.time;
        //}
    }
}
