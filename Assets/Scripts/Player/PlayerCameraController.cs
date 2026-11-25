using UnityEngine;

/// <summary>
/// Mouse-look camera with Third Person as the default view and V to toggle First Person.
/// Attach this component to the scene's Main Camera.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform visualRoot;

    [Header("View mode")]
    [SerializeField] private bool startInThirdPerson = true;
    [SerializeField] private KeyCode toggleViewKey = KeyCode.V;
    [SerializeField, Min(1f)] private float thirdPersonDistance = 4.2f;
    [SerializeField, Min(0.1f)] private float thirdPersonFocusHeight = 1.35f;
    [SerializeField, Min(0.1f)] private float firstPersonFallbackHeight = 1.6f;

    [Header("Mouse look")]
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 2.2f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool lockCursorOnClick = true;

    private float yaw;
    private float pitch = 12f;
    private bool isFirstPerson;
    private Renderer[] visualRenderers;
    private bool[] originalRendererStates;

    public bool IsFirstPerson => isFirstPerson;

    private void Awake()
    {
        if (target != null && playerMovement == null)
            playerMovement = target.GetComponent<PlayerMovement>();

        if (visualRoot != null)
        {
            visualRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            originalRendererStates = new bool[visualRenderers.Length];
            for (int i = 0; i < visualRenderers.Length; i++)
                originalRendererStates[i] = visualRenderers[i] != null && visualRenderers[i].enabled;
        }

        if (target != null)
            yaw = target.eulerAngles.y;

        isFirstPerson = !startInThirdPerson;
        ApplyVisualVisibility();

        if (playerMovement != null)
            playerMovement.SetCameraTransform(transform);
    }

    private void Update()
    {
        if (target == null) return;

        if (lockCursorOnClick && Input.GetMouseButtonDown(0))
            SetCursorLocked(true);

        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLocked(false);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        if (Input.GetKeyDown(toggleViewKey))
        {
            isFirstPerson = !isFirstPerson;
            ApplyVisualVisibility();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Quaternion lookRotation = Quaternion.Euler(pitch, yaw, 0f);

        if (isFirstPerson)
        {
            float eyeHeight = playerMovement != null ? playerMovement.EyeHeight : firstPersonFallbackHeight;
            transform.position = target.position + Vector3.up * eyeHeight;
            transform.rotation = lookRotation;
        }
        else
        {
            float focusHeight = playerMovement != null
                ? Mathf.Min(thirdPersonFocusHeight, playerMovement.EyeHeight * 0.85f)
                : thirdPersonFocusHeight;

            Vector3 focusPoint = target.position + Vector3.up * focusHeight;
            Vector3 cameraOffset = lookRotation * new Vector3(0f, 0f, -thirdPersonDistance);
            transform.position = focusPoint + cameraOffset;
            transform.rotation = lookRotation;
        }
    }

    private void ApplyVisualVisibility()
    {
        if (visualRenderers == null) return;

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] == null) continue;
            visualRenderers[i].enabled = isFirstPerson ? false : originalRendererStates[i];
        }
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
        if (visualRenderers != null && originalRendererStates != null)
        {
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                if (visualRenderers[i] != null)
                    visualRenderers[i].enabled = originalRendererStates[i];
            }
        }
    }
}
