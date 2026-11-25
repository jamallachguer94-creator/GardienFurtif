using UnityEngine;
using UnityEngine.AI;

// Phase 3: the guard only patrols. The state machine comes in Phase 6.
[RequireComponent(typeof(NavMeshAgent))]
public class Guard : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitTimeAtPoint = 1.5f;

    private NavMeshAgent agent;
    private int currentIndex = -1;
    private float waitTimer;
    private bool waiting;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("Guard: assign patrol points in the Inspector.", this);
            enabled = false;
            return;
        }
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                GoToNextPatrolPoint();
            }
            return;
        }

        if (HasArrived())
        {
            waiting = true;
            waitTimer = waitTimeAtPoint;
            Debug.Log($"[Guard] Reached patrol point {currentIndex} (status={agent.pathStatus})");
        }
    }

    private bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    private void GoToNextPatrolPoint()
    {
        currentIndex = (currentIndex + 1) % patrolPoints.Length; // loops 0,1,2,3,0...
        agent.SetDestination(patrolPoints[currentIndex].position);
    }

    // Draws the route in the Scene view when the guard is selected.
    private void OnDrawGizmosSelected()
    {
        if (patrolPoints == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
            Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
            if (next != null) Gizmos.DrawLine(patrolPoints[i].position, next.position);
        }
    }
}