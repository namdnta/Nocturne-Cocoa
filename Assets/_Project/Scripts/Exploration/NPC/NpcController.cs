using System;
using System.Collections.Generic;
using NocturneCocoa.Characters;
using NocturneCocoa.Core;
using NocturneCocoa.Data;
using NocturneCocoa.Dialogue;
using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// An NPC the player can talk to in the top-down café. Picks the first conversation whose conditions
    /// pass (e.g. "first meeting" vs "regular chat"), stops moving and faces the player while talking,
    /// and marks the character as met.
    /// </summary>
    [AddComponentMenu("Nocturne Cocoa/Exploration/NPC Controller")]
    public class NpcController : Interactable
    {
        [Serializable]
        public class Conversation
        {
            public DialogueSO dialogue;

            [Tooltip("All must pass for this conversation to be chosen.")]
            public List<DialogueCondition> conditions = new List<DialogueCondition>();
        }

        [Header("Character")]
        [SerializeField] private CharacterProfileSO profile;

        [Header("Dialogue")]
        [Tooltip("Checked top to bottom; the first one whose conditions pass is used.")]
        [SerializeField] private List<Conversation> conversations = new List<Conversation>();

        [Tooltip("Used when no conversation above matches.")]
        [SerializeField] private DialogueSO fallbackDialogue;

        [SerializeField] private bool facePlayerWhenTalking = true;

        private NpcMovement movement;
        private DialogueManager dialogueManager;
        private RelationshipSystem relationships;
        private GameSession session;
        private bool isTalking;

        public CharacterProfileSO Profile
        {
            get { return profile; }
        }

        private void Awake()
        {
            movement = GetComponent<NpcMovement>();
        }

        private void Start()
        {
            Services.TryGet(out dialogueManager);
            Services.TryGet(out relationships);
            Services.TryGet(out session);

            if (dialogueManager != null)
            {
                dialogueManager.DialogueEnded += HandleDialogueEnded;
            }
        }

        private void OnDestroy()
        {
            if (dialogueManager != null)
            {
                dialogueManager.DialogueEnded -= HandleDialogueEnded;
            }
        }

        public override bool CanInteract(PlayerInteractor interactor)
        {
            return base.CanInteract(interactor)
                && dialogueManager != null
                && !dialogueManager.IsBusy
                && SelectDialogue() != null;
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            DialogueSO dialogue = SelectDialogue();
            if (dialogue == null)
            {
                return;
            }

            if (movement != null)
            {
                movement.Pause(this);
                if (facePlayerWhenTalking)
                {
                    movement.FaceTowards(interactor.transform.position - transform.position);
                }
            }

            if (relationships != null && profile != null)
            {
                relationships.MarkMet(profile.CharacterId);
            }

            isTalking = dialogueManager.StartDialogue(dialogue);
            if (!isTalking && movement != null)
            {
                movement.Resume(this);
            }
        }

        /// <summary>The dialogue this NPC would start right now, or null.</summary>
        public DialogueSO SelectDialogue()
        {
            foreach (Conversation conversation in conversations)
            {
                if (conversation != null && conversation.dialogue != null
                    && DialogueConditionEvaluator.AllMet(conversation.conditions, session, relationships))
                {
                    return conversation.dialogue;
                }
            }

            return fallbackDialogue;
        }

        private void HandleDialogueEnded(DialogueSO dialogue)
        {
            if (!isTalking)
            {
                return;
            }

            isTalking = false;
            if (movement != null)
            {
                movement.Resume(this);
            }
        }

        protected override void Reset()
        {
            base.Reset();
            promptText = "Talk";
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup tool to create sample NPCs.</summary>
        public void EditorConfigure(CharacterProfileSO character, List<Conversation> list, DialogueSO fallback)
        {
            profile = character;
            conversations = list;
            fallbackDialogue = fallback;
            promptText = "Talk";
        }
#endif
    }
}