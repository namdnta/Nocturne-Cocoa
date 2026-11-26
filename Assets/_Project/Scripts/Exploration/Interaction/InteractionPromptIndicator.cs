using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// PLACEHOLDER prompt until the real HUD (M7): shows a small world-space icon (e.g. an "E" key sprite)
    /// above whatever the player can currently interact with.
    /// Put it on any object in the exploration scene and assign a child sprite as Visual.
    /// </summary>
    [AddComponentMenu("Nocturne Cocoa/Exploration/Interaction Prompt Indicator")]
    public class InteractionPromptIndicator : MonoBehaviour
    {
        [Tooltip("Leave empty to find the player's interactor automatically.")]
        [SerializeField] private PlayerInteractor interactor;

        [Tooltip("The object to show/hide and move above the target (e.g. a SpriteRenderer child).")]
        [SerializeField] private GameObject visual;

        [SerializeField] private bool bob = true;
        [SerializeField, Min(0f)] private float bobHeight = 0.06f;
        [SerializeField, Min(0f)] private float bobSpeed = 4f;

        private void Start()
        {
            if (interactor == null)
            {
                interactor = FindFirstObjectByType<PlayerInteractor>();
            }

            if (visual != null)
            {
                visual.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (visual == null)
            {
                return;
            }

            Interactable target = interactor != null ? interactor.CurrentTarget : null;
            bool show = target != null;

            if (visual.activeSelf != show)
            {
                visual.SetActive(show);
            }

            if (show)
            {
                float offset = bob ? Mathf.Sin(Time.time * bobSpeed) * bobHeight : 0f;
                visual.transform.position = target.PromptPosition + new Vector3(0f, offset, 0f);
            }
        }
    }
}