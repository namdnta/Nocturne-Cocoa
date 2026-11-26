using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.Data
{
    /// <summary>
    /// One step of a conversation.
    ///
    /// - A normal node shows Text (spoken by Speaker), then continues to Next Node Id.
    /// - If it has Choices, the player picks one instead (each choice has its own next node).
    /// - If Text is empty, it's a "logic" node: nothing is shown; it runs its actions and jumps straight on.
    /// - Jumps are checked before Next Node Id: the first jump whose conditions all pass wins.
    ///   Use this for "if affection &gt;= 10 say X, otherwise say Y".
    /// - An empty next node ID ends the dialogue.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        [Tooltip("Unique within this dialogue, e.g. \"start\", \"ask_name\", \"end_happy\".")]
        public string id = "start";

        [Tooltip("Who speaks. Empty = narrator (no name, no portrait).")]
        public CharacterProfileSO speaker;

        [Tooltip("Optional: show this name instead of the speaker's (e.g. \"???\" before you learn their name).")]
        public string speakerNameOverride;

        [Tooltip("Portrait/pose to use, e.g. \"happy\". Empty = the character's default.")]
        public string expression;

        [TextArea(2, 5)]
        public string text;

        [Tooltip("Node to go to after this one. Empty = end of dialogue.")]
        public string nextNodeId;

        [Tooltip("Conditional jumps, checked in order before Next Node Id.")]
        public List<DialogueJump> jumps = new List<DialogueJump>();

        [Tooltip("If not empty, the player must pick one of these.")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [Tooltip("Run when this node is reached (set flags, change affection, raise events).")]
        public List<DialogueAction> onEnter = new List<DialogueAction>();

        public bool IsLogicNode
        {
            get { return string.IsNullOrWhiteSpace(text); }
        }
    }

    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("What the player says.")]
        public string text;

        [Tooltip("Node to go to when picked. Empty = end of dialogue.")]
        public string nextNodeId;

        [Tooltip("Choice is hidden unless all of these pass.")]
        public List<DialogueCondition> conditions = new List<DialogueCondition>();

        [Tooltip("Run when picked.")]
        public List<DialogueAction> actions = new List<DialogueAction>();
    }

    [Serializable]
    public class DialogueJump
    {
        public List<DialogueCondition> conditions = new List<DialogueCondition>();
        public string targetNodeId;
    }
}