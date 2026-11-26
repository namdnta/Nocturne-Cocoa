using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// A place where the player can appear in an exploration scene.
    /// - Mark one per scene as Default (used for a new game).
    /// - Give the others IDs that doors/stations point to, e.g. "FromKitchen", "FrontDoor".
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [Tooltip("Referenced by SceneTransitionInteractable / SceneTransitionTrigger 'Target Entry Point Id'.")]
        [SerializeField] private string spawnId = "Default";

        [Tooltip("Used when the scene is entered without a specific entry point and there is no saved position.")]
        [SerializeField] private bool isDefault;

        [SerializeField] private Vector2 facingDirection = Vector2.down;

        public string SpawnId
        {
            get { return spawnId; }
        }

        public bool IsDefault
        {
            get { return isDefault; }
        }

        public Vector2 FacingDirection
        {
            get { return facingDirection; }
        }

        public static PlayerSpawnPoint Find(string id)
        {
            foreach (PlayerSpawnPoint point in FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None))
            {
                if (point.spawnId == id)
                {
                    return point;
                }
            }

            return null;
        }

        /// <summary>The spawn point marked Default, or any spawn point if none is marked.</summary>
        public static PlayerSpawnPoint FindDefault()
        {
            PlayerSpawnPoint fallback = null;
            foreach (PlayerSpawnPoint point in FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None))
            {
                if (point.isDefault)
                {
                    return point;
                }

                if (fallback == null)
                {
                    fallback = point;
                }
            }

            return fallback;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isDefault ? new Color(0.4f, 1f, 0.5f) : new Color(0.4f, 0.8f, 1f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(facingDirection.normalized * 0.5f));
        }
    }
}