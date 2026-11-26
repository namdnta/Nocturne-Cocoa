using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.SaveSystem
{
    /// <summary>
    /// Everything that is saved to disk for one save slot. This is the player's runtime progress and
    /// is kept completely separate from ScriptableObject assets (which hold static game data only).
    ///
    /// Rules for editing this class (JsonUtility is used for serialization):
    /// - Use public fields or [SerializeField] fields. Properties are not saved.
    /// - Dictionaries are not supported; use a List of small [Serializable] entry classes instead.
    /// - Refer to static data by ID string (e.g. an ingredient ID), never by asset reference.
    /// - When you rename/remove a field or change its meaning, bump SaveMigrator.CurrentVersion
    ///   and add a migration step.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int saveVersion;
        public string createdUtc;
        public string lastSavedUtc;
        public double totalPlayTimeSeconds;

        public PlayerSaveData player = new PlayerSaveData();
        public ProgressSaveData progress = new ProgressSaveData();

        // PLACEHOLDER sections for later milestones. Add them when the system is designed:
        // M3: relationships  (List<RelationshipEntry>)
        // M4: inventory      (List<ItemStackEntry>), discoveredRecipeIds (List<string>)
        // M6: currency, completed orders, upgrades

        public static SaveData CreateNew()
        {
            string now = DateTime.UtcNow.ToString("o");
            return new SaveData
            {
                saveVersion = SaveMigrator.CurrentVersion,
                createdUtc = now,
                lastSavedUtc = now,
                totalPlayTimeSeconds = 0d
            };
        }

        /// <summary>Checks the data is safe to use. Returns false with a reason if not.</summary>
        public bool Validate(out string error)
        {
            if (saveVersion <= 0)
            {
                error = $"Invalid save version {saveVersion}.";
                return false;
            }

            if (player == null)
            {
                error = "Missing player section.";
                return false;
            }

            if (progress == null)
            {
                error = "Missing progress section.";
                return false;
            }

            if (progress.day < 1)
            {
                error = $"Invalid day {progress.day}.";
                return false;
            }

            if (totalPlayTimeSeconds < 0d || double.IsNaN(totalPlayTimeSeconds))
            {
                error = "Invalid play time.";
                return false;
            }

            if (progress.storyFlags == null)
            {
                progress.storyFlags = new List<string>();
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public class PlayerSaveData
    {
        /// <summary>Scene to return to when continuing. Empty means "use the default start scene".</summary>
        public string sceneName;

        /// <summary>Top-down position in that scene. Filled in by the player controller in M2.</summary>
        public bool hasPosition;
        public Vector2 position;
    }

    [Serializable]
    public class ProgressSaveData
    {
        public int day = 1;

        /// <summary>
        /// Simple story/dialogue flags such as "met_luna" or "tutorial_done".
        /// Dialogue conditions in M3 will read these.
        /// </summary>
        public List<string> storyFlags = new List<string>();
    }
}
