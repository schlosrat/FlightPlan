using BepInEx.Logging;
using KSP.Game;
using KSP.Input;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FlightPlan.API.Extensions;

public static class UIToolkitExtensions
{
    private static readonly ManualLogSource _Logger = Logger.CreateLogSource("FP.UIToolkitExtensions");

    private static GameInstance Game => GameManager.Instance.Game;

    private static List<InputAction> _maskedInputActions = [];

    private static List<InputAction> MaskedInputActions
    {
        get
        {
            if (_maskedInputActions.Count == 0)
                _maskedInputActions =
                [
                    Game.Input.Flight.CameraZoom,
                    Game.Input.Flight.mouseDoubleTap,
                    Game.Input.Flight.mouseSecondaryTap,

                    Game.Input.MapView.cameraZoom,
                    Game.Input.MapView.Focus,
                    Game.Input.MapView.mousePrimary,
                    Game.Input.MapView.mouseSecondary,
                    Game.Input.MapView.mouseTertiary,
                    Game.Input.MapView.mousePosition,

                    Game.Input.VAB.cameraZoom,
                    Game.Input.VAB.mousePrimary,
                    Game.Input.VAB.mouseSecondary
                ];

            return _maskedInputActions;
        }
    }

    private static readonly Dictionary<int, bool> MaskedInputActionsState = new();

    /// <summary>
    /// Stop the mouse events (scroll and click) from propagating to the game (e.g. zoom).
    /// The only place where the Click still doesn't get stopped is in the MapView, neither the Focus or the Orbit mouse events.
    /// </summary>
    public static void StopMouseEventsPropagation(this VisualElement element)
    {
        element.RegisterCallback<PointerEnterEvent>(OnVisualElementPointerEnter);
        element.RegisterCallback<PointerLeaveEvent>(OnVisualElementPointerLeave);
    }

    private static void OnVisualElementPointerEnter(PointerEnterEvent evt)
    {
        for (var i = 0; i < MaskedInputActions.Count; i++)
        {
            var inputAction = MaskedInputActions[i];
            MaskedInputActionsState[i] = inputAction.enabled;
            inputAction.Disable();
        }
    }

    private static void OnVisualElementPointerLeave(PointerLeaveEvent evt)
    {
        for (var i = 0; i < MaskedInputActions.Count; i++)
        {
            var inputAction = MaskedInputActions[i];
            if (MaskedInputActionsState[i])
                inputAction.Enable();
        }
    }
}