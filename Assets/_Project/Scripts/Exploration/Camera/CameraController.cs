using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// Smoothly follows the player in exploration scenes and stays inside the room's bounds.
    /// Put it on the Main Camera of each exploration scene (orthographic camera).
    ///
    /// Pixel art: either add URP's "Pixel Perfect Camera" component to the same camera (recommended),
    /// or turn on Snap To Pixel Grid here. Don't use both.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        [Tooltip("Leave empty to follow the PlayerController in the scene.")]
        [SerializeField] private Transform target;

        [Tooltip("Shift the view, e.g. (0, 0.5) to centre on the body instead of the feet.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.5f);

        [Tooltip("Seconds to catch up with the target. 0 = locked to the target.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        [Header("Room bounds (optional)")]
        [Tooltip("A Collider2D covering the room (e.g. a BoxCollider2D set to Is Trigger). The camera never shows outside it.")]
        [SerializeField] private Collider2D bounds;

        [Header("Pixel art")]
        [SerializeField] private bool snapToPixelGrid;
        [SerializeField, Min(1)] private int pixelsPerUnit = 16;

        private Camera cam;
        private Vector3 smoothedPosition;
        private Vector3 velocity;

        public Transform Target
        {
            get { return target; }
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                }
            }

            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = GetDesiredPosition();
            smoothedPosition = smoothTime <= 0f
                ? desired
                : Vector3.SmoothDamp(smoothedPosition, desired, ref velocity, smoothTime);

            Apply(smoothedPosition);
        }

        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            if (snap)
            {
                SnapToTarget();
            }
        }

        public void SetBounds(Collider2D newBounds)
        {
            bounds = newBounds;
        }

        /// <summary>Jump straight to the target without smoothing (scene start, teleports).</summary>
        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            smoothedPosition = GetDesiredPosition();
            velocity = Vector3.zero;
            Apply(smoothedPosition);
        }

        private Vector3 GetDesiredPosition()
        {
            Vector2 point = (Vector2)target.position + offset;
            point = ClampToBounds(point);
            return new Vector3(point.x, point.y, transform.position.z);
        }

        private Vector2 ClampToBounds(Vector2 point)
        {
            if (bounds == null || cam == null || !cam.orthographic)
            {
                return point;
            }

            Bounds area = bounds.bounds;
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            float minX = area.min.x + halfWidth;
            float maxX = area.max.x - halfWidth;
            float minY = area.min.y + halfHeight;
            float maxY = area.max.y - halfHeight;

            // If the room is smaller than the view, centre on the room on that axis.
            point.x = minX > maxX ? area.center.x : Mathf.Clamp(point.x, minX, maxX);
            point.y = minY > maxY ? area.center.y : Mathf.Clamp(point.y, minY, maxY);
            return point;
        }

        private void Apply(Vector3 position)
        {
            if (snapToPixelGrid)
            {
                float unit = 1f / pixelsPerUnit;
                position.x = Mathf.Round(position.x / unit) * unit;
                position.y = Mathf.Round(position.y / unit) * unit;
            }

            transform.position = position;
        }
    }
}