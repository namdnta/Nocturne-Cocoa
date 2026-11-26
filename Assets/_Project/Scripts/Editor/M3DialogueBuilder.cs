using System.Collections.Generic;
using NocturneCocoa.Characters;
using NocturneCocoa.Core;
using NocturneCocoa.Data;
using NocturneCocoa.Dialogue;
using NocturneCocoa.Exploration;
using NocturneCocoa.SceneManagement;
using NocturneCocoa.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NocturneCocoa.EditorTools
{
    /// <summary>
    /// Menu: Nocturne Cocoa > Setup > Build M3 Dialogue & NPCs. Safe to run again: existing assets and objects are kept.
    ///
    /// 1. Sample data: 2 characters, 4 dialogues, Character + Dialogue databases (ScriptableObjects/).
    /// 2. Bootstrap: RelationshipSystem, DialogueManager, EventSystem and the persistent DialogueCanvas.
    /// 3. Dialogue_Scene: the side-view stage.
    /// 4. Cafe_Exploration: two sample NPCs (Luna stands still, Mochi wanders).
    /// </summary>
    public static class M3DialogueBuilder
    {
        private const string CharactersFolder = "Assets/_Project/ScriptableObjects/Characters";
        private const string DialogueFolder = "Assets/_Project/ScriptableObjects/Dialogue";
        private const string ScenesFolder = "Assets/_Project/Scenes";

        private static readonly Color BoxColor = new Color(0.12f, 0.08f, 0.14f, 0.94f);
        private static readonly Color NameBoxColor = new Color(0.28f, 0.17f, 0.3f, 1f);
        private static readonly Color ChoiceColor = new Color(0.22f, 0.14f, 0.26f, 0.96f);
        private static readonly Color TextColor = new Color(0.96f, 0.92f, 0.86f, 1f);

        [MenuItem("Nocturne Cocoa/Setup/Build M3 Dialogue & NPCs", false, 42)]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Nocturne Cocoa", "Exit Play Mode first.", "OK");
                return;
            }

            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog(
                    "TextMeshPro resources needed",
                    "The dialogue UI uses TextMeshPro.\n\nOpen Window > TextMeshPro > Import TMP Essential Resources, " +
                    "click Import, then run this menu again.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SampleContent content = CreateSampleContent();
            SetupBootstrap(content);
            SetupDialogueScene();
            SetupCafeNpcs(content);

            EditorUtility.DisplayDialog(
                "Nocturne Cocoa — M3",
                "Dialogue & NPCs are set up.\n\nPress Play, walk to Luna (blue) or Mochi (orange) and press E to talk.\n" +
                "Mochi's first conversation uses the side-view Dialogue_Scene.",
                "OK");
        }

        // ================================================================ 1. Sample data

        private class SampleContent
        {
            public CharacterProfileSO Luna;
            public CharacterProfileSO Mochi;
            public DialogueSO LunaFirstMeeting;
            public DialogueSO LunaChat;
            public DialogueSO MochiIntro;
            public DialogueSO MochiPurr;
            public DialogueDatabaseSO DialogueDatabase;
            public CharacterDatabaseSO CharacterDatabase;
        }

        private static SampleContent CreateSampleContent()
        {
            EnsureFolder(CharactersFolder + "/Samples");
            EnsureFolder(DialogueFolder + "/Samples");

            SampleContent c = new SampleContent();

            c.Luna = LoadOrCreate<CharacterProfileSO>(CharactersFolder + "/Samples/CHR_Luna.asset", luna =>
            {
                Color skin = new Color(0.62f, 0.68f, 0.98f);
                luna.EditorConfigure("luna", "Luna", new Color(0.72f, 0.82f, 1f),
                    "SAMPLE: A moon-witch who used to visit the atelier every full moon.",
                    Expressions("luna", skin));
            });

            c.Mochi = LoadOrCreate<CharacterProfileSO>(CharactersFolder + "/Samples/CHR_Mochi.asset", mochi =>
            {
                Color fur = new Color(0.98f, 0.7f, 0.4f);
                mochi.EditorConfigure("mochi", "Mochi", new Color(1f, 0.78f, 0.5f),
                    "SAMPLE: The atelier's cat familiar. Keeps the ovens warm.",
                    Expressions("mochi", fur));
            });

            c.LunaFirstMeeting = LoadOrCreate<DialogueSO>(DialogueFolder + "/Samples/DLG_Luna_FirstMeeting.asset", d =>
                d.EditorConfigure("DLG_Luna_FirstMeeting", DialoguePresentation.Overlay, "start", LunaFirstMeetingNodes(c.Luna)));

            c.LunaChat = LoadOrCreate<DialogueSO>(DialogueFolder + "/Samples/DLG_Luna_Chat.asset", d =>
                d.EditorConfigure("DLG_Luna_Chat", DialoguePresentation.Overlay, "start", LunaChatNodes(c.Luna)));

            c.MochiIntro = LoadOrCreate<DialogueSO>(DialogueFolder + "/Samples/DLG_Mochi_Intro.asset", d =>
                d.EditorConfigure("DLG_Mochi_Intro", DialoguePresentation.SideViewScene, "start", MochiIntroNodes(c.Mochi)));

            c.MochiPurr = LoadOrCreate<DialogueSO>(DialogueFolder + "/Samples/DLG_Mochi_Purr.asset", d =>
                d.EditorConfigure("DLG_Mochi_Purr", DialoguePresentation.Overlay, "start", new List<DialogueNode>
                {
                    Line("start", c.Mochi, "happy", "Prrr... the ovens are warm. That's all that matters.", null)
                }));

            c.CharacterDatabase = LoadOrCreate<CharacterDatabaseSO>(CharactersFolder + "/CharacterDatabase.asset", null);
            AddUnique(c.CharacterDatabase, c.Luna, c.Mochi);

            c.DialogueDatabase = LoadOrCreate<DialogueDatabaseSO>(DialogueFolder + "/DialogueDatabase.asset", null);
            AddUnique(c.DialogueDatabase, c.LunaFirstMeeting, c.LunaChat, c.MochiIntro, c.MochiPurr);

            AssetDatabase.SaveAssets();
            return c;
        }

        private static List<CharacterProfileSO.ExpressionSprites> Expressions(string key, Color color)
        {
            Sprite sideView = PlaceholderSprites.SideView(key, color);
            return new List<CharacterProfileSO.ExpressionSprites>
            {
                new CharacterProfileSO.ExpressionSprites { expression = "neutral", portrait = PlaceholderSprites.Portrait(key, color, false), sideView = sideView },
                new CharacterProfileSO.ExpressionSprites { expression = "happy", portrait = PlaceholderSprites.Portrait(key, color, true), sideView = sideView }
            };
        }

        private static List<DialogueNode> LunaFirstMeetingNodes(CharacterProfileSO luna)
        {
            DialogueNode start = Line("start", luna, "neutral", "Oh! The lanterns are lit again. Is the atelier open tonight?", null);
            start.speakerNameOverride = "???";
            start.choices.Add(Choice("Welcome! We just reopened.", "welcome", Relationship(luna, 5)));
            start.choices.Add(Choice("...And you are?", "name"));

            DialogueNode end = Line("end", null, null, "Luna smiles and drifts toward the window seat.", null);
            end.onEnter.Add(new DialogueAction { type = DialogueActionType.SetFlag, key = "met_luna" });

            return new List<DialogueNode>
            {
                start,
                Line("welcome", luna, "happy", "How wonderful. I'm Luna. I used to come here every full moon.", "end"),
                Line("name", luna, "neutral", "Luna. I used to come here every full moon... I hope that's still allowed.", "end"),
                end
            };
        }

        private static List<DialogueNode> LunaChatNodes(CharacterProfileSO luna)
        {
            // Logic node: no text, just branches on affection.
            DialogueNode start = new DialogueNode { id = "start", nextNodeId = "plain" };
            start.jumps.Add(new DialogueJump
            {
                targetNodeId = "warm",
                conditions = new List<DialogueCondition>
                {
                    new DialogueCondition { type = DialogueConditionType.RelationshipAtLeast, character = luna, value = 5 }
                }
            });

            return new List<DialogueNode>
            {
                start,
                Line("warm", luna, "happy", "The whole room feels warmer since you came back. I like it.", null),
                Line("plain", luna, "neutral", "Evening. The moon looks a little shy tonight.", null)
            };
        }

        private static List<DialogueNode> MochiIntroNodes(CharacterProfileSO mochi)
        {
            DialogueNode s2 = Line("s2", mochi, "happy", "I'm Mochi. I keep the ovens warm and the mice honest.", null);
            s2.choices.Add(Choice("Nice to meet you, Mochi.", "polite", Relationship(mochi, 3)));
            s2.choices.Add(Choice("Cats can talk?", "cheeky"));

            DialogueNode done = new DialogueNode { id = "done" };
            done.onEnter.Add(new DialogueAction { type = DialogueActionType.RaiseEvent, key = "mochi_intro_done" });

            return new List<DialogueNode>
            {
                Line("start", mochi, "neutral", "Mrrp. So you're the one who woke up the old atelier.", "s2"),
                s2,
                Line("polite", mochi, "happy", "Polite. I approve. Now go make something sweet.", "done"),
                Line("cheeky", mochi, "neutral", "Only to people worth talking to. Consider yourself lucky.", "done"),
                done
            };
        }

        private static DialogueNode Line(string id, CharacterProfileSO speaker, string expression, string text, string next)
        {
            return new DialogueNode { id = id, speaker = speaker, expression = expression, text = text, nextNodeId = next };
        }

        private static DialogueChoice Choice(string text, string next, DialogueAction action = null)
        {
            DialogueChoice choice = new DialogueChoice { text = text, nextNodeId = next };
            if (action != null) choice.actions.Add(action);
            return choice;
        }

        private static DialogueAction Relationship(CharacterProfileSO character, int amount)
        {
            return new DialogueAction { type = DialogueActionType.ChangeRelationship, character = character, value = amount };
        }

        // ================================================================ 2. Bootstrap

        private static void SetupBootstrap(SampleContent content)
        {
            Scene scene = EditorSceneManager.OpenScene($"{ScenesFolder}/{SceneNames.Bootstrap}.unity", OpenSceneMode.Single);
            GameObject root = M2TestRoomBuilder.FindRoot(scene, "GameSystems");
            if (root == null)
            {
                Debug.LogError("[Nocturne] GameSystems not found in Bootstrap. Run 'Run M1 Project Setup' first.");
                return;
            }

            RelationshipSystem relationships = GetOrAdd<RelationshipSystem>(GetOrCreateChild(root.transform, "RelationshipSystem"));
            DialogueManager dialogueManager = GetOrAdd<DialogueManager>(GetOrCreateChild(root.transform, "DialogueManager"));
            SetReference(dialogueManager, "dialogueDatabase", content.DialogueDatabase);

            BootstrapLoader loader = root.GetComponent<BootstrapLoader>();
            if (loader != null)
            {
                SetReference(loader, "relationshipSystem", relationships);
                SetReference(loader, "dialogueManager", dialogueManager);
            }

            // EventSystem (needed for clicking and selecting UI). Persistent with the rest of GameSystems.
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObject = GetOrCreateChild(root.transform, "EventSystem");
                eventSystemObject.AddComponent<EventSystem>();
                InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
                if (InputSystem.actions != null)
                {
                    module.actionsAsset = InputSystem.actions;
                }
            }

            if (root.transform.Find("DialogueCanvas") == null)
            {
                BuildDialogueCanvas(root.transform);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildDialogueCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("DialogueCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            // Dialogue box along the bottom of the screen.
            RectTransform box = CreateUI("DialogueBox", canvasObject.transform);
            box.anchorMin = new Vector2(0f, 0f);
            box.anchorMax = new Vector2(1f, 0f);
            box.pivot = new Vector2(0.5f, 0f);
            box.sizeDelta = new Vector2(-32f, 104f);
            box.anchoredPosition = new Vector2(0f, 12f);
            box.gameObject.AddComponent<Image>().color = BoxColor;
            box.gameObject.AddComponent<CanvasGroup>();
            DialogueUI ui = box.gameObject.AddComponent<DialogueUI>();

            // Portrait on the left.
            RectTransform portraitRect = CreateUI("Portrait", box);
            portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.sizeDelta = new Vector2(88f, 88f);
            portraitRect.anchoredPosition = new Vector2(56f, 0f);
            Image portrait = portraitRect.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            // Body text.
            RectTransform bodyRect = CreateUI("Body", box);
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(120f, 12f);
            bodyRect.offsetMax = new Vector2(-24f, -14f);
            TextMeshProUGUI body = CreateText(bodyRect.gameObject, string.Empty, 14f, TextAlignmentOptions.TopLeft);
            TypewriterEffect typewriter = bodyRect.gameObject.AddComponent<TypewriterEffect>();

            // "Continue" indicator, bottom right.
            RectTransform indicatorRect = CreateUI("ContinueIndicator", box);
            indicatorRect.anchorMin = indicatorRect.anchorMax = new Vector2(1f, 0f);
            indicatorRect.sizeDelta = new Vector2(10f, 10f);
            indicatorRect.anchoredPosition = new Vector2(-14f, 12f);
            Image indicator = indicatorRect.gameObject.AddComponent<Image>();
            indicator.sprite = PlaceholderSprites.Prompt;
            indicator.color = new Color(1f, 0.9f, 0.5f);
            indicator.raycastTarget = false;

            // Name tag sitting on the top edge of the box.
            RectTransform nameBoxRect = CreateUI("NameBox", box);
            nameBoxRect.anchorMin = nameBoxRect.anchorMax = new Vector2(0f, 1f);
            nameBoxRect.pivot = new Vector2(0f, 0f);
            nameBoxRect.sizeDelta = new Vector2(150f, 22f);
            nameBoxRect.anchoredPosition = new Vector2(12f, 0f);
            Image nameBoxImage = nameBoxRect.gameObject.AddComponent<Image>();
            nameBoxImage.color = NameBoxColor;
            nameBoxImage.raycastTarget = false;

            RectTransform nameRect = CreateUI("Name", nameBoxRect);
            Stretch(nameRect, 8f, 2f);
            TextMeshProUGUI nameText = CreateText(nameRect.gameObject, "Name", 14f, TextAlignmentOptions.MidlineLeft);
            nameText.fontStyle = FontStyles.Bold;

            // Choices stacked above the right side of the box.
            RectTransform choicesRect = CreateUI("Choices", box);
            choicesRect.anchorMin = choicesRect.anchorMax = new Vector2(1f, 1f);
            choicesRect.pivot = new Vector2(1f, 0f);
            choicesRect.sizeDelta = new Vector2(230f, 0f);
            choicesRect.anchoredPosition = new Vector2(-12f, 6f);
            VerticalLayoutGroup layout = choicesRect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.LowerRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = choicesRect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            DialogueChoiceButton template = BuildChoiceTemplate(choicesRect);

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("nameLabel").objectReferenceValue = nameText;
            so.FindProperty("nameBox").objectReferenceValue = nameBoxRect.gameObject;
            so.FindProperty("portrait").objectReferenceValue = portrait;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("typewriter").objectReferenceValue = typewriter;
            so.FindProperty("continueIndicator").objectReferenceValue = indicatorRect.gameObject;
            so.FindProperty("choicesContainer").objectReferenceValue = choicesRect;
            so.FindProperty("choiceTemplate").objectReferenceValue = template;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DialogueChoiceButton BuildChoiceTemplate(Transform parent)
        {
            RectTransform rect = CreateUI("ChoiceTemplate", parent);
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = Color.white;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = ChoiceColor;
            colors.highlightedColor = new Color(0.45f, 0.28f, 0.5f, 1f);
            colors.selectedColor = new Color(0.55f, 0.34f, 0.58f, 1f);
            colors.pressedColor = new Color(0.7f, 0.45f, 0.7f, 1f);
            button.colors = colors;

            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 24f;

            RectTransform labelRect = CreateUI("Label", rect);
            Stretch(labelRect, 10f, 3f);
            TextMeshProUGUI label = CreateText(labelRect.gameObject, "Choice", 13f, TextAlignmentOptions.MidlineLeft);

            DialogueChoiceButton choice = rect.gameObject.AddComponent<DialogueChoiceButton>();
            SetReference(choice, "label", label);

            rect.gameObject.SetActive(false);
            return choice;
        }

        // ================================================================ 3. Dialogue_Scene stage

        private static void SetupDialogueScene()
        {
            string path = $"{ScenesFolder}/{SceneNames.Dialogue}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning("[Nocturne] Dialogue_Scene not found; run 'Run M1 Project Setup' first.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (M2TestRoomBuilder.FindRoot(scene, "DialogueStage") != null)
            {
                return;
            }

            GameObject stageObject = new GameObject("DialogueStage");
            DialogueStage stage = stageObject.AddComponent<DialogueStage>();

            SpriteRenderer backdrop = M2TestRoomBuilder.CreateSprite("Backdrop", stageObject.transform, PlaceholderSprites.Fill,
                new Color(0.2f, 0.13f, 0.24f), new Vector2(0f, 0f), new Vector2(30f, 14f));
            backdrop.sortingOrder = -100;

            SpriteRenderer floor = M2TestRoomBuilder.CreateSprite("Floor", stageObject.transform, PlaceholderSprites.Fill,
                new Color(0.32f, 0.22f, 0.2f), new Vector2(0f, -4f), new Vector2(30f, 4f));
            floor.sortingOrder = -90;

            SpriteRenderer left = M2TestRoomBuilder.CreateSprite("LeftSlot", stageObject.transform, null, Color.white, new Vector2(-3.5f, -2f), new Vector2(3f, 3f));
            SpriteRenderer right = M2TestRoomBuilder.CreateSprite("RightSlot", stageObject.transform, null, Color.white, new Vector2(3.5f, -2f), new Vector2(3f, 3f));

            M2TestRoomBuilder.CreateGlobalLight(stageObject.transform);

            SerializedObject so = new SerializedObject(stage);
            so.FindProperty("leftSlot").objectReferenceValue = left;
            so.FindProperty("rightSlot").objectReferenceValue = right;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ================================================================ 4. NPCs in the café

        private static void SetupCafeNpcs(SampleContent content)
        {
            string path = $"{ScenesFolder}/{SceneNames.CafeExploration}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (M2TestRoomBuilder.FindRoot(scene, "M3_SampleNPCs") != null)
            {
                return;
            }

            GameObject group = new GameObject("M3_SampleNPCs");

            BuildNpc(group.transform, "NPC_Luna", content.Luna, new Vector2(4.5f, -1.5f), new Color(0.62f, 0.68f, 0.98f),
                NpcMovement.MovementMode.Idle,
                new List<NpcController.Conversation>
                {
                    new NpcController.Conversation
                    {
                        dialogue = content.LunaChat,
                        conditions = new List<DialogueCondition> { new DialogueCondition { type = DialogueConditionType.HasFlag, key = "met_luna" } }
                    }
                },
                content.LunaFirstMeeting);

            BuildNpc(group.transform, "NPC_Mochi", content.Mochi, new Vector2(-5f, -2.5f), new Color(0.98f, 0.7f, 0.4f),
                NpcMovement.MovementMode.Wander,
                new List<NpcController.Conversation>
                {
                    new NpcController.Conversation
                    {
                        dialogue = content.MochiPurr,
                        conditions = new List<DialogueCondition> { new DialogueCondition { type = DialogueConditionType.DialogueCompleted, key = "DLG_Mochi_Intro" } }
                    }
                },
                content.MochiIntro);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildNpc(Transform parent, string objectName, CharacterProfileSO profile, Vector2 position, Color tint,
            NpcMovement.MovementMode mode, List<NpcController.Conversation> conversations, DialogueSO fallback)
        {
            GameObject npc = new GameObject(objectName);
            npc.transform.SetParent(parent, false);
            npc.transform.position = position;

            Rigidbody2D body = npc.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;

            CapsuleCollider2D feet = npc.AddComponent<CapsuleCollider2D>();
            feet.direction = CapsuleDirection2D.Horizontal;
            feet.size = new Vector2(0.6f, 0.3f);
            feet.offset = new Vector2(0f, 0.15f);

            NpcMovement movement = npc.AddComponent<NpcMovement>();
            SerializedObject movementSo = new SerializedObject(movement);
            movementSo.FindProperty("mode").enumValueIndex = (int)mode;
            movementSo.FindProperty("wanderRadius").floatValue = 2.5f;
            movementSo.ApplyModifiedPropertiesWithoutUndo();

            NpcController controller = npc.AddComponent<NpcController>();
            controller.EditorConfigure(profile, conversations, fallback);
            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("promptOffset").vector2Value = new Vector2(0f, 1.7f);
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            npc.AddComponent<CharacterAnimator>();
            M2TestRoomBuilder.CreateSprite("Sprite", npc.transform, PlaceholderSprites.Character, tint, Vector2.zero, Vector2.one);
        }

        // ================================================================ Helpers

        private static T LoadOrCreate<T>(string path, System.Action<T> configureNew) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            if (configureNew != null) configureNew(asset);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void AddUnique(CharacterDatabaseSO database, params CharacterProfileSO[] profiles)
        {
            List<CharacterProfileSO> list = new List<CharacterProfileSO>(database.Characters);
            foreach (CharacterProfileSO profile in profiles)
            {
                if (profile != null && !list.Contains(profile)) list.Add(profile);
            }

            database.EditorSetCharacters(list);
            EditorUtility.SetDirty(database);
        }

        private static void AddUnique(DialogueDatabaseSO database, params DialogueSO[] dialogues)
        {
            List<DialogueSO> list = new List<DialogueSO>(database.Dialogues);
            foreach (DialogueSO dialogue in dialogues)
            {
                if (dialogue != null && !list.Contains(dialogue)) list.Add(dialogue);
            }

            database.EditorSetDialogues(list);
            EditorUtility.SetDirty(database);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
        }

        private static RectTransform CreateUI(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, float horizontalPadding, float verticalPadding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }

        private static TextMeshProUGUI CreateText(GameObject target, string text, float size, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI tmp = target.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = TextColor;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject GetOrCreateChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void SetReference(Object target, string property, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogError($"[Nocturne] Field '{property}' not found on {target.GetType().Name}.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}