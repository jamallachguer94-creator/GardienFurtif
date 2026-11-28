using System;
using UnityEngine;

/// <summary>
/// Lightweight ambient movement for actors that live outside the playable main office.
/// Uses authored waypoints and does not depend on NavMesh, Rigidbody or gameplay AI.
/// </summary>
[DisallowMultipleComponent]
public sealed class CompoundAmbientMover : MonoBehaviour
{
    [SerializeField] private Vector3[] waypoints = Array.Empty<Vector3>();
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 7f;
    [SerializeField] private bool loop = true;
    [SerializeField] private Transform[] gaitPivots = Array.Empty<Transform>();
    [SerializeField] private float[] gaitPhaseOffsets = Array.Empty<float>();
    [SerializeField] private float gaitFrequency = 7f;
    [SerializeField] private float gaitAmplitude = 24f;
    [SerializeField] private float bodyBob = 0.018f;
    [SerializeField] private Transform[] rotatingWheels = Array.Empty<Transform>();
    [SerializeField] private float wheelRadius = 0.34f;

    private int waypointIndex = 1;
    private float baseY;
    private float gaitTime;

    public void Configure(
        Vector3[] route,
        float speed,
        bool shouldLoop,
        Transform[] limbs = null,
        float[] phaseOffsets = null,
        Transform[] wheels = null,
        float wheelRadiusMetres = 0.34f,
        float gaitRate = 7f,
        float swingDegrees = 24f,
        float bobAmount = 0.018f)
    {
        waypoints = route != null ? (Vector3[])route.Clone() : Array.Empty<Vector3>();
        moveSpeed = Mathf.Max(0.05f, speed);
        loop = shouldLoop;
        gaitPivots = limbs ?? Array.Empty<Transform>();
        gaitPhaseOffsets = phaseOffsets ?? Array.Empty<float>();
        rotatingWheels = wheels ?? Array.Empty<Transform>();
        wheelRadius = Mathf.Max(0.05f, wheelRadiusMetres);
        gaitFrequency = gaitRate;
        gaitAmplitude = swingDegrees;
        bodyBob = bobAmount;
        waypointIndex = waypoints.Length > 1 ? 1 : 0;
        baseY = transform.position.y;
    }

    private void Start()
    {
        baseY = transform.position.y;
        if (waypoints == null) waypoints = Array.Empty<Vector3>();
        if (waypointIndex <= 0 && waypoints.Length > 1) waypointIndex = 1;
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Vector3 current = transform.position;
        Vector3 target = waypoints[waypointIndex];
        Vector3 toTarget = target - current;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.20f * 0.20f)
        {
            AdvanceWaypoint();
            target = waypoints[waypointIndex];
            toTarget = target - current;
            toTarget.y = 0f;
        }

        float distanceBefore = toTarget.magnitude;
        Vector3 direction = distanceBefore > 0.001f ? toTarget / distanceBefore : Vector3.zero;
        float step = moveSpeed * Time.deltaTime;
        Vector3 next = Vector3.MoveTowards(current, target, step);
        next.y = baseY;
        transform.position = next;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion wanted = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, wanted, turnSpeed * Time.deltaTime);
        }

        float moved = Vector3.Distance(current, next);
        if (rotatingWheels != null && rotatingWheels.Length > 0 && moved > 0f)
        {
            float degrees = moved / wheelRadius * Mathf.Rad2Deg;
            for (int i = 0; i < rotatingWheels.Length; i++)
            {
                if (rotatingWheels[i] != null)
                    rotatingWheels[i].Rotate(Vector3.right, degrees, Space.Self);
            }
        }

        gaitTime += Time.deltaTime * gaitFrequency;
        AnimateGait();
    }

    private void AdvanceWaypoint()
    {
        waypointIndex++;
        if (waypointIndex >= waypoints.Length)
            waypointIndex = loop ? 0 : waypoints.Length - 1;
    }

    private void AnimateGait()
    {
        if (gaitPivots == null || gaitPivots.Length == 0) return;

        for (int i = 0; i < gaitPivots.Length; i++)
        {
            Transform pivot = gaitPivots[i];
            if (pivot == null) continue;
            float phase = gaitPhaseOffsets != null && i < gaitPhaseOffsets.Length
                ? gaitPhaseOffsets[i]
                : (i % 2 == 0 ? 0f : Mathf.PI);
            float swing = Mathf.Sin(gaitTime + phase) * gaitAmplitude;
            pivot.localRotation = Quaternion.Euler(swing, 0f, 0f);
        }

        if (bodyBob > 0f && gaitPivots.Length >= 4)
        {
            // A tiny vertical body motion makes the walk cycle read without changing ground clearance.
            Transform visualRoot = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (visualRoot != null)
            {
                Vector3 local = visualRoot.localPosition;
                local.y = Mathf.Sin(gaitTime * 2f) * bodyBob;
                visualRoot.localPosition = local;
            }
        }
    }
}
