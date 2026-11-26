using NocturneCocoa.Core;
using NocturneCocoa.SceneManagement;
using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// Walk into this trigger to change scene (e.g. the café's back door to a garden later).
    /// Needs a Collider2D with "Is Trigger" ticked on the same object.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Nocturne Cocoa/Exploration/Scene Transition Trigger")]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [SerializeField] private string targetScene = SceneNames.CafeExploration;

        [Tooltip("Optional PlayerSpawnPoint ID in the target scene.")]
        [SerializeField] private string targetEntryPointId;

        [Tooltip("Ignore the trigger for this long after the scene starts, so a player who spawns on it isn't sent straight back.")]
        [SerializeField, Min(0f)] private float armDelay = 0.5f;

        private bool used;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (used || Time.timeSinceLevelLoad < armDelay || other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            SceneFlowController sceneFlow;
            if (Services.TryGet(out sceneFlow) && sceneFlow.LoadScene(targetScene, targetEntryPointId))
            {
                used = true;
            }
        }
    }
}