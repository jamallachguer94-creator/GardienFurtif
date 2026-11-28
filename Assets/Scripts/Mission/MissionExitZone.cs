using UnityEngine;

/// <summary>
/// Completes the heist when the player reaches the exit while carrying the dossier
/// and satisfying any configured required-loot rules.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionExitZone : MonoBehaviour
{
    [Header("World Marker")]
    public ExitObjectiveMarker exitMarker;

    private Collider zoneCollider;
    private MissionManager mission;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null) zoneCollider.isTrigger = true;
        EnsureExitMarker();
    }

    private void Start()
    {
        mission = MissionManager.Instance;
        if (mission == null) mission = FindFirstObjectByType<MissionManager>();
        EnsureExitMarker();
    }

    public ExitObjectiveMarker EnsureExitMarker()
    {
        if (exitMarker == null)
            exitMarker = GetComponent<ExitObjectiveMarker>();
        if (exitMarker == null)
            exitMarker = GetComponentInChildren<ExitObjectiveMarker>(true);
        if (exitMarker == null)
            exitMarker = gameObject.AddComponent<ExitObjectiveMarker>();

        exitMarker.SetExitTransform(transform);
        return exitMarker;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (mission == null) mission = MissionManager.Instance;
        if (mission != null && mission.IsPlayer(other))
            mission.PlayerReachedExit();
    }
}
