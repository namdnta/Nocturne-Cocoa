using System.Collections.Generic;
using NocturneCocoa.Data;
using UnityEngine;

namespace NocturneCocoa.Dialogue
{
    /// <summary>
    /// Everything a presentation (dialogue box, side-view stage) needs to show one line.
    /// Built by DialogueManager, so UI code never has to understand nodes, conditions or IDs.
    /// </summary>
    public class DialogueLine
    {
        public DialogueLine(DialogueSO dialogue, DialogueNode node, List<string> choices)
        {
            Dialogue = dialogue;
            Speaker = node.speaker;
            Expression = node.expression;
            Text = node.text;
            Choices = choices;

            if (!string.IsNullOrEmpty(node.speakerNameOverride))
            {
                SpeakerName = node.speakerNameOverride;
            }
            else
            {
                SpeakerName = Speaker != null ? Speaker.DisplayName : string.Empty;
            }
        }

        public DialogueSO Dialogue { get; private set; }

        /// <summary>Null for narrator lines.</summary>
        public CharacterProfileSO Speaker { get; private set; }

        public string SpeakerName { get; private set; }
        public string Expression { get; private set; }
        public string Text { get; private set; }

        /// <summary>Texts of the choices the player may pick (already filtered by conditions). Empty = just continue.</summary>
        public List<string> Choices { get; private set; }

        public bool HasChoices
        {
            get { return Choices != null && Choices.Count > 0; }
        }

        public bool IsNarrator
        {
            get { return Speaker == null; }
        }

        public Sprite Portrait
        {
            get { return Speaker != null ? Speaker.GetPortrait(Expression) : null; }
        }

        public Color NameColor
        {
            get { return Speaker != null ? Speaker.NameColor : Color.white; }
        }
    }
}