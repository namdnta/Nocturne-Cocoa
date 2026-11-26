using System.Collections.Generic;
using System.Text;
using NocturneCocoa.Core;
using NocturneCocoa.SaveSystem;
using NocturneCocoa.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NocturneCocoa.EditorTools
{
    /// <summary>
    /// One-click M1 setup: Nocturne Cocoa > Setup > Run M1 Project Setup.
    /// Safe to run more than once — it only adds what's missing and never deletes anything.
    ///
    /// 1. Creates placeholder Cafe_Exploration / Dialogue_Scene / Cooking_Scene scenes if missing.
    /// 2. Puts Bootstrap first in the build scene list, followed by the gameplay scenes.
    /// 3. Adds the system components under GameSystems in the Bootstrap scene and wires them up.
    /// </summary>
    public static class NocturneProjectSetup
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string SystemsRootName = "GameSystems";

        [MenuItem("Nocturne Cocoa/Setup/Run M1 Project Setup", false, 40)]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Nocturne Cocoa", "Exit Play Mode first.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            StringBuilder report = new StringBuilder();

            foreach (string sceneName in SceneNames.Gameplay)
            {
                CreatePlaceholderSceneIfMissing(sceneName, report);
            }

            UpdateBuildSceneList(report);
            WireBootstrapScene(report);
            AssetDatabase.SaveAssets();

            Debug.Log("[Nocturne] M1 setup finished:\n" + report);
            EditorUtility.DisplayDialog("Nocturne Cocoa — M1 setup", report.ToString(), "OK");
        }

        private static string ScenePath(string sceneName)
        {
            return $"{ScenesFolder}/{sceneName}.unity";
        }

        // ---------------------------------------------------------------- 1. Placeholder scenes

        private static void CreatePlaceholderSceneIfMissing(string sceneName, StringBuilder report)
        {
            string path = ScenePath(sceneName);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            {
                report.AppendLine($"• {sceneName}: already exists");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PlaceholderColor(sceneName); // a different colour per scene, so you can see which one loaded
            cameraObject.AddComponent<AudioListener>();

            new GameObject($"--- {sceneName} (PLACEHOLDER) ---");

            EditorSceneManager.SaveScene(scene, path);
            report.AppendLine($"• {sceneName}: created placeholder scene");
        }

        private static Color PlaceholderColor(string sceneName)
        {
            if (sceneName == SceneNames.CafeExploration) return new Color(0.36f, 0.22f, 0.16f); // warm cocoa
            if (sceneName == SceneNames.Dialogue) return new Color(0.27f, 0.18f, 0.36f);        // plum
            if (sceneName == SceneNames.Cooking) return new Color(0.16f, 0.24f, 0.33f);         // midnight blue
            return Color.black;
        }

        // ---------------------------------------------------------------- 2. Build scene list

        private static void UpdateBuildSceneList(StringBuilder report)
        {
            List<string> required = new List<string> { ScenePath(SceneNames.Bootstrap) };
            foreach (string sceneName in SceneNames.Gameplay)
            {
                required.Add(ScenePath(sceneName));
            }

            List<EditorBuildSettingsScene> result = new List<EditorBuildSettingsScene>();
            foreach (string path in required)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                {
                    result.Add(new EditorBuildSettingsScene(path, true));
                }
                else
                {
                    report.AppendLine($"• WARNING: {path} not found, not added to build");
                }
            }

            // Keep any other scenes you added yourself, after ours.
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (!required.Contains(existing.path))
                {
                    result.Add(existing);
                }
            }

            EditorBuildSettings.scenes = result.ToArray();
            report.AppendLine("• Build scene list: Bootstrap, Cafe_Exploration, Dialogue_Scene, Cooking_Scene");
        }

        // ---------------------------------------------------------------- 3. Bootstrap wiring

        private static void WireBootstrapScene(StringBuilder report)
        {
            string path = ScenePath(SceneNames.Bootstrap);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                report.AppendLine($"• WARNING: {path} not found, Bootstrap not wired");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            GameObject root = FindRoot(scene, SystemsRootName);
            if (root == null)
            {
                root = new GameObject(SystemsRootName);
            }

            BootstrapLoader loader = GetOrAdd<BootstrapLoader>(root);
            SceneFlowController sceneFlow = GetOrAdd<SceneFlowController>(GetOrCreateChild(root, "SceneFlowController"));
            SaveManager saveManager = GetOrAdd<SaveManager>(GetOrCreateChild(root, "SaveManager"));
            InputManager inputManager = GetOrAdd<InputManager>(GetOrCreateChild(root, "InputManager"));
            AudioManager audioManager = GetOrAdd<AudioManager>(GetOrCreateChild(root, "AudioManager"));
            GameSession gameSession = GetOrAdd<GameSession>(GetOrCreateChild(root, "GameSession"));
            GetOrAdd<DevHotkeys>(GetOrCreateChild(root, "DevHotkeys"));

            SerializedObject serializedLoader = new SerializedObject(loader);
            SetReference(serializedLoader, "sceneFlow", sceneFlow);
            SetReference(serializedLoader, "saveManager", saveManager);
            SetReference(serializedLoader, "inputManager", inputManager);
            SetReference(serializedLoader, "audioManager", audioManager);
            SetReference(serializedLoader, "gameSession", gameSession);
            serializedLoader.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine("• Bootstrap: GameSystems components added and wired");
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject candidate in scene.GetRootGameObjects())
            {
                if (candidate.name == objectName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static GameObject GetOrCreateChild(GameObject parent, string childName)
        {
            Transform existing = parent.transform.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[Nocturne] Field '{propertyName}' not found on {serializedObject.targetObject.GetType().Name}.");
                return;
            }

            property.objectReferenceValue = value;
        }
    }
}