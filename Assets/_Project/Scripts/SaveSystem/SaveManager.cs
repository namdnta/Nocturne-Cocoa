using System;
using System.IO;
using NocturneCocoa.Core.Events;
using UnityEngine;

namespace NocturneCocoa.SaveSystem
{
    /// <summary>
    /// Reads and writes SaveData files. It only does persistence: it does not know about scenes or
    /// gameplay. GameSession decides *what* to save and *when*; SaveManager decides *how*.
    ///
    /// Files: {Application.persistentDataPath}/Saves/autosave.json and slot_1.json .. slot_N.json.
    /// Each write goes to a .tmp file first, and the previous file is kept as .bak, so a crash
    /// mid-save cannot destroy the last good save.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveManager : MonoBehaviour
    {
        public const int AutoSaveSlot = 0;

        [SerializeField] private string saveFolderName = "Saves";

        [Tooltip("Number of manual save slots (slot 1..N). Slot 0 is always the autosave.")]
        [SerializeField, Min(1)] private int manualSlotCount = 3;

        [Tooltip("Readable JSON is easier to debug. Turn off for release builds if you like.")]
        [SerializeField] private bool prettyPrintJson = true;

        [Header("Optional event channels")]
        [Tooltip("Raised with the slot number after a successful save.")]
        [SerializeField] private IntEventChannelSO gameSavedChannel;

        [Tooltip("Raised with the slot number after a successful load.")]
        [SerializeField] private IntEventChannelSO gameLoadedChannel;

        public event Action<int> Saved;
        public event Action<int> Loaded;

        /// <summary>Slot and reason, for showing an error notification later.</summary>
        public event Action<int, string> SaveFailed;
        public event Action<int, string> LoadFailed;

        private ISaveSerializer serializer;
        private string saveDirectory;
        private bool initialized;

        public string SaveDirectory
        {
            get { return saveDirectory; }
        }

        public int ManualSlotCount
        {
            get { return manualSlotCount; }
        }

        /// <summary>Called by BootstrapLoader. Optionally pass a different serializer (e.g. for tests).</summary>
        public void Initialize(ISaveSerializer customSerializer = null)
        {
            serializer = customSerializer ?? new JsonSaveSerializer(prettyPrintJson);
            saveDirectory = Path.Combine(Application.persistentDataPath, saveFolderName);

            try
            {
                Directory.CreateDirectory(saveDirectory);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Could not create save folder '{saveDirectory}': {e.Message}");
            }

            initialized = true;
            Debug.Log($"[Save] Save folder: {saveDirectory}");
        }

        public bool IsValidSlot(int slot)
        {
            return slot >= AutoSaveSlot && slot <= manualSlotCount;
        }

        public string GetSlotPath(int slot)
        {
            string fileName = slot == AutoSaveSlot ? "autosave.json" : $"slot_{slot}.json";
            return Path.Combine(saveDirectory, fileName);
        }

        public bool HasSave(int slot)
        {
            return initialized && IsValidSlot(slot) && File.Exists(GetSlotPath(slot));
        }

        public bool Save(SaveData data, int slot)
        {
            string error;
            if (!CanUseSlot(slot, out error))
            {
                return FailSave(slot, error);
            }

            if (data == null)
            {
                return FailSave(slot, "No data to save.");
            }

            data.saveVersion = SaveMigrator.CurrentVersion;
            data.lastSavedUtc = DateTime.UtcNow.ToString("o");

            if (!data.Validate(out error))
            {
                return FailSave(slot, error);
            }

            string path = GetSlotPath(slot);
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";

            try
            {
                File.WriteAllText(tempPath, serializer.Serialize(data));

                if (File.Exists(path))
                {
                    File.Copy(path, backupPath, true);
                    File.Delete(path);
                }

                File.Move(tempPath, path);
            }
            catch (Exception e)
            {
                return FailSave(slot, e.Message);
            }

            Debug.Log($"[Save] Saved slot {slot} -> {path}");
            if (Saved != null) Saved.Invoke(slot);
            if (gameSavedChannel != null) gameSavedChannel.Raise(slot);
            return true;
        }

        /// <summary>
        /// Loads, migrates and validates a slot. Falls back to the .bak file if the main file is corrupt.
        /// </summary>
        public bool TryLoad(int slot, out SaveData data)
        {
            data = null;

            string error;
            if (!CanUseSlot(slot, out error))
            {
                return FailLoad(slot, error);
            }

            string path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                return FailLoad(slot, "No save in this slot.");
            }

            if (!TryReadFile(path, out data, out error))
            {
                string backupPath = path + ".bak";
                string backupError;
                if (File.Exists(backupPath) && TryReadFile(backupPath, out data, out backupError))
                {
                    Debug.LogWarning($"[Save] Slot {slot} was unreadable ({error}). Loaded the backup instead.");
                }
                else
                {
                    return FailLoad(slot, error);
                }
            }

            Debug.Log($"[Save] Loaded slot {slot}.");
            if (Loaded != null) Loaded.Invoke(slot);
            if (gameLoadedChannel != null) gameLoadedChannel.Raise(slot);
            return true;
        }

        public bool Delete(int slot)
        {
            string error;
            if (!CanUseSlot(slot, out error))
            {
                Debug.LogWarning($"[Save] Cannot delete slot {slot}: {error}");
                return false;
            }

            string path = GetSlotPath(slot);
            try
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Could not delete slot {slot}: {e.Message}");
                return false;
            }

            Debug.Log($"[Save] Deleted slot {slot}.");
            return true;
        }

        private bool TryReadFile(string path, out SaveData data, out string error)
        {
            data = null;

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            data = serializer.Deserialize(text);
            if (data == null)
            {
                error = "File is empty or not valid save data.";
                return false;
            }

            if (!SaveMigrator.TryMigrate(data, out error) || !data.Validate(out error))
            {
                data = null;
                return false;
            }

            return true;
        }

        private bool CanUseSlot(int slot, out string error)
        {
            if (!initialized)
            {
                error = "SaveManager has not been initialized by BootstrapLoader.";
                return false;
            }

            if (!IsValidSlot(slot))
            {
                error = $"Slot {slot} is out of range (0..{manualSlotCount}).";
                return false;
            }

            error = null;
            return true;
        }

        private bool FailSave(int slot, string error)
        {
            Debug.LogError($"[Save] Save to slot {slot} failed: {error}");
            if (SaveFailed != null) SaveFailed.Invoke(slot, error);
            return false;
        }

        private bool FailLoad(int slot, string error)
        {
            Debug.LogWarning($"[Save] Load from slot {slot} failed: {error}");
            if (LoadFailed != null) LoadFailed.Invoke(slot, error);
            return false;
        }
    }
}