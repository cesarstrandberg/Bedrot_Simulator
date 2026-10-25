using TMPro;
using UnityEngine;

// The game's clock: which day it is and what time. Time runs while you play, sleeping skips to
// the clock 5-7 hours ahead (Bed.cs calls SkipHours), and the sun light is rotated to match the hour
// so the procedural sky goes through sunrise/sunset/night on its own.
public class DayClock : MonoBehaviour
{
    public static DayClock Instance { get; private set; }

    [Header("Tid")]
    public float realMinutesPerDay = 24f;   // One full in-game day (24h) takes this many real minutes
    public float startHour = 8f;

    [Header("Sol")]
    public Light sun;                        // The Directional Light
    public float sunMaxIntensity = 2f;       // Its intensity at noon (what it was set to before)
    public float sunYaw = 330f;              // Compass direction the sun travels along
    [Range(0f, 1f)] public float nightAmbient = 0.15f; // How dark the sky light gets at night (1 = no change)

    [Header("UI (byggs automatiskt om tom)")]
    public TextMeshProUGUI clockText;

    public int Day { get; private set; } = 1;
    public float Hour { get; private set; }   // 0-24

    private float ambientRefreshTimer;

    void Awake()
    {
        Instance = this;
        Hour = startHour;
        if (GameState.hasData)   // came through a SceneDoor - keep the same day and time
        {
            Day = GameState.day;
            Hour = GameState.hour;
        }
        if (sun == null) sun = RenderSettings.sun;
        if (clockText == null) clockText = BuildClockText();
    }

    void Update()
    {
        Hour += Time.deltaTime * 24f / (realMinutesPerDay * 60f);
        if (Hour >= 24f)
        {
            Hour -= 24f;
            Day++;
        }

        UpdateSun();
        UpdateText();
        Rent.Tick(Day, Hour);
    }

    // Jumps the clock forward (sleeping). Rolls over into the next day only if it passes midnight.
    public void SkipHours(float hours)
    {
        Hour += hours;
        while (Hour >= 24f)
        {
            Hour -= 24f;
            Day++;
        }
        UpdateSun();
        UpdateText();
    }

    void UpdateSun()
    {
        if (sun == null) return;

        // 06:00 = sun on the horizon, 12:00 = straight up, 18:00 = horizon again, night below it.
        float elevation = (Hour - 6f) / 24f * 360f;
        sun.transform.rotation = Quaternion.Euler(elevation, sunYaw, 0f);

        float daylight = Mathf.Clamp01(Mathf.Sin(elevation * Mathf.Deg2Rad) * 3f); // soft fade at dawn/dusk
        sun.intensity = sunMaxIntensity * daylight;
        sun.enabled = daylight > 0f;
        RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbient, 1f, daylight);

        // Sky lighting is only recalculated when asked to - a few times a second is plenty.
        ambientRefreshTimer -= Time.deltaTime;
        if (ambientRefreshTimer <= 0f)
        {
            DynamicGI.UpdateEnvironment();
            ambientRefreshTimer = 0.5f;
        }
    }

    void UpdateText()
    {
        if (clockText == null) return;
        int h = Mathf.FloorToInt(Hour);
        int m = Mathf.FloorToInt((Hour - h) * 60f);
        string pocket = GameState.pocketJoints > 0 ? $"\nJoints in pocket: {GameState.pocketJoints}" : "";
        clockText.text = $"Day {Day}  {h:00}:{m:00}\n<size=70%>Rent {Rent.amountDue:0} kr - Day {Rent.dueDay}{pocket}</size>";
    }

    TextMeshProUGUI BuildClockText()
    {
        var canvasGO = new GameObject("DayClockCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var textGO = new GameObject("DayClockText");
        textGO.transform.SetParent(canvasGO.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 28;
        text.color = new Color(1f, 1f, 1f, 0.85f);
        text.alignment = TextAlignmentOptions.TopRight;
        text.raycastTarget = false;

        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -18f);
        rt.sizeDelta = new Vector2(400f, 120f);
        return text;
    }
}
