using NocturneCocoa.Exploration;
using NocturneCocoa.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NocturneCocoa.EditorTools
{
    /// <summary>
    /// Menu: Nocturne Cocoa > Setup > Build M2 Test Room.
    /// Fills Cafe_Exploration with a grey-box café room to test M2: a player, walls, a table to walk
    /// behind/in front of (Y-sorting), an inspectable counter, a cooking station that opens Cooking_Scene,
    /// spawn points, an interaction prompt and a following camera.
    ///
    /// Everything it makes is under the root objects "Player", "M2_TestRoom" and "InteractionPrompt",
    /// so it's easy to delete once real art and level design replace it.
    /// </summary>
    public static class M2TestRoomBuilder
    {
        private const string PlayerName = "Player";
        private const string RoomName = "M2_TestRoom";
        private const string PromptName = "InteractionPrompt";

        private const float RoomWidth = 20f;
        private const float RoomHeight = 14f;

        private static readonly Color FloorColor = new Color(0.55f, 0.4f, 0.32f);
        private static readonly Color WallColor = new Color(0.28f, 0.18f, 0.2f);
        private static readonly Color FurnitureColor = new Color(0.78f, 0.6f, 0.45f);
        private static readonly Color StationColor = new Color(0.95f, 0.55f, 0.45f);
        private static readonly Color PlayerColor = new Color(0.8f, 0.7f, 1f);
        private static readonly Color PromptColor = new Color(1f, 0.9f, 0.5f);

        [MenuItem("Nocturne Cocoa/Setup/Build M2 Test Room", false, 41)]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Nocturne Cocoa", "Exit Play Mode first.", "OK");
                return;
            }

            string scenePath = $"Assets/_Project/Scenes/{SceneNames.CafeExploration}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                EditorUtility.DisplayDialog("Nocturne Cocoa", "Cafe_Exploration not found. Run 'Run M1 Project Setup' first.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureYSorting();

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            if (FindRoot(scene, PlayerName) != null || FindRoot(scene, RoomName) != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Rebuild test room?",
                        "Cafe_Exploration already has a Player / M2_TestRoom. Delete them and build a fresh test room?",
                        "Rebuild", "Cancel"))
                {
                    return;
                }

                DestroyRoot(scene, PlayerName);
                DestroyRoot(scene, RoomName);
                DestroyRoot(scene, PromptName);
            }

            Collider2D cameraBounds = BuildRoom();
            GameObject player = BuildPlayer(new Vector2(0f, -4f));
            BuildPrompt();
            SetupCamera(cameraBounds);

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog(
                "Nocturne Cocoa — M2 test room",
                "Cafe_Exploration now has a test room.\n\nPress Play, walk with WASD / arrows / left stick, " +
                "face the counter or the red cooking station and press E.",
                "OK");
        }

        // ---------------------------------------------------------------- Room

        private static Collider2D BuildRoom()
        {
            GameObject room = new GameObject(RoomName);

            // Floor (drawn behind everything).
            SpriteRenderer floor = CreateSprite("Floor", room.transform, PlaceholderSprites.Fill, FloorColor, Vector2.zero, new Vector2(RoomWidth, RoomHeight));
            floor.sortingOrder = -100;

            // Walls (solid).
            float halfW = RoomWidth / 2f;
            float halfH = RoomHeight / 2f;
            CreateWall("Wall_Top", room.transform, new Vector2(0f, halfH - 0.5f), new Vector2(RoomWidth, 1f));
            CreateWall("Wall_Bottom", room.transform, new Vector2(0f, -halfH + 0.5f), new Vector2(RoomWidth, 1f));
            CreateWall("Wall_Left", room.transform, new Vector2(-halfW + 0.5f, 0f), new Vector2(1f, RoomHeight));
            CreateWall("Wall_Right", room.transform, new Vector2(halfW - 0.5f, 0f), new Vector2(1f, RoomHeight));

            // Table in the middle: walk above it (behind) and below it (in front) to check Y-sorting.
            CreateFurniture("Table", room.transform, new Vector2(0f, 0f), new Vector2(3f, 1.5f), FurnitureColor);

            // Counter you can inspect.
            GameObject counter = CreateFurniture("Counter", room.transform, new Vector2(-5f, 4.2f), new Vector2(5f, 1.5f), FurnitureColor);
            UnityEventInteractable inspect = counter.AddComponent<UnityEventInteractable>();
            SetString(inspect, "promptText", "Inspect");
            SetString(inspect, "debugMessage", "A jar of moonlit cocoa beans hums softly on the counter.");
            SetVector2(inspect, "promptOffset", new Vector2(0f, 2f));

            // Cooking station that opens the Cooking scene.
            GameObject station = CreateFurniture("CookingStation", room.transform, new Vector2(6f, 4.2f), new Vector2(2f, 1.5f), StationColor);
            SceneTransitionInteractable cook = station.AddComponent<SceneTransitionInteractable>();
            SetString(cook, "promptText", "Cook");
            SetString(cook, "targetScene", SceneNames.Cooking);
            SetVector2(cook, "promptOffset", new Vector2(0f, 2f));

            // Spawn points.
            CreateSpawnPoint("Spawn_Default", room.transform, new Vector2(0f, -4f), "Default", true, Vector2.up);
            CreateSpawnPoint("Spawn_FromKitchen", room.transform, new Vector2(6f, 3f), "FromKitchen", false, Vector2.down);

            // Sprites use URP's Lit material, so the room needs light.
            CreateGlobalLight(room.transform);

            // Camera bounds: the whole room.
            GameObject boundsObject = new GameObject("CameraBounds");
            boundsObject.transform.SetParent(room.transform, false);
            BoxCollider2D bounds = boundsObject.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size = new Vector2(RoomWidth, RoomHeight);

            return bounds;
        }

        internal static void CreateGlobalLight(Transform parent)
        {
            // Looked up by name so the editor assembly doesn't need a URP reference.
            System.Type lightType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (lightType == null)
            {
                Debug.LogWarning("[Nocturne] Light2D type not found. Add a Global Light 2D to the scene by hand (GameObject > Light > Global Light 2D).");
                return;
            }

            GameObject lightObject = new GameObject("GlobalLight2D");
            lightObject.transform.SetParent(parent, false);
            Component light = lightObject.AddComponent(lightType);

            SerializedObject so = new SerializedObject(light);
            SerializedProperty type = so.FindProperty("m_LightType");
            if (type != null)
            {
                type.intValue = 4; // Light2D.LightType.Global
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CreateWall(string objectName, Transform parent, Vector2 position, Vector2 size)
        {
            SpriteRenderer wall = CreateSprite(objectName, parent, PlaceholderSprites.Fill, WallColor, position, size);
            wall.sortingOrder = -50;
            wall.gameObject.AddComponent<BoxCollider2D>();
        }

        /// <summary>Furniture uses a bottom-pivot sprite and a collider only on its base, so the player can walk "behind" it.</summary>
        private static GameObject CreateFurniture(string objectName, Transform parent, Vector2 basePosition, Vector2 size, Color color)
        {
            SpriteRenderer sprite = CreateSprite(objectName, parent, PlaceholderSprites.Box, color, basePosition, size);
            BoxCollider2D footprint = sprite.gameObject.AddComponent<BoxCollider2D>();
            footprint.size = new Vector2(1f, 0.35f);
            footprint.offset = new Vector2(0f, 0.175f);
            return sprite.gameObject;
        }

        private static void CreateSpawnPoint(string objectName, Transform parent, Vector2 position, string id, bool isDefault, Vector2 facing)
        {
            GameObject point = new GameObject(objectName);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = position;

            PlayerSpawnPoint spawn = point.AddComponent<PlayerSpawnPoint>();
            SerializedObject so = new SerializedObject(spawn);
            so.FindProperty("spawnId").stringValue = id;
            so.FindProperty("isDefault").boolValue = isDefault;
            so.FindProperty("facingDirection").vector2Value = facing;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Player

        private static GameObject BuildPlayer(Vector2 position)
        {
            GameObject player = new GameObject(PlayerName);
            player.transform.position = position;

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            // Collider at the feet only, so the head can overlap furniture drawn behind it.
            CapsuleCollider2D feet = player.AddComponent<CapsuleCollider2D>();
            feet.direction = CapsuleDirection2D.Horizontal;
            feet.size = new Vector2(0.6f, 0.3f);
            feet.offset = new Vector2(0f, 0.15f);

            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerAnimator>();

            CreateSprite("Sprite", player.transform, PlaceholderSprites.Character, PlayerColor, Vector2.zero, Vector2.one);
            return player;
        }

        // ---------------------------------------------------------------- Prompt

        private static void BuildPrompt()
        {
            GameObject prompt = new GameObject(PromptName);
            InteractionPromptIndicator indicator = prompt.AddComponent<InteractionPromptIndicator>();

            SpriteRenderer icon = CreateSprite("Icon", prompt.transform, PlaceholderSprites.Prompt, PromptColor, Vector2.zero, Vector2.one);
            icon.sortingOrder = 100;

            SerializedObject so = new SerializedObject(indicator);
            so.FindProperty("visual").objectReferenceValue = icon.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Camera

        private static void SetupCamera(Collider2D bounds)
        {
            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 5f;

            CameraController follow = camera.GetComponent<CameraController>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<CameraController>();
            }

            SerializedObject so = new SerializedObject(follow);
            so.FindProperty("bounds").objectReferenceValue = bounds;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Y-sorting

        /// <summary>
        /// 3/4 top-down needs "lower on screen = drawn in front". In URP 2D this is the Renderer 2D asset's
        /// Transparency Sort Mode = Custom Axis (0, 1, 0).
        /// </summary>
        private static void ConfigureYSorting()
        {
            const int customAxis = 3; // TransparencySortMode.CustomAxis

            foreach (string guid in AssetDatabase.FindAssets("t:Renderer2DData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object rendererData = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (rendererData == null)
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(rendererData);
                SerializedProperty mode = so.FindProperty("m_TransparencySortMode");
                SerializedProperty axis = so.FindProperty("m_TransparencySortAxis");
                if (mode == null || axis == null)
                {
                    Debug.LogWarning($"[Nocturne] Couldn't set Y-sorting on {path}. Set Transparency Sort Mode = Custom Axis (0,1,0) by hand.");
                    continue;
                }

                mode.intValue = customAxis;
                axis.vector3Value = new Vector3(0f, 1f, 0f);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rendererData);
                Debug.Log($"[Nocturne] Y-sorting enabled on {path}.");
            }

            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- Helpers

        internal static SpriteRenderer CreateSprite(string objectName, Transform parent, Sprite sprite, Color color, Vector2 position, Vector2 scale)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return renderer;
        }

        private static void SetString(Object target, string property, string value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(property).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector2(Object target, string property, Vector2 value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(property).vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }
            }

            return null;
        }

        private static void DestroyRoot(Scene scene, string objectName)
        {
            GameObject root = FindRoot(scene, objectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}