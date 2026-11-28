using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the HUD Objective Tracker (Main Dossier, Dynamic Exit Objective, Loot Progress Ring & Counter,
/// Individual Loot Checklist, Temporary Feedback Banner, and Screen-Edge Objective Indicator).
/// </summary>
public class MissionObjectiveTracker : MonoBehaviour
{
    [Header("Main Objective Configuration")]
    [SerializeField] private Sprite dossierIcon;
    [SerializeField] private Sprite exitIcon;
    [SerializeField] private Sprite completedIcon;
    [SerializeField] private Sprite lockedIcon;
    [SerializeField] private Sprite lootIcon;
    [SerializeField] private bool hideExitObjectiveUntilDossierCollected = true;

    [Header("Main & Exit UI References")]
    [SerializeField] private Image dossierStatusIcon;
    [SerializeField] private Text dossierObjectiveText;
    [SerializeField] private GameObject exitObjectiveRow;
    [SerializeField] private Image exitStatusIcon;
    [SerializeField] private Text exitObjectiveText;

    [Header("Loot Tracker UI References")]
    [SerializeField] private Image lootHeaderIcon;
    [SerializeField] private Text lootCountText;
    [SerializeField] private Text lootStatusLabel;
    [SerializeField] private Image lootRadialFill;
    [SerializeField] private Image lootBarFill;
    [SerializeField] private RectTransform individualLootListContainer;

    [Header("Interaction Prompt & Feedback UI")]
    [SerializeField] private GameObject interactionPromptRoot;
    [SerializeField] private Image interactionPromptIcon;
    [SerializeField] private Text interactionPromptText;
    [SerializeField] private GameObject feedbackBannerRoot;
    [SerializeField] private Text feedbackBannerText;

    [Header("Screen-Edge Indicator")]
    [SerializeField] private RectTransform screenEdgeIndicatorRect;
    [SerializeField] private Image screenEdgeArrowImage;
    [SerializeField] private Image screenEdgeTargetIcon;

    private readonly List<LootRowWidgets> spawnedLootRows = new List<LootRowWidgets>();
    private float displayedLootFill;
    private float targetLootFill;
    private float feedbackHideTime;

    private struct LootRowWidgets
    {
        public LootCollectible Collectible;
        public GameObject RowObject;
        public Image IconImage;
        public Text LabelText;
        public Text ValueText;
    }

    private static readonly Color IncompleteColor = new Color(0.92f, 0.95f, 1f, 1f);
    private static readonly Color CompletedColor = new Color(0.24f, 0.92f, 0.48f, 1f);
    private static readonly Color LockedColor = new Color(1f, 0.65f, 0.22f, 1f);
    private static readonly Color GoldLootColor = new Color(1f, 0.80f, 0.20f, 1f);

    public void BindUIReferences(
        Image dossierImg,
        Text dossierTxt,
        GameObject exitRow,
        Image exitImg,
        Text exitTxt,
        Image lootImg,
        Text lootCntTxt,
        Text lootStatLbl,
        Image radialFill,
        Image barFill,
        RectTransform lootListParent,
        GameObject promptRoot,
        Image promptImg,
        Text promptTxt,
        GameObject feedbackRoot,
        Text feedbackTxt,
        RectTransform edgeRect,
        Image edgeArrow,
        Image edgeIcon)
    {
        dossierStatusIcon = dossierImg;
        dossierObjectiveText = dossierTxt;
        exitObjectiveRow = exitRow;
        exitStatusIcon = exitImg;
        exitObjectiveText = exitTxt;
        lootHeaderIcon = lootImg;
        lootCountText = lootCntTxt;
        lootStatusLabel = lootStatLbl;
        lootRadialFill = radialFill;
        lootBarFill = barFill;
        individualLootListContainer = lootListParent;
        interactionPromptRoot = promptRoot;
        interactionPromptIcon = promptImg;
        interactionPromptText = promptTxt;
        feedbackBannerRoot = feedbackRoot;
        feedbackBannerText = feedbackTxt;
        screenEdgeIndicatorRect = edgeRect;
        screenEdgeArrowImage = edgeArrow;
        screenEdgeTargetIcon = edgeIcon;
    }

    private void Update()
    {
        displayedLootFill = Mathf.MoveTowards(displayedLootFill, targetLootFill, Time.unscaledDeltaTime * 2.2f);
        if (lootRadialFill != null) lootRadialFill.fillAmount = displayedLootFill;
        if (lootBarFill != null) lootBarFill.fillAmount = displayedLootFill;

        if (feedbackBannerRoot != null && feedbackBannerRoot.activeSelf && Time.unscaledTime > feedbackHideTime)
            feedbackBannerRoot.SetActive(false);

        UpdateScreenEdgeIndicator();
    }

    public void RefreshTracker(
        bool hasDossier,
        bool missionCompleted,
        bool exitLockedByLoot,
        LootManager lootManager)
    {
        Sprite checkSpr = completedIcon != null ? completedIcon : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Checkmark);
        Sprite docSpr = dossierIcon != null ? dossierIcon : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Dossier);
        Sprite doorSpr = exitIcon != null ? exitIcon : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.ExitDoor);
        Sprite lockSpr = lockedIcon != null ? lockedIcon : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Lock);
        Sprite bagSpr = lootIcon != null ? lootIcon : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.LootBag);

        if (dossierStatusIcon != null)
        {
            dossierStatusIcon.sprite = hasDossier ? checkSpr : docSpr;
            dossierStatusIcon.color = Color.white;
        }

        if (dossierObjectiveText != null)
        {
            dossierObjectiveText.text = hasDossier ? "Main dossier — COMPLETED" : "Steal the main dossier";
            dossierObjectiveText.color = hasDossier ? CompletedColor : IncompleteColor;
        }

        if (exitObjectiveRow != null)
        {
            bool showExitRow = hasDossier || !hideExitObjectiveUntilDossierCollected;
            exitObjectiveRow.SetActive(showExitRow);

            if (showExitRow)
            {
                if (missionCompleted)
                {
                    if (exitStatusIcon != null) exitStatusIcon.sprite = checkSpr;
                    if (exitObjectiveText != null)
                    {
                        exitObjectiveText.text = "Reach the exit — COMPLETED";
                        exitObjectiveText.color = CompletedColor;
                    }
                }
                else if (!hasDossier)
                {
                    if (exitStatusIcon != null) exitStatusIcon.sprite = lockSpr;
                    if (exitObjectiveText != null)
                    {
                        exitObjectiveText.text = "Reach the exit (Requires Dossier)";
                        exitObjectiveText.color = new Color(0.6f, 0.65f, 0.72f, 0.75f);
                    }
                }
                else if (exitLockedByLoot)
                {
                    if (exitStatusIcon != null) exitStatusIcon.sprite = lockSpr;
                    if (exitObjectiveText != null)
                    {
                        exitObjectiveText.text = "Reach the exit (Collect required loot)";
                        exitObjectiveText.color = LockedColor;
                    }
                }
                else
                {
                    if (exitStatusIcon != null) exitStatusIcon.sprite = doorSpr;
                    if (exitObjectiveText != null)
                    {
                        exitObjectiveText.text = "Reach the exit";
                        exitObjectiveText.color = IncompleteColor;
                    }
                }
            }
        }

        int collected = lootManager != null ? lootManager.CollectedLootCount : 0;
        int total = lootManager != null ? lootManager.TotalLootCount : 0;
        bool allLootDone = total > 0 && collected >= total;

        targetLootFill = total > 0 ? Mathf.Clamp01((float)collected / total) : 0f;

        if (lootHeaderIcon != null)
            lootHeaderIcon.sprite = allLootDone ? checkSpr : bagSpr;

        if (lootCountText != null)
        {
            lootCountText.text = $"{collected} / {total}";
            lootCountText.color = allLootDone ? CompletedColor : GoldLootColor;
        }

        if (lootStatusLabel != null)
        {
            if (total == 0)
            {
                lootStatusLabel.text = "NO OPTIONAL LOOT";
                lootStatusLabel.color = IncompleteColor;
            }
            else if (allLootDone)
            {
                lootStatusLabel.text = "ALL LOOT COLLECTED";
                lootStatusLabel.color = CompletedColor;
            }
            else
            {
                int score = lootManager != null ? lootManager.CollectedLootValue : 0;
                lootStatusLabel.text = score > 0 ? $"LOOT • ${score}" : "LOOT COLLECTED";
                lootStatusLabel.color = IncompleteColor;
            }
        }

        if (lootRadialFill != null)
            lootRadialFill.color = allLootDone ? CompletedColor : GoldLootColor;
        if (lootBarFill != null)
            lootBarFill.color = allLootDone ? CompletedColor : GoldLootColor;

        RefreshIndividualLootRows(lootManager, checkSpr);
    }

    private void RefreshIndividualLootRows(LootManager lootManager, Sprite checkSpr)
    {
        if (individualLootListContainer == null || lootManager == null) return;

        IReadOnlyList<LootCollectible> allLoot = lootManager.RegisteredLoot;

        if (spawnedLootRows.Count != allLoot.Count)
        {
            for (int i = 0; i < spawnedLootRows.Count; i++)
                if (spawnedLootRows[i].RowObject != null)
                    Destroy(spawnedLootRows[i].RowObject);
            spawnedLootRows.Clear();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < allLoot.Count; i++)
            {
                LootCollectible item = allLoot[i];
                if (item == null) continue;

                GameObject row = new GameObject($"LootRow_{item.DisplayName}");
                row.transform.SetParent(individualLootListContainer, false);
                RectTransform rowRect = row.AddComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(360f, 30f);

                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(rowRect, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(14f, 0f);
                iconRect.sizeDelta = new Vector2(24f, 24f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.preserveAspect = true;

                GameObject txtObj = new GameObject("Label");
                txtObj.transform.SetParent(rowRect, false);
                RectTransform txtRect = txtObj.AddComponent<RectTransform>();
                txtRect.anchorMin = new Vector2(0f, 0f);
                txtRect.anchorMax = new Vector2(1f, 1f);
                txtRect.offsetMin = new Vector2(36f, 0f);
                txtRect.offsetMax = new Vector2(-58f, 0f);
                Text lbl = txtObj.AddComponent<Text>();
                lbl.font = font;
                lbl.fontSize = 16;
                lbl.alignment = TextAnchor.MiddleLeft;
                lbl.fontStyle = FontStyle.Bold;
                lbl.color = IncompleteColor;
                lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
                lbl.verticalOverflow = VerticalWrapMode.Truncate;

                GameObject valObj = new GameObject("Value");
                valObj.transform.SetParent(rowRect, false);
                RectTransform valRect = valObj.AddComponent<RectTransform>();
                valRect.anchorMin = new Vector2(1f, 0f);
                valRect.anchorMax = new Vector2(1f, 1f);
                valRect.offsetMin = new Vector2(-72f, 0f);
                valRect.offsetMax = new Vector2(-6f, 0f);
                Text valTxt = valObj.AddComponent<Text>();
                valTxt.font = font;
                valTxt.fontSize = 14;
                valTxt.fontStyle = FontStyle.Bold;
                valTxt.alignment = TextAnchor.MiddleRight;

                spawnedLootRows.Add(new LootRowWidgets
                {
                    Collectible = item,
                    RowObject = row,
                    IconImage = iconImg,
                    LabelText = lbl,
                    ValueText = valTxt
                });
            }
        }

        for (int i = 0; i < spawnedLootRows.Count; i++)
        {
            LootRowWidgets w = spawnedLootRows[i];
            if (w.Collectible == null) continue;

            bool collected = w.Collectible.IsCollected;
            if (w.IconImage != null)
                w.IconImage.sprite = collected ? checkSpr : w.Collectible.Icon;

            if (w.LabelText != null)
            {
                string reqPrefix = w.Collectible.RequiredForExit && !collected ? "[REQ] " : string.Empty;
                w.LabelText.text = collected
                    ? $"{w.Collectible.DisplayName} — COLLECTED"
                    : $"{reqPrefix}{w.Collectible.ObjectiveText}";
                w.LabelText.color = collected ? CompletedColor : new Color(0.86f, 0.90f, 0.96f, 0.95f);
            }

            if (w.ValueText != null)
            {
                w.ValueText.text = w.Collectible.MonetaryValue > 0 ? $"${w.Collectible.MonetaryValue}" : "";
                w.ValueText.color = collected ? CompletedColor : GoldLootColor;
            }
        }
    }

    public void SetInteractionPrompt(string message, Sprite icon = null)
    {
        if (interactionPromptRoot == null) return;

        bool show = !string.IsNullOrWhiteSpace(message);
        interactionPromptRoot.SetActive(show);
        if (!show) return;

        if (interactionPromptText != null)
            interactionPromptText.text = message;

        if (interactionPromptIcon != null)
        {
            interactionPromptIcon.gameObject.SetActive(icon != null);
            if (icon != null)
                interactionPromptIcon.sprite = icon;
        }
    }

    public void ShowFeedback(string message, float duration = 2.6f)
    {
        if (feedbackBannerRoot == null || feedbackBannerText == null) return;
        if (string.IsNullOrWhiteSpace(message))
        {
            feedbackBannerRoot.SetActive(false);
            return;
        }

        feedbackBannerText.text = message;
        feedbackBannerRoot.SetActive(true);
        feedbackHideTime = Time.unscaledTime + Mathf.Max(0.2f, duration);
    }

    private void UpdateScreenEdgeIndicator()
    {
        if (screenEdgeIndicatorRect == null) return;

        MissionManager mm = MissionManager.Instance;
        if (mm == null || mm.MissionCompleted || mm.MissionFailed)
        {
            screenEdgeIndicatorRect.gameObject.SetActive(false);
            return;
        }

        WorldObjectiveMarker primaryMarker = mm.GetActivePrimaryWorldMarker();
        if (primaryMarker == null || !primaryMarker.IsMarkerVisible || !primaryMarker.ShowScreenEdgeIndicator)
        {
            screenEdgeIndicatorRect.gameObject.SetActive(false);
            return;
        }

        Camera cam = primaryMarker.ResolveActiveCamera();
        if (cam == null)
        {
            screenEdgeIndicatorRect.gameObject.SetActive(false);
            return;
        }

        Vector3 viewportPos = cam.WorldToViewportPoint(primaryMarker.WorldPosition);
        bool isOnScreen = viewportPos.z > 0f &&
                          viewportPos.x > 0.05f && viewportPos.x < 0.95f &&
                          viewportPos.y > 0.05f && viewportPos.y < 0.95f;

        if (isOnScreen)
        {
            screenEdgeIndicatorRect.gameObject.SetActive(false);
            return;
        }

        screenEdgeIndicatorRect.gameObject.SetActive(true);

        if (viewportPos.z < 0f)
        {
            viewportPos.x = 1f - viewportPos.x;
            viewportPos.y = 1f - viewportPos.y;
        }

        Vector2 dir = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f).normalized;
        RectTransform parentCanvasRect = screenEdgeIndicatorRect.parent as RectTransform;
        Vector2 canvasSize = parentCanvasRect != null ? parentCanvasRect.rect.size : new Vector2(1920f, 1080f);

        float maxX = (canvasSize.x * 0.5f) - 64f;
        float maxY = (canvasSize.y * 0.5f) - 64f;
        screenEdgeIndicatorRect.anchoredPosition = new Vector2(dir.x * maxX, dir.y * maxY);

        if (screenEdgeArrowImage != null)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            screenEdgeArrowImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            screenEdgeArrowImage.color = primaryMarker.AccentColor;
        }

        if (screenEdgeTargetIcon != null && primaryMarker.MarkerIcon != null)
            screenEdgeTargetIcon.sprite = primaryMarker.MarkerIcon;
    }
}
