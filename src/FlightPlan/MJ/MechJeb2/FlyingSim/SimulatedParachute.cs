using System;
using KSP.Modules;
using KSP.Sim;
// using Smooth.Pools;
using UnityEngine;
using UtilScripts;

namespace MuMech
{
    public class SimulatedParachute : SimulatedPart
    {
        private Module_Parachute para;

        private Data_Parachute.DeploymentStates state; // was (KSP1): ModuleParachute.deploymentStates state;

        private double openningTime;

        // Store some data about when and how the parachute opened during the simulation which will be useful for debugging
        //double activatedASL = 0;
        //double activatedAGL = 0;
        //double semiDeployASL = 0;
        //double semiDeployAGL = 0;
        //double fullDeployASL = 0;
        //double fullDeployAGL = 0;
        //double targetASLAtSemiDeploy = 0;
        //double targetASLAtFullDeploy = 0;
        private float deployLevel;
        public  bool  deploying;

        private bool willDeploy;

        private static readonly Pool<SimulatedParachute> pool = new Pool<SimulatedParachute>(Create, Reset);

        public new static int PoolSize => pool.Size;

        private static SimulatedParachute Create()
        {
            return new SimulatedParachute();
        }

        public override void Release()
        {
            foreach (DragCube cube in Cubes)
            {
                DragCubePool.Instance.ReleaseInstance(cube);
            }

            pool.ReleaseInstance(this); // was (KSP1): Release(this);
        }

        private static void Reset(SimulatedParachute obj)
        {
        }

        public static SimulatedParachute Borrow(Module_Parachute mp, ReentrySimulation.SimCurves simCurve, double startTime, int limitChutesStage)
        {
            SimulatedParachute part = pool.FetchInstance(); // was (KSP1): Borrow()
            // FIX ME! Needs Init from SimulatedParts.cs
            // part.Init(mp.part, simCurve);
            part.Init(mp, startTime, limitChutesStage);
            return part;
        }

        private void Init(Module_Parachute mp, double startTime, int limitChutesStage)
        {
            para  = mp;
            state = mp.dataParachute.deployState.GetValue();

            // FIX ME! Need to locate a KSP2 equivalent for para.part.inverseStage
            // willDeploy = limitChutesStage != -1 && para.part.inverseStage >= limitChutesStage;

            // Work out when the chute was put into its current state based on the current drag as compared to the stowed, semi deployed and fully deployed drag

            double timeSinceDeployment = 0;

            switch (mp.dataParachute.deployState.GetValue())
            {
                case Data_Parachute.DeploymentStates.SEMIDEPLOYED:
                    if (mp._animationIsStarted) // was mp.Anim.isPlaying
                        timeSinceDeployment = mp.animator.playbackTime; // .Anim[mp.dataParachute.semiDeploymentSpeed].time; // semiDeployedAnimation
                    else
                        timeSinceDeployment = 10000000;
                    break;

                case Data_Parachute.DeploymentStates.DEPLOYED:
                    if (mp._animationIsStarted) // was mp.Anim.isPlaying
                        timeSinceDeployment = mp.animator.playbackTime; // .Anim[mp.fullyDeployedAnimation].time;
                    else
                        timeSinceDeployment = 10000000;
                    break;

                case Data_Parachute.DeploymentStates.STOWED:
                case Data_Parachute.DeploymentStates.ARMED: // Was (KSP1): ACTIVE
                    // If the parachute is stowed then for some reason para.parachuteDrag does not reflect the stowed drag. set this up by hand. 
                    timeSinceDeployment = 10000000;
                    break;

                default:
                    // otherwise set the time since deployment to be a very large number to indcate that it has been in that state for a long time (although we do not know how long!
                    timeSinceDeployment = 10000000;
                    break;
            }

            openningTime = startTime - timeSinceDeployment;

            //Debug.Log("Parachute " + para.name + " parachuteDrag:" + this.parachuteDrag + " stowedDrag:" + para.stowedDrag + " semiDeployedDrag:" + para.semiDeployedDrag + " fullyDeployedDrag:" + para.fullyDeployedDrag + " part.maximum_drag:" + para.part.maximum_drag + " part.minimum_drag:" + para.part.minimum_drag + " semiDeploymentSpeed:" + para.semiDeploymentSpeed + " deploymentSpeed:" + para.deploymentSpeed + " deploymentState:" + para.deploymentState + " timeSinceDeployment:" + timeSinceDeployment);
            // Keep that test code until they fix the bug in the new parachute module
            //if ((realDrag / parachuteDrag) > 1.01d || (realDrag / parachuteDrag) < 0.99d)
            //    Debug.Log("Parachute " + para.name + " parachuteDrag:" + this.parachuteDrag.ToString("F3") + " RealDrag:" + realDrag.ToString("F3") + " MinDrag:" + para.part.minimum_drag.ToString("F3") + " MaxDrag:" + para.part.maximum_drag.ToString("F3"));
        }

        public override Vector3d Drag(Vector3d vesselVelocity, double dragFactor, float mach)
        {
            if (state != Data_Parachute.DeploymentStates.SEMIDEPLOYED && state != Data_Parachute.DeploymentStates.DEPLOYED)
                return base.Drag(vesselVelocity, dragFactor, mach);

            return Vector3d.zero;
        }

