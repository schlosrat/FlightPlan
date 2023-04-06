# KSP1 to KSP2 Code Conversion Notes
Many KSP2 mods are best developed with a clean sheet approach, but in some cases it may be advantageous to start with old code that was proven to work well in KSP2. This is particularly true where the KSP1 code is mainly or entirely math, like with orbital mechanics, etc. Math is math, and the equations and approches should not gennerally change in most cases. However there are defintiely changes from KSP1 to KSP2 in where certain properties are found!
Since this project is leveraging MechJeb Orbital Mechanics code developed for KSP1 that code needs to be modified so that the good equations and math are fed with the right values so that they'll produce the results needed. This file documents the changes that have been made KSP2-izing the KSP2 MJ code. If you've got a project where you want to adapt some KSP1 code to work in KSP2, then this may help get you started. If you find errors in the assumptions made here, please update this file and share it back!

## General Game Info
Planetarium.GetUniversalTime() -> GameManager.Instance.Game.UniverseModel.UniversalTime
Orbit newOrbit = new Orbit(); -> PatchedConicsOrbit newOrbit = new PatchedConicsOrbit(GameManager.Instance.Game.UniverseModel);
Quaternion -> QuaternionD
Orbit.PatchTransitionType.FINAL -> PatchTransitionType.Final
Orbit.PatchTransitionType.INITIAL -> PatchTransitionType.Initial
PatchedConics.SolverParameters() -> PatchedConicSolver.SolverParameters()

## Orbit Type: Properties and Methods
### Type
* Orbit -> PatchedConicsOrbit

For a PatchedConicsOrbit object called "o"
### Properties
* o.referenceBody.position -> o.referenceBody.Position.localPosition
* o.referenceBody.transform.up -> o.referenceBody.transform.up.vector
* o.referenceBody.transform.right -> o.referenceBody.transform.right.vector
* o.referenceBody.Radius -> o.referenceBody.radius
* o.referenceBody.orbit -> o.referenceBody.Orbit
* o.LAN -> o.longitudeOfAscendingNode
* Planetarium.up -> o.ReferenceFrame.up.vector
* Planetarium.right -> o.ReferenceFrame.right.vector
* o.PeR -> o.Periapsis
* o.PeA -> o.PeriapsisArl
* o.ApR -> o.Apoapsis
* o.ApA -> o.ApoapsisArl
* o.trueAnomaly -> o.TrueAnomaly
* o.patchEndTransition -> o.PatchEndTransition
* o.referenceBody.timeWarpAltitudeLimits [4] -> o.referenceBody.TimeWarpAltitudeOffset*4

### Methods
* o.getOrbitalVelocityAtUT() -> o.GetOrbitalVelocityAtUTZup()
* o.getRelativePositionAtUT() -> o.GetRelativePositionAtUT()
* o.GetOrbitNormal() -> o.GetRelativeOrbitNormal()
* o.referenceBody.GetLatitude() -> o.referenceBody.GetLatLonAltFromRadius()

## Body Type: Properties and Methods
### Type
CelestialBody -> CelestialBodyComponent

## Vessel Type: Properties and Methods

## Coordinate Systems

## Useful Code Block Conversions
### Was
o.GetOrbitalStateVectorsAtUT(UT, out pos, out vel);
newOrbit.UpdateFromStateVectors(pos, vel, o.referenceBody, UT);
### Is
o.GetOrbitalStateVectorsAtUT(UT, out pos, out vel);
KSP.Sim.Position position = new KSP.Sim.Position(o.referenceBody.coordinateSystem, OrbitExtensions.SwapYZ(pos - o.referenceBody.Position.localPosition));
KSP.Sim.Velocity velocity = new KSP.Sim.Velocity(o.referenceBody.relativeToMotion, OrbitExtensions.SwapYZ(vel));
newOrbit.UpdateFromStateVectors(position, velocity, o.referenceBody, UT);
### Was
PatchedConics.SolverParameters solverParameters = new PatchedConics.SolverParameters();
### Is
PatchedConicSolver.SolverParameters solverParameters = new PatchedConicSolver.SolverParameters();
### Was
PatchedConics.CalculatePatch(o, nextOrbit, UT, solverParameters, null);
### Is
nextOrbit = o.NextPatch as PatchedConicsOrbit;
### Was
o.UTAtMeanAnomaly(o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)), UT);
### IS
o.GetUTforTrueAnomaly(trueAnomaly*UtilMath.Deg2Rad, o.period);

## New Code
public static Vector3d DvToBurnVec(PatchedConicsOrbit o, Vector3d dV, double UT)
{
    Vector3d burnVec;
    burnVec.x = Vector3d.Dot(dV, o.RadialPlus(UT));
    burnVec.y = Vector3d.Dot(dV, o.NormalPlus(UT));
    burnVec.z = Vector3d.Dot(dV, -1 * o.Prograde(UT));
    return burnVec;
}

public static Vector3d BurnVecToDv(PatchedConicsOrbit o, Vector3d burnVec, double UT)
{
    return burnVec.x * o.RadialPlus(UT) + burnVec.y * o.NormalPlus(UT) - burnVec.z * o.Prograde(UT);
}