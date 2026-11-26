using NocturneCocoa.Core;
using NocturneCocoa.SceneManagement;
using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// Press Interact to go to another scene, e.g. the cooking station that opens Cooking_Scene,
    /// or a door you have to open on purpose.
    /// </summary>
    [AddComponentMenu("Nocturne Cocoa/Exploration/Scene Transition Interactable")]
    public class SceneTransitionInteractable : Interactable
    {
        [Tooltip("Exact scene name (see SceneNames): Cafe_Exploration, Dialogue_Scene, Cooking_Scene.")]
        [SerializeField] private string targetScene = SceneNames.Cooking;

        [Tooltip("Optional PlayerSpawnPoint ID in the target scene. Only matters for exploration scenes.")]
        [SerializeField] private string targetEntryPointId;

        protected override void OnInteract(PlayerInteractor interactor)
        {
            SceneFlowController sceneFlow;
            if (Services.TryGet(out sceneFlow))
            {
                sceneFlow.LoadScene(targetScene, targetEntryPointId);
            }
            else
            {
                Debug.LogWarning("[Interact] SceneFlowController not found. Did Play Mode start from Bootstrap?", this);
            }
        }
    }
}