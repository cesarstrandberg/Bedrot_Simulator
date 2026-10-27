using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The end of the week. Call EndingScreen.Play() from anywhere (the dealer's shot, eviction...):
// hard cut to black, everything freezes, silence, then the week's stats one line at a time and
// "Nothing matters." Builds its own UI, nothing to wire. F9 triggers it for testing.
public class EndingScreen : MonoBehaviour
{
    public const string RestartScene = "ApartmentScene";

    static EndingScreen instance;
    public static bool IsPlaying => instance != null;

    TextMeshProUGUI body, closer, prompt;
    bool waitingForInput;

    // sound: played over the cut to black (e.g. the gunshot). Can be null.
    public static void Play(AudioClip sound = null)
    {
        if (instance != null) return;
        Create();
        instance.StartCoroutine(instance.Run(sound));
    }

    void Update()
    {
        if (!waitingForInput) return;
        if (Input.GetKeyDown(KeyCode.R)) Restart();
        else if (Input.GetKeyDown(KeyCode.Escape)) Quit();
    }

    IEnumerator Run(AudioClip sound)
    {
        string time = DayClock.Instance != null
            ? $"Day {DayClock.Instance.Day}. {Mathf.FloorToInt(DayClock.Instance.Hour):00}:{Mathf.FloorToInt(DayClock.Instance.Hour % 1f * 60f):00}."
            : "";

        // Freeze the world: clock, needs, NPCs, all sound
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (sound != null)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.ignoreListenerPause = true;
            src.PlayOneShot(sound);
        }

        yield return new WaitForSecondsRealtime(3f);   // silence

        string[] lines =
        {
            time,
            "",
            $"Exams taken: {GameState.examsTaken}",
            $"Joints sold: {GameState.jointsSold}",
            $"Money earned: {GameState.moneyEarned:0} kr",
            $"Rent paid: {(GameState.rentPaid ? "yes" : "no")}",
        };
        foreach (string line in lines)
        {
            body.text += line + "\n";
            yield return new WaitForSecondsRealtime(line == "" ? 0.3f : 0.9f);
        }

        yield return new WaitForSecondsRealtime(1.5f);
        closer.text = "Nothing matters.";
        yield return new WaitForSecondsRealtime(2.5f);
        prompt.text = "[R] Start over      [Esc] Quit";
        waitingForInput = true;
    }

    void Restart()
    {
        ResetWeek();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        instance = null;
        Destroy(gameObject);
        SceneManager.LoadScene(RestartScene);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Everything that's static (survives scene loads) back to how a fresh game starts
    public static void ResetWeek()
    {
        GameState.ResetAll();
        Rent.ResetAll();
        UniversityPortal.nextExamDay = 1;
        JointCraftingStation.globalBudCount = 7;
    }

    static void Create()
    {
        var go = new GameObject("EndingScreen");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<EndingScreen>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;   // above everything, Notify included
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var bg = new GameObject("Black").AddComponent<Image>();
        bg.transform.SetParent(go.transform, false);
        bg.color = Color.black;
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

        instance.body = MakeText(go.transform, new Vector2(0f, 80f), 40, new Color(0.85f, 0.85f, 0.85f));
        instance.closer = MakeText(go.transform, new Vector2(0f, -260f), 64, Color.white);
        instance.closer.fontStyle = FontStyles.Bold;
        instance.prompt = MakeText(go.transform, new Vector2(0f, -440f), 26, new Color(0.5f, 0.5f, 0.5f));
    }

    static TextMeshProUGUI MakeText(Transform parent, Vector2 pos, float size, Color color)
    {
        var text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = "";
        text.rectTransform.anchoredPosition = pos;
        text.rectTransform.sizeDelta = new Vector2(1400f, 500f);
        return text;
    }
}

// Test trigger - remove once the dealer's shot calls EndingScreen.Play() for real
public class EndingScreenDebugKey : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        var go = new GameObject("EndingScreenDebugKey");
        DontDestroyOnLoad(go);
        go.AddComponent<EndingScreenDebugKey>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9)) EndingScreen.Play();
    }
}
