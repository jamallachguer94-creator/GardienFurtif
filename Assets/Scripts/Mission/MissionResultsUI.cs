using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Polished Mission Completion / Failure Results Screen.
/// Calculates and displays accurate dossier status, loot collected count,
/// loot score, required loot completion, overall completion percentage, and 1-3 star rating.
/// </summary>
public class MissionResultsUI : MonoBehaviour
{
    [Header("Results UI References")]
    [SerializeField] private GameObject rootOverlay;
    [SerializeField] private Text headerTitleText;
    [SerializeField] private Text subtitleMessageText;
    [SerializeField] private Text dossierValueText;
    [SerializeField] private Text lootCollectedValueText;
    [SerializeField] private Text requiredLootValueText;
    [SerializeField] private Text lootScoreValueText;
    [SerializeField] private Text completionPercentValueText;
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private Button replayButton;

    public bool IsShowing => rootOverlay != null && rootOverlay.activeSelf;

    public void BindUIReferences(
        GameObject overlay,
        Text titleTxt,
        Text subTxt,
        Text dossierTxt,
        Text lootCountTxt,
        Text reqLootTxt,
        Text scoreTxt,
        Text percentTxt,
        Image[] stars,
        Button replayBtn)
    {
        rootOverlay = overlay;
        headerTitleText = titleTxt;
        subtitleMessageText = subTxt;
        dossierValueText = dossierTxt;
        lootCollectedValueText = lootCountTxt;
        requiredLootValueText = reqLootTxt;
        lootScoreValueText = scoreTxt;
        completionPercentValueText = percentTxt;
        starImages = stars;
        replayButton = replayBtn;

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplayCurrentMission);
        }
    }

    private void Awake()
    {
        if (rootOverlay != null)
            rootOverlay.SetActive(false);

        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(ReplayCurrentMission);
            replayButton.onClick.AddListener(ReplayCurrentMission);
        }
    }

    private void Update()
    {
        if (IsShowing && Input.GetKeyDown(KeyCode.R))
            ReplayCurrentMission();
    }

    public void ShowResults(
        bool won,
        string title,
        string subtitle,
        bool hasDossier,
        LootManager lootManager)
    {
        if (rootOverlay == null) return;
        rootOverlay.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        int collectedLoot = lootManager != null ? lootManager.CollectedLootCount : 0;
        int totalLoot = lootManager != null ? lootManager.TotalLootCount : 0;
        int collectedScore = lootManager != null ? lootManager.CollectedLootValue : 0;
        int totalScore = lootManager != null ? lootManager.TotalPossibleValue : 0;
        int reqCollected = lootManager != null ? lootManager.CollectedRequiredLootCount : 0;
        int reqTotal = lootManager != null ? lootManager.RequiredLootCount : 0;

        int totalUnits = 1 + totalLoot;
        int completedUnits = (hasDossier ? 1 : 0) + collectedLoot;
        int completionPercent = Mathf.RoundToInt(((float)completedUnits / Mathf.Max(1, totalUnits)) * 100f);
        if (!won && !hasDossier && collectedLoot == 0)
            completionPercent = 0;

        int starsEarned = 0;
        if (won)
        {
            if (totalLoot == 0 || collectedLoot >= totalLoot)
                starsEarned = 3;
            else if ((float)collectedLoot / totalLoot >= 0.5f)
                starsEarned = 2;
            else
                starsEarned = 1;
        }

        Color winColor = new Color(0.24f, 0.92f, 0.48f, 1f);
        Color failColor = new Color(1f, 0.36f, 0.32f, 1f);

        if (headerTitleText != null)
        {
            headerTitleText.text = string.IsNullOrWhiteSpace(title)
                ? (won ? "MISSION COMPLETED" : "MISSION FAILED")
                : title;
            headerTitleText.color = won ? winColor : failColor;
        }

        if (subtitleMessageText != null)
            subtitleMessageText.text = subtitle;

        if (dossierValueText != null)
        {
            dossierValueText.text = hasDossier ? "COMPLETED" : "INCOMPLETE";
            dossierValueText.color = hasDossier ? winColor : failColor;
        }

        if (lootCollectedValueText != null)
        {
            lootCollectedValueText.text = $"{collectedLoot} / {totalLoot}";
            lootCollectedValueText.color = (totalLoot > 0 && collectedLoot >= totalLoot)
                ? winColor
                : new Color(1f, 0.84f, 0.25f, 1f);
        }

        if (requiredLootValueText != null)
        {
            if (reqTotal > 0)
            {
                bool allReq = reqCollected >= reqTotal;
                requiredLootValueText.text = allReq ? $"COMPLETED ({reqCollected} / {reqTotal})" : $"{reqCollected} / {reqTotal}";
                requiredLootValueText.color = allReq ? winColor : failColor;
            }
            else
            {
                requiredLootValueText.text = "ALL COMPLETED";
                requiredLootValueText.color = winColor;
            }
        }

        if (lootScoreValueText != null)
        {
            lootScoreValueText.text = totalScore > 0 ? $"${collectedScore} / ${totalScore}" : $"{collectedScore}";
            lootScoreValueText.color = new Color(1f, 0.84f, 0.25f, 1f);
        }

        if (completionPercentValueText != null)
        {
            completionPercentValueText.text = $"{completionPercent}%";
            completionPercentValueText.color = completionPercent >= 100 ? winColor : Color.white;
        }

        Sprite filledStar = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.StarFilled);
        Sprite emptyStar = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.StarEmpty);

        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                starImages[i].sprite = (i < starsEarned) ? filledStar : emptyStar;
                starImages[i].color = Color.white;
            }
        }
    }

    public void ReplayCurrentMission()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
