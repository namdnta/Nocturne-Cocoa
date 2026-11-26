using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// Top-down (3/4 view) movement using a Rigidbody2D, so the player collides with walls and furniture.
    /// It only moves; it doesn't read input. PlayerController feeds it a direction every frame.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class PlayerMovement : MonoBehaviour
    {
        public enum DirectionMode
        {
            /// <summary>Free movement including diagonals.</summary>
            EightWay,
            /// <summary>Only up/down/left/right (classic RPG feel).</summary>
            FourWay
        }

        [Tooltip("Walking speed in world units per second.")]
        [SerializeField, Min(0f)] private float moveSpeed = 3.5f;

        [Tooltip("How quickly the player reaches full speed / stops. 0 = instant.")]
        [SerializeField, Min(0f)] private float acceleration = 40f;

        [SerializeField] private DirectionMode directionMode = DirectionMode.EightWay;

        [Tooltip("Direction the player faces before moving for the first time.")]
        [SerializeField] private Vector2 initialFacing = Vector2.down;

        private Rigidbody2D body;
        private Vector2 input;
        private Vector2 velocity;
        private bool initialized;

        /// <summary>Last direction the player moved or was turned towards (unit length).</summary>
        public Vector2 Facing { get; private set; }

        /// <summary>Velocity the player is trying to move at this frame.</summary>
        public Vector2 Velocity
        {
            get { return velocity; }
        }

        public bool IsMoving
        {
            get { return velocity.sqrMagnitude > 0.0001f; }
        }

        public float MoveSpeed
        {
            get { return moveSpeed; }
            set { moveSpeed = Mathf.Max(0f, value); }
        }

        /// <summary>When false the player stops and ignores input (dialogue, cutscenes, menus).</summary>
        public bool CanMove { get; set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            CanMove = true;
            Facing = initialFacing.sqrMagnitude > 0f ? initialFacing.normalized : Vector2.down;

            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        /// <summary>Raw input direction (e.g. from a stick or WASD). Magnitude is clamped to 1.</summary>
        public void SetMoveInput(Vector2 rawInput)
        {
            input = ProcessInput(rawInput);

            if (CanMove && input.sqrMagnitude > 0.01f)
            {
                Facing = input.normalized;
            }
        }

        public void FaceTowards(Vector2 direction)
        {
            EnsureInitialized();
            if (direction.sqrMagnitude > 0.0001f)
            {
                Facing = ProcessInput(direction).normalized;
            }
        }

        /// <summary>Instantly moves the player (spawn points, loading a save).</summary>
        public void Teleport(Vector2 position)
        {
            EnsureInitialized();
            Stop();
            body.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        public void Stop()
        {
            EnsureInitialized();
            input = Vector2.zero;
            velocity = Vector2.zero;
            body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            Vector2 target = CanMove ? input * moveSpeed : Vector2.zero;

            velocity = acceleration <= 0f
                ? target
                : Vector2.MoveTowards(velocity, target, acceleration * Time.fixedDeltaTime);

            body.linearVelocity = velocity;
        }

        private Vector2 ProcessInput(Vector2 raw)
        {
            if (directionMode == DirectionMode.FourWay)
            {
                raw = Mathf.Abs(raw.x) >= Mathf.Abs(raw.y)
                    ? new Vector2(raw.x, 0f)
                    : new Vector2(0f, raw.y);
            }

            return Vector2.ClampMagnitude(raw, 1f);
        }
    }
}