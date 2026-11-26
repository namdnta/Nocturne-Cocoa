using System.Collections.Generic;
using NocturneCocoa.Core;
using NocturneCocoa.SaveSystem;
using NocturneCocoa.SceneManagement;
using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// The player's "brain" in exploration scenes. Connects the persistent systems to the player parts:
    /// - reads InputManager and drives PlayerMovement and PlayerInteractor,
    /// - places the player at the right spot when a scene starts (entry point, saved position, or default spawn),
    /// - writes the player's position into the save data.
    ///
    /// Other systems take control away with AddControlLock(this) and give it back with RemoveControlLock(this),
    /// e.g. the dialogue system in M3. Several locks can be active at once.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerInteractor))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Move the player to a spawn point or saved position when the scene starts.")]
        [SerializeField] private bool placeOnSceneStart = true;

        private readonly HashSet<object> controlLocks = new HashSet<object>();

        private PlayerMovement movement;
        private PlayerInteractor interactor;
        private InputManager input;
        private SceneFlowController sceneFlow;
        private GameSession session;

        // The play-through this player belongs to. If a new game starts or a save is loaded while this
        // scene is still open, we must not write this (old) player's position into the new data.
        private SaveData sessionData;

        public PlayerMovement Movement
        {
            get { return movement; }
        }

        public PlayerInteractor Interactor
        {
            get { return interactor; }
        }

        public bool HasControl
        {
            get { return controlLocks.Count == 0; }
        }

        private bool IsSceneLoading
        {
            get { return sceneFlow != null && sceneFlow.IsLoading; }
        }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            interactor = GetComponent<PlayerInteractor>();

            Services.TryGet(out input);
            Services.TryGet(out sceneFlow);
            Services.TryGet(out session);

            if (input == null)
            {
                Debug.LogWarning("[Player] InputManager not found. Did Play Mode start from Bootstrap?", this);
            }

            if (session != null)
            {
                sessionData = session.Data;
            }

            if (placeOnSceneStart)
            {
                PlaceAtSceneStart();
            }
        }

        private void OnEnable()
        {
            if (input != null) input.InteractPressed += HandleInteractPressed;
            if (session != null) session.BeforeSave += WritePosition;
            if (sceneFlow != null) sceneFlow.SceneLoadStarted += HandleLeavingScene;
        }

        private void OnDisable()
        {
            if (input != null) input.InteractPressed -= HandleInteractPressed;
            if (session != null) session.BeforeSave -= WritePosition;
            if (sceneFlow != null) sceneFlow.SceneLoadStarted -= HandleLeavingScene;
        }

        private void Update()
        {
            bool canAct = HasControl && !IsSceneLoading;

            movement.CanMove = canAct;
            interactor.DetectionEnabled = canAct;
            movement.SetMoveInput(canAct && input != null ? input.Move : Vector2.zero);
        }

        // ---------------------------------------------------------------- Control locks

        public void AddControlLock(object owner)
        {
            if (owner != null && controlLocks.Add(owner))
            {
                movement.Stop();
            }
        }

        public void RemoveControlLock(object owner)
        {
            if (owner != null)
            {
                controlLocks.Remove(owner);
            }
        }

        // ---------------------------------------------------------------- Interaction

        private void HandleInteractPressed()
        {
            if (HasControl && !IsSceneLoading)
            {
                interactor.TryInteract();
            }
        }

        // ---------------------------------------------------------------- Placement & saving

        /// <summary>
        /// Priority: 1) the entry point we came through, 2) the saved position for this scene,
        /// 3) the default spawn point, 4) wherever the player was placed in the scene.
        /// </summary>
        private void PlaceAtSceneStart()
        {
            string sceneName = gameObject.scene.name;

            string entryPointId = sceneFlow != null ? sceneFlow.CurrentEntryPointId : null;
            if (!string.IsNullOrEmpty(entryPointId))
            {
                PlayerSpawnPoint entryPoint = PlayerSpawnPoint.Find(entryPointId);
                if (entryPoint != null)
                {
                    PlaceAt(entryPoint);
                    return;
                }

                Debug.LogWarning($"[Player] No PlayerSpawnPoint with ID '{entryPointId}' in {sceneName}.", this);
            }

            if (sessionData != null && sessionData.player.hasPosition && sessionData.player.positionSceneName == sceneName)
            {
                movement.Teleport(sessionData.player.position);
                return;
            }

            PlayerSpawnPoint defaultPoint = PlayerSpawnPoint.FindDefault();
            if (defaultPoint != null)
            {
                PlaceAt(defaultPoint);
            }
        }

        private void PlaceAt(PlayerSpawnPoint point)
        {
            movement.Teleport(point.transform.position);
            movement.FaceTowards(point.FacingDirection);
        }

        private void HandleLeavingScene(string nextScene)
        {
            if (session != null)
            {
                WritePosition(session.Data);
            }
        }

        private void WritePosition(SaveData data)
        {
            if (data == null || !ReferenceEquals(data, sessionData))
            {
                return;
            }

            data.player.hasPosition = true;
            data.player.positionSceneName = gameObject.scene.name;
            data.player.position = transform.position;
        }
    }
}