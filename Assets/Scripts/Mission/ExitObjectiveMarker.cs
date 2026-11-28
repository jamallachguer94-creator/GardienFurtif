using UnityEngine;

/// <summary>
/// Controls the dynamic world-space exit marker at Mission_ExitPoint.
/// Remains hidden at mission start, activates immediately when the main dossier is collected,
/// displays locked/ready states depending on required loot, and hides upon mission completion.
/// </summary>
public class ExitObjectiveMarker : MonoBehaviour
{
    [Header("Exit Marker Configuration")]
    [SerializeField] private Transform exitTransform;
    [SerializeField] private Vector3 markerOffset = new Vector3(0f, 1.85f, 0f);
    [SerializeField] private Sprite exitReadyIcon;
    [SerializeField] private Sprite exitLockedIcon;
    [SerializeField] private string readyLabel = "EXIT";
    [SerializeField] private string lockedLabel = "EXIT (LOOT REQUIRED)";
    [SerializeField] private Color readyColor = new Color(0.18f, 0.88f, 0.44f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.95f, 0.58f, 0.15f, 1f);

    private WorldObjectiveMarker worldMarker;
    private bool isUnlockedByDossier;

    public WorldObjectiveMarker WorldMarker => worldMarker;
    public bool IsActiveInWorld => worldMarker != null && worldMarker.IsMarkerVisible;

    private void Awake()
    {
        if (exitTransform == null)
            exitTransform = transform;

        EnsureMarkerComponent();
        SetExitMarkerActive(false, false);
    }

    public void SetExitTransform(Transform target)
    {
        if (target != null)
            exitTransform = target;
        EnsureMarkerComponent();
    }

    private void EnsureMarkerComponent()
    {
        if (worldMarker == null)
            worldMarker = GetComponentInChildren<WorldObjectiveMarker>(true);

        if (worldMarker == null)
        {
            GameObject markerChild = new GameObject("ExitWorldMarker");
            markerChild.transform.SetParent(transform, false);
            markerChild.transform.localPosition = markerOffset;
            worldMarker = markerChild.AddComponent<WorldObjectiveMarker>();
        }

        Sprite icon = exitReadyIcon != null
            ? exitReadyIcon
            : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.ExitDoor);

        worldMarker.Configure(
            exitTransform != null ? exitTransform : transform,
            markerOffset,
            icon,
            readyLabel,
            readyColor,
            showDist: true,
            showLabelOnHoverOrClose: true,
            enableScreenEdgeArrow: true);
    }

    public void SetExitMarkerActive(bool active, bool lockedByRequiredLoot)
    {
        isUnlockedByDossier = active;
        EnsureMarkerComponent();

        if (!active)
        {
            worldMarker.SetMarkerVisible(false);
            return;
        }

        Sprite iconToUse = lockedByRequiredLoot
            ? (exitLockedIcon != null ? exitLockedIcon : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Lock))
            : (exitReadyIcon != null ? exitReadyIcon : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.ExitDoor));

        string labelToUse = lockedByRequiredLoot ? lockedLabel : readyLabel;
        Color colorToUse = lockedByRequiredLoot ? lockedColor : readyColor;

        worldMarker.UpdateVisualState(iconToUse, labelToUse, colorToUse);
        worldMarker.SetMarkerVisible(true);
    }

    public void RefreshLockState(bool lockedByRequiredLoot)
    {
        if (!isUnlockedByDossier) return;
        SetExitMarkerActive(true, lockedByRequiredLoot);
    }
}
