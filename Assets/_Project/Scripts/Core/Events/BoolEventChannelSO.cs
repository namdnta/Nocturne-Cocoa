using UnityEngine;

namespace NocturneCocoa.Core.Events
{
    /// <summary>Event channel carrying a bool, e.g. "paused" on/off.</summary>
    [CreateAssetMenu(menuName = "Nocturne Cocoa/Events/Bool Event Channel", fileName = "BoolEventChannel")]
    public class BoolEventChannelSO : EventChannelSO<bool>
    {
    }
}
