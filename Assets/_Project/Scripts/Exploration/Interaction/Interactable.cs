using UnityEngine;
using UnityEngine.Events;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// Base class for anything the player can interact with by facing it and pressing Interact
    /// (counters, stations, doors, NPCs later in M3, ingredient shelves in M4).
    ///
    /// Needs a Collider2D on this object or a child so PlayerInteractor can detect it.
    /// To make a new kind of interaction, inherit from this and implement OnInteract().
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Short verb shown by the prompt UI, e.g. \"Cook\", \"Talk\", \"Open\".")]
        [SerializeField] protected string promptText = "Interact";

        [Tooltip("When several interactables are in range, the highest priority wins, then the closest.")]
        [SerializeField] private int priority;

        [SerializeField] private bool interactable = true;

        [Header("Prompt position")]
        [Tooltip("Optional. Where the prompt icon appears. If empty, this object's position + offset.")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private Vector2 promptOffset = new Vector2(0f, 1f);

        [Header("Feedback (optional)")]
        [Tooltip("Called when this becomes the player's target, e.g. enable an outline.")]
        [SerializeField] private UnityEvent onFocused = new UnityEvent();
        [SerializeField] private UnityEvent onUnfocused = new UnityEvent();

        public string PromptText
        {
            get { return promptText; }
        }

        public int Priority
        {
            get { return priority; }
        }

        /// <summary>Turn the interaction on/off at runtime (e.g. a station that is locked until later).</summary>
        public bool IsInteractable
        {
            get { return interactable && isActiveAndEnabled; }
            set { interactable = value; }
        }

        public Vector3 PromptPosition
        {
            get
            {
                return promptAnchor != null
                    ? promptAnchor.position
                    : transform.position + (Vector3)promptOffset;
            }
        }

        /// <summary>Override to add conditions (e.g. "needs the Kitchen Key").</summary>
        public virtual bool CanInteract(PlayerInteractor interactor)
        {
            return IsInteractable;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (CanInteract(interactor))
            {
                OnInteract(interactor);
            }
        }

        protected abstract void OnInteract(PlayerInteractor interactor);

        /// <summary>Called by PlayerInteractor when this becomes / stops being the current target.</summary>
        public void SetFocused(bool focused)
        {
            if (focused) onFocused.Invoke();
            else onUnfocused.Invoke();

            OnFocusChanged(focused);
        }

        protected virtual void OnFocusChanged(bool focused)
        {
        }

        // Runs when the component is first added in the Editor: make sure there is something to detect.
        protected virtual void Reset()
        {
            if (GetComponentInChildren<Collider2D>() == null)
            {
                CircleCollider2D trigger = gameObject.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = 0.5f;
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f);
            Gizmos.DrawWireSphere(PromptPosition, 0.1f);
        }
    }
}