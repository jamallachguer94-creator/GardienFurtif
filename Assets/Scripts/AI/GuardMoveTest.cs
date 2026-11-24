using UnityEngine;
using UnityEngine.AI;

// TEMPORARY test: makes the guard walk to a target using SetDestination.
// Will be replaced by the real Guard script in Phase 3.
[RequireComponent(typeof(NavMeshAgent))]
public class GuardMoveTest : MonoBehaviour
{
    [SerializeField] private Transform target;

    private NavMeshAgent agent;
    private bool arrived;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("GuardMoveTest: assign a target in the Inspector.", this);
            return;
        }

        agent.SetDestination(target.position);   // the key NavMesh call
        Debug.Log($"[GuardMoveTest] Going to {target.position}");
    }

    private void Update()
    {
        if (arrived || target == null) return;

        // pathPending = Unity is still computing the path
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            arrived = true;
            Debug.Log($"[GuardMoveTest] Arrived. pathStatus = {agent.pathStatus}");
        }
    }
}