using UnityEngine;
using UnityEngine.UI;

// "How to play" panel shown on spawn. Builds its own canvas at runtime, so nothing needs to be
// placed or wired in the scene. Pauses the game (timeScale 0 freezes movement, mouse look and stat
// ticking) until the player presses Enter.
public class InstructionsPanel : MonoBehaviour
{
    const string Body =
        "<size=40><b>BEDROT SIMULATOR</b></size>   <size=22><i>early prototype</i></size>\n\n" +
        "You're a broke student in Stockholm. Keep your grades up, keep yourself fed...\n" +
        "and try not to get in too deep.\n\n" +
        "<b>CONTROLS</b>\n" +
        "<b>WASD</b> move  ·  <b>Shift</b> sprint  ·  <b>Space</b> jump  ·  <b>C</b> crouch\n" +
        "<b>E</b> pick up / drop / talk  ·  <b>F</b> throw  ·  <b>R</b> pocket a joint  ·  <b>Left click</b> use / eat / drink / interact\n" +
        "<b>Scroll</b> rotate held item  ·  <b>Ctrl</b> zoom  ·  <b>Q</b> lean  ·  <b>Esc</b> exit laptop/station\n\n" +
        "<b>THINGS TO TRY</b>\n" +
        "1.  <b>Use the laptop.</b> Go to stockholmshogskola.se, log in and take an exam: memorize the Java\n" +
        "     code, then retype it. A better grade means more student-aid money.\n" +
        "2.  <b>Order food</b> on snabbmat.se. A driver brings it to your door. Click him, then open the bag and eat.\n" +
        "3.  <b>Order weed</b> on darkmarket.se. The dealer knocks on your door; click him to pay.\n" +
        "4.  <b>Roll a joint</b> at the table: open the jar, grind a bud, then roll it. Then smoke it.\n" +
        "5.  <b>Watch your stats.</b> Hunger, thirst and craving keep going down. Being high gives you the munchies.\n" +
        "6.  <b>Mind the neighbour.</b> He doesn't like the smell, and he gets angrier with every joint.\n" +
        "7.  <b>Pay the rent.</b> 5000 kr every week. CSN won't cover it - pocket joints (R), go outside and sell them.\n" +
        "8.  <b>Betsey Trotwood.</b> The pub across the park. Getting past the bouncer is another matter.\n\n" +
        "<size=26><b>Press [Enter] to start</b></size>";

    GameObject canvasObject;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        new GameObject("InstructionsPanel").AddComponent<InstructionsPanel>();
    }

    void Start()
    {
        canvasObject = new GameObject("InstructionsCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Dark backdrop over the whole screen
        Image backdrop = CreateChild("Backdrop", canvasObject.transform).AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.6f);
        Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one);

        // Centered box
        Image box = CreateChild("Box", canvasObject.transform).AddComponent<Image>();
        box.color = new Color(0.08f, 0.08f, 0.09f, 0.95f);
        RectTransform boxRect = box.rectTransform;
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(1400, 900);

        Text text = CreateChild("Text", box.transform).AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.lineSpacing = 1.15f;
        text.color = new Color(0.92f, 0.92f, 0.9f);
        text.supportRichText = true;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = Body;
        Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        text.rectTransform.offsetMin = new Vector2(60, 40);
        text.rectTransform.offsetMax = new Vector2(-60, -40);

        Time.timeScale = 0f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Time.timeScale = 1f;
            Destroy(gameObject);
        }
    }

    static GameObject CreateChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
