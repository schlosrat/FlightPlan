﻿using FlightPlan;
// using FlightPlan.KTools.UI;
using FPUtilities;
using JetBrains.Annotations;
using KSP.Game;
using KSP.Sim.impl;
using SpaceWarp.API.UI;
using UnityEngine;
using UnityEngine.UIElements;
using static MechJebLib.Utils.Statics;
using Object = UnityEngine.Object;

namespace MuMech
{
  [UsedImplicitly]
  public class OperationAdvancedTransfer : Operation
  {
    public enum Mode
    {
      LimitedTime,
      Porkchop
    }

    private static readonly GameInstance Game = GameManager.Instance.Game;

    private static readonly string[]
        modeNames =
        {
                "Limited time", "Porkchop selection"
                // Localizer.Format("#MechJeb_adv_modeName1"), Localizer.Format("#MechJeb_adv_modeName2")
            }; //"Limited time","Porkchop selection"

    public override string GetName() { return "advanced transfer to another planet"; } // Localizer.Format("#MechJeb_AdvancedTransfer_title"); } //"advanced transfer to another planet"

    private double minDepartureTime;
    private double minTransferTime;
    private double maxDepartureTime;
    private double maxTransferTime;

    public EditableTime maxArrivalTime = new EditableTime();

    private bool includeCaptureBurn;

    public bool guiChanged = false;

    private EditableDouble periapsisHeight = new EditableDouble(0);

    private const double minSamplingStep = 12 * 3600;

    private Mode selectionMode = Mode.Porkchop;
    private int windowWidth = 330; // 290;

    private CelestialBodyComponent lastTargetCelestial;

    public TransferCalculator worker;
    public PlotArea           plot;

    private static Texture2D texture;

    private         bool _draggable = true;
    public override bool Draggable => _draggable;

    private const int porkchop_Height = 200;

    private static GUIStyle progressStyle;

    private string CheckPreconditions(PatchedConicsOrbit o, CelestialBodyComponent target)  // was: MechJebModuleTargetController target
    {
      if (o.eccentricity >= 1)
        return "Initial orbit must not be hyperbolic"; // Localizer.Format("#MechJeb_adv_Preconditions1")

      if (o.Apoapsis >= o.referenceBody.sphereOfInfluence)
        return $"Initial orbit must not escape {o.referenceBody.Name.LocalizeRemoveGender()}'s sphere of influence."; //  Localizer.Format("#MechJeb_adv_Preconditions2");

      if (target == null)
        return "Must select a target for the interplanetary transfer."; // Localizer.Format("#MechJeb_adv_Preconditions3");

      if (o.referenceBody.referenceBody == null)
        return $"Doesn't make sense to plot an interplanetary transfer from an orbit around {o.referenceBody.Name}..LocalizeRemoveGender()";
      // Localizer.Format("#MechJeb_adv_Preconditions4"); //"doesn't make sense to plot an interplanetary transfer from an orbit around <<1>>."

      if (o.referenceBody.referenceBody != target.Orbit.referenceBody)
      {
        if (o.referenceBody == target.Orbit.referenceBody)
          return $"Use regular Hohmann transfer function to intercept another body orbiting {o.referenceBody.Name.LocalizeRemoveGender()}.";
        // Localizer.Format("#MechJeb_adv_Preconditions5", o.referenceBody.displayName.LocalizeRemoveGender()); //"use regular Hohmann transfer function to intercept another body orbiting <<1>>."
        return $"An interplanetary transfer from within {o.referenceBody.Name.LocalizeRemoveGender()}'s sphere of influence must target a body that orbits {target.Name.LocalizeRemoveGender()}'s parent, {o.referenceBody.referenceBody.Name.LocalizeRemoveGender()}";
        // Localizer.Format("#MechJeb_adv_Preconditions6", o.referenceBody.displayName.LocalizeRemoveGender(), o.referenceBody.displayName.LocalizeRemoveGender(),
        // o.referenceBody.referenceBody.displayName.LocalizeRemoveGender()); //"an interplanetary transfer from within <<1>>'s sphere of influence must target a body that orbits <<2>>'s parent,<<3>> "
      }

      if (o.referenceBody == target.Orbit.referenceBody.GetRelevantStar()) //  Planetarium.fetch.Sun)
      {
        return "Use regular Hohmann transfer function to intercept another body orbiting the Sun.";
        // Localizer.Format("#MechJeb_adv_Preconditions7");
      }

      if (target is CelestialBodyComponent && o.referenceBody == target.referenceBody)
      {
        return $"You are already orbiting {target.Name.LocalizeRemoveGender()}"; // Localizer.Format("#MechJeb_adv_Preconditions8", o.referenceBody.Name.); //you are already orbiting <<1>>.

      }

      return null;
    }

