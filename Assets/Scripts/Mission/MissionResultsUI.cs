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
    [SerializeField] private Image resultPortraitImage;
    [SerializeField] private Text scoreHeadlineText;

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

    public void BindResultArtwork(Image portrait, Text scoreHeadline)
    {
        resultPortraitImage = portrait;
        scoreHeadlineText = scoreHeadline;
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

        // Freeze gameplay behind the results screen while keeping its UI interactive.
        Time.timeScale = 0f;
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
            headerTitleText.text = won ? "BRAVO!" : "GAME OVER";
            headerTitleText.color = won
                ? new Color(0.16f, 0.10f, 0.045f, 1f)
                : new Color(0.42f, 0.055f, 0.045f, 1f);
        }

        if (resultPortraitImage != null)
        {
            resultPortraitImage.sprite = MissionIconFactory.GetIcon(
                won ? MissionIconFactory.IconKind.ThiefPortrait : MissionIconFactory.IconKind.VillainPortrait);
            resultPortraitImage.color = Color.white;
        }

        if (scoreHeadlineText != null)
        {
            scoreHeadlineText.text = "$" + collectedScore.ToString("N0");
            scoreHeadlineText.color = won
                ? new Color(0.12f, 0.10f, 0.06f, 1f)
                : new Color(0.40f, 0.07f, 0.04f, 1f);
        }

        if (subtitleMessageText != null)
        {
            subtitleMessageText.text = won ? "MISSION COMPLETE — YOU ESCAPED!" : subtitle;
            subtitleMessageText.color = new Color(0.28f, 0.18f, 0.08f, 1f);
        }

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

        if (lootScoreValueText != null && lootScoreValueText != scoreHeadlineText)
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
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Scene current = SceneManager.GetActiveScene();
        if (current.buildIndex >= 0)
            SceneManager.LoadScene(current.buildIndex);
        else
            SceneManager.LoadScene(current.name);
    }
}
