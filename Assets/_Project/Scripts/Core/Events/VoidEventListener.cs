using UnityEngine;
using UnityEngine.Events;

namespace NocturneCocoa.Core.Events
{
    /// <summary>
    /// Lets designers react to a VoidEventChannelSO from the Inspector without writing code
    /// (e.g. show a "Saved!" popup when the GameSaved channel is raised).
    /// </summary>
    [AddComponentMenu("Nocturne Cocoa/Events/Void Event Listener")]
    public class VoidEventListener : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO channel;
        [SerializeField] private UnityEvent response = new UnityEvent();

        private void OnEnable()
        {
            if (channel != null)
            {
                channel.OnRaised += HandleRaised;
            }
        }

        private void OnDisable()
        {
            if (channel != null)
            {
                channel.OnRaised -= HandleRaised;
            }
        }

        private void HandleRaised()
        {
            response.Invoke();
        }
    }
}