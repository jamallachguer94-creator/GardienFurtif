using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Floating world-space objective/loot marker inspired by Robbery Bob.
/// Automatically faces the active camera in both First-Person and Third-Person modes.
/// </summary>
public class WorldObjectiveMarker : MonoBehaviour
{
    [Header("Target & Positioning")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.35f, 0f);

    [Header("Visual Configuration")]
    [SerializeField] private Sprite markerIcon;
    [SerializeField] private string markerTitle = "OBJECTIVE";
    [SerializeField] private Color accentColor = new Color(0.20f, 0.65f, 1f, 1f);
    [SerializeField] private bool showDistance = true;
    [SerializeField] private bool showTitleLabel = true;
    [SerializeField] private bool showScreenEdgeIndicator = false;
    [SerializeField] private float maxLabelVisibleDistance = 16f;
    [SerializeField] private float maxIconVisibleDistance = 45f;

    [Header("Animation")]
    [SerializeField] private float bobAmplitude = 0.10f;
    [SerializeField] private float bobFrequency = 2.4f;
    [SerializeField] private float pulseAmplitude = 0.08f;
    [SerializeField] private float pulseFrequency = 3.0f;
    [SerializeField] private float baseWorldScale = 0.0085f;

    private Canvas worldCanvas;
    private RectTransform canvasRect;
    private Image outerRingImage;
    private Image badgeBgImage;
    private Image iconImage;
    private Text titleText;
    private CanvasGroup canvasGroup;

    private Camera cachedCamera;
    private float nextCameraSearchTime;
    private bool isMarkerVisible = true;
    private float animationSeed;

    public bool IsMarkerVisible => isMarkerVisible;
    public Sprite MarkerIcon => markerIcon;
    public string MarkerTitle => markerTitle;
    public Color AccentColor => accentColor;
    public bool ShowScreenEdgeIndicator => showScreenEdgeIndicator;
    public Vector3 WorldPosition => (targetTransform != null ? targetTransform.position : transform.position) + worldOffset;

    private void Awake()
    {
        animationSeed = Mathf.Abs(GetInstanceID() % 1000) * 0.13f;
        if (targetTransform == null && transform.parent != null)
            targetTransform = transform.parent;

        BuildWorldCanvasIfNeeded();
        ApplyVisuals();
    }

    public void Configure(
        Transform target,
        Vector3 offset,
        Sprite icon,
        string title,
        Color color,
        bool showDist = true,
        bool showLabelOnHoverOrClose = true,
        bool enableScreenEdgeArrow = false)
    {
        targetTransform = target != null ? target : transform;
        worldOffset = offset;
        markerIcon = icon;
        markerTitle = title;
        accentColor = color;
        showDistance = showDist;
        showTitleLabel = showLabelOnHoverOrClose;
        showScreenEdgeIndicator = enableScreenEdgeArrow;

        BuildWorldCanvasIfNeeded();
        ApplyVisuals();
    }

    public void UpdateVisualState(Sprite newIcon, string newTitle, Color newColor)
    {
        if (newIcon != null) markerIcon = newIcon;
        if (!string.IsNullOrEmpty(newTitle)) markerTitle = newTitle;
        accentColor = newColor;
        ApplyVisuals();
    }

    public void SetMarkerVisible(bool visible)
    {
        isMarkerVisible = visible;
        if (worldCanvas != null)
            worldCanvas.gameObject.SetActive(visible);
    }

