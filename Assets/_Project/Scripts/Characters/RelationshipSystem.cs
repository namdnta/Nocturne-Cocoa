using System;
using NocturneCocoa.Core;
using NocturneCocoa.Core.Events;
using NocturneCocoa.SaveSystem;
using UnityEngine;

namespace NocturneCocoa.Characters
{
    /// <summary>
    /// Tracks affection points and "has met" for every character. The values live in the current
    /// SaveData (GameSession.Data.relationships), so they are saved and loaded automatically.
    ///
    /// Character events: CharacterMet (first conversation) and LevelChanged (e.g. becoming Friends)
    /// let other systems react, such as unlocking a recipe or a story dialogue.
    /// </summary>
    [DisallowMultipleComponent]
    public class RelationshipSystem : MonoBehaviour
    {
        [Tooltip("Points can't go below / above these.")]
        [SerializeField] private int minPoints = 0;
        [SerializeField] private int maxPoints = 100;

        [Tooltip("Points needed for each level, ascending. Level 0 = first entry.")]
        [SerializeField] private int[] levelThresholds = { 0, 10, 30, 60, 100 };

        [Tooltip("Names shown in the Character Journal, same order as the thresholds.")]
        [SerializeField] private string[] levelNames = { "Stranger", "Acquaintance", "Friend", "Close Friend", "Kindred Spirit" };

        [Header("Optional event channels")]
        [Tooltip("Raised with the character ID when the player meets someone for the first time.")]
        [SerializeField] private StringEventChannelSO characterMetChannel;

        /// <summary>characterId, oldPoints, newPoints</summary>
        public event Action<string, int, int> PointsChanged;

        /// <summary>characterId, oldLevel, newLevel</summary>
        public event Action<string, int, int> LevelChanged;

        /// <summary>characterId</summary>
        public event Action<string> CharacterMet;

        private GameSession session;

        /// <summary>Called by BootstrapLoader.</summary>
        public void Initialize(GameSession gameSession)
        {
            session = gameSession;
        }

        public int GetPoints(string characterId)
        {
            RelationshipSaveEntry entry = Find(characterId, false);
            return entry != null ? entry.points : minPoints;
        }

        public int GetLevel(string characterId)
        {
            return LevelForPoints(GetPoints(characterId));
        }

        public string GetLevelName(string characterId)
        {
            int level = GetLevel(characterId);
            return level >= 0 && level < levelNames.Length ? levelNames[level] : level.ToString();
        }

        public bool HasMet(string characterId)
        {
            RelationshipSaveEntry entry = Find(characterId, false);
            return entry != null && entry.met;
        }

        public void MarkMet(string characterId)
        {
            RelationshipSaveEntry entry = Find(characterId, true);
            if (entry == null || entry.met)
            {
                return;
            }

            entry.met = true;
            Debug.Log($"[Relationship] Met '{characterId}'.");
            if (CharacterMet != null) CharacterMet.Invoke(characterId);
            if (characterMetChannel != null) characterMetChannel.Raise(characterId);
        }

        public void AddPoints(string characterId, int amount)
        {
            RelationshipSaveEntry entry = Find(characterId, true);
            if (entry == null || amount == 0)
            {
                return;
            }

            int oldPoints = entry.points;
            int oldLevel = LevelForPoints(oldPoints);
            entry.points = Mathf.Clamp(oldPoints + amount, minPoints, maxPoints);

            if (entry.points == oldPoints)
            {
                return;
            }

            Debug.Log($"[Relationship] '{characterId}' {oldPoints} -> {entry.points}.");
            if (PointsChanged != null) PointsChanged.Invoke(characterId, oldPoints, entry.points);

            int newLevel = LevelForPoints(entry.points);
            if (newLevel != oldLevel && LevelChanged != null)
            {
                LevelChanged.Invoke(characterId, oldLevel, newLevel);
            }
        }

        private int LevelForPoints(int points)
        {
            int level = 0;
            for (int i = 0; i < levelThresholds.Length; i++)
            {
                if (points >= levelThresholds[i])
                {
                    level = i;
                }
            }

            return level;
        }

        private RelationshipSaveEntry Find(string characterId, bool create)
        {
            if (string.IsNullOrEmpty(characterId) || session == null || !session.HasActiveSession)
            {
                return null;
            }

            foreach (RelationshipSaveEntry entry in session.Data.relationships)
            {
                if (entry.characterId == characterId)
                {
                    return entry;
                }
            }

            if (!create)
            {
                return null;
            }

            RelationshipSaveEntry created = new RelationshipSaveEntry { characterId = characterId, points = minPoints };
            session.Data.relationships.Add(created);
            return created;
        }
    }
}