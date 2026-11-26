using System;
using UnityEngine;

namespace NocturneCocoa.Data
{
    public enum DialogueConditionType
    {
        /// <summary>Story flag (Key) is set.</summary>
        HasFlag,
        /// <summary>Story flag (Key) is NOT set.</summary>
        MissingFlag,
        /// <summary>Character's affection points are &gt;= Value.</summary>
        RelationshipAtLeast,
        /// <summary>Character's affection points are &lt; Value.</summary>
        RelationshipBelow,
        /// <summary>The player has talked to Character before.</summary>
        HasMetCharacter,
        /// <summary>Dialogue with ID Key has been finished at least once.</summary>
        DialogueCompleted,
        /// <summary>Current in-game day is &gt;= Value.</summary>
        DayAtLeast
    }

    /// <summary>
    /// One requirement for a choice, a branch or an NPC conversation. All conditions in a list must pass.
    /// Only the fields relevant to the chosen Type are used.
    /// </summary>
    [Serializable]
    public class DialogueCondition
    {
        public DialogueConditionType type;

        [Tooltip("Flag name or dialogue ID (for HasFlag, MissingFlag, DialogueCompleted).")]
        public string key;

        [Tooltip("Character (for Relationship and HasMet conditions).")]
        public CharacterProfileSO character;

        [Tooltip("Number (for RelationshipAtLeast/Below, DayAtLeast).")]
        public int value;
    }

    public enum DialogueActionType
    {
        /// <summary>Set story flag Key.</summary>
        SetFlag,
        /// <summary>Clear story flag Key.</summary>
        ClearFlag,
        /// <summary>Add Value affection points to Character (negative to reduce). Empty Character = current speaker.</summary>
        ChangeRelationship,
        /// <summary>Broadcast a custom event named Key (e.g. "unlock_recipe_moon_truffle") for other systems.</summary>
        RaiseEvent
    }

    /// <summary>Something that happens when a dialogue node is reached or a choice is picked.</summary>
    [Serializable]
    public class DialogueAction
    {
        public DialogueActionType type;

        [Tooltip("Flag name or event name.")]
        public string key;

        [Tooltip("Character for ChangeRelationship. Leave empty to use the line's speaker.")]
        public CharacterProfileSO character;

        [Tooltip("Amount for ChangeRelationship.")]
        public int value;
    }
}