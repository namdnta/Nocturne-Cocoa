using UnityEngine;

namespace NocturneCocoa.Core.Events
{
    /// <summary>Event channel carrying an int, e.g. a save slot or an amount.</summary>
    [CreateAssetMenu(menuName = "Nocturne Cocoa/Events/Int Event Channel", fileName = "IntEventChannel")]
    public class IntEventChannelSO : EventChannelSO<int>
    {
    }
}