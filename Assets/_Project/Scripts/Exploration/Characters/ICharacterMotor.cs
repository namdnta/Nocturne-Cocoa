using UnityEngine;

namespace NocturneCocoa.Exploration
{
    /// <summary>
    /// What a moving character exposes to its animator. Implemented by PlayerMovement and NpcMovement,
    /// so one CharacterAnimator works for both.
    /// </summary>
    public interface ICharacterMotor
    {
        /// <summary>Direction the character faces (unit length).</summary>
        Vector2 Facing { get; }

        /// <summary>Current movement velocity in world units per second.</summary>
        Vector2 Velocity { get; }

        bool IsMoving { get; }

        float MoveSpeed { get; }
    }
}