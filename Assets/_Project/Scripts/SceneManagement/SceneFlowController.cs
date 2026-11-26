using System;
using System.Collections;
using NocturneCocoa.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NocturneCocoa.SceneManagement
{
    /// <summary>
    /// Loads one gameplay scene at a time (single-scene architecture). Lives on the persistent
    /// GameSystems object from the Bootstrap scene, so it survives every scene change.
    ///
    /// Other code gets it through Services.Get&lt;SceneFlowController&gt;() or listens to its events.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneFlowController : MonoBehaviour
    {
        [Header("Optional event channels")]
        [Tooltip("Raised with the scene name when a load begins (e.g. to start a fade-out).")]
        [SerializeField] private StringEventChannelSO sceneLoadStartedChannel;

        [Tooltip("Raised with the scene name once the new scene is active.")]
        [SerializeField] private StringEventChannelSO sceneLoadedChannel;

        [Header("Loading")]
        [Tooltip("Minimum seconds a load takes, so a future loading screen or fade does not just flash.")]
        [SerializeField, Min(0f)] private float minimumLoadDuration;

        /// <summary>Raised with the target scene name when a load begins.</summary>
        public event Action<string> SceneLoadStarted;

        /// <summary>Load progress from 0 to 1, for a future loading bar.</summary>
        public event Action<float> LoadProgressChanged;

        /// <summary>Raised with the scene name once the new scene is loaded and active.</summary>
        public event Action<string> SceneLoaded;

        public bool IsLoading { get; private set; }

        public string CurrentSceneName
        {
            get { return SceneManager.GetActiveScene().name; }
        }

        /// <summary>
        /// Starts loading a scene. Returns false (and logs why) if the request was rejected.
        /// </summary>
        public bool LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneFlow] Ignored request to load '{sceneName}': another scene is still loading.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[SceneFlow] Cannot load a scene with an empty name.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneFlow] Scene '{sceneName}' is not in the build. Add it in File > Build Profiles > Scene List " +
                    "(or run Nocturne Cocoa > Setup > Run M1 Project Setup).");
                return false;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
            return true;
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;
            float startTime = Time.unscaledTime;

            if (SceneLoadStarted != null) SceneLoadStarted.Invoke(sceneName);
            if (sceneLoadStartedChannel != null) sceneLoadStartedChannel.Raise(sceneName);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                // Should not happen after the CanStreamedLevelBeLoaded check, but never leave IsLoading stuck.
                Debug.LogError($"[SceneFlow] Unity refused to load '{sceneName}'.");
                IsLoading = false;
                yield break;
            }

            // Hold activation so the minimum duration (and later, a fade) can finish first.
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                ReportProgress(operation.progress / 0.9f);
                yield return null;
            }

            ReportProgress(1f);

            while (Time.unscaledTime - startTime < minimumLoadDuration)
            {
                yield return null;
            }

            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            IsLoading = false;
            Debug.Log($"[SceneFlow] Loaded '{sceneName}'.");

            if (SceneLoaded != null) SceneLoaded.Invoke(sceneName);
            if (sceneLoadedChannel != null) sceneLoadedChannel.Raise(sceneName);
        }

        private void ReportProgress(float progress)
        {
            if (LoadProgressChanged != null)
            {
                LoadProgressChanged.Invoke(Mathf.Clamp01(progress));
            }
        }
    }
}
