using UnityEngine;

/// <summary>
/// Player enters the interaction volume and presses E to collect the dossier.
/// Extended to support WorldObjectiveMarker, proximity guide dialogue, and shared interaction arbitration.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionFilePickup : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public string prompt = "Press E to steal the Main Dossier";
    public GameObject visualRoot;

    [Header("Objective & World Marker")]
    public string displayName = "Main Dossier";
    public Sprite dossierIcon;
    public WorldObjectiveMarker worldMarker;
    public Vector3 markerOffset = new Vector3(0f, 1.35f, 0f);
    public bool autoCreateWorldMarker = true;

    private Collider interactionCollider;
    private MissionManager mission;
    private bool playerInRange;
    private bool collected;
    private bool hasTriggeredProximityGuide;

    public Sprite Icon => dossierIcon != null ? dossierIcon : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Dossier);
    public WorldObjectiveMarker WorldMarker => worldMarker;
    public bool IsCollected => collected;

    private void Awake()
    {
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider != null) interactionCollider.isTrigger = true;
        if (visualRoot == null)
        {
            Transform visual = transform.Find("DossierVisual");
            if (visual != null) visualRoot = visual.gameObject;
        }
    }

    private void Start()
    {
        mission = MissionManager.Instance;
        if (mission == null) mission = FindFirstObjectByType<MissionManager>();
        EnsureWorldMarker();
    }

    public void EnsureWorldMarker()
    {
        if (collected)
        {
            if (worldMarker != null) worldMarker.SetMarkerVisible(false);
            return;
        }

        if (worldMarker == null)
            worldMarker = GetComponentInChildren<WorldObjectiveMarker>(true);

        if (worldMarker == null && autoCreateWorldMarker)
        {
            GameObject markerObj = new GameObject("DossierWorldMarker");
            markerObj.transform.SetParent(transform, false);
            markerObj.transform.localPosition = markerOffset;
            worldMarker = markerObj.AddComponent<WorldObjectiveMarker>();
        }

        if (worldMarker != null)
        {
            worldMarker.Configure(
                transform,
                markerOffset,
                Icon,
                "MAIN DOSSIER",
                new Color(0.18f, 0.58f, 1f, 1f),
                showDist: true,
                showLabelOnHoverOrClose: true,
                enableScreenEdgeArrow: true);
            worldMarker.SetMarkerVisible(true);
        }
    }

    private void Update()
    {
        if (collected || !playerInRange || mission == null) return;
        if (mission.MissionCompleted || mission.MissionFailed) return;

        if (!mission.IsPrimaryActiveInteractable(this))
            return;

        if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(mission.interactKey))
        {
            collected = true;
            playerInRange = false;
            if (visualRoot != null) visualRoot.SetActive(false);
            if (interactionCollider != null) interactionCollider.enabled = false;
            if (worldMarker != null) worldMarker.SetMarkerVisible(false);

            mission.UnregisterInteractable(this);
            mission.ClearPrompt();
            mission.CollectDossier();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (mission == null) mission = MissionManager.Instance;
        if (mission != null && mission.IsPlayer(other))
        {
            playerInRange = true;
            if (!mission.HasDossier)
            {
                mission.RegisterInteractable(this, transform, prompt, Icon, displayName);
                if (!hasTriggeredProximityGuide)
                {
                    hasTriggeredProximityGuide = true;
                    mission.NotifyPlayerNearDossier();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (mission == null) mission = MissionManager.Instance;
        if (mission != null && mission.IsPlayer(other))
        {
            playerInRange = false;
            mission.UnregisterInteractable(this);
        }
    }

    private void OnDisable()
    {
        playerInRange = false;
        if (mission == null) mission = MissionManager.Instance;
        if (mission != null)
            mission.UnregisterInteractable(this);
    }
}
