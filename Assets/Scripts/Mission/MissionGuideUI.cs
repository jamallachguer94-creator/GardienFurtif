using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Permanent Mission Guide Character UI inspired by Robbery Bob.
/// Displays a character portrait, speech bubble, optional objective badge icon,
/// smooth appearance animation, and typewriter dialogue effect.
/// </summary>
public class MissionGuideUI : MonoBehaviour
{
    [Header("Guide Character Visuals")]
    [SerializeField] private Sprite guidePortrait;
    [SerializeField] private string guideName = "HANDLER // FOX";

    [Header("Typewriter & Animation")]
    [SerializeField] private bool enableTypewriter = true;
    [SerializeField, Range(15f, 120f)] private float charactersPerSecond = 52f;
    [SerializeField] private float pulseDuration = 0.25f;

    [Header("UI References (Auto-built if unassigned)")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text speakerNameText;
    [SerializeField] private Text messageText;
    [SerializeField] private Image contextIconImage;

    private Coroutine typewriterCoroutine;
    private Coroutine pulseCoroutine;
    private string lastMessageKey = string.Empty;

    public Sprite GuidePortrait
    {
        get => guidePortrait != null ? guidePortrait : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.GuidePortrait);
        set
        {
            guidePortrait = value;
            if (portraitImage != null) portraitImage.sprite = GuidePortrait;
        }
    }

    private void Awake()
    {
        EnsureUIReferences();
        if (portraitImage != null)
            portraitImage.sprite = GuidePortrait;
    }

    public void BindUIElements(
        RectTransform root,
        CanvasGroup group,
        Image portrait,
        Text speaker,
        Text message,
        Image contextIcon)
    {
        panelRoot = root;
        panelCanvasGroup = group;
        portraitImage = portrait;
        speakerNameText = speaker;
        messageText = message;
        contextIconImage = contextIcon;

        if (portraitImage != null)
            portraitImage.sprite = GuidePortrait;
        if (speakerNameText != null)
            speakerNameText.text = guideName;
    }

    private void EnsureUIReferences()
    {
        if (panelRoot == null)
            panelRoot = GetComponent<RectTransform>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponent<CanvasGroup>();
        if (portraitImage != null && portraitImage.sprite == null)
            portraitImage.sprite = GuidePortrait;
        if (speakerNameText != null)
            speakerNameText.text = guideName;
    }

    public void ShowGuideMessage(string message, Sprite objectiveIcon = null, string deduplicationKey = null)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        string key = string.IsNullOrEmpty(deduplicationKey) ? message : deduplicationKey;
        if (key == lastMessageKey)
            return;

        lastMessageKey = key;
        EnsureUIReferences();

        if (portraitImage != null)
            portraitImage.sprite = GuidePortrait;

        if (contextIconImage != null)
        {
            if (objectiveIcon != null)
            {
                contextIconImage.gameObject.SetActive(true);
                contextIconImage.sprite = objectiveIcon;
            }
            else
            {
                contextIconImage.gameObject.SetActive(false);
            }
        }

        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        if (enableTypewriter && isActiveAndEnabled && messageText != null)
            typewriterCoroutine = StartCoroutine(TypeMessageRoutine(message));
        else if (messageText != null)
            messageText.text = message;

        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        if (isActiveAndEnabled && panelRoot != null)
            pulseCoroutine = StartCoroutine(AnimateBubblePulseRoutine());
    }

    private IEnumerator TypeMessageRoutine(string fullText)
    {
        messageText.text = string.Empty;
        float delay = 1f / Mathf.Max(10f, charactersPerSecond);

        for (int i = 0; i < fullText.Length; i++)
        {
            messageText.text = fullText.Substring(0, i + 1);
            yield return new WaitForSecondsRealtime(delay);
        }

        messageText.text = fullText;
    }

    private IEnumerator AnimateBubblePulseRoutine()
    {
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / pulseDuration);
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.045f;
            if (panelRoot != null)
                panelRoot.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        if (panelRoot != null)
            panelRoot.localScale = Vector3.one;
    }

    public void ShowMissionStartDialogue()
    {
        ShowGuideMessage(
            "Your mission: Find the main dossier. Search the office and collect valuable objects.",
            MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Dossier),
            "MISSION_START");
    }

    public void ShowNearDossierDialogue()
    {
        ShowGuideMessage(
            "The important dossier is nearby. Collect it!",
            MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Dossier),
            "NEAR_DOSSIER");
    }

    public void ShowDossierCollectedDialogue(bool exitReadyNow)
    {
        string text = exitReadyNow
            ? "Excellent! You got the main dossier. Now find the exit!"
            : "Excellent! You got the main dossier. Collect the required loot to unlock the exit!";

        ShowGuideMessage(
            text,
            MissionIconFactory.GetIcon(MissionIconFactory.IconKind.ExitDoor),
            "DOSSIER_COLLECTED");
    }

    public void ShowLootCollectedDialogue(LootCollectible item, bool allLootCollected, bool hasDossier)
    {
        if (item == null) return;

        if (allLootCollected)
        {
            ShowAllLootCollectedDialogue(hasDossier);
            return;
        }

        ShowGuideMessage(
            item.GuideCollectedMessage,
            item.Icon,
            $"LOOT_{item.LootId}");
    }

    public void ShowAllLootCollectedDialogue(bool hasDossier)
    {
        string msg = hasDossier
            ? "Excellent! All valuable objects have been collected. Now get to the exit!"
            : "Excellent! All valuable objects have been collected. Now grab the main dossier!";

        ShowGuideMessage(
            msg,
            MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Checkmark),
            "ALL_LOOT_COLLECTED");
    }

    public void ShowExitBlockedDialogue(string reason)
    {
        ShowGuideMessage(
            reason,
            MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Lock),
            $"EXIT_BLOCKED_{reason}");
    }

    public void ShowMissionCompletedDialogue()
    {
        ShowGuideMessage(
            "Perfect job! Mission completed!",
            MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Checkmark),
            "MISSION_COMPLETED");
    }

    public void ShowMissionFailedDialogue(string reason)
    {
        ShowGuideMessage(
            $"Mission failed! {reason}",
            MissionIconFactory.GetIcon(MissionIconFactory.IconKind.Lock),
            "MISSION_FAILED");
    }
}
