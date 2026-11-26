using NocturneCocoa.Core;
using NocturneCocoa.Data;
using UnityEngine;

namespace NocturneCocoa.Dialogue
{
    /// <summary>
    /// Side-view presentation for Dialogue_Scene: puts the speaking characters' full-body sprites on a
    /// left and a right slot and dims whoever isn't talking. The text itself is still shown by the
    /// persistent DialogueUI, so overlay and side-view dialogue share all logic.
    ///
    /// The first character to speak takes the left slot, the second the right slot.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueStage : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer leftSlot;
        [SerializeField] private SpriteRenderer rightSlot;

        [Tooltip("Tint for the character who is not speaking.")]
        [SerializeField] private Color inactiveTint = new Color(0.55f, 0.55f, 0.65f, 1f);

        [Tooltip("Mirror the right slot so both characters face each other.")]
        [SerializeField] private bool mirrorRightSlot = true;

        private DialogueManager manager;
        private CharacterProfileSO leftCharacter;
        private CharacterProfileSO rightCharacter;

        private void Start()
        {
            if (rightSlot != null) rightSlot.flipX = mirrorRightSlot;
            ClearSlots();

            if (Services.TryGet(out manager))
            {
                manager.DialogueStarted += HandleStarted;
                manager.LineShown += HandleLine;

                // The dialogue may already have started this frame, before Start() ran.
                if (manager.CurrentLine != null)
                {
                    HandleLine(manager.CurrentLine);
                }
            }
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.DialogueStarted -= HandleStarted;
                manager.LineShown -= HandleLine;
            }
        }

        private void HandleStarted(DialogueSO dialogue)
        {
            ClearSlots();
        }

        private void HandleLine(DialogueLine line)
        {
            CharacterProfileSO speaker = line.Speaker;

            if (speaker != null && speaker != leftCharacter && speaker != rightCharacter)
            {
                if (leftCharacter == null) leftCharacter = speaker;
                else if (rightCharacter == null) rightCharacter = speaker;
                else rightCharacter = speaker; // a third speaker replaces the right slot
            }

            UpdateSlot(leftSlot, leftCharacter, speaker, line.Expression);
            UpdateSlot(rightSlot, rightCharacter, speaker, line.Expression);
        }

        private void UpdateSlot(SpriteRenderer slot, CharacterProfileSO character, CharacterProfileSO speaker, string expression)
        {
            if (slot == null)
            {
                return;
            }

            if (character == null)
            {
                slot.enabled = false;
                return;
            }

            bool speaking = character == speaker;
            slot.enabled = true;

            // Only the speaker changes expression; the listener keeps their last pose.
            if (speaking || slot.sprite == null)
            {
                slot.sprite = character.GetSideView(speaking ? expression : null);
            }

            slot.color = speaking ? Color.white : inactiveTint;
        }

        private void ClearSlots()
        {
            leftCharacter = null;
            rightCharacter = null;
            if (leftSlot != null) { leftSlot.enabled = false; leftSlot.sprite = null; }
            if (rightSlot != null) { rightSlot.enabled = false; rightSlot.sprite = null; }
        }
    }
}