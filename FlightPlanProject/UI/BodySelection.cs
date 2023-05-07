
using KSP.Sim.impl;
using UnityEngine;
using FlightPlan.KTools.UI;

namespace FlightPlan;

public class BodySelection
{
    FlightPlanPlugin Plugin;
    private bool selecting = false;
    private Vector2 scrollPositionBodies;

    public BodySelection(FlightPlanPlugin main_plugin)
    {
        this.Plugin = main_plugin;
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
                Plugin._activeVessel.SetTargetByID(sub.GlobalId);
                Plugin._currentTarget = Plugin._activeVessel.TargetObject;
            }

            GUILayout.EndHorizontal();
             listSubBodies(sub, level + 1);
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
        UI_Tools.Label("Select target ");
        if (UI_Tools.SmallButton("Cancel"))
        {
            selecting = false;
        }
        GUILayout.EndHorizontal();

        UI_Tools.Separator();

        //GUI.SetNextControlName("Select Target");
        scrollPositionBodies = UI_Tools.BeginScrollView(scrollPositionBodies, 300);

        listSubBodies(_rootBody, 0);

        GUILayout.EndScrollView();

        return true;
    }

    public void BodySelectionGUI()
    {
        string _tgtName;
        if (Plugin._currentTarget == null)
            _tgtName = "None";
        else
            _tgtName = Plugin._currentTarget.Name;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Target : ");

        if (UI_Tools.SmallButton(_tgtName))
            selecting = true;

        GUILayout.EndHorizontal();
    }
}
