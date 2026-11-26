using System.Collections.Generic;
using NocturneCocoa.Characters;
using NocturneCocoa.Core;
using NocturneCocoa.Data;
using UnityEngine;

namespace NocturneCocoa.Dialogue
{
    /// <summary>
    /// Checks DialogueConditions against the current game state. Used by dialogue choices and jumps,
    /// and by NPCs to pick which conversation to start.
    /// </summary>
    public static class DialogueConditionEvaluator
    {
        /// <summary>True if every condition passes (an empty or null list passes).</summary>
        public static bool AllMet(List<DialogueCondition> conditions, GameSession session, RelationshipSystem relationships)
        {
            if (conditions == null)
            {
                return true;
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                if (!IsMet(conditions[i], session, relationships))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsMet(DialogueCondition condition, GameSession session, RelationshipSystem relationships)
        {
            if (condition == null)
            {
                return true;
            }

            string characterId = condition.character != null ? condition.character.CharacterId : null;

            switch (condition.type)
            {
                case DialogueConditionType.HasFlag:
                    return session != null && session.HasFlag(condition.key);

                case DialogueConditionType.MissingFlag:
                    return session == null || !session.HasFlag(condition.key);

                case DialogueConditionType.RelationshipAtLeast:
                    return relationships != null && relationships.GetPoints(characterId) >= condition.value;

                case DialogueConditionType.RelationshipBelow:
                    return relationships == null || relationships.GetPoints(characterId) < condition.value;

                case DialogueConditionType.HasMetCharacter:
                    return relationships != null && relationships.HasMet(characterId);

                case DialogueConditionType.DialogueCompleted:
                    return session != null && session.HasCompletedDialogue(condition.key);

                case DialogueConditionType.DayAtLeast:
                    return session != null && session.HasActiveSession && session.Data.progress.day >= condition.value;

                default:
                    Debug.LogWarning($"[Dialogue] Unknown condition type {condition.type}.");
                    return false;
            }
        }
    }
}