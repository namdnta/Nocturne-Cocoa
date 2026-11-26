using System.IO;
using UnityEditor;
using UnityEngine;

namespace NocturneCocoa.EditorTools
{
    /// <summary>Editor shortcuts for inspecting and clearing save files while testing.</summary>
    public static class SaveDebugMenu
    {
        private static string SaveFolder
        {
            get { return Path.Combine(Application.persistentDataPath, "Saves"); }
        }

        [MenuItem("Nocturne Cocoa/Saves/Open Save Folder", false, 20)]
        private static void OpenSaveFolder()
        {
            Directory.CreateDirectory(SaveFolder);
            EditorUtility.RevealInFinder(SaveFolder);
        }

        [MenuItem("Nocturne Cocoa/Saves/Delete All Saves", false, 21)]
        private static void DeleteAllSaves()
        {
            if (!Directory.Exists(SaveFolder))
            {
                Debug.Log("[Nocturne] No save folder yet.");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete all saves?", $"This deletes every file in:\n{SaveFolder}", "Delete", "Cancel"))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(SaveFolder))
            {
                File.Delete(file);
            }

            Debug.Log("[Nocturne] All saves deleted.");
        }
    }
}
