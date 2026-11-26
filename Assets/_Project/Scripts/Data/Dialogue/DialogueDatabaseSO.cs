using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.Data
{
    /// <summary>
    /// List of every dialogue, with lookup by ID, so dialogues can be started by name
    /// (e.g. from a story event or a customer in M6).
    /// </summary>
    [CreateAssetMenu(menuName = "Nocturne Cocoa/Dialogue/Dialogue Database", fileName = "DialogueDatabase")]
    public class DialogueDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<DialogueSO> dialogues = new List<DialogueSO>();

        private Dictionary<string, DialogueSO> lookup;

        public IReadOnlyList<DialogueSO> Dialogues
        {
            get { return dialogues; }
        }

        public DialogueSO Find(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId))
            {
                return null;
            }

            if (lookup == null)
            {
                lookup = new Dictionary<string, DialogueSO>();
                foreach (DialogueSO dialogue in dialogues)
                {
                    if (dialogue == null) continue;
                    if (lookup.ContainsKey(dialogue.DialogueId))
                    {
                        Debug.LogWarning($"[DialogueDatabase] Duplicate dialogue ID '{dialogue.DialogueId}'.", this);
                        continue;
                    }

                    lookup.Add(dialogue.DialogueId, dialogue);
                }
            }

            DialogueSO found;
            return lookup.TryGetValue(dialogueId, out found) ? found : null;
        }

        private void OnEnable()
        {
            lookup = null;
        }

        private void OnValidate()
        {
            lookup = null;
        }

#if UNITY_EDITOR
        public void EditorSetDialogues(List<DialogueSO> list)
        {
            dialogues = list;
            lookup = null;
        }
#endif
    }
}