using FlightPlan;
using FPUtilities;
using JetBrains.Annotations;
using KSP.Game;
using KSP.Sim.impl;
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
            None,
            LimitedTime,
            Porkchop
        }

        private static readonly GameInstance Game = GameManager.Instance.Game;

        private static readonly string[]
            modeNames =
            {
                "None",
                "Limited time",
                "Porkchop selection"
                // Localizer.Format("#MechJeb_adv_modeName1"), Localizer.Format("#MechJeb_adv_modeName2")
            }; //"Limited time","Porkchop selection"

        public override string GetName() { return "advanced transfer to another planet"; } // Localizer.Format("#MechJeb_AdvancedTransfer_title"); } //"advanced transfer to another planet"

        private double minDepartureTime;
        private double minTransferTime;
        private double maxDepartureTime;
        private double maxTransferTime;

        // public EditableTime maxArrivalTime = new EditableTime();

        // private bool includeCaptureBurn;

        public bool guiChanged = false;
        public bool doReset = false;
        public bool doSetLowestDv = false;
        public bool doSetASAP = false;

        // private EditableDouble periapsisHeight = new EditableDouble(0);

        private const double minSamplingStep = 12 * 3600;

        private Mode selectionMode = Mode.Porkchop;
        private Mode previousSelectiionMode = Mode.None;
        private CelestialBodyComponent previousTarget = null;
        private int windowWidth = 330;
        // int minWindowWidth = 330;

        private CelestialBodyComponent lastTargetCelestial;

        public TransferCalculator worker;
        public PlotArea plot;

        private static Texture2D texture;

        private bool _draggable = true;
        public override bool Draggable => _draggable;

        private const int porkchop_Height = 200;

        // private static GUIStyle progressStyle;

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
                    FlightPlanPlugin.Logger.LogDebug($"ComputeStuff: UT: {universalTime:N0}, MaxArrivalTime_s: {FpUiController.MaxArrivalTime_s}, minSamplingStep: {minSamplingStep}, Include Capture Burn: {FpUiController.CaptureBurnToggle.value}");
                    worker = new TransferCalculator(o, target.Orbit, universalTime, FpUiController.MaxArrivalTime_s, minSamplingStep, FpUiController.CaptureBurnToggle.value);
                    break;
                case Mode.Porkchop:
                    FlightPlanPlugin.Logger.LogDebug($"ComputeStuff: minDepartureTime: {minDepartureTime:N0}, maxDepartureTime: {maxDepartureTime:N0}, minTransferTime: {minTransferTime:N0}, maxTransferTime: {maxTransferTime:N0}, windowWidth: {windowWidth}, porkchop_Height: {porkchop_Height}, Include Capture Burn: {FpUiController.CaptureBurnToggle.value}");
                    worker = new AllGraphTransferCalculator(o, target.Orbit, minDepartureTime, maxDepartureTime, minTransferTime,
                        maxTransferTime, windowWidth, porkchop_Height, FpUiController.CaptureBurnToggle.value);
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
            minTransferTime = 3600;

            maxDepartureTime = minDepartureTime + synodic_period * 1.5;
            maxTransferTime = hohmann_transfer_time * 2.0;
            FpUiController.MaxArrivalTimeInput.value = FPUtility.SecondsToTimeString(synodic_period * 1.5 + hohmann_transfer_time * 2.0);
        }

        // private bool layoutSkipped;

        public void DoPorkchopGui(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target) // was: MechJebModuleTargetController target
        {
            var targetCelestial = target as CelestialBodyComponent;

            // FpUiController.LimitedTimeGroup.style.display = DisplayStyle.None;
            FpUiController.Computing.style.display = DisplayStyle.None;
            // FpUiController.PorkchopGroup.style.display = DisplayStyle.Flex;

            // If the Reset Button has been pressed
            if (doReset) // Localizer.Format("#MechJeb_adv_reset_button")
            {
                FlightPlanPlugin.Logger.LogDebug($"DoPorkchopGui: doReset = {doReset}");
                ComputeTimes(o, target.Orbit, universalTime);
                doReset = false;
                worker = null;
            }

            // If the Lowest Dv Button has been pressed
            if (doSetLowestDv) // Localizer.Format("#MechJeb_adv_button1")
            {
                FlightPlanPlugin.Logger.LogDebug($"DoPorkchopGui: doSetLowestDv = {doSetLowestDv}");
                if (plot != null)
                {
                    plot.SelectedPoint = new[] { worker.BestDate, worker.BestDuration };
                    guiChanged = false;
                    GUI.changed = false;
                    doSetLowestDv = false;
                }
            }

            // If the ASAP Button has been pressed
            if (doSetASAP) // Localizer.Format("#MechJeb_adv_button2")
            {
                FlightPlanPlugin.Logger.LogInfo($"DoPorkchopGui: doSetASAP = {doSetASAP}");

                int bestDuration = 0;
                for (int i = 1; i < worker.Computed.GetLength(1); i++)
                {
                    if (worker.Computed[0, bestDuration] > worker.Computed[0, i])
                        bestDuration = i;
                }

                plot.SelectedPoint = new[] { 0, bestDuration };
                GUI.changed = false;
                guiChanged = false;
                doSetASAP = false;
            }

            // includeCaptureBurn = FpUiController.CaptureBurnToggle.value;

            // That mess is why you should not compute anything inside a GUI call
            // TODO : rewrite all that...
            if (worker == null)
            {
                //if (Event.current.type == EventType.Layout)
                //  layoutSkipped = true;
                return;
            }

            //if (Event.current.type == EventType.Layout)
            //  layoutSkipped = false;
            //if (layoutSkipped)
            //  return;

            string dv = " - ";
            string departure = " - ";
            string duration = " - ";
            if (worker.Finished && worker.Computed.GetLength(1) == porkchop_Height)
            {
                // UITK: We're never gonna see an EventType.Layout, so do it on Repaint
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
                            minTransferTime = Math.Max(ymin, 3600);
                            maxTransferTime = ymax;
                            GUI.changed = true;
                            guiChanged = true;
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

                double p = worker.Computed[point[0], point[1]];
                if (p > 0)
                {
                    // Display the DeltaV of the hovered/selected point
 
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
                FpUiController.Computing.style.display = DisplayStyle.Flex;
                FpUiController.Computing.text = $"Computing: {worker.Progress}%";
            }

            // Display the DeltaV of the hovered/selected point
            FpUiController.XferDeltaVLabel.text = dv;

            // includeCaptureBurn = GUILayout.Toggle(includeCaptureBurn, "Include Capture Burn"); // Localizer.Format("#MechJeb_adv_captureburn")

            // fixup the default value of the periapsis if the target changes
            if (targetCelestial != null && lastTargetCelestial != targetCelestial)
            {
                // UITK periapsisHeight is now FpUiController.TargetAdvXferPe_m 
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

            lastTargetCelestial = targetCelestial;
        }

        public override void DoParametersGUI(PatchedConicsOrbit o, double universalTime, CelestialBodyComponent target, Mode thisSelectionMode) // was: MechJebModuleTargetController target
        {
            _draggable = true;
            selectionMode = thisSelectionMode;
            // If there is a worker running without a target selected (and event type is Layout) then stop the worker and clear the plot
            if (worker != null && (target == null || target != previousTarget || selectionMode != previousSelectiionMode)) // was: !target.NormalTargetExists && Event.current.type == EventType.Layout
            {
                FlightPlanPlugin.Logger.LogInfo($"DoParametersGUI: Stopping/Clearing worker and plot. Target = {target.Name}, thisSelectionMode = {modeNames[(int)thisSelectionMode]}");
                worker.Stop = true;
                worker = null;
                plot = null;
            }

            // selectionMode = (Mode)GuiUtils.ComboBox.Box((int)selectionMode, modeNames, this);
            //if (Event.current.type == EventType.Repaint)
            //{
            //  FlightPlanPlugin.Logger.LogInfo($"DoParametersGUI: Event = {Event.current.type}, windowWidth = {windowWidth}, minWindowWidth = {minWindowWidth}");
            //  windowWidth = minWindowWidth; // FlightPlanPlugin.Instance.windowWidth; //  (int)GUILayoutUtility.GetLastRect().width;
            //}

            switch (selectionMode)
            {
                case Mode.LimitedTime:
                    FpUiController.PorkchopGroup.style.display = DisplayStyle.None;
                    FpUiController.LimitedTimeGroup.style.display = DisplayStyle.Flex;
                    // FpUiController.MaxArrivalTimeInput.value = FPUtility.SecondsToTimeString(maxArrivalTime.val);
                    // Display Computing if needed
                    if (worker != null && !worker.Finished)
                    {
                        FpUiController.Computing.style.display = DisplayStyle.Flex;
                        FpUiController.Computing.text = $"Computing: {worker.Progress}%";
                        // GuiUtils.SimpleLabel("Computing: " + worker.Progress + "%"); // Localizer.Format("#MechJeb_adv_computing")
                    }
                    else
                        FpUiController.Computing.style.display = DisplayStyle.None;
                    // Switch off the PorkchopDisplay
                    break;
                case Mode.Porkchop:
                    FpUiController.LimitedTimeGroup.style.display = DisplayStyle.None;
                    FpUiController.PorkchopGroup.style.display = DisplayStyle.Flex;
                    DoPorkchopGui(o, universalTime, target);
                    break;
            }

            if (worker == null || worker.DestinationOrbit != target.Orbit || worker.OriginOrbit != o)
                ComputeTimes(o, target.Orbit, universalTime);

            // if (GUI.changed || worker == null || worker.DestinationOrbit != target.Orbit || worker.OriginOrbit != o)
            if (GUI.changed || guiChanged || worker == null || worker.DestinationOrbit != target.Orbit || worker.OriginOrbit != o)
                ComputeStuff(o, universalTime, target);

            previousSelectiionMode = thisSelectionMode;
            previousTarget = target;

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

            double departure, arrival;
            if (selectionMode == Mode.Porkchop)
            {
                if (plot == null || plot.SelectedPoint == null)
                    throw new OperationException("Invalid point selected"); // Localizer.Format("#MechJeb_adv_Exception4")
                departure = worker.DateFromIndex(plot.SelectedPoint[0]);
                arrival = worker.DateFromIndex(plot.SelectedPoint[0]) + worker.DurationFromIndex(plot.SelectedPoint[1]);
                FlightPlanPlugin.Logger.LogInfo($"MakeNodesImpl: Departure {FPUtility.SecondsToTimeString(departure)}, Duratiopn {FPUtility.SecondsToTimeString(arrival - departure)}, Arrival {FPUtility.SecondsToTimeString(arrival)}, UT {FPUtility.SecondsToTimeString(UT)}, target_PeR {target_PeR}, Include Capture Burn {FpUiController.CaptureBurnToggle.value}");
                return worker.OptimizeEjection(
                    worker.DateFromIndex(plot.SelectedPoint[0]),
                    o, target as CelestialBodyComponent,
                    worker.DateFromIndex(plot.SelectedPoint[0]) + worker.DurationFromIndex(plot.SelectedPoint[1]),
                    UT, target_PeR, FpUiController.CaptureBurnToggle.value);
            }

            departure = worker.DateFromIndex(worker.BestDate);
            arrival = worker.DateFromIndex(worker.BestDate) + worker.DurationFromIndex(worker.BestDuration);
            FlightPlanPlugin.Logger.LogInfo($"MakeNodesImpl: Departure {FPUtility.SecondsToTimeString(departure)}, Duratiopn {FPUtility.SecondsToTimeString(arrival - departure)}, Arrival {FPUtility.SecondsToTimeString(arrival)}, UT {FPUtility.SecondsToTimeString(UT)}, target_PeR {target_PeR}, Include Capture Burn {FpUiController.CaptureBurnToggle.value}");
            return worker.OptimizeEjection(
                worker.DateFromIndex(worker.BestDate),
                o, target as CelestialBodyComponent,
                worker.DateFromIndex(worker.BestDate) + worker.DurationFromIndex(worker.BestDuration),
                UT, target_PeR, FpUiController.CaptureBurnToggle.value);
        }
    }
}
