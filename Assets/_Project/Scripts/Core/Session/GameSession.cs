using System;
using NocturneCocoa.Core.Events;
using NocturneCocoa.SaveSystem;
using NocturneCocoa.SceneManagement;
using UnityEngine;

namespace NocturneCocoa.Core
{
    /// <summary>
    /// Owns the current play-through's runtime state (a SaveData instance) and the high-level
    /// session actions: New Game, Continue, Save, Auto Save.
    ///
    /// It is deliberately NOT a GameManager: it does not run crafting, dialogue or customers.
    /// Those systems read and write their own section of Data, and hook into BeforeSave/AfterLoad
    /// to sync any state they keep elsewhere.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameSession : MonoBehaviour
    {
        [Header("Auto Save")]
        [Tooltip("Write the autosave slot every time a gameplay scene finishes loading.")]
        [SerializeField] private bool autoSaveOnSceneChange = true;

        [Header("Optional event channels")]
        [Tooltip("Raised when a new game starts or a save is loaded.")]
        [SerializeField] private VoidEventChannelSO sessionStartedChannel;

        /// <summary>Raised after Data has been replaced (new game or load).</summary>
        public event Action SessionStarted;

        /// <summary>Raised right before saving, so systems can write their latest state into the data.</summary>
        public event Action<SaveData> BeforeSave;

        /// <summary>Raised after a save is loaded, so systems can read their state back out.</summary>
        public event Action<SaveData> AfterLoad;

        private SaveManager saveManager;
        private SceneFlowController sceneFlow;

        /// <summary>The current play-through. Null until StartNewGame or a load succeeds.</summary>
        public SaveData Data { get; private set; }

        public bool HasActiveSession
        {
            get { return Data != null; }
        }

        /// <summary>Called by BootstrapLoader.</summary>
        public void Initialize(SaveManager saveManager, SceneFlowController sceneFlow)
        {
            this.saveManager = saveManager;
            this.sceneFlow = sceneFlow;
            this.sceneFlow.SceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (sceneFlow != null)
            {
                sceneFlow.SceneLoaded -= HandleSceneLoaded;
            }
        }

        private void Update()
        {
            if (HasActiveSession && sceneFlow != null && !sceneFlow.IsLoading)
            {
                Data.totalPlayTimeSeconds += Time.unscaledDeltaTime;
            }
        }

        // ---------------------------------------------------------------- Session lifecycle

        public void StartNewGame()
        {
            Data = SaveData.CreateNew();
            Debug.Log("[Session] New game started.");
            RaiseSessionStarted();
        }

        /// <summary>Loads a slot into Data without changing scene. Returns false if the slot can't be used.</summary>
        public bool TryLoadSession(int slot)
        {
            if (!saveManager.HasSave(slot))
            {
                return false;
            }

            SaveData loaded;
            if (!saveManager.TryLoad(slot, out loaded))
            {
                return false;
            }

            Data = loaded;
            if (AfterLoad != null) AfterLoad.Invoke(Data);
            RaiseSessionStarted();
            return true;
        }

        /// <summary>Loads a slot and travels to the scene it was saved in (for a future Continue button).</summary>
        public bool ContinueGame(int slot, string fallbackScene = SceneNames.CafeExploration)
        {
            if (!TryLoadSession(slot))
            {
                return false;
            }

            return sceneFlow.LoadScene(GetSavedSceneOr(fallbackScene));
        }

        /// <summary>The scene stored in the save, or the fallback if none / invalid.</summary>
        public string GetSavedSceneOr(string fallbackScene)
        {
            if (!HasActiveSession || string.IsNullOrEmpty(Data.player.sceneName) || Data.player.sceneName == SceneNames.Bootstrap)
            {
                return fallbackScene;
            }

            return Data.player.sceneName;
        }

        // ---------------------------------------------------------------- Saving

        public bool SaveGame(int slot)
        {
            if (!HasActiveSession)
            {
                Debug.LogWarning("[Session] Nothing to save: no active session.");
                return false;
            }

            if (sceneFlow.IsLoading)
            {
                Debug.LogWarning("[Session] Save skipped: a scene is loading.");
                return false;
            }

            CaptureCurrentScene();
            if (BeforeSave != null) BeforeSave.Invoke(Data);
            return saveManager.Save(Data, slot);
        }

        public bool AutoSave()
        {
            return SaveGame(SaveManager.AutoSaveSlot);
        }

        // ---------------------------------------------------------------- Story flags

        public bool HasFlag(string flag)
        {
            return HasActiveSession && Data.progress.storyFlags.Contains(flag);
        }

        public void SetFlag(string flag)
        {
            if (HasActiveSession && !string.IsNullOrEmpty(flag) && !Data.progress.storyFlags.Contains(flag))
            {
                Data.progress.storyFlags.Add(flag);
            }
        }

        public void ClearFlag(string flag)
        {
            if (HasActiveSession)
            {
                Data.progress.storyFlags.Remove(flag);
            }
        }

        // ---------------------------------------------------------------- Internals

        private void HandleSceneLoaded(string sceneName)
        {
            if (!HasActiveSession || sceneName == SceneNames.Bootstrap)
            {
                return;
            }

            CaptureCurrentScene();

            if (autoSaveOnSceneChange)
            {
                AutoSave();
            }
        }

        private void CaptureCurrentScene()
        {
            string current = sceneFlow.CurrentSceneName;
            if (current != SceneNames.Bootstrap)
            {
                // DESIGN NOTE: this saves the exact scene (including Cooking/Dialogue). If you'd rather
                // always resume in the café, only store SceneNames.CafeExploration here.
                Data.player.sceneName = current;
            }
        }

        private void RaiseSessionStarted()
        {
            if (SessionStarted != null) SessionStarted.Invoke();
            if (sessionStartedChannel != null) sessionStartedChannel.Raise();
        }
    }
}
