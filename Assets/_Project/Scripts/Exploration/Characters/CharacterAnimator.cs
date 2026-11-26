using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// Sends movement state from any ICharacterMotor (PlayerMovement, NpcMovement) to an Animator
    /// (walk/idle blend trees) and optionally flips the sprite. Works without an Animator too,
    /// so placeholder characters still turn left/right.
    ///
    /// Expected Animator parameters (all optional; missing ones are skipped):
    ///   MoveX (float), MoveY (float)  - facing direction, -1..1. Use these for a 2D blend tree.
    ///   IsMoving (bool)               - walk vs idle.
    ///   Speed (float)                 - 0..1 of max speed, for walk/run blending.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Nocturne Cocoa/Exploration/Character Animator")]
    public class CharacterAnimator : MonoBehaviour
    {
        [Tooltip("Optional. Usually on the child object that holds the sprite.")]
        [SerializeField] private Animator animator;

        [Tooltip("Optional. Needed only if Flip Sprite For Left is on.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Turn on if you only draw right-facing side sprites and want them mirrored for left.")]
        [SerializeField] private bool flipSpriteForLeft = true;

        [Header("Animator parameter names")]
        [SerializeField] private string moveXParameter = "MoveX";
        [SerializeField] private string moveYParameter = "MoveY";
        [SerializeField] private string isMovingParameter = "IsMoving";
        [SerializeField] private string speedParameter = "Speed";

        private int moveXHash;
        private int moveYHash;
        private int isMovingHash;
        private int speedHash;
        private bool hasMoveX;
        private bool hasMoveY;
        private bool hasIsMoving;
        private bool hasSpeed;

        private ICharacterMotor movement;

        private void Awake()
        {
            movement = GetComponentInParent<ICharacterMotor>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            CacheParameters();
        }

        /// <summary>Call this if you swap the Animator Controller at runtime.</summary>
        public void CacheParameters()
        {
            moveXHash = Animator.StringToHash(moveXParameter);
            moveYHash = Animator.StringToHash(moveYParameter);
            isMovingHash = Animator.StringToHash(isMovingParameter);
            speedHash = Animator.StringToHash(speedParameter);

            hasMoveX = HasParameter(moveXHash);
            hasMoveY = HasParameter(moveYHash);
            hasIsMoving = HasParameter(isMovingHash);
            hasSpeed = HasParameter(speedHash);
        }

        private void LateUpdate()
        {
            if (movement == null)
            {
                return;
            }

            Vector2 facing = movement.Facing;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (hasMoveX) animator.SetFloat(moveXHash, facing.x);
                if (hasMoveY) animator.SetFloat(moveYHash, facing.y);
                if (hasIsMoving) animator.SetBool(isMovingHash, movement.IsMoving);
                if (hasSpeed)
                {
                    float speed01 = movement.MoveSpeed > 0f ? movement.Velocity.magnitude / movement.MoveSpeed : 0f;
                    animator.SetFloat(speedHash, speed01);
                }
            }

            // Only flip when the character clearly faces sideways, so up/down sprites aren't mirrored.
            if (flipSpriteForLeft && spriteRenderer != null && Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
            {
                spriteRenderer.flipX = facing.x < 0f;
            }
        }

        private bool HasParameter(int hash)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == hash)
                {
                    return true;
                }
            }

            return false;
        }
    }
}