        // Consider activating, semi deploying or deploying a parachute, but do not actually make any changes. returns true if the state has changed
        public override bool SimulateAndRollback(double altATGL, double altASL, double endASL, double pressure, double shockTemp, double time,
            double semiDeployMultiplier)
        {
            if (!willDeploy)
                return false;

            bool stateChanged = false;
            switch (state)
            {
                case Data_Parachute.DeploymentStates.STOWED:
                    if (altATGL < semiDeployMultiplier * para.dataParachute.deployAltitude.GetValue() && shockTemp * para.dataParachute.MachHeatBaseMultiplier < para.dataParachute.chuteMaxTemp * para.dataParachute.SafetyMultiplier)
                    {
                        stateChanged = true;
                    }

                    break;
                case Data_Parachute.DeploymentStates.ARMED: // was (KSP1): ACTIVE
                    if (pressure >= para.dataParachute.minAirPressureToOpen.GetValue())
                    {
                        stateChanged = true;
                    }

                    break;
                case Data_Parachute.DeploymentStates.SEMIDEPLOYED:
                    if (altATGL < para.dataParachute.deployAltitude.GetValue())
                    {
                        stateChanged = true;
                    }

                    break;
            }

            return stateChanged;
        }

        // Consider activating, semi deploying or deploying a parachute. returns true if the state has changed
        public override bool Simulate(double altATGL, double altASL, double endASL, double pressure, double shockTemp, double time,
            double semiDeployMultiplier)
        {
            if (!willDeploy)
                return false;
            switch (state)
            {
                case Data_Parachute.DeploymentStates.STOWED:
                    if (altATGL < semiDeployMultiplier * para.dataParachute.deployAltitude.GetValue() && shockTemp * para.dataParachute.MachHeatBaseMultiplier < para.dataParachute.chuteMaxTemp * para.dataParachute.SafetyMultiplier)
                    {
                        state = Data_Parachute.DeploymentStates.ARMED; // ACTIVE;
                        //activatedAGL = altATGL;
                        //activatedASL = altASL;
                        // Immediately check to see if the parachute should be semi deployed, rather than waiting for another iteration.
                        if (pressure >= para.dataParachute.minAirPressureToOpen.GetValue())
                        {
                            state        = Data_Parachute.DeploymentStates.SEMIDEPLOYED;
                            openningTime = time;
                            //semiDeployAGL = altATGL;
                            //semiDeployASL = altASL;
                            //targetASLAtSemiDeploy = endASL;
                        }
                    }

                    break;
                case Data_Parachute.DeploymentStates.ARMED: // ACTIVE:
                    if (pressure >= para.dataParachute.minAirPressureToOpen.GetValue())
                    {
                        state        = Data_Parachute.DeploymentStates.SEMIDEPLOYED;
                        openningTime = time;
                        //semiDeployAGL = altATGL;
                        //semiDeployASL = altASL;
                        //targetASLAtSemiDeploy = endASL;
                    }

                    break;
                case Data_Parachute.DeploymentStates.SEMIDEPLOYED:
                    if (altATGL < para.dataParachute.deployAltitude.GetValue())
                    {
                        state        = Data_Parachute.DeploymentStates.DEPLOYED;
                        openningTime = time;
                        //fullDeployAGL = altATGL;
                        //fullDeployASL = altASL;
                        //targetASLAtFullDeploy = endASL;
                    }

                    break;
            }

            // Now that we have potentially changed states calculate the current drag or the parachute in whatever state (or transition to a state) that it is in.
            float normalizedTime;
            // Depending on the state that we are in consider if we are part way through a deployment.
            if (state == Data_Parachute.DeploymentStates.SEMIDEPLOYED)
            {
                normalizedTime = (float)Math.Min((time - openningTime) / para.dataParachute.semiDeploymentSpeed, 1);
            }
            else if (state == Data_Parachute.DeploymentStates.DEPLOYED)
            {
                normalizedTime = (float)Math.Min((time - openningTime) / para.dataParachute.deploymentSpeed, 1);
            }
            else
            {
                normalizedTime = 1;
            }

            // Are we deploying in any way? We know if we are deploying or not if normalized time is less than 1
            if (normalizedTime < 1)
            {
                deploying = true;
            }
            else
            {
                deploying = false;
            }

            // If we are deploying or semi deploying then use Lerp to replicate the way the game increases the drag as we deploy.
            if (deploying && (state == Data_Parachute.DeploymentStates.SEMIDEPLOYED || state == Data_Parachute.DeploymentStates.DEPLOYED))
            {
                deployLevel = Mathf.Pow(normalizedTime, para.dataParachute.deploymentSpeed); // was para.dataParachute.deploymentCurve
            }
            else
            {
                deployLevel = 1;
            }

            switch (state)
            {
                case Data_Parachute.DeploymentStates.STOWED:
                case Data_Parachute.DeploymentStates.ARMED: // ACTIVE
                case Data_Parachute.DeploymentStates.CUT:
                    SetCubeWeight("PACKED", 1f);
                    SetCubeWeight("SEMIDEPLOYED", 0f);
                    SetCubeWeight("DEPLOYED", 0f);
                    break;

                case Data_Parachute.DeploymentStates.SEMIDEPLOYED:
                    SetCubeWeight("PACKED", 1f - deployLevel);
                    SetCubeWeight("SEMIDEPLOYED", deployLevel);
                    SetCubeWeight("DEPLOYED", 0f);
                    break;

                case Data_Parachute.DeploymentStates.DEPLOYED:
                    SetCubeWeight("PACKED", 0f);
                    SetCubeWeight("SEMIDEPLOYED", 1f - deployLevel);
                    SetCubeWeight("DEPLOYED", deployLevel);
                    break;
            }

            return deploying;
        }
    }
}