    private void BuildWorldCanvasIfNeeded()
    {
        if (worldCanvas != null) return;

        worldCanvas = GetComponentInChildren<Canvas>(true);
        if (worldCanvas != null)
        {
            canvasRect = worldCanvas.GetComponent<RectTransform>();
            canvasGroup = worldCanvas.GetComponent<CanvasGroup>();
            return;
        }

        GameObject canvasObj = new GameObject("WorldMarkerCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = Vector3.zero;

        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 25;

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(180f, 110f);
        canvasRect.localScale = Vector3.one * baseWorldScale;

        GameObject ringObj = new GameObject("PulseRing");
        ringObj.transform.SetParent(canvasRect, false);
        RectTransform ringRect = ringObj.AddComponent<RectTransform>();
        ringRect.anchoredPosition = new Vector2(0f, 14f);
        ringRect.sizeDelta = new Vector2(68f, 68f);
        outerRingImage = ringObj.AddComponent<Image>();
        outerRingImage.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.RingOutline);
        outerRingImage.raycastTarget = false;

        GameObject bgObj = new GameObject("BadgeBackground");
        bgObj.transform.SetParent(canvasRect, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchoredPosition = new Vector2(0f, 14f);
        bgRect.sizeDelta = new Vector2(56f, 56f);
        badgeBgImage = bgObj.AddComponent<Image>();
        badgeBgImage.sprite = MissionIconFactory.GetIcon(MissionIconFactory.IconKind.CircleFill);
        badgeBgImage.color = new Color(0.06f, 0.09f, 0.14f, 0.92f);
        badgeBgImage.raycastTarget = false;

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(bgRect, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(44f, 44f);
        iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        GameObject labelBgObj = new GameObject("LabelPlate");
        labelBgObj.transform.SetParent(canvasRect, false);
        RectTransform labelBgRect = labelBgObj.AddComponent<RectTransform>();
        labelBgRect.anchoredPosition = new Vector2(0f, -30f);
        labelBgRect.sizeDelta = new Vector2(175f, 26f);
        Image labelBg = labelBgObj.AddComponent<Image>();
        labelBg.color = new Color(0.04f, 0.06f, 0.10f, 0.86f);
        labelBg.raycastTarget = false;

        GameObject textObj = new GameObject("TitleText");
        textObj.transform.SetParent(labelBgRect, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 1f);
        textRect.offsetMax = new Vector2(-4f, -1f);

        titleText = textObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 14;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Overflow;
        titleText.raycastTarget = false;
    }

    private void ApplyVisuals()
    {
        if (iconImage != null)
        {
            iconImage.sprite = markerIcon != null
                ? markerIcon
                : MissionIconFactory.GetIcon(MissionIconFactory.IconKind.LootBag);
        }

        if (outerRingImage != null)
            outerRingImage.color = accentColor;
    }

    private void LateUpdate()
    {
        if (!isMarkerVisible || worldCanvas == null) return;

        Camera cam = ResolveActiveCamera();
        if (cam == null) return;

        Vector3 anchorPos = targetTransform != null ? targetTransform.position + worldOffset : transform.position;
        float time = Time.unscaledTime + animationSeed;
        float bobOffset = Mathf.Sin(time * bobFrequency) * bobAmplitude;
        worldCanvas.transform.position = anchorPos + new Vector3(0f, bobOffset, 0f);

        Vector3 camForward = cam.transform.forward;
        if (camForward.sqrMagnitude > 0.0001f)
            worldCanvas.transform.rotation = Quaternion.LookRotation(camForward, cam.transform.up);

        Vector3 distanceRef = MissionManager.Instance != null && MissionManager.Instance.player != null
            ? MissionManager.Instance.player.transform.position
            : cam.transform.position;

        float dist = Vector3.Distance(distanceRef, anchorPos);

        if (canvasGroup != null)
        {
            float alpha = dist <= maxIconVisibleDistance
                ? 1f
                : Mathf.Clamp01(1f - (dist - maxIconVisibleDistance) / 10f);
            canvasGroup.alpha = alpha;
        }

        float pulse = 1f + Mathf.Sin(time * pulseFrequency) * pulseAmplitude;
        float distanceScale = Mathf.Clamp(dist * 0.08f, 0.85f, 1.65f);
        canvasRect.localScale = Vector3.one * (baseWorldScale * pulse * distanceScale);

        if (titleText != null && titleText.transform.parent != null)
        {
            bool showLabelNow = showTitleLabel && dist <= maxLabelVisibleDistance;
            titleText.transform.parent.gameObject.SetActive(showLabelNow);
            if (showLabelNow)
            {
                titleText.text = showDistance
                    ? $"{markerTitle} • {Mathf.Max(1, Mathf.RoundToInt(dist))}m"
                    : markerTitle;
            }
        }
    }

    public Camera ResolveActiveCamera()
    {
        if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
            return cachedCamera;

        if (Time.unscaledTime < nextCameraSearchTime)
            return cachedCamera;

        nextCameraSearchTime = Time.unscaledTime + 0.25f;

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            cachedCamera = Camera.main;
            return cachedCamera;
        }

        Camera[] allCameras = Camera.allCameras;
        for (int i = 0; i < allCameras.Length; i++)
        {
            if (allCameras[i] != null && allCameras[i].isActiveAndEnabled)
            {
                cachedCamera = allCameras[i];
                return cachedCamera;
            }
        }

        return null;
    }
}
