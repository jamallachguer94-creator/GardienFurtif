using UnityEngine;

/// <summary>
/// Lightweight built-in OnGUI HUD preserved for backwards compatibility.
/// Automatically suppresses duplicate OnGUI boxes when MissionObjectiveTracker Canvas is active.
/// </summary>
public class MissionHUD : MonoBehaviour
{
    private MissionManager manager;
    private string objective = "Objective: Steal the main dossier";
    private string prompt = string.Empty;
    private string feedback = string.Empty;
    private float feedbackUntil;
    private bool showEnd;
    private bool success;
    private string endTitle = string.Empty;
    private string endMessage = string.Empty;

    private GUIStyle objectiveStyle;
    private GUIStyle promptStyle;
    private GUIStyle feedbackStyle;
    private GUIStyle endTitleStyle;
    private GUIStyle endMessageStyle;

    public bool SuppressLegacyOnGUI { get; set; }

    public void SetManager(MissionManager value) => manager = value;
    public void SetObjective(string value) => objective = value;
    public void SetPrompt(string value) => prompt = value;

    public void ShowFeedback(string value, float duration)
    {
        feedback = value;
        feedbackUntil = Time.unscaledTime + Mathf.Max(0.1f, duration);
    }

    public void ShowEndScreen(bool won, string title, string message)
    {
        showEnd = true;
        success = won;
        endTitle = title;
        endMessage = message;
    }

    private void EnsureStyles()
    {
        if (objectiveStyle != null) return;

        objectiveStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft
        };
        objectiveStyle.normal.textColor = Color.white;

        promptStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        promptStyle.normal.textColor = Color.white;

        feedbackStyle = new GUIStyle(promptStyle) { fontSize = 22 };
        endTitleStyle = new GUIStyle(promptStyle) { fontSize = 44 };
        endMessageStyle = new GUIStyle(promptStyle) { fontSize = 25, fontStyle = FontStyle.Normal };
    }

    private void OnGUI()
    {
        if (SuppressLegacyOnGUI) return;

        EnsureStyles();

        GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.82f);
        GUI.Box(new Rect(18, 18, 560, 96), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(40, 30, 520, 68), objective, objectiveStyle);

        if (!string.IsNullOrEmpty(prompt) && !showEnd)
        {
            float width = Mathf.Min(760f, Screen.width - 40f);
            Rect promptRect = new Rect((Screen.width - width) * 0.5f, Screen.height - 138f, width, 72f);
            GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.88f);
            GUI.Box(promptRect, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(promptRect.x + 10, promptRect.y + 5, promptRect.width - 20, promptRect.height - 10), prompt, promptStyle);
        }

        if (!string.IsNullOrEmpty(feedback) && Time.unscaledTime <= feedbackUntil && !showEnd)
        {
            float width = Mathf.Min(840f, Screen.width - 40f);
            Rect feedbackRect = new Rect((Screen.width - width) * 0.5f, 112f, width, 52f);
            GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.88f);
            GUI.Box(feedbackRect, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(feedbackRect.x + 12, feedbackRect.y + 4, feedbackRect.width - 24, feedbackRect.height - 8), feedback, feedbackStyle);
        }

        if (showEnd)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.color = success ? new Color(0.25f, 0.9f, 0.45f) : new Color(1f, 0.35f, 0.3f);
            GUI.Label(new Rect(40, Screen.height * 0.32f, Screen.width - 80, 84), endTitle, endTitleStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.45f, Screen.width * 0.68f, 130), endMessage, endMessageStyle);
        }

        GUI.color = Color.white;
    }
}
