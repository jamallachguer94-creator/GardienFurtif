using System;
using UnityEngine;

/// <summary>
/// Emits gameplay noise events for guard hearing. Attach to the Player root.
/// Other systems can subscribe to NoiseEmitter.OnNoiseEmitted.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(CharacterController))]
public sealed class NoiseEmitter : MonoBehaviour
{
    /// <summary>Event signature: noise world position, then noise radius in metres.</summary>
    public static event Action<Vector3, float> OnNoiseEmitted;

    [Header("Footstep noise radius (metres)")]
    [SerializeField, Min(0f)] private float walkingRadius = 5f;
    [SerializeField, Min(0f)] private float sprintingRadius = 9f;
    [SerializeField, Min(0f)] private float crouchingRadius = 1.5f;

    [Header("Footstep timing (seconds)")]
    [SerializeField, Min(0.05f)] private float walkingInterval = 0.52f;
    [SerializeField, Min(0.05f)] private float sprintingInterval = 0.32f;
    [SerializeField, Min(0.05f)] private float crouchingInterval = 0.72f;
    [SerializeField, Min(0f)] private float minimumMovementSpeed = 0.15f;

    private PlayerMovement movement;
    private float stepTimer;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (movement == null || !movement.IsGrounded || movement.PlanarSpeed < minimumMovementSpeed)
        {
            stepTimer = 0f;
            return;
        }

        bool crouching = movement.IsCrouching;
        bool sprinting = !crouching && Input.GetKey(KeyCode.LeftShift) && movement.PlanarSpeed > 3.5f;

        float interval = crouching ? crouchingInterval : (sprinting ? sprintingInterval : walkingInterval);
        float radius = crouching ? crouchingRadius : (sprinting ? sprintingRadius : walkingRadius);

        stepTimer += Time.deltaTime;
        if (stepTimer >= interval)
        {
            stepTimer = 0f;
            BroadcastNoise(transform.position, radius);
        }
    }

    /// <summary>Call this from a thrown object, bottle, or other gameplay noise source.</summary>
    public static void BroadcastNoise(Vector3 worldPosition, float radius)
    {
        if (radius <= 0f) return;
        OnNoiseEmitted?.Invoke(worldPosition, radius);
    }

    /// <summary>Convenience instance method for scripts that hold a reference to this component.</summary>
    public void EmitNoiseAt(Vector3 worldPosition, float radius)
    {
        BroadcastNoise(worldPosition, radius);
    }
}
