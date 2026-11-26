using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.Data
{
    public enum DialoguePresentation
    {
        /// <summary>Dialogue box over the current (top-down) scene. For short chats with NPCs.</summary>
        Overlay,
        /// <summary>Switches to the side-view Dialogue_Scene, then returns. For story moments.</summary>
        SideViewScene
    }

    /// <summary>
    /// One conversation: a set of nodes connected by IDs. Create via
    /// Create &gt; Nocturne Cocoa &gt; Dialogue &gt; Dialogue, and store in ScriptableObjects/Dialogue/.
    /// The Console warns about broken node links whenever you edit the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Nocturne Cocoa/Dialogue/Dialogue", fileName = "DLG_NewDialogue")]
    public class DialogueSO : ScriptableObject
    {
        [Tooltip("Unique ID used by saves and conditions (DialogueCompleted). Defaults to the asset name.")]
        [SerializeField] private string dialogueId;

        [SerializeField] private DialoguePresentation presentation = DialoguePresentation.Overlay;

        [Tooltip("Node the dialogue begins with.")]
        [SerializeField] private string startNodeId = "start";

        [SerializeField] private List<DialogueNode> nodes = new List<DialogueNode>();

        private Dictionary<string, DialogueNode> lookup;

        public string DialogueId
        {
            get { return string.IsNullOrEmpty(dialogueId) ? name : dialogueId; }
        }

        public DialoguePresentation Presentation
        {
            get { return presentation; }
        }

        public string StartNodeId
        {
            get { return startNodeId; }
        }

        public IReadOnlyList<DialogueNode> Nodes
        {
            get { return nodes; }
        }

        public DialogueNode FindNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            if (lookup == null)
            {
                lookup = new Dictionary<string, DialogueNode>();
                foreach (DialogueNode node in nodes)
                {
                    if (node != null && !string.IsNullOrEmpty(node.id) && !lookup.ContainsKey(node.id))
                    {
                        lookup.Add(node.id, node);
                    }
                }
            }

            DialogueNode found;
            return lookup.TryGetValue(nodeId, out found) ? found : null;
        }

        private void OnEnable()
        {
            lookup = null;
        }

        private void OnValidate()
        {
            lookup = null;

            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                dialogueId = name;
            }

            ValidateLinks();
        }

        /// <summary>Logs a warning for duplicate IDs and links to nodes that don't exist.</summary>
        public void ValidateLinks()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (DialogueNode node in nodes)
            {
                if (node == null) continue;
                if (!ids.Add(node.id))
                {
                    Debug.LogWarning($"[Dialogue] {name}: duplicate node ID '{node.id}'.", this);
                }
            }

            if (nodes.Count > 0 && !ids.Contains(startNodeId))
            {
                Debug.LogWarning($"[Dialogue] {name}: start node '{startNodeId}' doesn't exist.", this);
            }

            foreach (DialogueNode node in nodes)
            {
                if (node == null) continue;
                CheckLink(ids, node.id, node.nextNodeId);
                foreach (DialogueChoice choice in node.choices) CheckLink(ids, node.id, choice.nextNodeId);
                foreach (DialogueJump jump in node.jumps) CheckLink(ids, node.id, jump.targetNodeId);
            }
        }

        private void CheckLink(HashSet<string> ids, string fromId, string toId)
        {
            if (!string.IsNullOrEmpty(toId) && !ids.Contains(toId))
            {
                Debug.LogWarning($"[Dialogue] {name}: node '{fromId}' points to missing node '{toId}'.", this);
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup tool to create sample dialogues.</summary>
        public void EditorConfigure(string id, DialoguePresentation mode, string start, List<DialogueNode> nodeList)
        {
            dialogueId = id;
            presentation = mode;
            startNodeId = start;
            nodes = nodeList;
            lookup = null;
        }
#endif
    }
}