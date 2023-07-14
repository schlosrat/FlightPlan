using KSP.Sim.impl;
using KSP.Sim;
using System;
using KSP.Game;
using FlightPlan;
// using UnityEngine;

namespace MuMech;
public class PatchedConicsExtended
{
  public static bool CalculatePatch(
      PatchedConicsOrbit p,
      PatchedConicsOrbit nextPatch,
      double startEpoch,
      PatchedConicSolver.SolverParameters pars,
      CelestialBodyComponent targetBody)
  {
    try
    {
      p.ActivePatch = true;
      p.NextPatch = nextPatch;
      p.PatchEndTransition = PatchTransitionType.Final;
      p.closestEncounterLevel = EncounterSolutionLevel.None;
      p.numClosePoints = 0;
      List<PatchedConicsOrbit> patchList = FlightPlanPlugin.Instance._activeVessel.Orbiter.PatchedConicSolver.CurrentTrajectory;
      int count = patchList.Count; // Planetarium.Orbits.Count;
      for (int index = 0; index < count; ++index)
      {
        // OrbitDriver is a MonoBehavior whichh seems to correspond to OrbiterComonent in KSP2
        // OrbitDriver orbit = Planetarium.Orbits[index];
        OrbiterComponent orbiter = FlightPlanPlugin.Instance._activeVessel.Orbiter;
        // PatchedConicsOrbitExtended orbit = patchList[index] as PatchedConicsOrbitExtended;
        if (orbiter.PatchedConicsOrbit != p) // was: (orbit.orbit != p)
        {
        label_2:
          switch (5)
          {
            case 0:
              goto label_2;
            default:
              if (false)
              {
                // ISSUE: method reference
                // RuntimeMethodHandle runtimeMethodHandle = __methodref(PatchedConics._CalculatePatch);
              }
              // if ((bool) (UnityEngine.Object) orbit.celestialBody)
              // if ((bool)(Object)orbiter.OrbitTargeter._targetObject.CelestialBody) // was celestialBody ==
              if (orbiter.PatchedConicsOrbit.referenceBody != null)
              {
              label_6:
                switch (7)
                {
                  case 0:
                    goto label_6;
                  default:
                    // if (!((UnityEngine.Object) targetBody == (UnityEngine.Object) null))
                    if (!((Object)targetBody == (Object)null))
                    {
                    label_8:
                      switch (2)
                      {
                        case 0:
                          goto label_8;
                        default:
                          // if ((UnityEngine.Object) orbit.celestialBody == (UnityEngine.Object) targetBody)
                          if ((Object)orbiter.PatchedConicsOrbit.referenceBody == (Object)targetBody) // was celestialBody ==
                          {
                          label_10:
                            switch (7)
                            {
                              case 0:
                                goto label_10;
                            }
                          }
                          else
                            goto label_12;
                          break;
                      }
                    }
                    p.closestTgtApprUT = 0.0;
                  label_12:
                    // if ((UnityEngine.Object) p.referenceBody != (UnityEngine.Object) null)
                    if ((Object)p.referenceBody != (Object)null)
                    {
                    label_13:
                      switch (7)
                      {
                        case 0:
                          goto label_13;
                        default:
                          // if ((UnityEngine.Object) orbit.referenceBody == (UnityEngine.Object) p.referenceBody)
                          if ((Object)orbiter.PatchedConicsOrbit.referenceBody == (Object)p.referenceBody) // was referenceBody ==
                          {
                          label_15:
                            switch (7)
                            {
                              case 0:
                                goto label_15;
                              default:
                                int num = CheckEncounter(p, nextPatch, startEpoch, orbiter, targetBody, pars) ? 1 : 0;
                                continue;
                            }
                          }
                          else
                            continue;
                      }
                    }
                    else
                      continue;
                }
              }
              else
                continue;
          }
        }
      }
    label_19:
      switch (2)
      {
        case 0:
          goto label_19;
        default:
          if (p.PatchEndTransition == PatchTransitionType.Final)
          {
          label_21:
            switch (3)
            {
              case 0:
                goto label_21;
              default:
                if (!pars.debug_disableEscapeCheck)
                {
                label_23:
                  switch (6)
                  {
                    case 0:
                      goto label_23;
                    default:
                      if (p.Apoapsis <= p.referenceBody.sphereOfInfluence) // was ApR
                      {
                      label_25:
                        switch (4)
                        {
                          case 0:
                            goto label_25;
                          default:
                            if (p.eccentricity >= 1.0)
                            {
                            label_27:
                              switch (1)
                              {
                                case 0:
                                  goto label_27;
                              }
                            }
                            else
                            {
                              p.UniversalTimeAtSoiEncounter = -1.0; // was UTsoi
                              p.StartUT = startEpoch;
                              p.EndUT = startEpoch + p.period;
                              p.PatchEndTransition = PatchTransitionType.Final;
                              goto label_33;
                            }
                            break;
                        }
                      }
                      if (double.IsInfinity(p.referenceBody.sphereOfInfluence))
                      {
                      label_29:
                        switch (7)
                        {
                          case 0:
                            goto label_29;
                          default:
                            p.TrueAnomalyFirstEncounterPriOrbit = Math.Acos(-(1.0 / p.eccentricity)); // p.FEVp
                            p.TrueAnomalySecEncounterPriOrbit = -p.TrueAnomalyFirstEncounterPriOrbit; // p.SEVp
                            p.StartUT = startEpoch;
                            p.EndUT = double.PositiveInfinity;
                            p.UniversalTimeAtSoiEncounter = double.PositiveInfinity; // was UTsoi
                            p.PatchEndTransition = PatchTransitionType.Final;
                            break;
                        }
                      }
                      else
                      {
                        p.TrueAnomalyFirstEncounterPriOrbit = p.TrueAnomalyAtRadius(p.referenceBody.sphereOfInfluence); // FEVp
                        p.TrueAnomalySecEncounterPriOrbit = -p.TrueAnomalyFirstEncounterPriOrbit; // SEVp
                        p.timeToTransition1 = p.GetDTforTrueAnomaly(p.TrueAnomalyFirstEncounterPriOrbit, 0.0); // FEVp
                        p.timeToTransition2 = p.GetDTforTrueAnomaly(p.TrueAnomalySecEncounterPriOrbit, 0.0); // SEVp
                        p.UniversalTimeAtSoiEncounter = startEpoch + p.timeToTransition1;
                        nextPatch.UpdateFromOrbitAtUT(p, p.UniversalTimeAtSoiEncounter, p.referenceBody.referenceBody);
                        p.StartUT = startEpoch;
                        p.EndUT = p.UniversalTimeAtSoiEncounter;
                        p.PatchEndTransition = PatchTransitionType.Escape;
                        break;
                      }
                      break;
                  }
                }
                else
                  break;
                break;
            }
          }
        label_33:
          nextPatch.StartUT = p.EndUT;
          PatchedConicsOrbit orbit1 = nextPatch;
          double num1;
          if (nextPatch.eccentricity >= 1.0)
          {
          label_34:
            switch (4)
            {
              case 0:
                goto label_34;
              default:
                num1 = nextPatch.period;
                break;
            }
          }
          else
            num1 = nextPatch.StartUT + nextPatch.period;
          orbit1.EndUT = num1;
          nextPatch.PatchStartTransition = p.PatchEndTransition;
          nextPatch.PreviousPatch = p;
          return p.PatchEndTransition != PatchTransitionType.Final;
      }
    }
    catch (Exception ex)
    {
      if (!Thread.CurrentThread.IsBackground)
      {
      label_39:
        switch (1)
        {
          case 0:
            goto label_39;
          default:
            Console.WriteLine((object)ex);
            break;
        }
      }
      return false;
    }
  }

