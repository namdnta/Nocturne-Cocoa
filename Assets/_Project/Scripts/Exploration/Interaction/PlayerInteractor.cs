using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// Finds the Interactable in front of the player and interacts with it on request.
    /// Detection is a small circle placed in the facing direction, so the player has to look at things.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;

        [Tooltip("Radius of the detection circle, in world units.")]
        [SerializeField, Min(0.05f)] private float detectionRadius = 0.6f;

        [Tooltip("How far in front of the player the circle sits.")]
        [SerializeField, Min(0f)] private float forwardOffset = 0.5f;

        [Tooltip("Offset from the pivot. If the sprite pivot is at the feet, raise this to the body centre (e.g. 0, 0.4).")]
        [SerializeField] private Vector2 originOffset = new Vector2(0f, 0.4f);

        [Tooltip("Layers to search. Everything by default; restrict to an \"Interactable\" layer later if you like.")]
        [SerializeField] private LayerMask interactableLayers = ~0;

        /// <summary>Raised when the current target changes (null = nothing in range). Use for prompts.</summary>
        public event Action<Interactable> TargetChanged;

        /// <summary>Raised after the player interacts with something.</summary>
        public event Action<Interactable> Interacted;

        private readonly List<Collider2D> hits = new List<Collider2D>(16);
        private ContactFilter2D filter;

        public Interactable CurrentTarget { get; private set; }

        /// <summary>When false, no target is selected (e.g. during dialogue).</summary>
        public bool DetectionEnabled { get; set; }

        private Vector2 DetectionCenter
        {
            get
            {
                Vector2 facing = movement != null ? movement.Facing : Vector2.down;
                return (Vector2)transform.position + originOffset + facing * forwardOffset;
            }
        }

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();

            DetectionEnabled = true;
            filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.SetLayerMask(interactableLayers);
        }

        private void Update()
        {
            RefreshTarget();
        }

        private void OnDisable()
        {
            SetTarget(null);
        }

        /// <summary>Interacts with the current target. Returns false if there was nothing to interact with.</summary>
        public bool TryInteract()
        {
            RefreshTarget();
            Interactable target = CurrentTarget;
            if (target == null)
            {
                return false;
            }

            target.Interact(this);
            if (Interacted != null) Interacted.Invoke(target);
            return true;
        }

        private void RefreshTarget()
        {
            SetTarget(DetectionEnabled ? FindBestTarget() : null);
        }

        private Interactable FindBestTarget()
        {
            Vector2 center = DetectionCenter;
            hits.Clear();
            Physics2D.OverlapCircle(center, detectionRadius, filter, hits);

            Interactable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Count; i++)
            {
                Interactable candidate = hits[i].GetComponentInParent<Interactable>();
                if (candidate == null || candidate.transform.IsChildOf(transform) || !candidate.CanInteract(this))
                {
                    continue;
                }

                float distance = ((Vector2)hits[i].ClosestPoint(center) - center).sqrMagnitude;

                bool better = best == null
                    || candidate.Priority > best.Priority
                    || (candidate.Priority == best.Priority && distance < bestDistance);

                if (better)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void SetTarget(Interactable target)
        {
            if (ReferenceEquals(target, CurrentTarget))
            {
                return;
            }

            if (CurrentTarget != null)
            {
                CurrentTarget.SetFocused(false);
            }

            CurrentTarget = target;

            if (CurrentTarget != null)
            {
                CurrentTarget.SetFocused(true);
            }

            if (TargetChanged != null) TargetChanged.Invoke(CurrentTarget);
        }

        private void OnDrawGizmosSelected()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            Gizmos.color = CurrentTarget != null ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(DetectionCenter, detectionRadius);
        }
    }
}