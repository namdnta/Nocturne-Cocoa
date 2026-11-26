using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NocturneCocoa.Core
{
    /// <summary>
    /// Wraps the Input System actions and turns them into simple game events, so gameplay code
    /// never touches devices directly. It also switches between Gameplay and UI input modes
    /// (e.g. the player stops walking while dialogue or the crafting UI is open).
    ///
    /// Uses the "Player" and "UI" maps of InputSystem_Actions (the project-wide actions asset).
    /// </summary>
    [DisallowMultipleComponent]
    public class InputManager : MonoBehaviour
    {
        public enum InputMode
        {
            /// <summary>All game input off (cutscenes, scene loading).</summary>
            None,
            /// <summary>Exploration: movement, interact and pause. UI navigation also stays on.</summary>
            Gameplay,
            /// <summary>Menus, dialogue and crafting screens: UI map only, no movement.</summary>
            UI
        }

        [Tooltip("Leave empty to use the project-wide actions (Project Settings > Input System Package).")]
        [SerializeField] private InputActionAsset actionsAsset;

        [SerializeField] private string gameplayMapName = "Player";
        [SerializeField] private string uiMapName = "UI";

        [Header("Action names")]
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string pauseActionName = "Pause";
        [SerializeField] private string cancelActionName = "Cancel";

        /// <summary>Movement vector changed (zero when released or when gameplay input is off).</summary>
        public event Action<Vector2> MoveChanged;
        public event Action InteractPressed;
        public event Action PausePressed;

        /// <summary>UI "back" (Esc / B button) while in UI mode, e.g. to close a menu.</summary>
        public event Action CancelPressed;
        public event Action<InputMode> ModeChanged;

        private InputActionMap gameplayMap;
        private InputActionMap uiMap;
        private InputAction moveAction;
        private InputAction interactAction;
        private InputAction pauseAction;
        private InputAction cancelAction;

        public Vector2 Move { get; private set; }
        public InputMode Mode { get; private set; }

        /// <summary>Called by BootstrapLoader.</summary>
        public void Initialize()
        {
            InputActionAsset asset = actionsAsset != null ? actionsAsset : InputSystem.actions;
            if (asset == null)
            {
                Debug.LogError("[Input] No input actions found. Assign an asset or set project-wide actions in Project Settings > Input System Package.");
                return;
            }

            gameplayMap = asset.FindActionMap(gameplayMapName, false);
            uiMap = asset.FindActionMap(uiMapName, false);

            if (gameplayMap == null) Debug.LogError($"[Input] Action map '{gameplayMapName}' not found in '{asset.name}'.");
            if (uiMap == null) Debug.LogWarning($"[Input] Action map '{uiMapName}' not found in '{asset.name}'.");

            moveAction = FindAction(gameplayMap, moveActionName);
            interactAction = FindAction(gameplayMap, interactActionName);
            pauseAction = FindAction(gameplayMap, pauseActionName);
            cancelAction = FindAction(uiMap, cancelActionName);

            if (moveAction != null)
            {
                moveAction.performed += HandleMove;
                moveAction.canceled += HandleMove;
            }

            if (interactAction != null) interactAction.performed += HandleInteract;
            if (pauseAction != null) pauseAction.performed += HandlePause;
            if (cancelAction != null) cancelAction.performed += HandleCancel;

            SetMode(InputMode.Gameplay);
        }

        private void OnDestroy()
        {
            if (moveAction != null)
            {
                moveAction.performed -= HandleMove;
                moveAction.canceled -= HandleMove;
            }

            if (interactAction != null) interactAction.performed -= HandleInteract;
            if (pauseAction != null) pauseAction.performed -= HandlePause;
            if (cancelAction != null) cancelAction.performed -= HandleCancel;
        }

        public void SetMode(InputMode mode)
        {
            switch (mode)
            {
                case InputMode.Gameplay:
                    SetMapEnabled(gameplayMap, true);
                    SetMapEnabled(uiMap, true);
                    break;
                case InputMode.UI:
                    SetMapEnabled(gameplayMap, false);
                    SetMapEnabled(uiMap, true);
                    break;
                default:
                    SetMapEnabled(gameplayMap, false);
                    SetMapEnabled(uiMap, false);
                    break;
            }

            if (mode != InputMode.Gameplay && Move != Vector2.zero)
            {
                Move = Vector2.zero;
                if (MoveChanged != null) MoveChanged.Invoke(Move);
            }

            bool changed = Mode != mode;
            Mode = mode;
            if (changed && ModeChanged != null)
            {
                ModeChanged.Invoke(mode);
            }
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            Move = context.ReadValue<Vector2>();
            if (MoveChanged != null) MoveChanged.Invoke(Move);
        }

        private void HandleInteract(InputAction.CallbackContext context)
        {
            if (InteractPressed != null) InteractPressed.Invoke();
        }

        private void HandlePause(InputAction.CallbackContext context)
        {
            if (PausePressed != null) PausePressed.Invoke();
        }

        private void HandleCancel(InputAction.CallbackContext context)
        {
            // In Gameplay mode Esc already means Pause; only report Cancel while a UI is open.
            if (Mode == InputMode.UI && CancelPressed != null)
            {
                CancelPressed.Invoke();
            }
        }

        private static InputAction FindAction(InputActionMap map, string actionName)
        {
            if (map == null)
            {
                return null;
            }

            InputAction action = map.FindAction(actionName, false);
            if (action == null)
            {
                Debug.LogWarning($"[Input] Action '{actionName}' not found in map '{map.name}'.");
            }

            return action;
        }

        private static void SetMapEnabled(InputActionMap map, bool enabled)
        {
            if (map == null)
            {
                return;
            }

            if (enabled) map.Enable();
            else map.Disable();
        }
    }
}