using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// Simple NPC movement for the top-down café: stand still, wander around a home point, or walk
    /// between patrol points. Uses a kinematic Rigidbody2D and checks ahead with a cast, so NPCs stop
    /// at walls, furniture and the player instead of walking through them.
    /// Other systems pause it (e.g. NpcController while talking) with Pause(owner) / Resume(owner).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class NpcMovement : MonoBehaviour, ICharacterMotor
    {
        public enum MovementMode
        {
            Idle,
            Wander,
            Patrol
        }

        [SerializeField] private MovementMode mode = MovementMode.Idle;
        [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
        [SerializeField] private Vector2 initialFacing = Vector2.down;

        [Tooltip("Seconds to wait at each stop (random between X and Y).")]
        [SerializeField] private Vector2 waitTimeRange = new Vector2(1.5f, 4f);

        [Header("Wander")]
        [Tooltip("How far from its starting point the NPC may wander.")]
        [SerializeField, Min(0.1f)] private float wanderRadius = 2f;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints = new Transform[0];
        [SerializeField] private bool loopPatrol = true;

        private readonly HashSet<object> pauseLocks = new HashSet<object>();
        private readonly RaycastHit2D[] castHits = new RaycastHit2D[4];

        private Rigidbody2D body;
        private ContactFilter2D obstacleFilter;
        private Vector2 home;
        private Vector2 target;
        private bool hasTarget;
        private float waitTimer;
        private int patrolIndex;
        private int patrolDirection = 1;

        public Vector2 Facing { get; private set; }
        public Vector2 Velocity { get; private set; }

        public bool IsMoving
        {
            get { return Velocity.sqrMagnitude > 0.0001f; }
        }

        public float MoveSpeed
        {
            get { return moveSpeed; }
        }

        public bool IsPaused
        {
            get { return pauseLocks.Count > 0; }
        }

        public MovementMode Mode
        {
            get { return mode; }
            set
            {
                mode = value;
                hasTarget = false;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            obstacleFilter = new ContactFilter2D();
            obstacleFilter.useTriggers = false;

            Facing = initialFacing.sqrMagnitude > 0f ? initialFacing.normalized : Vector2.down;
            home = body.position;
            waitTimer = RandomWait();
        }

        public void Pause(object owner)
        {
            if (owner != null) pauseLocks.Add(owner);
            Velocity = Vector2.zero;
        }

        public void Resume(object owner)
        {
            if (owner != null) pauseLocks.Remove(owner);
        }

        public void FaceTowards(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                Facing = direction.normalized;
            }
        }

        private void FixedUpdate()
        {
            Velocity = Vector2.zero;

            if (IsPaused || mode == MovementMode.Idle)
            {
                return;
            }

            if (!hasTarget)
            {
                waitTimer -= Time.fixedDeltaTime;
                if (waitTimer <= 0f)
                {
                    hasTarget = TryPickTarget(out target);
                    waitTimer = RandomWait();
                }

                return;
            }

            Vector2 position = body.position;
            Vector2 toTarget = target - position;
            float step = moveSpeed * Time.fixedDeltaTime;

            if (toTarget.magnitude <= step)
            {
                body.MovePosition(target);
                ArriveAtTarget();
                return;
            }

            Vector2 direction = toTarget.normalized;

            // Something in the way (wall, furniture, player)? Give up on this target and wait.
            if (body.Cast(direction, obstacleFilter, castHits, step + 0.02f) > 0)
            {
                hasTarget = false;
                return;
            }

            Facing = direction;
            Velocity = direction * moveSpeed;
            body.MovePosition(position + direction * step);
        }

        private void ArriveAtTarget()
        {
            hasTarget = false;

            if (mode == MovementMode.Patrol && patrolPoints.Length > 1)
            {
                AdvancePatrolIndex();
            }
        }

        private bool TryPickTarget(out Vector2 result)
        {
            result = body.position;

            if (mode == MovementMode.Patrol)
            {
                if (patrolPoints.Length == 0 || patrolPoints[patrolIndex] == null)
                {
                    return false;
                }

                result = patrolPoints[patrolIndex].position;
                return true;
            }

            // Wander: try a few random points that have a clear straight path.
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector2 candidate = home + Random.insideUnitCircle * wanderRadius;
                Vector2 offset = candidate - body.position;
                if (offset.magnitude < 0.3f)
                {
                    continue;
                }

                if (body.Cast(offset.normalized, obstacleFilter, castHits, offset.magnitude) == 0)
                {
                    result = candidate;
                    return true;
                }
            }

            return false;
        }

        private void AdvancePatrolIndex()
        {
            if (loopPatrol)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                return;
            }

            // Ping-pong: 0,1,2,1,0,1...
            if (patrolIndex + patrolDirection >= patrolPoints.Length || patrolIndex + patrolDirection < 0)
            {
                patrolDirection = -patrolDirection;
            }

            patrolIndex += patrolDirection;
        }

        private float RandomWait()
        {
            return Random.Range(Mathf.Min(waitTimeRange.x, waitTimeRange.y), Mathf.Max(waitTimeRange.x, waitTimeRange.y));
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? (Vector3)home : transform.position;

            if (mode == MovementMode.Wander)
            {
                Gizmos.color = new Color(0.6f, 0.9f, 1f);
                Gizmos.DrawWireSphere(center, wanderRadius);
            }
            else if (mode == MovementMode.Patrol && patrolPoints != null)
            {
                Gizmos.color = new Color(1f, 0.8f, 0.4f);
                for (int i = 0; i < patrolPoints.Length - 1; i++)
                {
                    if (patrolPoints[i] != null && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                }
            }
        }
    }
}