  public static bool CheckEncounter(
      PatchedConicsOrbit p,
      PatchedConicsOrbit nextPatch,
      double startEpoch,
      OrbiterComponent sec,
      CelestialBodyComponent targetBody,
      PatchedConicSolver.SolverParameters pars,
      bool logErrors = true)
  {
    try
    {
      PatchedConicsOrbit orbit = sec.PatchedConicsOrbit;
      double num1 = 1.1;
      if (true) // GameSettings.ALWAYS_SHOW_TARGET_APPROACH_MARKERS
      {
      label_1:
        switch (2)
        {
          case 0:
            goto label_1;
          default:
            if (false)
            {
              // ISSUE: method reference
              // RuntimeMethodHandle runtimeMethodHandle = __methodref(PatchedConics._CheckEncounter);
            }
            if (!((Object)sec.PatchedConicsOrbit.referenceBody == (Object)targetBody))
            {
            label_5:
              switch (6)
              {
                case 0:
                  goto label_5;
              }
            }
            else
              goto label_9;
            break;
        }
      }
      if (!p.PeApIntersects(orbit, sec.PatchedConicsOrbit.referenceBody.sphereOfInfluence * num1))
      {
      label_7:
        switch (2)
        {
          case 0:
            goto label_7;
          default:
            return false;
        }
      }
    label_9:
      if (p.closestEncounterLevel < EncounterSolutionLevel.OrbitIntersect)
      {
      label_10:
        switch (6)
        {
          case 0:
            goto label_10;
          default:
            p.closestEncounterLevel = EncounterSolutionLevel.OrbitIntersect;
            p.closestEncounterBody = sec.PatchedConicsOrbit.referenceBody;
            break;
        }
      }
      double clEctr1 = p.ClEctr1;
      double clEctr2 = p.ClEctr2;
      double feVp = p.TrueAnomalyFirstEncounterPriOrbit; // FEVp;
      double feVs = p.TrueAnomalyFirstEncounterSecOrbit; // FEVs;
      double seVp = p.TrueAnomalySecEncounterPriOrbit; // SEVp;
      double seVs = p.TrueAnomalySecEncounterSecOrbit; // SEVs;
      int num2 = p.FindClosestPoints(orbit, ref clEctr1, ref clEctr2, ref feVp, ref feVs, ref seVp, ref seVs, 0.0001, pars.maxGeometrySolverIterations, ref pars.GeoSolverIterations);
      if (num2 < 1)
      {
      label_13:
        switch (6)
        {
          case 0:
            goto label_13;
          default:
            if (logErrors)
            {
            label_15:
              switch (2)
              {
                case 0:
                  goto label_15;
                default:
                  if (!Thread.CurrentThread.IsBackground)
                  {
                  label_17:
                    switch (3)
                    {
                      case 0:
                        goto label_17;
                      default:
                        FlightPlanPlugin.Logger.LogDebug("CheckEncounter: failed to find any intercepts at all");
                        break;
                    }
                  }
                  else
                    break;
                  break;
              }
            }
            return false;
        }
      }
      else
      {
        double dtforTrueAnomaly1 = p.GetDTforTrueAnomaly(feVp, 0.0);
        double dtforTrueAnomaly2 = p.GetDTforTrueAnomaly(seVp, 0.0);
        double a = dtforTrueAnomaly1 + startEpoch;
        double b = dtforTrueAnomaly2 + startEpoch;
        if (double.IsInfinity(a))
        {
        label_21:
          switch (3)
          {
            case 0:
              goto label_21;
            default:
              if (double.IsInfinity(b))
              {
              label_23:
                switch (6)
                {
                  case 0:
                    goto label_23;
                  default:
                    if (logErrors)
                    {
                    label_25:
                      switch (1)
                      {
                        case 0:
                          goto label_25;
                        default:
                          if (!Thread.CurrentThread.IsBackground)
                          {
                          label_27:
                            switch (6)
                            {
                              case 0:
                                goto label_27;
                              default:
                                FlightPlanPlugin.Logger.LogDebug("CheckEncounter: both intercept UTs are infinite");
                                break;
                            }
                          }
                          else
                            break;
                          break;
                      }
                    }
                    return false;
                }
              }
              else
                break;
          }
        }
        if (a >= p.StartUT)
        {
        label_31:
          switch (2)
          {
            case 0:
              goto label_31;
            default:
              if (a > p.EndUT)
              {
              label_33:
                switch (3)
                {
                  case 0:
                    goto label_33;
                }
              }
              else
                goto label_39;
              break;
          }
        }
        if (b >= p.StartUT)
        {
        label_35:
          switch (3)
          {
            case 0:
              goto label_35;
            default:
              if (b > p.EndUT)
              {
              label_37:
                switch (3)
                {
                  case 0:
                    goto label_37;
                }
              }
              else
                goto label_39;
              break;
          }
        }
        return false;
      label_39:
        if (b >= a)
        {
        label_40:
          switch (7)
          {
            case 0:
              goto label_40;
            default:
              if (a >= p.StartUT)
              {
              label_42:
                switch (7)
                {
                  case 0:
                    goto label_42;
                  default:
                    if (a > p.EndUT)
                    {
                    label_44:
                      switch (1)
                      {
                        case 0:
                          goto label_44;
                      }
                    }
                    else
                      goto label_46;
                    break;
                }
              }
              else
                break;
              break;
          }
        }
        UtilMath.SwapValues(ref feVp, ref seVp);
        UtilMath.SwapValues(ref feVs, ref seVs);
        UtilMath.SwapValues(ref clEctr1, ref clEctr2);
        UtilMath.SwapValues(ref dtforTrueAnomaly1, ref dtforTrueAnomaly2);
        UtilMath.SwapValues(ref a, ref b);
      label_46:
        if (b >= p.StartUT)
        {
        label_47:
          switch (6)
          {
            case 0:
              goto label_47;
            default:
              if (b <= p.EndUT)
              {
              label_49:
                switch (1)
                {
                  case 0:
                    goto label_49;
                  default:
                    if (double.IsInfinity(b))
                    {
                    label_51:
                      switch (1)
                      {
                        case 0:
                          goto label_51;
                      }
                    }
                    else
                      goto label_53;
                    break;
                }
              }
              else
                break;
              break;
          }
        }
        num2 = 1;
      label_53:
        p.numClosePoints = num2;
        p.TrueAnomalyFirstEncounterPriOrbit = feVp;
        p.TrueAnomalyFirstEncounterSecOrbit = feVs;
        p.TrueAnomalySecEncounterPriOrbit = seVp;
        p.TrueAnomalySecEncounterSecOrbit = seVs;
        p.ClEctr1 = clEctr1;
        p.ClEctr2 = clEctr2;
        p.UniversalTimeAtClosestApproach = a; // UTappr
        if (Math.Min(p.ClEctr1, p.ClEctr2) > sec.PatchedConicsOrbit.referenceBody.sphereOfInfluence)
        {
        label_54:
          switch (3)
          {
            case 0:
              goto label_54;
            default:
              if (!true) // GameSettings.ALWAYS_SHOW_TARGET_APPROACH_MARKERS
              {
              label_56:
                switch (6)
                {
                  case 0:
                    goto label_56;
                  default:
                    if (Thread.CurrentThread.IsBackground)
                    {
                    label_58:
                      switch (1)
                      {
                        case 0:
                          goto label_58;
                      }
                    }
                    else
                      goto label_62;
                    break;
                }
              }
              if ((Object)sec.PatchedConicsOrbit.referenceBody == (Object)targetBody)
              {
              label_60:
                switch (7)
                {
                  case 0:
                    goto label_60;
                  default:
                    p.ClosestApproachDistance = PatchedConics.GetClosestApproach(p, orbit, startEpoch, p.nearestTT * 0.5, pars); // ClAppr
                    p.closestTgtApprUT = p.UniversalTimeAtClosestApproach; // UTappr;
                    break;
                }
              }
            label_62:
              return false;
          }
        }
        else
        {
          if (p.closestEncounterLevel < EncounterSolutionLevel.SoiIntersect1)
          {
          label_64:
            switch (7)
            {
              case 0:
                goto label_64;
              default:
                p.closestEncounterLevel = EncounterSolutionLevel.SoiIntersect1;
                p.closestEncounterBody = sec.PatchedConicsOrbit.referenceBody;
                break;
            }
          }
          p.timeToTransition1 = dtforTrueAnomaly1;
          p.secondaryPosAtTransition1 = orbit.GetTruePositionAtUT(a); // getPositionAtUT
          p.timeToTransition2 = dtforTrueAnomaly2;
          p.secondaryPosAtTransition2 = orbit.GetTruePositionAtUT(b); // getPositionAtUT
          p.nearestTT = p.timeToTransition1;
          p.nextTT = p.timeToTransition2;
          if (double.IsNaN(p.nearestTT))
          {
          label_67:
            switch (7)
            {
              case 0:
                goto label_67;
              default:
                if (!Thread.CurrentThread.IsBackground)
                {
                label_69:
                  switch (4)
                  {
                    case 0:
                      goto label_69;
                    default:
                      if (logErrors)
                      {
                      label_71:
                        switch (7)
                        {
                          case 0:
                            goto label_71;
                          default:
                            FlightPlanPlugin.Logger.LogDebug("nearestTT is NaN! t1: " + p.timeToTransition1 + ", t2: " + p.timeToTransition2 + ", FEVp: " + p.TrueAnomalyFirstEncounterPriOrbit + ", SEVp: " + p.TrueAnomalySecEncounterPriOrbit);
                            break;
                        }
                      }
                      else
                        break;
                      break;
                  }
                }
                else
                  break;
                break;
            }
          }
          p.ClosestApproachDistance = PatchedConics.GetClosestApproach(p, orbit, startEpoch, p.nearestTT * 0.5, pars); // ClAppr

          FlightPlanPlugin.Logger.LogDebug($"calling PatchedConics.EncountersBod:          p == null:{p == null}");
          FlightPlanPlugin.Logger.LogDebug($"calling PatchedConics.EncountersBod:      orbit == null:{orbit == null}");
          FlightPlanPlugin.Logger.LogDebug($"calling PatchedConics.EncountersBod:  nextPatch == null:{nextPatch == null}");
          FlightPlanPlugin.Logger.LogDebug($"calling PatchedConics.EncountersBod:        sec == null:{sec == null}");
          FlightPlanPlugin.Logger.LogDebug($"calling PatchedConics.EncountersBod: startEpoch == null:{startEpoch == null}");
          FlightPlanPlugin.Logger.LogDebug($"calling PatchedConics.EncountersBod:       pars == null:{pars == null}");

          if (PatchedConics.EncountersBody(p, orbit, nextPatch, sec, startEpoch, pars))
          {
          label_74:
            switch (6)
            {
              case 0:
                goto label_74;
              default:
                return true;
            }
          }
          else
          {
            if ((Object)sec.PatchedConicsOrbit.referenceBody == (Object)targetBody)
            {
            label_77:
              switch (5)
              {
                case 0:
                  goto label_77;
                default:
                  p.closestTgtApprUT = p.UniversalTimeAtClosestApproach; //  .UTappr;
                  break;
              }
            }
            return false;
          }
        }
      }
    }
    catch (Exception ex)
    {
      if (!Thread.CurrentThread.IsBackground)
      {
      label_81:
        switch (1)
        {
          case 0:
            goto label_81;
          default:
            Console.WriteLine((object)ex);
            break;
        }
      }
      return false;
    }
  }
}
