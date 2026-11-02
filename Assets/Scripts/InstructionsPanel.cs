using UnityEngine;
using UnityEngine.UI;

// Start of the game: a title card (the logo, Resources/bedrot_simulator_logo), then the "how to
// play" panel on the left side of the screen (the game stays visible on the right). Both pause the
// game (timeScale 0) until Enter. H opens the help panel again any time. Builds its own canvas at
// runtime, nothing to place or wire in a scene.
public class InstructionsPanel : MonoBehaviour
{
    const string Help =
        "<size=30><b>HOW TO PLAY</b></size>\n" +
        "<size=17><color=#aaaaaa>Scroll (mouse wheel / W S / arrow keys) to read everything.  [H] opens this again later.</color></size>\n\n" +

        "You're a broke student in Stockholm. You have one week.\n" +
        "Go to lectures, pay the rent, keep yourself fed... and try not to get in too deep.\n\n" +

        "<size=24><b><color=#f0c060>CONTROLS</color></b></size>\n" +
        "<b>Mouse</b>  look around\n" +
        "<b>W A S D</b>  walk   ·   <b>Shift</b>  sprint   ·   <b>Space</b>  jump   ·   <b>C</b>  crouch\n" +
        "<b>Q</b>  lean   ·   <b>Right mouse (hold)</b>  zoom\n" +
        "<b>E</b>  pick up / drop things, talk to people (look at them first)\n" +
        "<b>Left click</b>  use what you hold (smoke, drink, eat, open a bag)\n" +
        "        - with empty hands: doors, light switches, fridge, laptop, buttons, the bed\n" +
        "<b>F</b>  throw what you hold   ·   <b>Mouse wheel</b>  rotate what you hold\n" +
        "<b>R</b>  holding a joint: put it in your pocket.  Empty hands: take one out\n" +
        "<b>Tab</b>  phone up / down (answers a ringing call)\n" +
        "        - on the phone: <b>wheel</b> select, <b>left click</b> open, <b>right click</b> back\n" +
        "<b>Esc</b>  leave the laptop / computer / rolling tray\n" +
        "<b>1 - 4</b>  pick an answer when someone asks you something\n" +
        "<color=#aaaaaa>The crosshair turns red when you can use what you're looking at. In the browser:\n" +
        "click the game once to grab the mouse again after the laptop.</color>\n\n" +

        "<size=24><b><color=#f0c060>YOUR APARTMENT</color></b></size>\n" +
        "<b>The bed</b>  - left click it to sleep. Skips 5-7 hours.\n" +
        "<b>The laptop</b> (on the desk) - left click it. Three websites:\n" +
        "   <b>stockholmshogskola.se</b>  log in, pick a course and take the exam: memorize the Java code,\n" +
        "        then type it back. Better grade = more CSN (student money). Once a week.\n" +
        "   <b>snabbmat.se</b>  order food. The driver knocks on your door - walk up and click him,\n" +
        "        pick up the bag (E), left click to open it, pick up the food and left click to eat.\n" +
        "   <b>darkmarket.se</b>  order weed from Jonte (only once you have his number - see THE WEEK).\n" +
        "<b>Rolling a joint</b>  - carry the jar to the rolling tray on the table and left click to put it down.\n" +
        "   Left click the tray, then follow the red crosshair: open the lid, tip out a bud, put it in the\n" +
        "   grinder, close it, <b>scroll</b> to grind, open it, take a paper, pour, then <b>hold left mouse\n" +
        "   and drag up</b> to roll. Pick up the joint (E) and left click to smoke it.\n" +
        "<b>Fridge, sink, lights, doors</b>  - left click.\n" +
        "<b>Going outside</b>  - walk out into the hallway, left click the elevator button (the panel on\n" +
        "   the frame outside, the button inside), ride down and walk out the door on the ground floor.\n\n" +

        "<size=24><b><color=#f0c060>THE CITY</color></b></size>\n" +
        "<b>The school</b>  is right across the street - the big building's front door.\n" +
        "   The lecture starts when you walk in (you're always late). Sit in the <b>empty seat in the back\n" +
        "   row, at the end nearest the door</b>: look at it and press <b>E</b>. <b>Space</b> stands up.\n" +
        "   Sit through it - the guy next to you has something for you. Don't smoke in class.\n" +
        "<b>Going home</b>  - the door on the <b>white house on the corner, underneath the stairs</b>.\n" +
        "<b>Selling</b>  - pocket joints with R, then go to the park. Buyers show <b>[E] Sell joint</b> when\n" +
        "   you look at them (one sits by the bench, a few walk around). 150 kr each, 3 a day per person.\n" +
        "<b>Betsey Trotwood</b>  - the pub on the corner south of the park, open 18:00-03:00.\n" +
        "   Talk to the bouncer (E) and answer with 1-4. Don't show up wasted.\n\n" +

