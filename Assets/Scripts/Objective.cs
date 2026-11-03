using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// The "what do I do now" line at the top of the screen. Builds itself (no scene wiring) and works the
// current goal out from the game state every half second, so it never needs to be told anything.
// Edit the texts in Current() below.
public class Objective : MonoBehaviour
{
    static Objective instance;
    TextMeshProUGUI label;
    float refresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        if (instance != null) return;
        var go = new GameObject("Objective");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Objective>();
        instance.Build();
    }

    static string Current()
    {
        if (EndingScreen.IsPlaying || DayClock.Instance == null) return "";

        int day = DayClock.Instance.Day;
        float hour = DayClock.Instance.Hour;
        bool home = SceneManager.GetActiveScene().name == JonteStory.ApartmentScene;
        bool reached(int d, float h) => day > d || (day == d && hour >= h);

        if (reached(JonteStory.FinalDay, JonteStory.FinalHour))
            return home ? "Jonte is here." : "Go home. Jonte is waiting.";

        if (reached(JonteStory.WarnDay, JonteStory.WarnHour) && GameState.hasDealerNumber)
            return $"Be home tomorrow at {JonteStory.FinalHour:00}:00. Jonte is coming.";

        if (!GameState.metClassmate)
            return "Go to the Java lecture - the school's front door, right across the street";

        if (JonteStory.DeliveryPending || (home && DealerAI.Instance != null && DealerAI.Instance.CurrentState != DealerAI.DealerState.Idle))
            return home ? "Jonte is on his way up. Wait for him." : "Jonte is on his way. Go home.";

        if (GameState.jarsFromDealer == 0)
        {
            if (!JonteStory.Called)
                return $"Wait for Jonte to call - Day {JonteStory.CallDay}, {JonteStory.CallHour:00}:00  (Tab = phone)";
            return "Call Jonte back - Tab, Contacts (he won't pick up before noon)";
        }

        if (JointCraftingStation.globalBudCount <= 0 && GameState.pocketJoints <= 0)
            return "Out of weed. Call Jonte for more - Tab, Contacts";

        if (GameState.pocketJoints > 0 && !home)
            return "Sell your joints in the park - 150 kr each";

        if (GameState.pocketJoints > 0)
            return "Go sell your joints in the park  (rent 5000 kr is due Day 7, 12:00)";

        return "Roll joints at the table, pocket them with R, sell them in the park";
    }

    void Update()
    {
        refresh -= Time.unscaledDeltaTime;
        if (refresh > 0f) return;
        refresh = 0.5f;
        string text = Current();
        label.text = text;
        label.enabled = !string.IsNullOrEmpty(text);
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;   // under the instructions panel and the ending screen
        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(transform, false);
        label = textGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = 26;
        label.alignment = TextAlignmentOptions.Top;
        label.color = new Color(1f, 0.93f, 0.75f, 0.95f);
        label.outlineWidth = 0.2f;
        label.outlineColor = Color.black;
        label.raycastTarget = false;
        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -22f);   // Notify messages show below, at -90
        rt.sizeDelta = new Vector2(1200f, 60f);
        label.text = "";
    }
}
