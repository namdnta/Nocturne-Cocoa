using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.Data
{
    /// <summary>
    /// Static data for one character (NPC, customer, familiar). Shared by both perspectives:
    /// the top-down NPC in the café and the side-view portrait/stage sprite in dialogue use the same profile.
    ///
    /// Runtime state (affection, "has met") is NOT stored here; it lives in the save data via RelationshipSystem.
    /// </summary>
    [CreateAssetMenu(menuName = "Nocturne Cocoa/Characters/Character Profile", fileName = "CHR_NewCharacter")]
    public class CharacterProfileSO : ScriptableObject
    {
        [Serializable]
        public class ExpressionSprites
        {
            [Tooltip("e.g. neutral, happy, sad, surprised, angry. Referenced by dialogue lines.")]
            public string expression = "neutral";

            [Tooltip("Face portrait shown in the dialogue box (overlay dialogue).")]
            public Sprite portrait;

            [Tooltip("Full-body side-view sprite shown on the dialogue stage (Dialogue_Scene).")]
            public Sprite sideView;
        }

        [Header("Identity")]
        [Tooltip("Unique, never-changing ID used in save files, e.g. \"luna\". Don't rename after release.")]
        [SerializeField] private string characterId;
        [SerializeField] private string displayName = "New Character";
        [SerializeField] private Color nameColor = Color.white;

        [TextArea(2, 4)]
        [Tooltip("Short description for the Character Journal (M7).")]
        [SerializeField] private string journalDescription;

        [Header("Art")]
        [Tooltip("First entry is the default expression.")]
        [SerializeField] private List<ExpressionSprites> expressions = new List<ExpressionSprites>();

        [Header("Voice")]
        [Tooltip("Optional short 'blip' played while this character's text types out.")]
        [SerializeField] private AudioClip voiceBlip;
        [SerializeField, Range(0.5f, 2f)] private float voicePitch = 1f;

        public string CharacterId
        {
            get { return characterId; }
        }

        public string DisplayName
        {
            get { return displayName; }
        }

        public Color NameColor
        {
            get { return nameColor; }
        }

        public string JournalDescription
        {
            get { return journalDescription; }
        }

        public AudioClip VoiceBlip
        {
            get { return voiceBlip; }
        }

        public float VoicePitch
        {
            get { return voicePitch; }
        }

        public IReadOnlyList<ExpressionSprites> Expressions
        {
            get { return expressions; }
        }

        public Sprite GetPortrait(string expression)
        {
            ExpressionSprites entry = FindExpression(expression);
            return entry != null ? entry.portrait : null;
        }

        public Sprite GetSideView(string expression)
        {
            ExpressionSprites entry = FindExpression(expression);
            if (entry != null && entry.sideView != null)
            {
                return entry.sideView;
            }

            // Fall back to the default expression's side-view sprite.
            return expressions.Count > 0 ? expressions[0].sideView : null;
        }

        /// <summary>The requested expression, or the default (first) one if it doesn't exist.</summary>
        private ExpressionSprites FindExpression(string expression)
        {
            if (expressions.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(expression))
            {
                for (int i = 0; i < expressions.Count; i++)
                {
                    if (string.Equals(expressions[i].expression, expression, StringComparison.OrdinalIgnoreCase))
                    {
                        return expressions[i];
                    }
                }
            }

            return expressions[0];
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                characterId = name.Replace("CHR_", string.Empty).ToLowerInvariant();
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup tool to create sample characters.</summary>
        public void EditorConfigure(string id, string shownName, Color color, string description, List<ExpressionSprites> sprites)
        {
            characterId = id;
            displayName = shownName;
            nameColor = color;
            journalDescription = description;
            expressions = sprites;
        }
#endif
    }
}