        "<size=24><b><color=#f0c060>THE WEEK</color></b></size>\n" +
        "<b>Day 1</b>  go to the lecture. The classmate next to you gives you a joint and the number\n" +
        "   of his dealer, <b>Jonte</b>.\n" +
        "<b>Day 2 afternoon</b>  Jonte calls (Tab to answer). He brings weed <b>on credit</b> - what you owe\n" +
        "   him is shown under the clock. Call him for more from the phone's Contacts (not before noon),\n" +
        "   or order on darkmarket.se. Each visit he takes whatever cash you have toward the old debt.\n" +
        "<b>Rent</b>  5000 kr, taken automatically on Day 7 at 12:00.\n" +
        "<b>Göran, the neighbour</b>  hates the smell. Every joint you smoke at home makes him angrier.\n" +
        "   He comes up and knocks - click him and he says his piece. The third time he doesn't come to talk.\n" +
        "<b>Your body</b>  hunger, thirst and craving go down over time (bars bottom left). Weed gives\n" +
        "   you the munchies. Too drunk or too high and you black out.\n" +
        "<b>Day 7</b>  ...you'll see.\n" +
        "<color=#aaaaaa>A day is 24 real minutes. Sleep to skip ahead.</color>\n";

    const string LogoResource = "bedrot_simulator_logo";

    static InstructionsPanel instance;

