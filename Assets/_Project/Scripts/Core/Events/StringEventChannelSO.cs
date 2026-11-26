using UnityEngine;

namespace NocturneCocoa.Core.Events
{
    /// <summary>Event channel carrying a string, e.g. a scene name or an ID.</summary>
    [CreateAssetMenu(menuName = "Nocturne Cocoa/Events/String Event Channel", fileName = "StringEventChannel")]
    public class StringEventChannelSO : EventChannelSO<string>
    {
    }
}
