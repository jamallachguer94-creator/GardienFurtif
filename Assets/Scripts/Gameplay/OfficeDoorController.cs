using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Interactive swing door. The interaction trigger stays fixed while only the
/// pivot and door panel rotate. Aim at the panel and press E while nearby.
/// </summary>
[DisallowMultipleComponent]
public sealed class OfficeDoorController : MonoBehaviour
{
    [SerializeField] private Transform doorPivot;
    [SerializeField] private Transform doorPanel;
    [SerializeField] private string doorLabel = "Door";
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField, Min(0.5f)] private float interactionDistance = 3f;
    [SerializeField] private float openAngle = 105f;
    [SerializeField, Min(0.1f)] private float animationSpeed = 5.5f;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private NavMeshObstacle navObstacle;
    private bool playerInRange;
    private bool focused;

    public bool IsOpen { get; private set; }

    public void Configure(
        Transform pivot,
        Transform panel,
        string label,
        float angle,
        float speed)
    {
        doorPivot = pivot;
        doorPanel = panel;
        doorLabel = string.IsNullOrWhiteSpace(label) ? "Door" : label;
        openAngle = angle;
        animationSpeed = Mathf.Max(0.1f, speed);
        CacheReferences();
        RecalculateRotations();
    }

    private void Awake()
    {
        CacheReferences();
        RecalculateRotations();
    }

    private void CacheReferences()
    {
        if (doorPivot == null)
            doorPivot = transform.Find("SwingPivot");

        if (doorPivot == null)
            doorPivot = transform;

        if (doorPanel == null && doorPivot != null)
            doorPanel = doorPivot.Find("DoorPanel");

        if (doorPanel == null) return;

        navObstacle = doorPanel.GetComponent<NavMeshObstacle>();
        if (navObstacle == null)
            navObstacle = doorPanel.gameObject.AddComponent<NavMeshObstacle>();

        navObstacle.shape = NavMeshObstacleShape.Box;
        navObstacle.size = Vector3.one;
        navObstacle.carving = true;
        navObstacle.carvingMoveThreshold = 0.08f;
        navObstacle.carvingTimeToStationary = 0.10f;
        navObstacle.enabled = !IsOpen;
    }

    private void RecalculateRotations()
    {
        if (doorPivot == null) return;
        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation * Quaternion.AngleAxis(openAngle, Vector3.up);
    }

    private void Update()
    {
        focused = playerInRange && IsCameraAimingAtDoor();

        if (focused && Input.GetKeyDown(interactionKey))
        {
            IsOpen = !IsOpen;

            if (navObstacle != null)
                navObstacle.enabled = false;

            Debug.Log($"[OfficeDoor] {doorLabel}: {(IsOpen ? "opened" : "closing")}");
        }

        if (doorPivot == null) return;

        Quaternion target = IsOpen ? openRotation : closedRotation;
        doorPivot.localRotation = Quaternion.Slerp(
            doorPivot.localRotation, target, Time.deltaTime * animationSpeed);

        if (navObstacle != null && !IsOpen &&
            Quaternion.Angle(doorPivot.localRotation, closedRotation) < 2f)
        {
            navObstacle.enabled = true;
        }
    }

    private bool IsCameraAimingAtDoor()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null || doorPanel == null) return false;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        Transform hitTransform = hit.collider.transform;
        return hitTransform == doorPanel || hitTransform.IsChildOf(doorPanel);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other)) playerInRange = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsPlayerCollider(other)) playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other)) playerInRange = false;
    }

    private static bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;

        CharacterController controller = other.GetComponentInParent<CharacterController>();
        return controller != null &&
               (controller.CompareTag("Player") || controller.gameObject.name == "Player");
    }

    private void OnGUI()
    {
        if (!focused) return;

        const float width = 250f;
        const float height = 34f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 105f, width, height);
        GUI.Box(rect, $"[E]  {(IsOpen ? "CLOSE" : "OPEN")}  {doorLabel}");
    }
}
