using System;
using UnityEngine;

/// <summary>
/// Represents an individual collectible loot item in Gardien Furtif with its own identity,
/// display name, objective description, icon, value, required-for-exit flag, and world marker.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LootCollectible : MonoBehaviour
{
    public enum LootPresetType
    {
        CashBundle,
        Laptop,
        ValuablePainting,
        Briefcase,
        JewelryBox,
        GoldObject,
        ValuableDocuments,
        Custom
    }

    [Header("Optional ScriptableObject Preset")]
    [SerializeField] private LootItemData presetData;

    [Header("Loot Identity")]
    [SerializeField] private string lootId = "";
    [SerializeField] private LootPresetType presetType = LootPresetType.CashBundle;
    [SerializeField] private string displayName = "Cash Bundle";
    [TextArea(1, 3)]
    [SerializeField] private string description = "Unmarked stack of bills left in the office.";
    [SerializeField] private string objectiveText = "Collect the cash bundle";
    [SerializeField] private Sprite icon;
    [SerializeField, Min(0)] private int monetaryValue = 250;

    [Header("Mission Rules")]
    [Tooltip("If enabled, this specific loot item must be collected before the exit can complete the mission.")]
    [SerializeField] private bool requiredForExit = false;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string customPrompt = "";
    [SerializeField] private string customGuideCollectedMessage = "";

    [Header("Visuals & World Marker")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private WorldObjectiveMarker worldMarker;
    [SerializeField] private Vector3 markerOffset = new Vector3(0f, 1.15f, 0f);
    [SerializeField] private bool autoCreateWorldMarker = true;

    private Collider interactionCollider;
    private MissionManager mission;
    private LootManager lootManager;
    private bool playerInRange;
    private bool isCollected;

    public string LootId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(lootId))
                lootId = $"{gameObject.name}_{GetInstanceID()}";
            return lootId;
        }
    }

    public LootPresetType PresetType => presetData != null ? presetData.presetType : presetType;
    public string DisplayName => presetData != null && !string.IsNullOrWhiteSpace(presetData.displayName)
        ? presetData.displayName
        : (string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName);
    public string Description => presetData != null ? presetData.description : description;
    public string ObjectiveText => presetData != null && !string.IsNullOrWhiteSpace(presetData.objectiveText)
        ? presetData.objectiveText
        : (string.IsNullOrWhiteSpace(objectiveText) ? $"Collect the {DisplayName.ToLowerInvariant()}" : objectiveText);
    public int MonetaryValue => Mathf.Max(0, presetData != null ? presetData.monetaryValue : monetaryValue);
    public bool RequiredForExit => presetData != null ? presetData.requiredForExit : requiredForExit;
    public bool IsCollected => isCollected;
    public bool IsPlayerInRange => playerInRange && !isCollected;
    public WorldObjectiveMarker WorldMarker => worldMarker;

    public Sprite Icon
    {
        get
        {
            if (icon != null) return icon;
            if (presetData != null && presetData.icon != null) return presetData.icon;
            return MissionIconFactory.GetIcon(GetFallbackIconKind(PresetType));
        }
        set => icon = value;
    }

    public string InteractionPrompt
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(customPrompt))
                return customPrompt;
            string valueSuffix = MonetaryValue > 0 ? $" (${MonetaryValue})" : string.Empty;
            return $"Press {interactKey} to collect {DisplayName}{valueSuffix}";
        }
    }

    public string GuideCollectedMessage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(customGuideCollectedMessage))
                return customGuideCollectedMessage;
            if (presetData != null && !string.IsNullOrWhiteSpace(presetData.customGuideCollectedMessage))
                return presetData.customGuideCollectedMessage;

            switch (PresetType)
            {
                case LootPresetType.CashBundle:
                    return $"You found the {DisplayName}!";
                case LootPresetType.ValuablePainting:
                    return $"Great! The {DisplayName} has been collected.";
                case LootPresetType.Laptop:
                    return $"Excellent! You collected the {DisplayName}.";
                default:
                    return $"Nice grab! {DisplayName} secured.";
            }
        }
    }

    public event Action<LootCollectible> OnCollected;

    public void ConfigurePreset(
        string id,
        LootPresetType preset,
        string itemDisplayName,
        string itemObjectiveText,
        int value,
        bool isRequired = false,
        Sprite assignedIcon = null)
    {
        lootId = id;
        presetType = preset;
        displayName = itemDisplayName;
        objectiveText = itemObjectiveText;
        monetaryValue = value;
        requiredForExit = isRequired;
        if (assignedIcon != null) icon = assignedIcon;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(lootId))
            lootId = $"{gameObject.name}_{Mathf.Abs(GetInstanceID())}";
    }

    private void Awake()
    {
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;

        if (visualRoot == null)
        {
            Transform foundVisual = transform.Find("LootVisual");
            if (foundVisual != null)
                visualRoot = foundVisual.gameObject;
        }
    }

    private void OnEnable()
    {
        RegisterWithManagers();
    }

    private void Start()
    {
        RegisterWithManagers();
        EnsureWorldMarker();
    }

    private void RegisterWithManagers()
    {
        if (mission == null)
            mission = MissionManager.Instance != null ? MissionManager.Instance : FindFirstObjectByType<MissionManager>();

        if (lootManager == null)
            lootManager = LootManager.Instance != null ? LootManager.Instance : FindFirstObjectByType<LootManager>();

        if (lootManager != null)
            lootManager.RegisterLoot(this);
    }

    public void EnsureWorldMarker()
    {
        if (isCollected)
        {
            if (worldMarker != null) worldMarker.SetMarkerVisible(false);
            return;
        }

        if (worldMarker == null)
            worldMarker = GetComponentInChildren<WorldObjectiveMarker>(true);

        if (worldMarker == null && autoCreateWorldMarker)
        {
            GameObject markerObj = new GameObject($"Marker_{DisplayName}");
            markerObj.transform.SetParent(transform, false);
            markerObj.transform.localPosition = markerOffset;
            worldMarker = markerObj.AddComponent<WorldObjectiveMarker>();
        }

        if (worldMarker != null)
        {
            Color badgeColor = RequiredForExit
                ? new Color(1f, 0.58f, 0.14f, 1f)
                : new Color(0.95f, 0.75f, 0.15f, 1f);

            worldMarker.Configure(
                transform,
                markerOffset,
                Icon,
                DisplayName,
                badgeColor,
                showDist: true,
                showLabelOnHoverOrClose: true,
                enableScreenEdgeArrow: RequiredForExit);
            worldMarker.SetMarkerVisible(true);
        }
    }

    private void Update()
    {
        if (isCollected || !playerInRange) return;
        if (mission == null) mission = MissionManager.Instance;
        if (mission != null && (mission.MissionCompleted || mission.MissionFailed)) return;

        if (mission != null && !mission.IsPrimaryActiveInteractable(this))
            return;

        KeyCode keyToUse = mission != null ? mission.interactKey : interactKey;
        if (Input.GetKeyDown(keyToUse) || Input.GetKeyDown(interactKey))
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (isCollected) return;
        if (mission != null && (mission.MissionCompleted || mission.MissionFailed)) return;

        isCollected = true;
        playerInRange = false;

        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
        else
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (worldMarker != null && renderers[i].transform.IsChildOf(worldMarker.transform))
                    continue;
                renderers[i].enabled = false;
            }
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;

        if (worldMarker != null)
            worldMarker.SetMarkerVisible(false);

        if (mission != null)
            mission.UnregisterInteractable(this);

        if (lootManager == null)
            lootManager = LootManager.Instance != null ? LootManager.Instance : FindFirstObjectByType<LootManager>();

        if (lootManager != null)
            lootManager.NotifyLootCollected(this);
        else if (mission != null)
            mission.NotifyLootCollected(this);

        OnCollected?.Invoke(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (mission == null) mission = MissionManager.Instance;
        if (mission != null && mission.IsPlayer(other))
        {
            playerInRange = true;
            mission.RegisterInteractable(this, transform, InteractionPrompt, Icon, DisplayName);
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

    public static MissionIconFactory.IconKind GetFallbackIconKind(LootPresetType preset)
    {
        switch (preset)
        {
            case LootPresetType.CashBundle: return MissionIconFactory.IconKind.CashBundle;
            case LootPresetType.Laptop: return MissionIconFactory.IconKind.Laptop;
            case LootPresetType.ValuablePainting: return MissionIconFactory.IconKind.Painting;
            case LootPresetType.Briefcase: return MissionIconFactory.IconKind.Briefcase;
            case LootPresetType.JewelryBox: return MissionIconFactory.IconKind.JewelryBox;
            case LootPresetType.GoldObject: return MissionIconFactory.IconKind.GoldObject;
            case LootPresetType.ValuableDocuments: return MissionIconFactory.IconKind.ValuableDocuments;
            default: return MissionIconFactory.IconKind.LootBag;
        }
    }
}