    GameObject titlePage, helpPage;
    RectTransform helpContent, helpViewport;
    float helpScroll;
    bool showingTitle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        var go = new GameObject("InstructionsPanel");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<InstructionsPanel>();
    }

    // True while the title card or help panel is open (game paused)
    public static bool IsOpen => instance != null && instance.titlePage != null
        && (instance.titlePage.activeSelf || instance.helpPage.activeSelf);

    void Start()
    {
        var canvasObject = new GameObject("InstructionsCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        BuildTitle(canvasObject.transform);
        BuildHelp(canvasObject.transform);

        showingTitle = true;
        titlePage.SetActive(true);
        helpPage.SetActive(false);
        Time.timeScale = 0f;
    }

    void BuildTitle(Transform parent)
    {
        titlePage = CreateChild("Title", parent);
        Stretch(titlePage.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        Image bg = CreateChild("Black", titlePage.transform).AddComponent<Image>();
        bg.color = new Color(0.03f, 0.02f, 0.05f, 1f);
        Stretch(bg.rectTransform, Vector2.zero, Vector2.one);

        Texture2D logo = Resources.Load<Texture2D>(LogoResource);
        if (logo != null)
        {
            RawImage img = CreateChild("Logo", titlePage.transform).AddComponent<RawImage>();
            img.texture = logo;
            float w = 1300f;
            img.rectTransform.sizeDelta = new Vector2(w, w * logo.height / logo.width);
            img.rectTransform.anchoredPosition = new Vector2(0f, 90f);
        }

        Text sub = MakeText(titlePage.transform, "A week in Stockholm.", 30, new Color(0.8f, 0.8f, 0.8f));
        sub.alignment = TextAnchor.MiddleCenter;
        sub.fontStyle = FontStyle.Italic;
        sub.rectTransform.sizeDelta = new Vector2(1200, 60);
        sub.rectTransform.anchoredPosition = new Vector2(0f, -330f);

        Text press = MakeText(titlePage.transform, "<b>Press [Enter]</b>", 30, Color.white);
        press.alignment = TextAnchor.MiddleCenter;
        press.rectTransform.sizeDelta = new Vector2(1200, 60);
        press.rectTransform.anchoredPosition = new Vector2(0f, -420f);

        Text proto = MakeText(titlePage.transform, "early prototype", 18, new Color(0.45f, 0.45f, 0.5f));
        proto.alignment = TextAnchor.LowerRight;
        Stretch(proto.rectTransform, Vector2.zero, Vector2.one);
        proto.rectTransform.offsetMin = new Vector2(30, 20);
        proto.rectTransform.offsetMax = new Vector2(-30, -20);
    }

    void BuildHelp(Transform parent)
    {
        helpPage = CreateChild("Help", parent);
        Stretch(helpPage.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        // Panel on the left, full height; the game is visible to the right
        Image box = CreateChild("Box", helpPage.transform).AddComponent<Image>();
        box.color = new Color(0.07f, 0.07f, 0.08f, 0.93f);
        RectTransform boxRect = box.rectTransform;
        boxRect.anchorMin = new Vector2(0f, 0f);
        boxRect.anchorMax = new Vector2(0f, 1f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.sizeDelta = new Vector2(940f, -60f);
        boxRect.anchoredPosition = new Vector2(30f, 0f);

        // Scrolling text area (scrolled by code - the mouse is locked, so no ScrollRect dragging)
        GameObject viewport = CreateChild("Viewport", box.transform);
        viewport.AddComponent<RectMask2D>();
        helpViewport = viewport.GetComponent<RectTransform>();
        Stretch(helpViewport, Vector2.zero, Vector2.one);
        helpViewport.offsetMin = new Vector2(40, 90);
        helpViewport.offsetMax = new Vector2(-30, -30);

        Text text = MakeText(viewport.transform, Help, 19, new Color(0.92f, 0.92f, 0.9f));
        text.lineSpacing = 1.12f;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        helpContent = text.rectTransform;
        helpContent.anchorMin = new Vector2(0f, 1f);
        helpContent.anchorMax = new Vector2(1f, 1f);
        helpContent.pivot = new Vector2(0.5f, 1f);
        helpContent.anchoredPosition = Vector2.zero;
        helpContent.sizeDelta = new Vector2(0f, text.preferredHeight + 20f);

        Text press = MakeText(box.transform, "<b>Press [Enter] to play</b>     <color=#999999>scroll for more</color>", 24, Color.white);
        press.alignment = TextAnchor.MiddleLeft;
        RectTransform pr = press.rectTransform;
        pr.anchorMin = new Vector2(0f, 0f);
        pr.anchorMax = new Vector2(1f, 0f);
        pr.pivot = new Vector2(0.5f, 0f);
        pr.sizeDelta = new Vector2(-80f, 60f);
        pr.anchoredPosition = new Vector2(0f, 20f);
    }

    void Update()
    {
        bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (showingTitle)
        {
            if (enter)
            {
                showingTitle = false;
                titlePage.SetActive(false);
                OpenHelp();
            }
            return;
        }

        if (helpPage.activeSelf)
        {
            // Content height can only be measured once the font is ready, so keep it fresh
            Text t = helpContent.GetComponent<Text>();
            helpContent.sizeDelta = new Vector2(0f, t.preferredHeight + 20f);

            float max = Mathf.Max(0f, helpContent.sizeDelta.y - helpViewport.rect.height);
            float step = Input.mouseScrollDelta.y * -60f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) step += 700f * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) step -= 700f * Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.PageDown)) step += helpViewport.rect.height * 0.9f;
            if (Input.GetKeyDown(KeyCode.PageUp)) step -= helpViewport.rect.height * 0.9f;
            helpScroll = Mathf.Clamp(helpScroll + step, 0f, max);
            helpContent.anchoredPosition = new Vector2(0f, helpScroll);

            if (enter || Input.GetKeyDown(KeyCode.H))
            {
                helpPage.SetActive(false);
                Time.timeScale = 1f;
            }
            return;
        }

        // H = help again (not over the ending screen)
        if (Input.GetKeyDown(KeyCode.H) && !EndingScreen.IsPlaying && Time.timeScale > 0f && !TypingInField()) OpenHelp();
    }

    // Don't steal H while typing (exam, login fields)
    static bool TypingInField()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        GameObject sel = es != null ? es.currentSelectedGameObject : null;
        return sel != null && (sel.GetComponent<InputField>() != null || sel.GetComponent<TMPro.TMP_InputField>() != null);
    }

    void OpenHelp()
    {
        helpPage.SetActive(true);
        Time.timeScale = 0f;
    }

    static Text MakeText(Transform parent, string content, int size, Color color)
    {
        Text text = CreateChild("Text", parent).AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.supportRichText = true;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.raycastTarget = false;
        text.text = content;
        return text;
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
