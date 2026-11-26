using System.Collections.Generic;
using NocturneCocoa.Core;
using NocturneCocoa.Data;
using NocturneCocoa.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NocturneCocoa.UI
{
    /// <summary>
    /// The dialogue box: speaker name, portrait, typewriter text, "continue" indicator and choice buttons.
    /// Lives on the persistent DialogueCanvas (under GameSystems), so it works in every scene.
    /// It only displays what DialogueManager sends and forwards the player's input back to it.
    ///
    /// Advance with Submit (Enter / Space / E / gamepad A) or by clicking the box.
    /// The first press finishes the typing; the next press continues.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DialogueUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private GameObject nameBox;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TypewriterEffect typewriter;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private RectTransform choicesContainer;
        [SerializeField] private DialogueChoiceButton choiceTemplate;

        [Header("Layout")]
        [Tooltip("Left padding of the text when a portrait is shown / hidden.")]
        [SerializeField] private float textLeftWithPortrait = 120f;
        [SerializeField] private float textLeftWithoutPortrait = 20f;

        [Tooltip("In the side-view Dialogue_Scene the characters stand on the stage, so hide the box portrait there.")]
        [SerializeField] private bool hidePortraitInSideView = true;

        private readonly List<DialogueChoiceButton> spawnedChoices = new List<DialogueChoiceButton>();

        private CanvasGroup canvasGroup;
        private DialogueManager manager;
        private InputManager input;
        private DialogueLine pendingLine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (choiceTemplate != null) choiceTemplate.gameObject.SetActive(false);
            SetVisible(false);
        }

        private void Start()
        {
            Services.TryGet(out manager);
            Services.TryGet(out input);

            if (manager == null)
            {
                Debug.LogWarning("[DialogueUI] DialogueManager not found.", this);
                return;
            }

            manager.DialogueStarted += HandleStarted;
            manager.LineShown += HandleLine;
            manager.DialogueEnded += HandleEnded;
            if (input != null) input.SubmitPressed += RequestAdvance;
            if (typewriter != null) typewriter.Completed += HandleTypingCompleted;
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.DialogueStarted -= HandleStarted;
                manager.LineShown -= HandleLine;
                manager.DialogueEnded -= HandleEnded;
            }

            if (input != null) input.SubmitPressed -= RequestAdvance;
            if (typewriter != null) typewriter.Completed -= HandleTypingCompleted;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            RequestAdvance();
        }

        // ---------------------------------------------------------------- Manager events

        private void HandleStarted(DialogueSO dialogue)
        {
            SetVisible(true);
        }

        private void HandleLine(DialogueLine line)
        {
            pendingLine = line;
            ClearChoices();
            if (continueIndicator != null) continueIndicator.SetActive(false);

            // Name
            bool hasName = !string.IsNullOrEmpty(line.SpeakerName);
            if (nameBox != null) nameBox.SetActive(hasName);
            if (nameLabel != null)
            {
                nameLabel.text = line.SpeakerName;
                nameLabel.color = line.NameColor;
            }

            // Portrait
            bool sideView = line.Dialogue != null && line.Dialogue.Presentation == DialoguePresentation.SideViewScene;
            Sprite face = line.Portrait;
            bool showPortrait = face != null && !(sideView && hidePortraitInSideView);
            if (portrait != null)
            {
                portrait.gameObject.SetActive(showPortrait);
                portrait.sprite = face;
            }

            SetTextLeftPadding(showPortrait ? textLeftWithPortrait : textLeftWithoutPortrait);

            // Text
            if (typewriter != null)
            {
                AudioClip voice = line.Speaker != null ? line.Speaker.VoiceBlip : null;
                float pitch = line.Speaker != null ? line.Speaker.VoicePitch : 1f;
                typewriter.Play(line.Text, voice, pitch);
            }
            else if (bodyText != null)
            {
                bodyText.text = line.Text;
                HandleTypingCompleted();
            }
        }

        private void HandleEnded(DialogueSO dialogue)
        {
            pendingLine = null;
            ClearChoices();
            if (typewriter != null) typewriter.Clear();
            SetVisible(false);
        }

        private void HandleTypingCompleted()
        {
            if (pendingLine == null)
            {
                return;
            }

            if (pendingLine.HasChoices)
            {
                ShowChoices(pendingLine.Choices);
            }
            else if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
            }
        }

        // ---------------------------------------------------------------- Input

        private void RequestAdvance()
        {
            if (manager == null || !manager.AcceptsInput || pendingLine == null)
            {
                return;
            }

            if (typewriter != null && typewriter.IsTyping)
            {
                typewriter.Complete();
                return;
            }

            // With choices on screen, Submit is handled by the selected button instead.
            if (!pendingLine.HasChoices)
            {
                manager.Advance();
            }
        }

        // ---------------------------------------------------------------- Choices

        private void ShowChoices(List<string> choices)
        {
            if (choiceTemplate == null || choicesContainer == null)
            {
                Debug.LogWarning("[DialogueUI] Choice template/container not assigned.", this);
                return;
            }

            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                DialogueChoiceButton button = Instantiate(choiceTemplate, choicesContainer);
                button.gameObject.SetActive(true);
                button.Setup(choices[i], () => manager.Choose(index));
                spawnedChoices.Add(button);
            }

            // Select the first choice so keyboard/gamepad navigation works immediately.
            if (spawnedChoices.Count > 0 && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(spawnedChoices[0].gameObject);
            }
        }

        private void ClearChoices()
        {
            foreach (DialogueChoiceButton button in spawnedChoices)
            {
                if (button != null) Destroy(button.gameObject);
            }

            spawnedChoices.Clear();

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        // ---------------------------------------------------------------- Helpers

        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void SetTextLeftPadding(float left)
        {
            if (bodyText == null)
            {
                return;
            }

            RectTransform rect = bodyText.rectTransform;
            rect.offsetMin = new Vector2(left, rect.offsetMin.y);
        }
    }
}