    private void ComputeStuff(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was MechJebModuleTargetController target
    {
      ErrorMessage = CheckPreconditions(o, target);
      if (ErrorMessage == null)
        ErrorMessage = "";
      else
        return;

      if (worker != null)
        worker.Stop = true;
      plot = null;

      switch (selectionMode)
      {
        case Mode.LimitedTime:
          worker = new TransferCalculator(o, target.Orbit, universalTime, maxArrivalTime, minSamplingStep, includeCaptureBurn);
          break;
        case Mode.Porkchop:
          if (windowWidth < minWindowWidth)
          {
            FlightPlanPlugin.Logger.LogDebug($"windowWidth = {windowWidth} < {minWindowWidth}! Updating it to minimum acceptable value.");
            windowWidth = minWindowWidth;
          }
          //worker = new AllGraphTransferCalculator(o, target.Orbit, minDepartureTime, maxDepartureTime, minTransferTime,
          //    maxTransferTime, windowWidth, porkchop_Height, includeCaptureBurn);
          worker = new AllGraphTransferCalculator(o, target.Orbit, minDepartureTime, maxDepartureTime, minTransferTime,
              maxTransferTime, windowWidth, porkchop_Height, includeCaptureBurn);
          break;
      }
    }

    public void ComputeTimes(PatchedConicsOrbit o, PatchedConicsOrbit destination, double universalTime)
    {
      if (destination == null || o == null || o.referenceBody.Orbit == null)
        return;

      double synodic_period = o.referenceBody.Orbit.SynodicPeriod(destination);
      // double hohmann_transfer_time = OrbitUtil.GetTransferTime(o.referenceBody.Orbit, destination);
      double hohmann_transfer_time = Math.PI * Math.Sqrt(Math.Pow(o.referenceBody.Orbit.radius + destination.radius, 3.0) / (8.0 * o.referenceBody.Orbit.referenceBody.gravParameter));

      // Both orbit have the same period
      if (double.IsInfinity(synodic_period))
        synodic_period = o.referenceBody.Orbit.period;

      minDepartureTime = universalTime;
      minTransferTime  = 3600;

      maxDepartureTime   = minDepartureTime + synodic_period * 1.5;
      maxTransferTime    = hohmann_transfer_time * 2.0;
      maxArrivalTime.val = synodic_period * 1.5 + hohmann_transfer_time * 2.0;
    }

    private bool layoutSkipped;

    public void DoPorkchopGui(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
    {
      var targetCelestial = target as CelestialBodyComponent;

      FpUiController.MaxArrivalTime.style.display = DisplayStyle.None;
      FpUiController.Computing.style.display = DisplayStyle.None;

      // That mess is why you should not compute anything inside a GUI call
      // TODO : rewrite all that...
      if (worker == null)
      {
        if (Event.current.type == EventType.Layout)
          layoutSkipped = true;
        return;
      }

      if (Event.current.type == EventType.Layout)
        layoutSkipped = false;
      if (layoutSkipped)
        return;

      string dv = " - ";
      string departure = " - ";
      string duration = " - ";
      if (worker.Finished && worker.Computed.GetLength(1) == porkchop_Height)
      {
        // UITK: We never gonna see an EventType.Layout, so do it on Repaint
        if (plot == null && Event.current.type == EventType.Repaint) // was: EventType.Layout
        {
          int width = worker.Computed.GetLength(0);
          int height = worker.Computed.GetLength(1);

          if (texture != null && (texture.width != width || texture.height != height))
          {
            Object.Destroy(texture);
            texture = null;
          }

          if (texture == null)
            texture = new Texture2D(width, height, TextureFormat.RGB24, false);

          Porkchop.RefreshTexture(worker.Computed, texture);
          
          plot = new PlotArea(
              worker.MinDepartureTime,
              worker.MaxDepartureTime,
              worker.MinTransferTime,
              worker.MaxTransferTime,
              texture,
              (xmin, xmax, ymin, ymax) =>
              {
                minDepartureTime = Math.Max(xmin, universalTime);
                maxDepartureTime = xmax;
                minTransferTime  = Math.Max(ymin, 3600);
                maxTransferTime  = ymax;
                guiChanged       = true;
                // GUI.changed   = true;
              });
          plot.SelectedPoint = new[] { worker.BestDate, worker.BestDuration };
        }
      }

      if (plot != null)
      {
        FpUiController.Computing.style.display = DisplayStyle.None;
        int[] point = plot.SelectedPoint;
        if (plot.HoveredPoint != null)
          point = plot.HoveredPoint;

        double p = 0;
        try { p = worker.Computed[point[0], point[1]]; }
        catch (Exception ex) { FlightPlanPlugin.Logger.LogError($"Suppressed {ex}: point = [{point[0]},{point[1]}], worker.Computed is [{worker.Computed.GetLength(0)},{worker.Computed.GetLength(1)}]"); }

        if (p > 0)
        {
          dv = p.ToSI() + "m/s";
          if (worker.DateFromIndex(point[0]) < Game.UniverseModel.UniversalTime) // Planetarium.GetUniversalTime())
            departure = "any time now"; // Localizer.Format("#MechJeb_adv_label1")
          else
            departure = FPUtility.SecondsToTimeString(worker.DateFromIndex(point[0]) - Game.UniverseModel.UniversalTime, false, false, true); // was: GuiUtils.TimeToDHMS()
          duration = FPUtility.SecondsToTimeString(worker.DurationFromIndex(point[1]), false, false, true); // was: GuiUtils.TimeToDHMS()
          FpUiController.DepartureTimeLabel.text = departure;
          FpUiController.TransitDurationTimeLabel.text = duration;
        }

        plot.DoGUI();
        guiChanged = false;
        if (!plot.Draggable) _draggable = false;
      }
      else
      {
        if (progressStyle == null)
          progressStyle = new GUIStyle()
          {
            font      = Skins.ConsoleSkin.font, // GuiUtils.skin.font,
            fontSize  = Skins.ConsoleSkin.label.fontSize, // GuiUtils.skin.label.fontSize,
            fontStyle = Skins.ConsoleSkin.label.fontStyle, // GuiUtils.skin.label.fontStyle,
            normal    = { textColor = Skins.ConsoleSkin.label.normal.textColor } //GuiUtils.skin.label.normal.textColor }
          };
        FpUiController.Computing.style.display = DisplayStyle.Flex;
        FpUiController.Computing.text = $"Computing: {worker.Progress}%";
        // GUILayout.Box("Computing: " + worker.Progress + "%", progressStyle, GUILayout.Width(windowWidth), // Localizer.Format("#MechJeb_adv_computing")
        //   GUILayout.Height(porkchop_Height)); //"Computing:"
      }

      // GUILayout.BeginHorizontal();
      // GUILayout.Label("ΔV: " + dv);
      FpUiController.XferDeltaVLabel.text = dv;
      // GUILayout.FlexibleSpace();
      // if (GUILayout.Button("Reset", GuiUtils.yellowOnHover)) // Localizer.Format("#MechJeb_adv_reset_button")
      //   ComputeTimes(o, target.Orbit, universalTime);
      // GUILayout.EndHorizontal();

      // includeCaptureBurn = GUILayout.Toggle(includeCaptureBurn, "Include Capture Burn"); // Localizer.Format("#MechJeb_adv_captureburn")
      // TOFO: Make UITK GUI with this toggle
      // includeCaptureBurn = DrawSoloToggle("Include Capture Burn", includeCaptureBurn); // Localizer.Format("#MechJeb_adv_captureburn")
      includeCaptureBurn = false;

      // fixup the default value of the periapsis if the target changes
      if (targetCelestial != null && lastTargetCelestial != targetCelestial)
      {
        if (targetCelestial.hasAtmosphere)
        {
          FpUiController.TargetAdvXferPe_m = Math.Max(targetCelestial.atmosphereDepth + 10000, FpUiController.TargetAdvXferPe_m);
          FpUiController.AdvXferPeriapsisInput.value = (FpUiController.TargetAdvXferPe_m / 1000).ToString();
        }
        else
        {
          FpUiController.TargetAdvXferPe_m = Math.Max(100000, FpUiController.TargetAdvXferPe_m);
          FpUiController.AdvXferPeriapsisInput.value = (FpUiController.TargetAdvXferPe_m / 1000).ToString();
        }
      }

      // GuiUtils.SimpleTextBox("Periapsis", periapsisHeight, "km"); // Localizer.Format("#MechJeb_adv_periapsis")
      // TODO: Build UITK GUI that includes these this input text field
      // periapsisHeight = DrawEntryTextField("Periapsis", periapsisHeight, "km");
      // periapsisHeight = 100;
      //GUILayout.BeginHorizontal();
      //GUILayout.Label("Select: "); // Localizer.Format("#MechJeb_adv_label2")
      //GUILayout.FlexibleSpace();
      //if (GUILayout.Button("Lowest ΔV")) // Localizer.Format("#MechJeb_adv_button1")
      //{
      //  plot.SelectedPoint = new[] { worker.BestDate, worker.BestDuration };
      //  GUI.changed = false;
      //}

      //if (GUILayout.Button("ASAP")) // Localizer.Format("#MechJeb_adv_button2")
      //{
      //  int bestDuration = 0;
      //  for (int i = 1; i < worker.Computed.GetLength(1); i++)
      //  {
      //    if (worker.Computed[0, bestDuration] > worker.Computed[0, i])
      //      bestDuration = i;
      //  }

      //  plot.SelectedPoint = new[] { 0, bestDuration };
      //  GUI.changed = false;
      //}

      //GUILayout.EndHorizontal();

      // TODO: Build UITK GUI that includes these two entries
      // DrawEntry("Departure in ", departure, " ");
      // DrawEntry("Transit duration ", duration, " ");

      // GUILayout.Label("Departure in" + " " + departure); // Localizer.Format("#MechJeb_adv_label3")
      // GUILayout.Label("Transit duration" + " " + duration);  // Localizer.Format("#MechJeb_adv_label4")

      lastTargetCelestial = targetCelestial;
    }

    int minWindowWidth = 330; // 290;

    public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target, Mode selectionMode) // was: MechJebModuleTargetController target
    {
      _draggable = true;
      if (worker != null && target == null && Event.current.type == EventType.Layout) // was: !target.NormalTargetExists
      {
        worker.Stop = true;
        worker      = null;
        plot        = null;
      }

      // selectionMode = (Mode)GuiUtils.ComboBox.Box((int)selectionMode, modeNames, this);
      if (Event.current.type == EventType.Repaint)
        windowWidth = minWindowWidth; // FlightPlanPlugin.Instance.windowWidth; //  (int)GUILayoutUtility.GetLastRect().width;

      switch (selectionMode)
      {
        case Mode.LimitedTime:
          FpUiController.MaxArrivalTime.style.display = DisplayStyle.Flex;
          FpUiController.MaxArrivalTime.text = $"Max Arrival Time: {maxArrivalTime.text}";
          // GuiUtils.SimpleTextBox("Max Arrival Time", maxArrivalTime, null, 175); // Localizer.Format("#MechJeb_adv_label5")
          if (worker != null && !worker.Finished)
          {
            FpUiController.Computing.style.display = DisplayStyle.Flex;
            FpUiController.Computing.text = $"Computing: {worker.Progress}%";
            // GuiUtils.SimpleLabel("Computing: " + worker.Progress + "%"); // Localizer.Format("#MechJeb_adv_computing")
          }
          else
            FpUiController.Computing.style.display = DisplayStyle.None;
          break;
        case Mode.Porkchop:
          windowWidth = minWindowWidth;
          DoPorkchopGui(o, universalTime, target);
          break;
      }

      if (worker == null || worker.DestinationOrbit != target.Orbit || worker.OriginOrbit != o)
        ComputeTimes(o, target.Orbit, universalTime);

      // if (GUI.changed || worker == null || worker.DestinationOrbit != target.Orbit || worker.OriginOrbit != o)
      if (guiChanged || worker == null || worker.DestinationOrbit != target.Orbit || worker.OriginOrbit != o)
        ComputeStuff(o, universalTime, target);
    }

    protected override List<ManeuverParameters> MakeNodesImpl(PatchedConicsOrbit o, double UT, CelestialBodyComponent target) // was: MechJebModuleTargetController target
    {
      // Check preconditions
      string message = CheckPreconditions(o, target);
      if (message != null)
        throw new OperationException(message);

      // Check if computation is finished
      if (worker != null && !worker.Finished)
        throw new OperationException("Computation not finished"); // Localizer.Format("#MechJeb_adv_Exception1")
      if (worker == null)
      {
        ComputeStuff(o, UT, target);
        throw new OperationException("Started computation"); // Localizer.Format("#MechJeb_adv_Exception2")
      }

      if (worker.ArrivalDate < 0)
      {
        throw new OperationException("Computation failed"); // Localizer.Format("#MechJeb_adv_Exception3")
      }

      double target_PeR = lastTargetCelestial.radius + FpUiController.TargetAdvXferPe_m;

      if (selectionMode == Mode.Porkchop)
      {
        if (plot == null || plot.SelectedPoint == null)
          throw new OperationException("Invalid point selected"); // Localizer.Format("#MechJeb_adv_Exception4")
        return worker.OptimizeEjection(
            worker.DateFromIndex(plot.SelectedPoint[0]),
            o, target as CelestialBodyComponent,
            worker.DateFromIndex(plot.SelectedPoint[0]) + worker.DurationFromIndex(plot.SelectedPoint[1]),
            UT, target_PeR, includeCaptureBurn);
      }

      return worker.OptimizeEjection(
          worker.DateFromIndex(worker.BestDate),
          o, target as CelestialBodyComponent,
          worker.DateFromIndex(worker.BestDate) + worker.DurationFromIndex(worker.BestDuration),
          UT, target_PeR, includeCaptureBurn);
    }

    //public bool DrawSoloToggle(string toggleStr, bool toggle, bool error = false)
    //{
    //    GUILayout.Space(FPStyles.SpacingAfterSection);
    //    GUILayout.BeginHorizontal();
    //    if (error)
    //    {
    //        GUILayout.Toggle(toggle, toggleStr, KBaseStyle.ToggleError);
    //        toggle = false;
    //    }
    //    else
    //        toggle = GUILayout.Toggle(toggle, toggleStr, KBaseStyle.Toggle);
    //    GUILayout.FlexibleSpace();
    //    GUILayout.EndHorizontal();
    //    GUILayout.Space(-FPStyles.SpacingAfterSection);
    //    return toggle;
    //}

    //public void DrawEntry(string entryName, string value = "", string unit = "")
    //{
    //    GUILayout.BeginHorizontal();
    //    UI_Tools.Label(entryName);
    //    if (value.Length > 0)
    //    {
    //        GUILayout.FlexibleSpace();
    //        UI_Tools.Label(value);
    //        if (unit.Length > 0)
    //        {
    //            GUILayout.Space(5);
    //            UI_Tools.Label(unit);
    //        }
    //    }
    //    GUILayout.EndHorizontal();
    //    GUILayout.Space(FPStyles.SpacingAfterEntry);
    //}

    //public double DrawEntryTextField(string entryName, double value, string unit = "", GUIStyle thisStyle = null)
    //{
    //    if (!UI_Fields.InputFields.Contains(entryName))
    //        UI_Fields.InputFields.Add(entryName);

    //    GUILayout.BeginHorizontal();
    //    if (thisStyle != null)
    //        UI_Tools.Label(entryName, KBaseStyle.Label); // NameLabelStyle
    //    else
    //        UI_Tools.Label(entryName);
    //    // UI_Tools.Label(entryName, thisStyle ?? KBaseStyle.NameLabelStyle);
    //    GUILayout.FlexibleSpace();
    //    GUI.SetNextControlName(entryName);
    //    value = UI_Fields.DoubleField(entryName, value, thisStyle ?? KBaseStyle.TextInputStyle);
    //    GUILayout.Space(3);
    //    if (thisStyle != null)
    //        UI_Tools.Label(unit, thisStyle); // , KBaseStyle.UnitLabelStyle
    //    else
    //        UI_Tools.Label(unit);
    //    // UI_Tools.Label(unit, thisStyle ?? KBaseStyle.UnitLabelStyle);
    //    GUILayout.EndHorizontal();
    //    GUILayout.Space(FPStyles.SpacingAfterTallEntry);
    //    return value;
    //}
  }
}
