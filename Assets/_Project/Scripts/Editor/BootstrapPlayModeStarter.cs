using NocturneCocoa.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NocturneCocoa.EditorTools
{
    /// <summary>
    /// Makes Play Mode always start from the Bootstrap scene (so every system exists), then
    /// BootstrapLoader sends you to the scene you were editing. Toggle it from the menu:
    /// Nocturne Cocoa > Always Start From Bootstrap.
    /// </summary>
    [InitializeOnLoad]
    public static class BootstrapPlayModeStarter
    {
        public const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        private const string MenuPath = "Nocturne Cocoa/Always Start From Bootstrap";
        private const string EnabledPrefKey = "NocturneCocoa.AlwaysStartFromBootstrap";

        static BootstrapPlayModeStarter()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += Apply;
        }

        private static bool Enabled
        {
            get { return EditorPrefs.GetBool(EnabledPrefKey, true); }
            set { EditorPrefs.SetBool(EnabledPrefKey, value); }
        }

        [MenuItem(MenuPath, false, 0)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            if (!Enabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            SceneAsset bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrap == null)
            {
                UnityEngine.Debug.LogWarning($"[Nocturne] Bootstrap scene not found at {BootstrapScenePath}.");
            }

            EditorSceneManager.playModeStartScene = bootstrap;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            Apply();

            if (Enabled)
            {
                // Remember the scene being edited; BootstrapLoader reads (and clears) this.
                SessionState.SetString(BootstrapLoader.EditorStartSceneKey, SceneManager.GetActiveScene().name);
            }
        }
    }
}