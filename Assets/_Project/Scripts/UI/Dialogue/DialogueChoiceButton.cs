using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NocturneCocoa.UI
{
    /// <summary>One answer button in the dialogue choice list. DialogueUI clones a template of this.</summary>
    [RequireComponent(typeof(Button))]
    public class DialogueChoiceButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private Button button;
        private Action onPicked;

        public Button Button
        {
            get
            {
                if (button == null) button = GetComponent<Button>();
                return button;
            }
        }

        public void Setup(string text, Action picked)
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;

            onPicked = picked;
            Button.onClick.RemoveListener(HandleClick);
            Button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (onPicked != null) onPicked.Invoke();
        }
    }
}