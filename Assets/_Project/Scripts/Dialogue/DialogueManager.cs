using System;
using System.Collections.Generic;
using NocturneCocoa.Characters;
using NocturneCocoa.Core;
using NocturneCocoa.Core.Events;
using NocturneCocoa.Data;
using NocturneCocoa.SceneManagement;
using UnityEngine;

namespace NocturneCocoa.Dialogue
{
    /// <summary>
    /// Runs conversations: walks the nodes, checks conditions, runs actions and tells presentations
    /// (DialogueUI, DialogueStage) what to show through events. It has no UI code itself, so the same
    /// dialogue works as a top-down overlay or in the side-view Dialogue_Scene.
    ///
    /// Persistent system (child of GameSystems), found via Services.Get&lt;DialogueManager&gt;().
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueManager : MonoBehaviour
    {
        private const int MaxLogicSteps = 64;

        [Tooltip("Lets dialogues be started by ID: StartDialogue(\"DLG_Luna_FirstMeeting\").")]
        [SerializeField] private DialogueDatabaseSO dialogueDatabase;

        [Tooltip("Ignore advance/choice input for this long after a dialogue starts, so the key that " +
                 "started it doesn't also skip the first line.")]
        [SerializeField, Min(0f)] private float inputGraceSeconds = 0.2f;

        [Header("Optional event channels")]
        [Tooltip("Raised with the event name of every RaiseEvent dialogue action.")]
        [SerializeField] private StringEventChannelSO dialogueEventChannel;

        public event Action<DialogueSO> DialogueStarted;

        /// <summary>A new line is ready to show.</summary>
        public event Action<DialogueLine> LineShown;

        public event Action<DialogueSO> DialogueEnded;

        /// <summary>A RaiseEvent action ran (e.g. "unlock_recipe_moon_truffle").</summary>
        public event Action<string> CustomEvent;

        private readonly List<DialogueChoice> availableChoices = new List<DialogueChoice>();

        private GameSession session;
        private RelationshipSystem relationships;
        private InputManager input;
        private SceneFlowController sceneFlow;

        private DialogueNode currentNode;
        private float inputUnlockTime;

        // Side-view dialogues: the dialogue waiting for Dialogue_Scene to load, and where to go back to.
        private DialogueSO pendingSideViewDialogue;
        private string returnScene;

        public bool IsRunning { get; private set; }

        public DialogueSO CurrentDialogue { get; private set; }

        public DialogueLine CurrentLine { get; private set; }

        /// <summary>True when Advance/Choose calls are accepted (dialogue running and grace period over).</summary>
        public bool AcceptsInput
        {
            get { return IsRunning && Time.unscaledTime >= inputUnlockTime; }
        }

        /// <summary>True while a side-view dialogue is loading or running.</summary>
        public bool IsBusy
        {
            get { return IsRunning || pendingSideViewDialogue != null; }
        }

        public DialogueDatabaseSO Database
        {
            get { return dialogueDatabase; }
        }

        /// <summary>Called by BootstrapLoader.</summary>
        public void Initialize(GameSession gameSession, RelationshipSystem relationshipSystem, InputManager inputManager, SceneFlowController sceneFlowController)
        {
            session = gameSession;
            relationships = relationshipSystem;
            input = inputManager;
            sceneFlow = sceneFlowController;
            sceneFlow.SceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (sceneFlow != null)
            {
                sceneFlow.SceneLoaded -= HandleSceneLoaded;
            }
        }

        // ---------------------------------------------------------------- Starting

        public bool StartDialogue(string dialogueId)
        {
            DialogueSO dialogue = dialogueDatabase != null ? dialogueDatabase.Find(dialogueId) : null;
            if (dialogue == null)
            {
                Debug.LogWarning($"[Dialogue] No dialogue with ID '{dialogueId}' in the Dialogue Database.");
                return false;
            }

            return StartDialogue(dialogue);
        }

        public bool StartDialogue(DialogueSO dialogue)
        {
            if (dialogue == null)
            {
                return false;
            }

            if (IsBusy)
            {
                Debug.LogWarning($"[Dialogue] Can't start '{dialogue.DialogueId}': another dialogue is running.");
                return false;
            }

            if (dialogue.FindNode(dialogue.StartNodeId) == null)
            {
                Debug.LogError($"[Dialogue] '{dialogue.DialogueId}' has no start node '{dialogue.StartNodeId}'.", dialogue);
                return false;
            }

            bool needsStage = dialogue.Presentation == DialoguePresentation.SideViewScene
                && sceneFlow != null
                && sceneFlow.CurrentSceneName != SceneNames.Dialogue;

            if (needsStage)
            {
                returnScene = sceneFlow.CurrentSceneName;
                pendingSideViewDialogue = dialogue;
                if (input != null) input.SetMode(InputManager.InputMode.None);

                if (!sceneFlow.LoadScene(SceneNames.Dialogue))
                {
                    pendingSideViewDialogue = null;
                    returnScene = null;
                    if (input != null) input.SetMode(InputManager.InputMode.Gameplay);
                    return false;
                }

                return true;
            }

            Begin(dialogue);
            return true;
        }

        private void HandleSceneLoaded(string sceneName)
        {
            if (pendingSideViewDialogue != null && sceneName == SceneNames.Dialogue)
            {
                DialogueSO dialogue = pendingSideViewDialogue;
                pendingSideViewDialogue = null;
                Begin(dialogue);
            }
        }

        private void Begin(DialogueSO dialogue)
        {
            IsRunning = true;
            CurrentDialogue = dialogue;
            inputUnlockTime = Time.unscaledTime + inputGraceSeconds;

            if (input != null) input.SetMode(InputManager.InputMode.UI);

            Debug.Log($"[Dialogue] Started '{dialogue.DialogueId}'.");
            if (DialogueStarted != null) DialogueStarted.Invoke(dialogue);

            GoTo(dialogue.StartNodeId);
        }

        // ---------------------------------------------------------------- Player input

        /// <summary>Continue past the current line. Ignored while choices are shown.</summary>
        public void Advance()
        {
            if (!AcceptsInput || currentNode == null || availableChoices.Count > 0)
            {
                return;
            }

            GoTo(ResolveNext(currentNode));
        }

        /// <summary>Pick one of CurrentLine.Choices by index.</summary>
        public void Choose(int index)
        {
            if (!AcceptsInput || index < 0 || index >= availableChoices.Count)
            {
                return;
            }

            DialogueChoice choice = availableChoices[index];
            DialogueNode speakerNode = currentNode;
            availableChoices.Clear();

            RunActions(choice.actions, speakerNode);
            GoTo(choice.nextNodeId);
        }

        /// <summary>Stop immediately (e.g. if something interrupts the conversation).</summary>
        public void EndDialogue()
        {
            if (IsRunning)
            {
                Finish();
            }
        }

        // ---------------------------------------------------------------- Node flow

        private void GoTo(string nodeId)
        {
            // Logic nodes (no text) are processed in a loop until a node with text is reached or the dialogue ends.
            for (int step = 0; step < MaxLogicSteps; step++)
            {
                if (string.IsNullOrEmpty(nodeId))
                {
                    Finish();
                    return;
                }

                DialogueNode node = CurrentDialogue.FindNode(nodeId);
                if (node == null)
                {
                    Debug.LogError($"[Dialogue] '{CurrentDialogue.DialogueId}': node '{nodeId}' not found. Ending dialogue.", CurrentDialogue);
                    Finish();
                    return;
                }

                currentNode = node;
                RunActions(node.onEnter, node);

                if (!node.IsLogicNode)
                {
                    ShowNode(node);
                    return;
                }

                nodeId = ResolveNext(node);
            }

            Debug.LogError($"[Dialogue] '{CurrentDialogue.DialogueId}': more than {MaxLogicSteps} logic nodes in a row (loop?). Ending dialogue.", CurrentDialogue);
            Finish();
        }

        private void ShowNode(DialogueNode node)
        {
            availableChoices.Clear();
            List<string> choiceTexts = new List<string>();

            foreach (DialogueChoice choice in node.choices)
            {
                if (DialogueConditionEvaluator.AllMet(choice.conditions, session, relationships))
                {
                    availableChoices.Add(choice);
                    choiceTexts.Add(choice.text);
                }
            }

            CurrentLine = new DialogueLine(CurrentDialogue, node, choiceTexts);
            if (LineShown != null) LineShown.Invoke(CurrentLine);
        }

        private string ResolveNext(DialogueNode node)
        {
            foreach (DialogueJump jump in node.jumps)
            {
                if (DialogueConditionEvaluator.AllMet(jump.conditions, session, relationships))
                {
                    return jump.targetNodeId;
                }
            }

            return node.nextNodeId;
        }

        private void Finish()
        {
            DialogueSO finished = CurrentDialogue;

            IsRunning = false;
            currentNode = null;
            CurrentLine = null;
            CurrentDialogue = null;
            availableChoices.Clear();

            if (session != null && finished != null)
            {
                session.MarkDialogueCompleted(finished.DialogueId);
            }

            Debug.Log($"[Dialogue] Ended '{(finished != null ? finished.DialogueId : "?")}'.");
            if (DialogueEnded != null) DialogueEnded.Invoke(finished);

            if (input != null) input.SetMode(InputManager.InputMode.Gameplay);

            if (!string.IsNullOrEmpty(returnScene))
            {
                string target = returnScene;
                returnScene = null;
                sceneFlow.LoadScene(target);
            }
        }

        // ---------------------------------------------------------------- Actions

        private void RunActions(List<DialogueAction> actions, DialogueNode contextNode)
        {
            if (actions == null)
            {
                return;
            }

            foreach (DialogueAction action in actions)
            {
                if (action == null) continue;

                switch (action.type)
                {
                    case DialogueActionType.SetFlag:
                        if (session != null) session.SetFlag(action.key);
                        break;

                    case DialogueActionType.ClearFlag:
                        if (session != null) session.ClearFlag(action.key);
                        break;

                    case DialogueActionType.ChangeRelationship:
                        CharacterProfileSO target = action.character != null ? action.character : (contextNode != null ? contextNode.speaker : null);
                        if (relationships != null && target != null)
                        {
                            relationships.AddPoints(target.CharacterId, action.value);
                        }
                        else
                        {
                            Debug.LogWarning("[Dialogue] ChangeRelationship needs a Character (or a speaker on the node).");
                        }
                        break;

                    case DialogueActionType.RaiseEvent:
                        Debug.Log($"[Dialogue] Event '{action.key}'.");
                        if (CustomEvent != null) CustomEvent.Invoke(action.key);
                        if (dialogueEventChannel != null) dialogueEventChannel.Raise(action.key);
                        break;
                }
            }
        }
    }
}