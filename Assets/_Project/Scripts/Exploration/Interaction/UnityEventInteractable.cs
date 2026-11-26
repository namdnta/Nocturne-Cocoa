using UnityEngine;
using UnityEngine.Events;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// General-purpose interactable: wire up what happens in the Inspector (play a sound, toggle an object,
    /// open a door...). Until the dialogue system exists (M3), "Debug Message" is a quick way to fake
    /// "inspect" text like "A jar of moonlit cocoa beans."
    /// </summary>
    [AddComponentMenu("Nocturne Cocoa/Exploration/Unity Event Interactable")]
    public class UnityEventInteractable : Interactable
    {
        [TextArea]
        [Tooltip("PLACEHOLDER: printed to the Console on interact, until dialogue exists.")]
        [SerializeField] private string debugMessage;

        [Tooltip("Disable after the first use (e.g. picking something up).")]
        [SerializeField] private bool singleUse;

        [SerializeField] private UnityEvent onInteract = new UnityEvent();

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (!string.IsNullOrEmpty(debugMessage))
            {
                Debug.Log($"[Interact] {name}: {debugMessage}", this);
            }

            onInteract.Invoke();

            if (singleUse)
            {
                IsInteractable = false;
            }
        }
    }
}