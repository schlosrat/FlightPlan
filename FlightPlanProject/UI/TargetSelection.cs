
using KSP.Sim.impl;
using UnityEngine;
using FlightPlan.KTools.UI;
using KSP.Game;
using KSP.Sim.DeltaV;
using KSP.Iteration.UI.Binding;

namespace FlightPlan;

public class TargetSelection
{
    FlightPlanPlugin Plugin;
    private bool selecting = false, selectingVessel = false;
    private Vector2 scrollPosition, scrollPositionVessels;
    private static List<VesselComponent> allVessels;
    private static List<PartComponent> allPorts;
    private static SimulationObjectModel thisVessel = null;
    public static bool SelectTarget, doNewList;
    public static bool SelectDockingPort = false;

    public TargetSelection(FlightPlanPlugin main_plugin)
    {
        this.Plugin = main_plugin;
    }

    void ListSubBodies(CelestialBodyComponent body, int level)
    {
        foreach (CelestialBodyComponent sub in body.orbitingBodies)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(level * 30);
            if (UI_Tools.ListButton(sub.Name))
            {
                selecting = false;
                Plugin._activeVessel.SetTargetByID(sub.GlobalId);
                Plugin._currentTarget = Plugin._activeVessel.TargetObject;
            }

            GUILayout.EndHorizontal();
            ListSubBodies(sub, level + 1);
        }
    }

    void ListVessels()
    {
        if (SelectDockingPort)
        {
            if (Plugin._currentTarget == null)
            {
                selecting = false;
                return;
            }
            // IF we've not been here before
            if (thisVessel == null)
            {
                thisVessel = Plugin._currentTarget;
                doNewList = true;
            }
            // Make a list of all docking ports on current vessel
            if (Plugin._currentTarget.IsVessel)
            {
                // If we've not made a list for this vessel or we need a new list
                if (Plugin._currentTarget.GlobalId != thisVessel.GlobalId || doNewList)
                {
                    doNewList = false;
                    thisVessel = Plugin._currentTarget;
                    //allPorts = thisVessel.PartOwner.Parts.ToList().Where<PartComponentModule_DockingNode>;
                    //allPorts = thisVessel.PartOwner.Parts.ToList().SelectMany<PartComponentModule_DockingNode>;
                    //allPorts = thisVessel.PartOwner.Parts.ToList().Select<>;
                    allPorts = thisVessel.PartOwner.Parts.ToList();
                    allPorts.RemoveAll(part => !part.IsPartDockingPort(out _));
                }
            }
            else if (Plugin._currentTarget.IsPart)// We've got a part selected
            {
                // The current target is a part (probably a docking port, but not necessarily)
                // Find the vessel this part is in, and make a list of all docking ports on that vessel
               
                if (thisVessel.GlobalId != Plugin._currentTarget.Part.PartOwner.SimulationObject.Vessel.GlobalId)
                {
                    thisVessel = Plugin._currentTarget.Part.PartOwner.SimulationObject; //.Vessel as SimulationObjectModel;
                    allPorts = thisVessel.PartOwner.Parts.ToList();
                    allPorts.RemoveAll(part => !part.IsPartDockingPort(out _));
                }
            }
            else if (allPorts == null)
            {
                // Rebuild the last list of ports
                Plugin._activeVessel.SetTargetByID(thisVessel.GlobalId);
                Plugin._currentTarget = Plugin._activeVessel.TargetObject;
                allPorts = thisVessel.PartOwner.Parts.ToList();
                allPorts.RemoveAll(part => !part.IsPartDockingPort(out _));
            }
            else if (allPorts.Count > 0)
            {
                // Jump back to the last list of ports
                Plugin._activeVessel.SetTargetByID(thisVessel.GlobalId);
                Plugin._currentTarget = Plugin._activeVessel.TargetObject;
            }
            else
            {
                selecting = false;
                return;
            }
        }
        else
        {
            // Make a list of all vessels other than this one
            allVessels = GameManager.Instance.Game.SpaceSimulation.UniverseModel.GetAllVessels();
            allVessels.Remove(Plugin._activeVessel);
            allVessels.RemoveAll(v => v.IsDebris());
        }


        if ((SelectDockingPort && allPorts.Count < 1) || (!SelectDockingPort && allVessels.Count < 1))
        {
            selecting = false;
        }
        else
        {
            if (SelectDockingPort)
            {
                foreach (PartComponent part in allPorts)
                {
                    GUILayout.BeginHorizontal();
                    if (UI_Tools.ListButton(part.Name))
                    {
                        selecting = false;
                        Plugin._activeVessel.SetTargetByID(part.GlobalId);
                        Plugin._currentTarget = Plugin._activeVessel.TargetObject;
                    }

                    GUILayout.EndHorizontal();
                }

            }
            else
            {
                foreach (VesselComponent vessel in allVessels)
                {
                    GUILayout.BeginHorizontal();
                    if (UI_Tools.ListButton(vessel.Name))
                    {
                        selecting = false;
                        Plugin._activeVessel.SetTargetByID(vessel.GlobalId);
                        Plugin._currentTarget = Plugin._activeVessel.TargetObject;
                    }

                    GUILayout.EndHorizontal();
                }
            }
        }
    }

    internal bool ListGUI()
    {
        if (!selecting)
            return false;

        //bodies = GameManager.Instance.Game.SpaceSimulation.GetBodyNameKeys().ToList();

        CelestialBodyComponent _rootBody = Plugin._activeVessel.mainBody;
        while (_rootBody.referenceBody != null)
        {
            _rootBody = _rootBody.referenceBody;
        }

        // bodies = GameManager.Instance.Game.SpaceSimulation.GetAllObjectsWithComponent<CelestialBodyComponent>();

        GUILayout.BeginHorizontal();
        // UI_Tools.Label("Select target ");
        if (SelectDockingPort)
            SelectTarget = UI_Tools.SmallToggleButton(SelectTarget, "Select Celestial", "Select Port", 30);
        else
            SelectTarget = UI_Tools.SmallToggleButton(SelectTarget, "Select Celestial", "Select Vessel", 30);

        if (UI_Tools.SmallButton("Cancel"))
        {
            selecting = false;
            // selectingVessel = false;
        }
        GUILayout.EndHorizontal();

        UI_Tools.Separator();

        //GUI.SetNextControlName("Select Target");
        // scrollPositionBodies = UI_Tools.BeginScrollView(scrollPositionBodies, 300);

        if (SelectTarget)
        {
            scrollPosition = UI_Tools.BeginScrollView(scrollPosition, 300);
            ListVessels();
        }
        else
        {
            scrollPosition = UI_Tools.BeginScrollView(scrollPosition, 300);
            ListSubBodies(_rootBody, 0);
        }

        GUILayout.EndScrollView();

        return true;
    }

    public void TargetSelectionGUI()
    {
        string _tgtName;
        if (Plugin._currentTarget == null)
            _tgtName = "None";
        else
            _tgtName = Plugin._currentTarget.Name;
        GUILayout.BeginHorizontal();
        if (SelectDockingPort)
            SelectTarget = UI_Tools.SmallToggleButton(SelectTarget, "Select Celestial", "Select Port", 30);
        else
            SelectTarget = UI_Tools.SmallToggleButton(SelectTarget, "Select Celestial", "Select Vessel", 30);
        // GUILayout.Label("Target : ");

        if (UI_Tools.SmallButton(_tgtName))
            selecting = true;
            //if (SelectVessel)
            //    selectingVessel = true;
            //else
            //    selectingVessel = true;

        GUILayout.EndHorizontal();
    }
}
