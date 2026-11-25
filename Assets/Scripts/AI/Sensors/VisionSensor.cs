using UnityEngine;

// Sees the target if: close enough, inside the cone, and nothing blocks the ray.
// Anti-flicker: short delay before "seen" / "lost" + a small margin once already seeing.
public class VisionSensor : MonoBehaviour
{
    [Header("Detection (project spec)")]
    [SerializeField] private float viewDistance = 12f;
    [SerializeField, Range(1f, 180f)] private float viewAngle = 50f; // each side of forward

    [Header("Eyes and target")]
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 0.6f, 0f); // from guard pivot
    [SerializeField] private string targetTag = "Player";

    [Header("Anti-flicker")]
    [SerializeField] private float acquireTime = 0.15f; // seconds visible before "seen"
    [SerializeField] private float loseTime = 0.5f;     // seconds hidden before "lost"
    [SerializeField] private float rangeMargin = 1f;    // extra metres once already seeing
    [SerializeField] private float angleMargin = 5f;    // extra degrees once already seeing

    [Header("Debug (read only, watch in Play mode)")]
    [SerializeField] private bool debugIsSeeing;
    [SerializeField] private bool debugVisibleNow;
    [SerializeField] private float debugDistance;
    [SerializeField] private float debugAngle;
    [SerializeField] private string debugRayHit;
    [SerializeField] private float debugHiddenTimer;

    public bool IsSeeing { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }
    public float LastSeenDistance { get; private set; }

    private Transform target;
    private Collider targetCollider;
    private float visibleTimer;
    private float hiddenTimer;
    private bool prevVisibleNow;

    private void Start()
    {
        FindTarget();
        if (target == null)
            Debug.LogWarning($"[VisionSensor] {name}: no object with tag '{targetTag}' found yet.", this);
    }

    private void FindTarget()
    {
        GameObject go = GameObject.FindWithTag(targetTag);
        if (go == null) return;
        target = go.transform;
        targetCollider = go.GetComponentInChildren<Collider>();
    }

    private void Update()
    {
        if (target == null) { FindTarget(); if (target == null) return; }

        bool visibleNow = CheckVisible(IsSeeing, out float dist);
                if (visibleNow != prevVisibleNow)
        {
            prevVisibleNow = visibleNow;
            Debug.Log($"[VisionSensor-debug] {name}: visibleNow={visibleNow} dist={debugDistance:F1} angle={debugAngle:F0} ray={debugRayHit}");
        }

        if (visibleNow)
        {
            visibleTimer += Time.deltaTime;
            hiddenTimer = 0f;
            LastSeenPosition = target.position;
            LastSeenDistance = dist;
            if (!IsSeeing && visibleTimer >= acquireTime)
            {
                IsSeeing = true;
                Debug.Log($"[VisionSensor] {name}: spotted {target.name} at {dist:F1} m");
            }
        }
        else
        {
            hiddenTimer += Time.deltaTime;
            visibleTimer = 0f;
            if (IsSeeing && hiddenTimer >= loseTime)
            {
                IsSeeing = false;
                Debug.Log($"[VisionSensor] {name}: lost sight of {target.name} (last seen at {LastSeenDistance:F1} m)");
            }
        }

        debugIsSeeing = IsSeeing;
        debugVisibleNow = visibleNow;
        debugHiddenTimer = hiddenTimer;
    }

    // useMargin = true when we are already seeing (hysteresis, avoids flicker at the edge)
    private bool CheckVisible(bool useMargin, out float dist)
    {
        Vector3 eye = transform.position + eyeOffset;
        Vector3 point = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up;
        Vector3 toTarget = point - eye;
        dist = toTarget.magnitude;
        float angle = Vector3.Angle(transform.forward, toTarget);

        debugDistance = dist;
        debugAngle = angle;
        debugRayHit = "(not tested)";

        if (dist > viewDistance + (useMargin ? rangeMargin : 0f)) return false;
        if (angle > viewAngle + (useMargin ? angleMargin : 0f)) return false;

        // Ray from the eye to the target: it must hit the target first, not a wall.
        if (Physics.Raycast(eye, toTarget.normalized, out RaycastHit hit, dist + 0.1f, ~0, QueryTriggerInteraction.Ignore))
        {
            debugRayHit = hit.transform.name;
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        debugRayHit = "(nothing hit)";
        return false;
    }

    // Draws the cone in the Scene view (green = nothing, red = seeing).
    private void OnDrawGizmos()
    {
        Vector3 eye = transform.position + eyeOffset;
        Gizmos.color = IsSeeing ? Color.red : Color.green;

        Vector3 prev = eye + Quaternion.Euler(0f, -viewAngle, 0f) * transform.forward * viewDistance;
        Gizmos.DrawLine(eye, prev);
        const int steps = 24;
        for (int i = 1; i <= steps; i++)
        {
            float a = Mathf.Lerp(-viewAngle, viewAngle, i / (float)steps);
            Vector3 next = eye + Quaternion.Euler(0f, a, 0f) * transform.forward * viewDistance;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        Gizmos.DrawLine(eye, prev);

        if (IsSeeing) Gizmos.DrawLine(eye, LastSeenPosition);
    }
}