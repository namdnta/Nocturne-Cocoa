using NocturneCocoa.Characters;
using NocturneCocoa.Dialogue;
using NocturneCocoa.SaveSystem;
using NocturneCocoa.SceneManagement;
using UnityEngine;

namespace NocturneCocoa.Core
{
    /// <summary>
    /// Entry point of the game. Sits on the root "GameSystems" object in the Bootstrap scene.
    ///
    /// On Awake it:
    /// 1. Makes GameSystems (and every system under it) survive scene changes.
    /// 2. Initializes each system in a fixed order, passing dependencies in explicitly.
    /// 3. Registers the systems in Services so scene objects can find them.
    /// On Start it begins a session and loads the first gameplay scene.
    ///
    /// If the Bootstrap scene is ever loaded a second time, the duplicate destroys itself.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class BootstrapLoader : MonoBehaviour
    {
        /// <summary>SessionState key the editor uses to say "start in the scene I was editing".</summary>
        public const string EditorStartSceneKey = "NocturneCocoa.EditorStartScene";

        [Header("Systems (children of GameSystems; auto-found if left empty)")]
        [SerializeField] private SceneFlowController sceneFlow;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private InputManager inputManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private GameSession gameSession;

        [Header("M3 systems (optional until M3 setup has run)")]
        [SerializeField] private RelationshipSystem relationshipSystem;
        [SerializeField] private DialogueManager dialogueManager;

        [Header("Startup")]
        [SerializeField] private string firstScene = SceneNames.CafeExploration;

        [Tooltip("PLACEHOLDER until the Main Menu exists (M7): resume from the autosave if there is one.")]
        [SerializeField] private bool continueFromAutoSave;

        public static bool IsInitialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsInitialized = false;
        }

        private void Awake()
        {
            if (IsInitialized)
            {
                // Bootstrap was loaded again; the original systems are already running.
                Destroy(transform.root.gameObject);
                return;
            }

            if (!ResolveReferences())
            {
                enabled = false;
                return;
            }

            IsInitialized = true;
            DontDestroyOnLoad(transform.root.gameObject);

            // Order matters: systems with no dependencies first.
            saveManager.Initialize();
            audioManager.Initialize();
            inputManager.Initialize();
            gameSession.Initialize(saveManager, sceneFlow);

            Services.Register(sceneFlow);
            Services.Register(saveManager);
            Services.Register(inputManager);
            Services.Register(audioManager);
            Services.Register(gameSession);

            if (relationshipSystem != null)
            {
                relationshipSystem.Initialize(gameSession);
                Services.Register(relationshipSystem);
            }

            if (dialogueManager != null)
            {
                dialogueManager.Initialize(gameSession, relationshipSystem, inputManager, sceneFlow);
                Services.Register(dialogueManager);
            }

            Debug.Log("[Bootstrap] Nocturne Cocoa systems initialized.");
        }

        private void Start()
        {
            bool resumed = continueFromAutoSave && gameSession.TryLoadSession(SaveManager.AutoSaveSlot);
            if (!resumed)
            {
                gameSession.StartNewGame();
            }

            string editorScene = ConsumeEditorStartScene();
            string target = editorScene ?? (resumed ? gameSession.GetSavedSceneOr(firstScene) : firstScene);

            sceneFlow.LoadScene(target);
        }

        private bool ResolveReferences()
        {
            if (sceneFlow == null) sceneFlow = GetComponentInChildren<SceneFlowController>(true);
            if (saveManager == null) saveManager = GetComponentInChildren<SaveManager>(true);
            if (inputManager == null) inputManager = GetComponentInChildren<InputManager>(true);
            if (audioManager == null) audioManager = GetComponentInChildren<AudioManager>(true);
            if (gameSession == null) gameSession = GetComponentInChildren<GameSession>(true);
            if (relationshipSystem == null) relationshipSystem = GetComponentInChildren<RelationshipSystem>(true);
            if (dialogueManager == null) dialogueManager = GetComponentInChildren<DialogueManager>(true);

            bool ok = true;
            ok &= Require(sceneFlow, "SceneFlowController");
            ok &= Require(saveManager, "SaveManager");
            ok &= Require(inputManager, "InputManager");
            ok &= Require(audioManager, "AudioManager");
            ok &= Require(gameSession, "GameSession");

            if (relationshipSystem == null || dialogueManager == null)
            {
                Debug.LogWarning("[Bootstrap] Dialogue/Relationship systems not found; NPCs can't talk. Run Nocturne Cocoa > Setup > Build M3 Dialogue & NPCs.", this);
            }

            return ok;
        }

        private bool Require(Object system, string systemName)
        {
            if (system != null)
            {
                return true;
            }

            Debug.LogError(
                $"[Bootstrap] Missing {systemName}. Add it as a child of GameSystems, or run " +
                "Nocturne Cocoa > Setup > Run M1 Project Setup.", this);
            return false;
        }

        /// <summary>
        /// In the Editor, Play Mode always starts from Bootstrap; this returns the scene you were
        /// editing so you land back in it. Always null in builds.
        /// </summary>
        private static string ConsumeEditorStartScene()
        {
#if UNITY_EDITOR
            string sceneName = UnityEditor.SessionState.GetString(EditorStartSceneKey, string.Empty);
            UnityEditor.SessionState.EraseString(EditorStartSceneKey);

            if (string.IsNullOrEmpty(sceneName) || sceneName == SceneNames.Bootstrap)
            {
                return null;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[Bootstrap] '{sceneName}' is not in the build scene list, so starting in the default scene instead.");
                return null;
            }

            return sceneName;
#else
            return null;
#endif
        }
    }
}