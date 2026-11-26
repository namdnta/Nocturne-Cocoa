using System;
using UnityEngine;

namespace NocturneCocoa.Core.Events
{
    /// <summary>
    /// Base class for ScriptableObject event channels that carry a value.
    /// A channel is an asset: the sender raises it, any number of listeners subscribe to it,
    /// and neither side needs a reference to the other.
    ///
    /// Unity cannot create assets of a generic type, so make a small concrete subclass per payload
    /// type (see StringEventChannelSO, IntEventChannelSO, BoolEventChannelSO).
    /// </summary>
    public abstract class EventChannelSO<T> : ScriptableObject
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

        public event Action<T> OnRaised;

        public void Raise(T value)
        {
            if (logWhenRaised)
            {
                Debug.Log($"[Event] {name} raised with '{value}'.", this);
            }

            if (OnRaised != null)
            {
                OnRaised.Invoke(value);
            }
        }
    }
}
