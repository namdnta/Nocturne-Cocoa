using System;
using UnityEngine;

namespace NocturneCocoa.Core.Events
{
    /// <summary>
    /// Event channel with no payload, e.g. "GameSaved", "DayEnded", "PauseRequested".
    /// </summary>
    [CreateAssetMenu(menuName = "Nocturne Cocoa/Events/Void Event Channel", fileName = "VoidEventChannel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        [TextArea]
        [SerializeField] private string description;

        [Tooltip("Log every Raise() to the Console. Useful while debugging event flow.")]
        [SerializeField] private bool logWhenRaised;

        /// <summary>Designer notes: who raises this channel and who listens.</summary>
        public string Description
        {
            get { return description; }
        }

        public event Action OnRaised;

        public void Raise()
        {
            if (logWhenRaised)
            {
                Debug.Log($"[Event] {name} raised.", this);
            }

            if (OnRaised != null)
            {
                OnRaised.Invoke();
            }
        }
    }
}
