using UnityEngine;

/// <summary>
/// Core third-person / first-person player locomotion using Unity's CharacterController
/// and the legacy Input Manager (WASD, Left Shift, Left Ctrl).
/// Attach this component to the Player root, not to the visual model.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;

    [Header("Movement speeds (metres / second)")]
    [SerializeField, Min(0f)] private float walkSpeed = 3.2f;
    [SerializeField, Min(0f)] private float sprintSpeed = 5.2f;
    [SerializeField, Min(0f)] private float crouchSpeed = 1.65f;
    [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.12f;

    [Header("Crouching")]
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField, Min(0.5f)] private float crouchingHeight = 1.2f;
    [SerializeField, Min(0.1f)] private float crouchTransitionSpeed = 8f;

    [Header("Sprint")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Gravity")]
    [SerializeField] private float gravity = -22f;
    [SerializeField] private float groundedGravity = -2f;

    private float standingHeight;
    private Vector3 standingCenter;
    private float verticalVelocity;
    private float rotationVelocity;
    private readonly Collider[] standCheckResults = new Collider[16];

    public bool IsCrouching { get; private set; }
    public bool IsGrounded => controller != null && controller.isGrounded;
    public float PlanarSpeed { get; private set; }

    /// <summary>Approximate eye height above the Player root, used by first-person camera.</summary>
    public float EyeHeight
    {
        get
        {
            if (controller == null) return 1.6f;
            return controller.center.y + (controller.height * 0.5f) - 0.12f;
        }
    }

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("PlayerMovement requires a CharacterController on the same GameObject.", this);
            enabled = false;
            return;
        }

        standingHeight = controller.height;
        standingCenter = controller.center;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (controller == null) return;

        UpdateCrouch();
        MoveCharacter();
    }

    /// <summary>May be called by PlayerCameraController to provide the active camera transform.</summary>
    public void SetCameraTransform(Transform cameraToUse)
    {
        cameraTransform = cameraToUse;
    }

    private void MoveCharacter()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector2 input = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * input.y + right * input.x).normalized;
        bool wantsSprint = Input.GetKey(sprintKey) && !IsCrouching && input.y > 0.1f;
        float speed = IsCrouching ? crouchSpeed : (wantsSprint ? sprintSpeed : walkSpeed);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref rotationVelocity,
                rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedGravity;
        else
            verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        Vector3 actualVelocity = controller.velocity;
        PlanarSpeed = new Vector3(actualVelocity.x, 0f, actualVelocity.z).magnitude;
    }

    private void UpdateCrouch()
    {
        bool crouchRequested = Input.GetKey(crouchKey);
        if (!crouchRequested && IsCrouching && !CanStandUp())
            crouchRequested = true;

        float targetHeight = crouchRequested ? Mathf.Min(crouchingHeight, standingHeight) : standingHeight;
        float newHeight = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // Keep the bottom of the CharacterController at the same world height while resizing.
        float heightDifference = standingHeight - newHeight;
        controller.height = newHeight;
        controller.center = standingCenter - new Vector3(0f, heightDifference * 0.5f, 0f);
        IsCrouching = newHeight < standingHeight - 0.03f;
    }

    private bool CanStandUp()
    {
        float radius = Mathf.Max(0.05f, controller.radius * 0.9f);
        Vector3 worldBottom = transform.position + standingCenter - Vector3.up * (standingHeight * 0.5f) + Vector3.up * (radius + 0.08f);
        Vector3 worldTop = transform.position + standingCenter + Vector3.up * (standingHeight * 0.5f - radius);
        int count = Physics.OverlapCapsuleNonAlloc(
            worldBottom,
            worldTop,
            radius,
            standCheckResults,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider other = standCheckResults[i];
            if (other == null || other == controller) continue;
            if (other.transform == transform || other.transform.IsChildOf(transform)) continue;
            return false;
        }

        return true;
    }
}
