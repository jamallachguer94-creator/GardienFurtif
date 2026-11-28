using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the heist objective loop: collect the main dossier, collect optional/required loot,
/// and reach the exit. Preserves all existing public APIs while integrating MissionGuideUI,
/// MissionObjectiveTracker, LootManager, WorldObjectiveMarker, ExitObjectiveMarker, and MissionResultsUI.
/// </summary>
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Scene references")]
    public GameObject player;
    public MissionFilePickup filePickup;
    public MissionExitZone exitZone;
    public KeyCode interactKey = KeyCode.E;

    [Header("Loot & Exit Requirements")]
    [Tooltip("If enabled, ALL loot items in the scene must be collected before the exit can complete the mission.")]
    public bool RequireAllLootBeforeExit = false;

    [Header("Upgraded Mission Systems")]
    public LootManager lootManager;
    public MissionGuideUI guideUI;
    public MissionObjectiveTracker objectiveTracker;
    public MissionResultsUI resultsUI;
    public ExitObjectiveMarker exitObjectiveMarker;

    public bool HasDossier { get; private set; }
    public bool MissionCompleted { get; private set; }
    public bool MissionFailed { get; private set; }

    public event Action OnMissionStarted;
    public event Action OnDossierCollected;
    public event Action<LootCollectible> OnLootCollected;
    public event Action OnAllLootCollected;
    public event Action OnMissionCompleted;
    public event Action<string> OnMissionFailed;

    private MissionHUD hud;

    private struct ActiveInteractableEntry
    {
        public UnityEngine.Object Source;
        public Transform Transform;
        public string Prompt;
        public Sprite Icon;
        public string DisplayName;
    }

    private readonly List<ActiveInteractableEntry> activeInteractables = new List<ActiveInteractableEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MissionManager] Another MissionManager already exists. Disabling this duplicate.", this);
            enabled = false;
            return;
        }

        Instance = this;
        hud = GetComponent<MissionHUD>();
        if (hud == null) hud = gameObject.AddComponent<MissionHUD>();
        hud.SetManager(this);

        if (lootManager == null)
            lootManager = GetComponent<LootManager>();
        if (lootManager == null)
            lootManager = FindFirstObjectByType<LootManager>();
        if (lootManager == null)
            lootManager = gameObject.AddComponent<LootManager>();
    }

    private void Start()
    {
        ResolveReferences();
        EnsureRuntimeMissionUIIfNeeded();

        if (hud != null)
            hud.SuppressLegacyOnGUI = (objectiveTracker != null);

        if (exitObjectiveMarker != null)
            exitObjectiveMarker.SetExitMarkerActive(false, false);

        RefreshObjective();

        if (guideUI != null)
            guideUI.ShowMissionStartDialogue();

        OnMissionStarted?.Invoke();
    }

    private void Update()
    {
        if (activeInteractables.Count > 1)
            RefreshActiveInteractionPrompt();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = GameObject.Find("Player");
            if (player == null)
            {
                try { player = GameObject.FindGameObjectWithTag("Player"); }
                catch (UnityException) { }
            }
        }

        if (filePickup == null)
        {
            filePickup = FindFirstObjectByType<MissionFilePickup>();
            if (filePickup == null)
            {
                GameObject fileSpawn = GameObject.Find("Mission_FileSpawnPoint");
                if (fileSpawn != null)
                    filePickup = fileSpawn.GetComponentInChildren<MissionFilePickup>(true);
            }
        }

        if (exitZone == null)
        {
            exitZone = FindFirstObjectByType<MissionExitZone>();
            if (exitZone == null)
            {
                GameObject exitPoint = GameObject.Find("Mission_ExitPoint");
                if (exitPoint != null)
                    exitZone = exitPoint.GetComponentInChildren<MissionExitZone>(true);
            }
        }

        if (exitObjectiveMarker == null && exitZone != null)
        {
            exitObjectiveMarker = exitZone.EnsureExitMarker();
        }
        else if (exitObjectiveMarker == null)
        {
            GameObject exitPoint = GameObject.Find("Mission_ExitPoint");
            if (exitPoint != null)
            {
                exitObjectiveMarker = exitPoint.GetComponentInChildren<ExitObjectiveMarker>(true);
                if (exitObjectiveMarker == null)
                    exitObjectiveMarker = exitPoint.AddComponent<ExitObjectiveMarker>();
            }
        }

        if (lootManager == null)
            lootManager = LootManager.Instance != null ? LootManager.Instance : FindFirstObjectByType<LootManager>();

        if (guideUI == null)
            guideUI = FindFirstObjectByType<MissionGuideUI>();

        if (objectiveTracker == null)
            objectiveTracker = FindFirstObjectByType<MissionObjectiveTracker>();

        if (resultsUI == null)
            resultsUI = FindFirstObjectByType<MissionResultsUI>();
    }

    public bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (player == null) ResolveReferences();

        if (player != null)
        {
            Transform playerRoot = player.transform;
            Transform hit = other.transform;
            if (hit == playerRoot || hit.IsChildOf(playerRoot) || playerRoot.IsChildOf(hit))
                return true;

            CharacterController controller = other.GetComponentInParent<CharacterController>();
            if (controller != null && controller.gameObject == player)
                return true;
        }

        try { return other.CompareTag("Player"); }
        catch (UnityException) { return false; }
    }

    public void RegisterInteractable(UnityEngine.Object source, Transform targetTransform, string prompt, Sprite icon, string displayName)
    {
        if (source == null) return;
        for (int i = 0; i < activeInteractables.Count; i++)
        {
            if (activeInteractables[i].Source == source)
            {
                activeInteractables[i] = new ActiveInteractableEntry
                {
                    Source = source,
                    Transform = targetTransform,
                    Prompt = prompt,
                    Icon = icon,
                    DisplayName = displayName
                };
                RefreshActiveInteractionPrompt();
                return;
            }
        }

        activeInteractables.Add(new ActiveInteractableEntry
        {
            Source = source,
            Transform = targetTransform,
            Prompt = prompt,
            Icon = icon,
            DisplayName = displayName
        });
        RefreshActiveInteractionPrompt();
    }

    public void UnregisterInteractable(UnityEngine.Object source)
    {
        if (source == null) return;
        activeInteractables.RemoveAll(e => e.Source == null || e.Source == source);
        RefreshActiveInteractionPrompt();
    }

    public bool IsPrimaryActiveInteractable(UnityEngine.Object source)
    {
        if (source == null) return false;
        UnityEngine.Object primary = GetClosestInteractableSource(out _, out _);
        return primary == null || primary == source;
    }

    private UnityEngine.Object GetClosestInteractableSource(out string prompt, out Sprite icon)
    {
        prompt = string.Empty;
        icon = null;
        activeInteractables.RemoveAll(e => e.Source == null);
        if (activeInteractables.Count == 0) return null;

        Vector3 refPos = player != null ? player.transform.position : Vector3.zero;
        int bestIndex = 0;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < activeInteractables.Count; i++)
        {
            Transform t = activeInteractables[i].Transform;
            float dSq = t != null ? (t.position - refPos).sqrMagnitude : 0f;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                bestIndex = i;
            }
        }

        prompt = activeInteractables[bestIndex].Prompt;
        icon = activeInteractables[bestIndex].Icon;
        return activeInteractables[bestIndex].Source;
    }

    private void RefreshActiveInteractionPrompt()
    {
        UnityEngine.Object primary = GetClosestInteractableSource(out string prompt, out Sprite icon);
        if (primary != null)
        {
            if (hud != null) hud.SetPrompt(prompt);
            if (objectiveTracker != null) objectiveTracker.SetInteractionPrompt(prompt, icon);
        }
        else
        {
            if (hud != null) hud.SetPrompt(string.Empty);
            if (objectiveTracker != null) objectiveTracker.SetInteractionPrompt(string.Empty, null);
        }
    }

    public void NotifyPlayerNearDossier()
    {
        if (HasDossier || MissionCompleted || MissionFailed) return;
        if (guideUI != null)
            guideUI.ShowNearDossierDialogue();
    }

    public void CollectDossier()
    {
        if (HasDossier || MissionCompleted || MissionFailed) return;

        HasDossier = true;
        bool exitLockedByLoot = HasUncollectedRequiredLoot(out _);

        SetFeedback("Main dossier collected! Reach the exit.");

        if (exitObjectiveMarker != null)
            exitObjectiveMarker.SetExitMarkerActive(true, exitLockedByLoot);

        RefreshObjective();

        if (guideUI != null)
            guideUI.ShowDossierCollectedDialogue(!exitLockedByLoot);

        OnDossierCollected?.Invoke();
        Debug.Log("[Mission] Dossier collected. Reach the exit.");
    }

    public void NotifyLootCollected(LootCollectible collectible)
    {
        if (collectible == null || MissionCompleted || MissionFailed) return;

        bool allCollected = lootManager != null && lootManager.AreAllLootCollected;
        bool exitLockedByLoot = HasUncollectedRequiredLoot(out _);

        if (HasDossier && exitObjectiveMarker != null)
            exitObjectiveMarker.RefreshLockState(exitLockedByLoot);

        SetFeedback($"Collected: {collectible.DisplayName}" + (collectible.MonetaryValue > 0 ? $" (+${collectible.MonetaryValue})" : ""));
        RefreshObjective();

        if (guideUI != null)
            guideUI.ShowLootCollectedDialogue(collectible, allCollected, HasDossier);

        OnLootCollected?.Invoke(collectible);
    }

    public void NotifyAllLootCollected()
    {
        if (MissionCompleted || MissionFailed) return;
        RefreshObjective();

        if (guideUI != null)
            guideUI.ShowAllLootCollectedDialogue(HasDossier);

        OnAllLootCollected?.Invoke();
    }

    public bool HasUncollectedRequiredLoot(out List<LootCollectible> remainingRequired)
    {
        if (lootManager == null)
        {
            remainingRequired = new List<LootCollectible>();
            return false;
        }

        remainingRequired = lootManager.GetRemainingRequiredLoot(RequireAllLootBeforeExit);
        return remainingRequired.Count > 0;
    }

    public void PlayerReachedExit()
    {
        if (MissionCompleted || MissionFailed) return;

        if (!HasDossier)
        {
            string msg = "You must steal the main dossier before leaving!";
            SetFeedback(msg);
            if (guideUI != null)
                guideUI.ShowExitBlockedDialogue(msg);
            return;
        }

        if (HasUncollectedRequiredLoot(out List<LootCollectible> missingLoot))
        {
            string itemNames = BuildRemainingLootNames(missingLoot);
            string msg = RequireAllLootBeforeExit
                ? $"Exit locked! Collect all remaining loot first ({missingLoot.Count} left: {itemNames})."
                : $"Exit locked! Required loot remaining: {itemNames}.";

            SetFeedback(msg);
            if (exitObjectiveMarker != null)
                exitObjectiveMarker.RefreshLockState(true);
            if (guideUI != null)
                guideUI.ShowExitBlockedDialogue(msg);
            return;
        }

        MissionCompleted = true;
        activeInteractables.Clear();
        ClearPrompt();

        if (exitObjectiveMarker != null)
            exitObjectiveMarker.SetExitMarkerActive(false, false);

        RefreshObjective();

        if (guideUI != null)
            guideUI.ShowMissionCompletedDialogue();

        string victorySubtitle = "You secured the main dossier and escaped the office undetected.";
        if (hud != null)
            hud.ShowEndScreen(true, "MISSION COMPLETED", victorySubtitle);

        if (resultsUI != null)
            resultsUI.ShowResults(true, "MISSION COMPLETED", victorySubtitle, HasDossier, lootManager);

        OnMissionCompleted?.Invoke();
        Debug.Log("[Mission] SUCCESS — dossier secured and exit reached.");
    }

    public void FailMission(string reason = "You were spotted by a security guard.")
    {
        if (MissionCompleted || MissionFailed) return;

        MissionFailed = true;
        activeInteractables.Clear();
        ClearPrompt();

        if (exitObjectiveMarker != null)
            exitObjectiveMarker.SetExitMarkerActive(false, false);

        RefreshObjective();

        if (guideUI != null)
            guideUI.ShowMissionFailedDialogue(reason);

        if (hud != null)
            hud.ShowEndScreen(false, "MISSION FAILED", reason);

        if (resultsUI != null)
            resultsUI.ShowResults(false, "MISSION FAILED", reason, HasDossier, lootManager);

        OnMissionFailed?.Invoke(reason);
        Debug.LogWarning("[Mission] FAILED — " + reason);
    }

    public void SetPrompt(string message)
    {
        if (hud != null) hud.SetPrompt(message);
        if (objectiveTracker != null) objectiveTracker.SetInteractionPrompt(message, null);
    }

    public void ClearPrompt()
    {
        if (hud != null) hud.SetPrompt(string.Empty);
        if (objectiveTracker != null) objectiveTracker.SetInteractionPrompt(string.Empty, null);
    }

    public void SetFeedback(string message)
    {
        if (hud != null) hud.ShowFeedback(message, 2.5f);
        if (objectiveTracker != null) objectiveTracker.ShowFeedback(message, 2.5f);
    }

    public void RefreshObjective()
    {
        if (hud != null)
        {
            if (MissionCompleted)
                hud.SetObjective("Mission completed");
            else if (MissionFailed)
                hud.SetObjective("Mission failed");
            else if (HasDossier)
                hud.SetObjective("Objective: Reach the exit");
            else
                hud.SetObjective("Objective: Steal the main dossier");
        }

        if (objectiveTracker != null)
        {
            bool exitLocked = HasUncollectedRequiredLoot(out _);
            objectiveTracker.RefreshTracker(HasDossier, MissionCompleted, exitLocked, lootManager);
        }
    }

    public WorldObjectiveMarker GetActivePrimaryWorldMarker()
    {
        if (!HasDossier && filePickup != null && filePickup.WorldMarker != null)
            return filePickup.WorldMarker;

        if (HasDossier && exitObjectiveMarker != null && exitObjectiveMarker.WorldMarker != null)
            return exitObjectiveMarker.WorldMarker;

        return null;
    }

    private static string BuildRemainingLootNames(List<LootCollectible> items)
    {
        if (items == null || items.Count == 0) return string.Empty;
        int max = Mathf.Min(3, items.Count);
        List<string> names = new List<string>(max);
        for (int i = 0; i < max; i++)
            if (items[i] != null)
                names.Add(items[i].DisplayName);
        string joined = string.Join(", ", names);
        if (items.Count > max)
            joined += $" +{items.Count - max} more";
        return joined;
    }

    public void EnsureRuntimeMissionUIIfNeeded()
    {
        if (guideUI != null && objectiveTracker != null && resultsUI != null)
            return;

        GameObject existingCanvasObj = GameObject.Find("MissionUI");
        if (existingCanvasObj != null)
        {
            if (guideUI == null) guideUI = existingCanvasObj.GetComponentInChildren<MissionGuideUI>(true);
            if (objectiveTracker == null) objectiveTracker = existingCanvasObj.GetComponentInChildren<MissionObjectiveTracker>(true);
            if (resultsUI == null) resultsUI = existingCanvasObj.GetComponentInChildren<MissionResultsUI>(true);
            if (guideUI != null && objectiveTracker != null && resultsUI != null)
                return;
        }

        BuildOrUpdateMissionUICanvas(this);
    }

    public static GameObject BuildOrUpdateMissionUICanvas(MissionManager manager)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObj = GameObject.Find("MissionUI");
        if (canvasObj == null)
            canvasObj = new GameObject("MissionUI");

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            canvasObj.AddComponent<GraphicRaycaster>();

        for (int i = canvasObj.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(canvasObj.transform.GetChild(i).gameObject);

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();

        // 1. OBJECTIVE TRACKER PANEL (Top-Right)
        GameObject trackerObj = CreateUIObject("ObjectiveTrackerPanel", canvasRect);
        RectTransform trackerRect = trackerObj.GetComponent<RectTransform>();
        trackerRect.anchorMin = new Vector2(1f, 1f);
        trackerRect.anchorMax = new Vector2(1f, 1f);
        trackerRect.pivot = new Vector2(1f, 1f);
        trackerRect.anchoredPosition = new Vector2(-24f, -24f);
        trackerRect.sizeDelta = new Vector2(420f, 400f);

        Image trackerBg = trackerObj.AddComponent<Image>();
        trackerBg.color = new Color(0.05f, 0.08f, 0.13f, 0.88f);

        MissionObjectiveTracker tracker = trackerObj.AddComponent<MissionObjectiveTracker>();

        GameObject headerBar = CreateUIObject("HeaderBar", trackerRect);
        RectTransform headerRect = headerBar.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 42f);
        Image headerImg = headerBar.AddComponent<Image>();
        headerImg.color = new Color(0.10f, 0.18f, 0.30f, 0.95f);

        Text headerTxt = CreateUIText("HeaderTitle", headerRect, font, "MISSION OBJECTIVES", 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.65f, 0.85f, 1f, 1f));
        headerTxt.rectTransform.offsetMin = new Vector2(14f, 0f);

        GameObject dossierRow = CreateUIObject("DossierObjectiveRow", trackerRect);
        RectTransform dossierRowRect = dossierRow.GetComponent<RectTransform>();
        dossierRowRect.anchorMin = new Vector2(0f, 1f);
        dossierRowRect.anchorMax = new Vector2(1f, 1f);
        dossierRowRect.pivot = new Vector2(0.5f, 1f);
        dossierRowRect.anchoredPosition = new Vector2(0f, -54f);
        dossierRowRect.sizeDelta = new Vector2(-24f, 42f);

        Image dossierIconImg = CreateUIIcon("DossierIcon", dossierRowRect, new Vector2(20f, 0f), new Vector2(38f, 38f), MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Dossier));
        Text dossierObjTxt = CreateUIText("DossierText", dossierRowRect, font, "Steal the main dossier", 19, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        dossierObjTxt.rectTransform.offsetMin = new Vector2(48f, 0f);

        GameObject exitRow = CreateUIObject("ExitObjectiveRow", trackerRect);
        RectTransform exitRowRect = exitRow.GetComponent<RectTransform>();
        exitRowRect.anchorMin = new Vector2(0f, 1f);
        exitRowRect.anchorMax = new Vector2(1f, 1f);
        exitRowRect.pivot = new Vector2(0.5f, 1f);
        exitRowRect.anchoredPosition = new Vector2(0f, -100f);
        exitRowRect.sizeDelta = new Vector2(-24f, 42f);

        Image exitIconImg = CreateUIIcon("ExitIcon", exitRowRect, new Vector2(20f, 0f), new Vector2(38f, 38f), MissionIconFactory.GetIcon(MissionIconFactory.IconKind.ExitDoor));
        Text exitObjTxt = CreateUIText("ExitText", exitRowRect, font, "Reach the exit", 19, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        exitObjTxt.rectTransform.offsetMin = new Vector2(48f, 0f);

        GameObject lootCard = CreateUIObject("LootTrackerCard", trackerRect);
        RectTransform lootCardRect = lootCard.GetComponent<RectTransform>();
        lootCardRect.anchorMin = new Vector2(0f, 1f);
        lootCardRect.anchorMax = new Vector2(1f, 1f);
        lootCardRect.pivot = new Vector2(0.5f, 1f);
        lootCardRect.anchoredPosition = new Vector2(0f, -150f);
        lootCardRect.sizeDelta = new Vector2(-24f, 78f);
        Image lootCardBg = lootCard.AddComponent<Image>();
        lootCardBg.color = new Color(0.08f, 0.12f, 0.19f, 0.95f);

        GameObject ringBgObj = CreateUIObject("RadialRingBg", lootCardRect);
        RectTransform ringBgRect = ringBgObj.GetComponent<RectTransform>();
        ringBgRect.anchorMin = new Vector2(0f, 0.5f);
        ringBgRect.anchorMax = new Vector2(0f, 0.5f);
        ringBgRect.anchoredPosition = new Vector2(40f, 0f);
        ringBgRect.sizeDelta = new Vector2(58f, 58f);
        Image ringBgImg = ringBgObj.AddComponent<Image>();
        ringBgImg.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.RingOutline);
        ringBgImg.color = new Color(0.2f, 0.25f, 0.35f, 0.8f);

        GameObject ringFillObj = CreateUIObject("RadialRingFill", ringBgRect);
        RectTransform ringFillRect = ringFillObj.GetComponent<RectTransform>();
        ringFillRect.anchorMin = Vector2.zero;
        ringFillRect.anchorMax = Vector2.one;
        ringFillRect.sizeDelta = Vector2.zero;
        Image radialFillImg = ringFillObj.AddComponent<Image>();
        radialFillImg.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.RingOutline);
        radialFillImg.type = Image.Type.Filled;
        radialFillImg.fillMethod = Image.FillMethod.Radial360;
        radialFillImg.fillOrigin = (int)Image.Origin360.Top;
        radialFillImg.fillClockwise = true;
        radialFillImg.fillAmount = 0f;
        radialFillImg.color = new Color(1f, 0.80f, 0.20f, 1f);

        Image lootIconImg = CreateUIIcon("LootCenterIcon", ringBgRect, Vector2.zero, new Vector2(34f, 34f), MissionIconFactory.GetIcon(MissionIconFactory.IconKind.LootBag));
        lootIconImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        lootIconImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        Text lootStatusLbl = CreateUIText("LootStatusLabel", lootCardRect, font, "LOOT COLLECTED", 15, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.8f, 0.86f, 0.95f, 1f));
        lootStatusLbl.rectTransform.offsetMin = new Vector2(82f, 10f);
        lootStatusLbl.rectTransform.offsetMax = new Vector2(-100f, -10f);

        Text lootCountTxt = CreateUIText("LootCountText", lootCardRect, font, "0 / 0", 22, FontStyle.Bold, TextAnchor.UpperRight, new Color(1f, 0.82f, 0.22f, 1f));
        lootCountTxt.rectTransform.offsetMin = new Vector2(82f, 6f);
        lootCountTxt.rectTransform.offsetMax = new Vector2(-14f, -6f);

        GameObject barBgObj = CreateUIObject("LootBarBg", lootCardRect);
        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 0f);
        barBgRect.anchorMax = new Vector2(1f, 0f);
        barBgRect.pivot = new Vector2(0.5f, 0f);
        barBgRect.anchoredPosition = new Vector2(34f, 14f);
        barBgRect.sizeDelta = new Vector2(-96f, 12f);
        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = new Color(0.03f, 0.05f, 0.08f, 0.95f);

        GameObject barFillObj = CreateUIObject("LootBarFill", barBgRect);
        RectTransform barFillRect = barFillObj.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.sizeDelta = Vector2.zero;
        Image barFillImg = barFillObj.AddComponent<Image>();
        barFillImg.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.CircleFill);
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillAmount = 0f;
        barFillImg.color = new Color(1f, 0.80f, 0.20f, 1f);

        GameObject lootListObj = CreateUIObject("IndividualLootList", trackerRect);
        RectTransform lootListRect = lootListObj.GetComponent<RectTransform>();
        lootListRect.anchorMin = new Vector2(0f, 0f);
        lootListRect.anchorMax = new Vector2(1f, 1f);
        lootListRect.offsetMin = new Vector2(16f, 12f);
        lootListRect.offsetMax = new Vector2(-16f, -244f);
        VerticalLayoutGroup vlg = lootListObj.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 5f;

        // 2. MISSION GUIDE CHARACTER UI (Top-Left)
        GameObject guideObj = CreateUIObject("MissionGuidePanel", canvasRect);
        RectTransform guideRect = guideObj.GetComponent<RectTransform>();
        guideRect.anchorMin = new Vector2(0f, 0f);
        guideRect.anchorMax = new Vector2(0f, 0f);
        guideRect.pivot = new Vector2(0f, 0f);
        guideRect.anchoredPosition = new Vector2(24f, 24f);
        guideRect.sizeDelta = new Vector2(1320f, 240f);

        CanvasGroup guideCg = guideObj.AddComponent<CanvasGroup>();
        MissionGuideUI guide = guideObj.AddComponent<MissionGuideUI>();

        GameObject portraitFrame = CreateUIObject("PortraitFrame", guideRect);
        RectTransform pfRect = portraitFrame.GetComponent<RectTransform>();
        pfRect.anchorMin = new Vector2(0f, 0.5f);
        pfRect.anchorMax = new Vector2(0f, 0.5f);
        pfRect.pivot = new Vector2(0f, 0.5f);
        pfRect.anchoredPosition = Vector2.zero;
        pfRect.sizeDelta = new Vector2(190f, 190f);
        Image pfBg = portraitFrame.AddComponent<Image>();
        pfBg.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.CircleFill);
        pfBg.color = new Color(1f, 0.65f, 0.12f, 1f);

        Image portraitImg = CreateUIIcon("PortraitImage", pfRect, Vector2.zero, new Vector2(174f, 174f), MissionIconFactory.GetIcon(MissionIconFactory.IconKind.ThiefPortrait));
        portraitImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        portraitImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        GameObject bubbleObj = CreateUIObject("SpeechBubble", guideRect);
        RectTransform bubbleRect = bubbleObj.GetComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0f, 0f);
        bubbleRect.anchorMax = new Vector2(1f, 1f);
        bubbleRect.offsetMin = new Vector2(208f, 5f);
        bubbleRect.offsetMax = new Vector2(0f, -5f);
        Image bubbleBg = bubbleObj.AddComponent<Image>();
        bubbleBg.color = new Color(0.97f, 0.88f, 0.68f, 0.98f);

        Outline bubbleOutline = bubbleObj.AddComponent<Outline>();
        bubbleOutline.effectColor = new Color(0.12f, 0.09f, 0.055f, 1f);
        bubbleOutline.effectDistance = new Vector2(3f, -3f);

        GameObject accentBar = CreateUIObject("BubbleAccent", bubbleRect);
        RectTransform accentRect = accentBar.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(5f, 0f);
        Image accentImg = accentBar.AddComponent<Image>();
        accentImg.color = new Color(0.96f, 0.48f, 0.08f, 1f);

        Text speakerTxt = CreateUIText("SpeakerName", bubbleRect, font, "FIELD AGENT // SHADOW", 23, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.30f, 0.17f, 0.075f, 1f));
        speakerTxt.rectTransform.offsetMin = new Vector2(26f, 12f);
        speakerTxt.rectTransform.offsetMax = new Vector2(-90f, -12f);
        speakerTxt.resizeTextForBestFit = true;
        speakerTxt.resizeTextMinSize = 19;
        speakerTxt.resizeTextMaxSize = 23;

        Text guideMsgTxt = CreateUIText("DialogueMessage", bubbleRect, font, "Your mission: steal the main dossier, collect the valuables and escape without getting caught.", 28, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.18f, 0.12f, 0.07f, 1f));
        guideMsgTxt.rectTransform.offsetMin = new Vector2(26f, 16f);
        guideMsgTxt.rectTransform.offsetMax = new Vector2(-96f, -62f);
        guideMsgTxt.resizeTextForBestFit = true;
        guideMsgTxt.resizeTextMinSize = 20;
        guideMsgTxt.resizeTextMaxSize = 28;
        guideMsgTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        guideMsgTxt.verticalOverflow = VerticalWrapMode.Overflow;

        Image contextBadgeImg = CreateUIIcon("ContextObjectiveIcon", bubbleRect, new Vector2(-46f, 0f), new Vector2(64f, 64f), MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Dossier));
        contextBadgeImg.rectTransform.anchorMin = new Vector2(1f, 0.5f);
        contextBadgeImg.rectTransform.anchorMax = new Vector2(1f, 0.5f);

        guide.BindUIElements(guideRect, guideCg, portraitImg, speakerTxt, guideMsgTxt, contextBadgeImg);

        // 3. INTERACTION PROMPT & FEEDBACK BANNERS
        GameObject promptRoot = CreateUIObject("InteractionPromptBanner", canvasRect);
        RectTransform promptRect = promptRoot.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 292f);
        promptRect.sizeDelta = new Vector2(620f, 72f);
        Image promptBg = promptRoot.AddComponent<Image>();
        promptBg.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.CircleFill);
        promptBg.color = new Color(0.12f, 0.085f, 0.045f, 0.96f);
        Outline promptOutline = promptRoot.AddComponent<Outline>();
        promptOutline.effectColor = new Color(1f, 0.63f, 0.14f, 1f);
        promptOutline.effectDistance = new Vector2(3f, -3f);

        Image promptIconImg = CreateUIIcon("PromptIcon", promptRect, new Vector2(38f, 0f), new Vector2(48f, 48f), MissionIconFactory.GetIcon(MissionIconFactory.IconKind.LootBag));
        Text promptTxt = CreateUIText("PromptText", promptRect, font, "PRESS E TO INTERACT", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        promptTxt.rectTransform.offsetMin = new Vector2(82f, 6f);
        promptTxt.rectTransform.offsetMax = new Vector2(-16f, -4f);
        promptRoot.SetActive(false);

        GameObject feedbackRoot = CreateUIObject("FeedbackBanner", canvasRect);
        RectTransform feedbackRect = feedbackRoot.GetComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0.5f, 1f);
        feedbackRect.anchorMax = new Vector2(0.5f, 1f);
        feedbackRect.pivot = new Vector2(0.5f, 1f);
        feedbackRect.anchoredPosition = new Vector2(0f, -32f);
        feedbackRect.sizeDelta = new Vector2(640f, 60f);
        Image feedbackBg = feedbackRoot.AddComponent<Image>();
        feedbackBg.color = new Color(0.06f, 0.14f, 0.24f, 0.92f);
        Text feedbackTxt = CreateUIText("FeedbackText", feedbackRect, font, "", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.95f, 0.98f, 1f, 1f));
        feedbackRoot.SetActive(false);

        // 4. SCREEN-EDGE DIRECTIONAL INDICATOR
        GameObject edgeObj = CreateUIObject("ScreenEdgeIndicator", canvasRect);
        RectTransform edgeRect = edgeObj.GetComponent<RectTransform>();
        edgeRect.anchorMin = new Vector2(0.5f, 0.5f);
        edgeRect.anchorMax = new Vector2(0.5f, 0.5f);
        edgeRect.sizeDelta = new Vector2(68f, 68f);
        Image edgeArrowImg = edgeObj.AddComponent<Image>();
        edgeArrowImg.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.ArrowPointer);
        edgeArrowImg.raycastTarget = false;

        Image edgeIconImg = CreateUIIcon("TargetIcon", edgeRect, Vector2.zero, new Vector2(36f, 36f), MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Dossier));
        edgeIconImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        edgeIconImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        edgeObj.SetActive(false);

        tracker.BindUIReferences(
            dossierIconImg,
            dossierObjTxt,
            exitRow,
            exitIconImg,
            exitObjTxt,
            lootIconImg,
            lootCountTxt,
            lootStatusLbl,
            radialFillImg,
            barFillImg,
            lootListRect,
            promptRoot,
            promptIconImg,
            promptTxt,
            feedbackRoot,
            feedbackTxt,
            edgeRect,
            edgeArrowImg,
            edgeIconImg);

        // 5. CARTOON-STYLE MISSION RESULTS MEDALLION
        GameObject resultsHolder = CreateUIObject("MissionResultsController", canvasRect);
        RectTransform rhRect = resultsHolder.GetComponent<RectTransform>();
        rhRect.anchorMin = Vector2.zero;
        rhRect.anchorMax = Vector2.one;
        rhRect.sizeDelta = Vector2.zero;
        MissionResultsUI results = resultsHolder.AddComponent<MissionResultsUI>();

        GameObject overlayObj = CreateUIObject("ResultsOverlay", rhRect);
        RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        Image overlayBg = overlayObj.AddComponent<Image>();
        overlayBg.color = new Color(0.035f, 0.045f, 0.055f, 0.76f);

        GameObject outerObj = CreateUIObject("ResultsMedallionOuter", overlayRect);
        RectTransform outerRect = outerObj.GetComponent<RectTransform>();
        outerRect.anchorMin = new Vector2(0.5f, 0.5f);
        outerRect.anchorMax = new Vector2(0.5f, 0.5f);
        outerRect.sizeDelta = new Vector2(780f, 780f);
        Image outerImage = outerObj.AddComponent<Image>();
        outerImage.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.CircleFill);
        outerImage.color = new Color(1f, 0.49f, 0.035f, 1f);
        Outline outerOutline = outerObj.AddComponent<Outline>();
        outerOutline.effectColor = new Color(0.08f, 0.055f, 0.035f, 1f);
        outerOutline.effectDistance = new Vector2(7f, -7f);

        GameObject cardObj = CreateUIObject("ResultsCard", outerRect);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(720f, 720f);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.CircleFill);
        cardBg.color = new Color(0.98f, 0.87f, 0.66f, 1f);
        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.20f, 0.13f, 0.075f, 1f);
        cardOutline.effectDistance = new Vector2(4f, -4f);

        Text resTitle = CreateUIText("ResultsTitle", cardRect, font, "BRAVO!", 48, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.12f, 0.085f, 0.045f, 1f));
        SetCenteredRect(resTitle.rectTransform, new Vector2(0f, 310f), new Vector2(620f, 72f));

        Text resSub = CreateUIText("ResultsSubtitle", cardRect, font, "YOU MADE IT OUT!", 25, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.28f, 0.18f, 0.08f, 1f));
        SetCenteredRect(resSub.rectTransform, new Vector2(0f, 258f), new Vector2(600f, 48f));

        GameObject portraitFrameObj = CreateUIObject("ResultPortraitFrame", cardRect);
        RectTransform portraitFrameRect = portraitFrameObj.GetComponent<RectTransform>();
        portraitFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
        portraitFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
        portraitFrameRect.anchoredPosition = new Vector2(0f, 160f);
        portraitFrameRect.sizeDelta = new Vector2(152f, 152f);
        Image portraitFrame = portraitFrameObj.AddComponent<Image>();
        portraitFrame.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.CircleFill);
        portraitFrame.color = new Color(1f, 0.63f, 0.10f, 1f);
        Outline portraitOutline = portraitFrameObj.AddComponent<Outline>();
        portraitOutline.effectColor = new Color(0.20f, 0.13f, 0.075f, 1f);
        portraitOutline.effectDistance = new Vector2(3f, -3f);

        Image resultPortraitImg = CreateUIIcon("ResultCharacterPortrait", portraitFrameRect, Vector2.zero,
            new Vector2(136f, 136f), MissionIconFactory.GetIcon(MissionIconFactory.IconKind.ThiefPortrait));
        resultPortraitImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        resultPortraitImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        resultPortraitImg.raycastTarget = false;

        Image[] stars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject starObj = CreateUIObject("Star_" + (i + 1), cardRect);
            RectTransform starRect = starObj.GetComponent<RectTransform>();
            starRect.anchorMin = new Vector2(0.5f, 0.5f);
            starRect.anchorMax = new Vector2(0.5f, 0.5f);
            starRect.anchoredPosition = new Vector2((i - 1) * 76f, 54f);
            starRect.sizeDelta = new Vector2(66f, 66f);
            Image starImg = starObj.AddComponent<Image>();
            starImg.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.StarFilled);
            starImg.preserveAspect = true;
            stars[i] = starImg;
        }

        Text scoreLabel = CreateUIText("ScoreLabel", cardRect, font, "LOOT VALUE", 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.30f, 0.20f, 0.08f, 1f));
        SetCenteredRect(scoreLabel.rectTransform, new Vector2(0f, -20f), new Vector2(560f, 34f));

        Text scoreHeadline = CreateUIText("ResultsScoreHeadline", cardRect, font, "$0", 62, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.12f, 0.085f, 0.045f, 1f));
        SetCenteredRect(scoreHeadline.rectTransform, new Vector2(0f, -72f), new Vector2(590f, 78f));

        Text dossierVal = CreateResultStatRow(cardRect, font, "MAIN DOSSIER", "COMPLETED", -142f);
        Text lootCntVal = CreateResultStatRow(cardRect, font, "LOOT COLLECTED", "0 / 0", -190f);
        Text reqLootVal = CreateResultStatRow(cardRect, font, "REQUIRED LOOT", "COMPLETED", -238f);
        Text pctVal = CreateResultStatRow(cardRect, font, "COMPLETION", "100%", -286f);

        GameObject btnObj = CreateUIObject("ReplayButton", cardRect);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0f, -350f);
        btnRect.sizeDelta = new Vector2(300f, 58f);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.91f, 0.41f, 0.035f, 1f);
        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.14f, 0.09f, 0.035f, 1f);
        btnOutline.effectDistance = new Vector2(3f, -3f);
        Button replayBtn = btnObj.AddComponent<Button>();
        replayBtn.targetGraphic = btnImg;

        CreateUIText("ReplayBtnText", btnRect, font, "RETRY MISSION  (R)", 23, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        overlayObj.SetActive(false);

        results.BindUIReferences(
            overlayObj,
            resTitle,
            resSub,
            dossierVal,
            lootCntVal,
            reqLootVal,
            scoreHeadline,
            pctVal,
            stars,
            replayBtn);
        results.BindResultArtwork(resultPortraitImg, scoreHeadline);

        return canvasObj;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static Image CreateUIIcon(string name, RectTransform parent, Vector2 anchoredPos, Vector2 size, Sprite sprite)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static Text CreateUIText(
        string name,
        RectTransform parent,
        Font font,
        string content,
        int size,
        FontStyle style,
        TextAnchor anchor,
        Color color)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Text txt = go.AddComponent<Text>();
        txt.font = font;
        txt.text = content;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.alignment = anchor;
        txt.color = color;
        txt.raycastTarget = false;
        return txt;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static Text CreateResultStatRow(RectTransform cardRect, Font font, string label, string defaultVal, float yOffset)
    {
        GameObject rowObj = CreateUIObject("StatRow_" + label, cardRect);
        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, yOffset);
        rowRect.sizeDelta = new Vector2(560f, 42f);

        Image rowBg = rowObj.AddComponent<Image>();
        rowBg.color = new Color(0.26f, 0.19f, 0.10f, 0.14f);
        Outline rowOutline = rowObj.AddComponent<Outline>();
        rowOutline.effectColor = new Color(0.40f, 0.26f, 0.12f, 0.28f);
        rowOutline.effectDistance = new Vector2(1f, -1f);

        Text lblTxt = CreateUIText("Label", rowRect, font, label, 19, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.26f, 0.18f, 0.09f, 1f));
        lblTxt.rectTransform.offsetMin = new Vector2(18f, 0f);

        Text valTxt = CreateUIText("Value", rowRect, font, defaultVal, 19, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.14f, 0.10f, 0.05f, 1f));
        valTxt.rectTransform.offsetMax = new Vector2(-18f, 0f);

        return valTxt;
    }
}
