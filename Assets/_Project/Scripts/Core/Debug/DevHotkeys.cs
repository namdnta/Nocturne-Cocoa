using NocturneCocoa.SaveSystem;
using NocturneCocoa.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NocturneCocoa.Core
{
    /// <summary>
    /// DEVELOPMENT ONLY. Keyboard shortcuts for testing M1 before any menus exist.
    /// Does nothing in release builds. Remove it from GameSystems once the Pause Menu exists (M7).
    ///
    ///   F1 / F2 / F3  load Cafe / Dialogue / Cooking scene
    ///   F5            save to slot 1          F9   load slot 1 (Continue)
    ///   F6            autosave now            F10  start a new game
    ///   F12           print session info
    /// </summary>
    [AddComponentMenu("Nocturne Cocoa/Debug/Dev Hotkeys")]
    public class DevHotkeys : MonoBehaviour
    {
#pragma warning disable 0414 // unused in release builds
        [Tooltip("Also print a line whenever Interact or Pause is pressed.")]
        [SerializeField] private bool logInputEvents = true;
#pragma warning restore 0414

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private SceneFlowController sceneFlow;
        private GameSession session;
        private InputManager input;

        private void Start()
        {
            Services.TryGet(out sceneFlow);
            Services.TryGet(out session);
            Services.TryGet(out input);

            if (input != null && logInputEvents)
            {
                input.InteractPressed += LogInteract;
                input.PausePressed += LogPause;
            }

            Debug.Log("[Dev] Hotkeys: F1 Cafe, F2 Dialogue, F3 Cooking, F5 Save, F6 AutoSave, F9 Load, F10 New Game, F12 Info.");
        }

        private void OnDestroy()
        {
            if (input != null)
            {
                input.InteractPressed -= LogInteract;
                input.PausePressed -= LogPause;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || sceneFlow == null || session == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame) sceneFlow.LoadScene(SceneNames.CafeExploration);
            if (keyboard.f2Key.wasPressedThisFrame) sceneFlow.LoadScene(SceneNames.Dialogue);
            if (keyboard.f3Key.wasPressedThisFrame) sceneFlow.LoadScene(SceneNames.Cooking);
            if (keyboard.f5Key.wasPressedThisFrame) session.SaveGame(1);
            if (keyboard.f6Key.wasPressedThisFrame) session.AutoSave();
            if (keyboard.f9Key.wasPressedThisFrame && !session.ContinueGame(1)) Debug.Log("[Dev] Slot 1 is empty or invalid.");
            if (keyboard.f10Key.wasPressedThisFrame)
            {
                session.StartNewGame();
                sceneFlow.LoadScene(SceneNames.CafeExploration);
            }

            if (keyboard.f12Key.wasPressedThisFrame) LogSessionInfo();
        }

        private void LogSessionInfo()
        {
            if (!session.HasActiveSession)
            {
                Debug.Log("[Dev] No active session.");
                return;
            }

            SaveData data = session.Data;
            Debug.Log(
                $"[Dev] Scene: {sceneFlow.CurrentSceneName} | Saved scene: {data.player.sceneName} | Day {data.progress.day} | " +
                $"Play time: {data.totalPlayTimeSeconds:0.0}s | Flags: {data.progress.storyFlags.Count} | Created: {data.createdUtc}");
        }

        private void LogInteract()
        {
            Debug.Log("[Dev] Interact pressed.");
        }

        private void LogPause()
        {
            Debug.Log("[Dev] Pause pressed.");
        }
#endif
    }
}
