
using KSP.Sim.impl;
using UnityEngine;
using FlightPlan.KTools.UI;

namespace FlightPlan;

public class BodySelection
{
    FlightPlanPlugin plugin;
    private bool selecting = false;
    private Vector2 scrollPositionBodies;

    public BodySelection(FlightPlanPlugin main_plugin)
    {
        this.plugin = main_plugin;
    }

    void listSubBodies(CelestialBodyComponent body, int level)
    {
        foreach (CelestialBodyComponent sub in body.orbitingBodies)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(level * 30);
            if (UI_Tools.ListButton(sub.Name))
            {
                selecting = false;
                plugin.activeVessel.SetTargetByID(sub.GlobalId);
                plugin.currentTarget = plugin.activeVessel.TargetObject;
            }

            GUILayout.EndHorizontal();
             listSubBodies(sub, level + 1);
        }
    }

    internal bool listGui()
    {
        if (!selecting)
            return false;

        //bodies = GameManager.Instance.Game.SpaceSimulation.GetBodyNameKeys().ToList();

        CelestialBodyComponent root_body = plugin.activeVessel.mainBody;
        while (root_body.referenceBody != null)
        {
            root_body = root_body.referenceBody;
        }

        // bodies = GameManager.Instance.Game.SpaceSimulation.GetAllObjectsWithComponent<CelestialBodyComponent>();

        GUILayout.BeginHorizontal();
        UI_Tools.Label("Select target ");
        if (UI_Tools.SmallButton("Cancel"))
        {
            selecting = false;
        }
        GUILayout.EndHorizontal();

        UI_Tools.Separator();

        //GUI.SetNextControlName("Select Target");
        scrollPositionBodies = UI_Tools.BeginScrollView(scrollPositionBodies, 300);

        listSubBodies(root_body, 0);

        GUILayout.EndScrollView();

        return true;
    }

    public void BodySelectionGUI()
    {
        string tgtName;
        if (plugin.currentTarget == null)
            tgtName = "None";
        else
            tgtName = plugin.currentTarget.Name;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Target : ");

        if (UI_Tools.SmallButton(tgtName))
            selecting = true;

        GUILayout.EndHorizontal();
    }
}
