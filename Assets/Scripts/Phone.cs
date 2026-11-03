using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The player's phone. Builds itself (no scene wiring) and survives scene loads.
//   Tab            - raise / lower the phone (answers a ringing call)
//   Scroll wheel   - move the selection,  Left click - open,  Right click - back
// Story API (call from anywhere):
//   Phone.Text("Jonte", "you up?")                 - text message + popup on the right + buzz
//   Phone.AddContact("Jonte")                       - shows up in Contacts
//   Phone.IncomingCall("Jonte", lines, answered => ...)   - rings until answered or missed
//   Phone.OnCallContact = name => lines or null     - what happens when YOU call someone (null = no answer)
//   Phone.OnCallEnded = name => ...                 - after a call YOU made was answered and finished
// Call lines: "Jonte: text" or "You: text" - shown as subtitles.
public class Phone : MonoBehaviour
{
    public class Message { public string from, text; public int day; public float hour; public bool read; }
    public class Contact { public string name; public Color color; }

    static Phone instance;
    static readonly List<Message> messages = new List<Message>();
    static readonly List<Contact> contacts = new List<Contact>();
    public static Func<string, string[]> OnCallContact;
    public static Action<string> OnCallEnded;

    public static bool IsUp => instance != null && instance.up;
    public static bool Busy => instance != null && (instance.callRoutine != null || instance.ringing);

    // ---------- public API ----------

    public static void Text(string from, string text)
    {
        Ensure();
        AddContact(from);
        var m = new Message { from = from, text = text, read = false };
        if (DayClock.Instance != null) { m.day = DayClock.Instance.Day; m.hour = DayClock.Instance.Hour; }
        messages.Add(m);
        instance.Popup(from, "MESSAGES", text);
        instance.Play(instance.buzzClip, 0.3f);   // was 0.8 - too loud
        instance.Refresh();
    }

    public static void AddContact(string name)
    {
        Ensure();
        if (contacts.Exists(c => c.name == name)) return;
        contacts.Add(new Contact { name = name, color = ColorFor(name) });
        instance.Refresh();
    }

    public static void IncomingCall(string from, string[] lines, Action<bool> onEnd = null, float ringSeconds = 25f)
    {
        Ensure();
        AddContact(from);
        instance.StartCoroutine(instance.Ring(from, lines, onEnd, ringSeconds));
    }

    public static void ResetAll()
    {
        messages.Clear();
        contacts.Clear();
        OnCallContact = null;
        OnCallEnded = null;
        if (instance != null) { instance.page = Page.Home; instance.Refresh(); }
    }

    // ---------- setup ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { Ensure(); }

    static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("Phone");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Phone>();
        instance.Build();
    }

    enum Page { Home, Messages, Thread, Contacts, Call }
    Page page = Page.Home;
    int selection;
    string openThread;
    bool up, ringing;
    float raise;   // 0 = down, 1 = up
    Coroutine callRoutine;
    string callStatus = "";

    Transform phoneModel, screenRoot;
    TextMeshProUGUI headerText, bodyText, subtitle;
    RectTransform popupArea;
    readonly List<RectTransform> popups = new List<RectTransform>();
    AudioSource audioSource, ringSource;
    AudioClip buzzClip, ringClip, beepClip;
    static Sprite rounded, circle;

    readonly Vector3 shownPos = new Vector3(0.085f, -0.065f, 0.23f);
    readonly Vector3 hiddenPos = new Vector3(0.085f, -0.36f, 0.2f);
    readonly Quaternion heldRot = Quaternion.Euler(-12f, 172f, 0f);   // screen toward your face, slight tilt

    void Build()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        ringSource = gameObject.AddComponent<AudioSource>();
        ringSource.spatialBlend = 0f;
        ringSource.loop = true;
        buzzClip = MakeBuzz();
        ringClip = MakeRing();
        beepClip = MakeBeep();

        BuildOverlay();
        SceneManager.sceneLoaded += (s, m) => AttachModel();
        AttachModel();
    }

    void AttachModel()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (phoneModel == null)
        {
            var prefab = Resources.Load<GameObject>("Phone");
            if (prefab == null) return;
            phoneModel = Instantiate(prefab).transform;
            DontDestroyOnLoad(phoneModel.gameObject);
            BuildScreen(phoneModel.Find("ScreenAnchor"));
        }
        phoneModel.SetParent(cam.transform, false);
        phoneModel.localRotation = heldRot;
        phoneModel.localPosition = Vector3.Lerp(hiddenPos, shownPos, raise);
        phoneModel.gameObject.SetActive(raise > 0.01f);
        Refresh();
    }

    // ---------- input ----------

    void Update()
    {
        if (phoneModel == null || phoneModel.parent == null) AttachModel();
        bool blocked = DialogueChoiceUI.IsOpen || EndingScreen.IsPlaying;

        if (!blocked && Input.GetKeyDown(KeyCode.Tab))
        {
            if (ringing) answerPressed = true;
            else SetUp(!up);
        }

        // slide in/out
        raise = Mathf.MoveTowards(raise, up ? 1f : 0f, Time.deltaTime * 4f);
        if (phoneModel != null)
        {
            float t = Mathf.SmoothStep(0f, 1f, raise);
            phoneModel.localPosition = Vector3.Lerp(hiddenPos, shownPos, t);
            phoneModel.gameObject.SetActive(raise > 0.01f);
        }

        if (up && !blocked) HandleScreenInput();
        if (up && DayClock.Instance != null && Time.frameCount % 30 == 0) Refresh();   // keep the clock ticking

        LayoutPopups();

#if UNITY_EDITOR
        // Test keys (editor only): F7 = a text, F8 = an incoming call
        if (Input.GetKeyDown(KeyCode.F7)) Text("Jonte", "you up? need to talk about the thing");
        if (Input.GetKeyDown(KeyCode.F8))
            IncomingCall("Jonte", new[] { "Jonte: Yo. Elias' friend?", "You: ...Yeah.", "Jonte: Good. Don't call me before noon.", "Jonte: I'll text you." });
#endif
    }

    void SetUp(bool value)
    {
        up = value;
        var hands = FindFirstObjectByType<PlayerInteraction>();
        if (hands != null) hands.enabled = !up;   // so clicks work the phone, not your hands
        if (!up && callRoutine == null) page = Page.Home;
        selection = 0;
        Refresh();
    }

    void HandleScreenInput()
    {
        if (page == Page.Call) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        int count = RowCount();
        if (scroll < 0f) selection = Mathf.Min(selection + 1, Mathf.Max(0, count - 1));
        if (scroll > 0f) selection = Mathf.Max(selection - 1, 0);
        if (scroll != 0f) Refresh();

        if (Input.GetMouseButtonDown(0)) Open();
        if (Input.GetMouseButtonDown(1)) Back();
    }

    int RowCount()
    {
        switch (page)
        {
            case Page.Home: return 2;
            case Page.Messages: return Threads().Count;
            case Page.Contacts: return contacts.Count;
            default: return 0;
        }
    }

    void Open()
    {
        switch (page)
        {
            case Page.Home:
                page = selection == 0 ? Page.Messages : Page.Contacts;
                selection = 0;
                break;
            case Page.Messages:
                var threads = Threads();
                if (selection < threads.Count)
                {
                    openThread = threads[selection];
                    foreach (var m in messages) if (m.from == openThread) m.read = true;
                    page = Page.Thread;
                }
                break;
            case Page.Contacts:
                if (selection < contacts.Count) callRoutine = StartCoroutine(OutgoingCall(contacts[selection].name));
                break;
        }
        Refresh();
    }

    void Back()
    {
        if (page == Page.Thread) page = Page.Messages;
        else if (page == Page.Messages || page == Page.Contacts) page = Page.Home;
        else { SetUp(false); return; }
        selection = 0;
        Refresh();
    }

    // ---------- calls ----------

    bool answerPressed;

    IEnumerator Ring(string from, string[] lines, Action<bool> onEnd, float seconds)
    {
        while (callRoutine != null || ringing) yield return null;   // one call at a time
        ringing = true;
        answerPressed = false;
        RectTransform card = Popup(from, "INCOMING CALL", "<color=#9be29b>[Tab]</color> Answer", seconds);
        ringSource.clip = ringClip;
        ringSource.volume = 0.35f;
        ringSource.Play();

        float t = 0f;
        while (t < seconds && !answerPressed) { t += Time.deltaTime; yield return null; }
        ringSource.Stop();
        ringing = false;
        if (card != null) DismissPopup(card);

        if (!answerPressed)
        {
            var m = new Message { from = from, text = "<i>Missed call</i>", read = false };
            if (DayClock.Instance != null) { m.day = DayClock.Instance.Day; m.hour = DayClock.Instance.Hour; }
            messages.Add(m);
            Popup(from, "MISSED CALL", "Missed call");
            onEnd?.Invoke(false);
            yield break;
        }

        callRoutine = StartCoroutine(InCall(from, lines, onEnd));
    }

    IEnumerator OutgoingCall(string name)
    {
        string[] lines = OnCallContact?.Invoke(name);
        page = Page.Call;
        openThread = name;
        callStatus = "Calling...";
        Refresh();
        for (int i = 0; i < 3; i++) { Play(beepClip, 0.25f); yield return new WaitForSeconds(1.6f); }

        if (lines == null)
        {
            callStatus = "No answer";
            Refresh();
            yield return new WaitForSeconds(1.5f);
            callRoutine = null;
            page = Page.Contacts;
            Refresh();
            yield break;
        }
        yield return InCall(name, lines, answered => OnCallEnded?.Invoke(name));
    }

    IEnumerator InCall(string from, string[] lines, Action<bool> onEnd)
    {
        if (!up) SetUp(true);
        page = Page.Call;
        openThread = from;
        float start = Time.time;
        Coroutine timer = StartCoroutine(CallTimer(start));

        foreach (string raw in lines)
        {
            string line = raw;
            int colon = raw.IndexOf(':');
            if (colon > 0)
            {
                string who = raw.Substring(0, colon);
                string said = raw.Substring(colon + 1).Trim();
                string col = who == "You" ? "#9fb7c9" : "#d8c49a";
                line = $"<color={col}>{who}:</color> {said}";
            }
            subtitle.text = line;
            yield return new WaitForSeconds(1.6f + raw.Length * 0.055f);
            subtitle.text = "";
            yield return new WaitForSeconds(0.35f);
        }

        StopCoroutine(timer);
        callStatus = "Call ended";
        Refresh();
        Play(beepClip, 0.2f);
        yield return new WaitForSeconds(1.2f);
        callRoutine = null;
        page = Page.Home;
        SetUp(false);
        onEnd?.Invoke(true);
    }

    IEnumerator CallTimer(float start)
    {
        while (true)
        {
            int s = Mathf.FloorToInt(Time.time - start);
            callStatus = $"{s / 60:00}:{s % 60:00}";
            Refresh();
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ---------- screen ----------

    List<string> Threads()
    {
        var list = new List<string>();
        for (int i = messages.Count - 1; i >= 0; i--) if (!list.Contains(messages[i].from)) list.Add(messages[i].from);
        return list;
    }

    void Refresh()
    {
        if (headerText == null) return;
        string time = DayClock.Instance != null ? $"{Mathf.FloorToInt(DayClock.Instance.Hour):00}:{Mathf.FloorToInt(DayClock.Instance.Hour % 1f * 60f):00}" : "--:--";
        string Row(int i, string text) => i == selection ? $"<color=#ffffff><b>›  {text}</b></color>" : $"<color=#9a9aa2>   {text}</color>";

        switch (page)
        {
            case Page.Home:
            {
                int unread = messages.FindAll(m => !m.read).Count;
                string day = DayClock.Instance != null ? $"{Weekday(DayClock.Instance.Day)} · Day {DayClock.Instance.Day}" : "";
                headerText.text = $"<size=260%><b>{time}</b></size>\n<color=#9a9aa2>{day}</color>";
                bodyText.text = Row(0, unread > 0 ? $"Messages  <color=#e05555>({unread})</color>" : "Messages") + "\n\n" +
                                Row(1, "Contacts") + "\n\n\n" +
                                $"<size=75%><color=#77777f>Rent {Rent.amountDue:0} kr · Day {Rent.dueDay}</color></size>";
                break;
            }
            case Page.Messages:
            {
                headerText.text = $"<b>Messages</b>   <size=70%><color=#77777f>{time}</color></size>";
                var threads = Threads();
                var sb = new System.Text.StringBuilder();
                if (threads.Count == 0) sb.Append("<color=#77777f>No messages</color>");
                for (int i = 0; i < threads.Count; i++)
                {
                    Message last = messages.FindLast(m => m.from == threads[i]);
                    bool unread = messages.Exists(m => m.from == threads[i] && !m.read);
                    string preview = last.text.Length > 16 ? last.text.Substring(0, 16) + "..." : last.text;
                    sb.Append(Row(i, (unread ? "<color=#e05555>●</color> " : "") + threads[i]))
                      .Append($"\n<size=70%><color=#77777f>      {preview}</color></size>\n\n");
                }
                bodyText.text = sb.ToString();
                break;
            }
            case Page.Thread:
            {
                headerText.text = $"<b>{openThread}</b>";
                var sb = new System.Text.StringBuilder();
                var list = messages.FindAll(m => m.from == openThread);
                for (int i = Mathf.Max(0, list.Count - 6); i < list.Count; i++)
                    sb.Append($"<size=65%><color=#77777f>Day {list[i].day} {Mathf.FloorToInt(list[i].hour):00}:{Mathf.FloorToInt(list[i].hour % 1f * 60f):00}</color></size>\n")
                      .Append(list[i].text).Append("\n\n");
                bodyText.text = sb.ToString();
                break;
            }
            case Page.Contacts:
            {
                headerText.text = "<b>Contacts</b>";
                var sb = new System.Text.StringBuilder();
                if (contacts.Count == 0) sb.Append("<color=#77777f>No contacts</color>");
                for (int i = 0; i < contacts.Count; i++) sb.Append(Row(i, contacts[i].name)).Append("\n\n");
                bodyText.text = sb.ToString() + "<size=70%><color=#77777f>Click to call</color></size>";
                break;
            }
            case Page.Call:
                headerText.text = $"\n\n<size=160%><b>{openThread}</b></size>\n<color=#9be29b>{callStatus}</color>";
                bodyText.text = "";
                break;
        }
    }

    static string Weekday(int day) => new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }[(day - 1) % 7];

    void BuildScreen(Transform anchor)
    {
        if (anchor == null) return;
        var canvasGO = new GameObject("ScreenCanvas");
        canvasGO.transform.SetParent(anchor, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600f, 1060f);           // ~6.1 x 10.8 cm at this scale
        rt.localScale = Vector3.one * 0.0001f;
        screenRoot = canvasGO.transform;

        var bg = new GameObject("Wallpaper").AddComponent<Image>();
        bg.transform.SetParent(canvasGO.transform, false);
        bg.color = new Color(0.07f, 0.07f, 0.09f);
        Stretch(bg.rectTransform, 0f);

        headerText = MakeText(canvasGO.transform, 58, Color.white);
        headerText.alignment = TextAlignmentOptions.Top;
        headerText.rectTransform.anchorMin = new Vector2(0f, 0.62f);
        headerText.rectTransform.anchorMax = Vector2.one;
        headerText.rectTransform.offsetMin = new Vector2(30f, 0f);
        headerText.rectTransform.offsetMax = new Vector2(-30f, -40f);

        bodyText = MakeText(canvasGO.transform, 54, Color.white);
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.rectTransform.anchorMin = Vector2.zero;
        bodyText.rectTransform.anchorMax = new Vector2(1f, 0.62f);
        bodyText.rectTransform.offsetMin = new Vector2(36f, 30f);
        bodyText.rectTransform.offsetMax = new Vector2(-30f, -10f);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        Refresh();
    }

    // ---------- popups (right side, Schedule I style) ----------

    void BuildOverlay()
    {
        var go = new GameObject("PhoneOverlay");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        popupArea = new GameObject("Popups").AddComponent<RectTransform>();
        popupArea.SetParent(go.transform, false);
        popupArea.anchorMin = popupArea.anchorMax = new Vector2(1f, 1f);
        popupArea.pivot = new Vector2(1f, 1f);
        popupArea.anchoredPosition = new Vector2(-24f, -170f);
        popupArea.sizeDelta = new Vector2(420f, 600f);

        subtitle = MakeText(go.transform, 32, Color.white);
        subtitle.alignment = TextAlignmentOptions.Bottom;
        subtitle.outlineWidth = 0.2f;
        subtitle.outlineColor = Color.black;
        var srt = subtitle.rectTransform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 270f);
        srt.sizeDelta = new Vector2(1400f, 120f);
    }

    RectTransform Popup(string from, string kind, string text, float seconds = 6f)
    {
        var card = new GameObject("Popup").AddComponent<Image>();
        card.transform.SetParent(popupArea, false);
        card.sprite = Rounded();
        card.type = Image.Type.Sliced;
        card.color = new Color(0.1f, 0.1f, 0.12f, 0.94f);
        var rt = card.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(400f, 96f);
        rt.anchoredPosition = new Vector2(460f, 0f);   // starts off-screen, slides in

        // Contact icon: coloured circle with the first letter
        var icon = new GameObject("Icon").AddComponent<Image>();
        icon.transform.SetParent(rt, false);
        icon.sprite = Circle();
        icon.color = ColorFor(from);
        var irt = icon.rectTransform;
        irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.anchoredPosition = new Vector2(16f, 0f);
        irt.sizeDelta = new Vector2(60f, 60f);
        var letter = MakeText(irt, 30, Color.white);
        letter.text = $"<b>{from.Substring(0, 1).ToUpper()}</b>";
        letter.alignment = TextAlignmentOptions.Center;
        Stretch(letter.rectTransform, 0f);

        var title = MakeText(rt, 20, new Color(0.6f, 0.6f, 0.66f));
        title.text = $"<b><color=#ffffff><size=120%>{from}</size></color></b>   {kind}";
        title.alignment = TextAlignmentOptions.TopLeft;
        title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(92f, 0f);
        title.rectTransform.offsetMax = new Vector2(-12f, -10f);

        var body = MakeText(rt, 21, new Color(0.88f, 0.88f, 0.9f));
        body.text = text;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Ellipsis;
        body.rectTransform.anchorMin = Vector2.zero;
        body.rectTransform.anchorMax = new Vector2(1f, 0.55f);
        body.rectTransform.offsetMin = new Vector2(92f, 8f);
        body.rectTransform.offsetMax = new Vector2(-12f, 0f);

        popups.Insert(0, rt);
        StartCoroutine(PopupLife(rt, seconds));
        return rt;
    }

    IEnumerator PopupLife(RectTransform rt, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DismissPopup(rt);
    }

    void DismissPopup(RectTransform rt)
    {
        if (rt == null || !popups.Contains(rt)) return;
        popups.Remove(rt);
        StartCoroutine(SlideOut(rt));
    }

    IEnumerator SlideOut(RectTransform rt)
    {
        Vector2 from = rt.anchoredPosition;
        for (float t = 0f; t < 1f && rt != null; t += Time.deltaTime / 0.25f)
        {
            rt.anchoredPosition = Vector2.Lerp(from, new Vector2(460f, from.y), t);
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    void LayoutPopups()
    {
        for (int i = 0; i < popups.Count; i++)
        {
            if (popups[i] == null) continue;
            Vector2 target = new Vector2(0f, -i * 106f);
            popups[i].anchoredPosition = Vector2.Lerp(popups[i].anchoredPosition, target, Time.deltaTime * 12f);
        }
    }

    // ---------- helpers ----------

    static Color ColorFor(string name)
    {
        switch (name)
        {
            case "Jonte": return new Color(0.75f, 0.2f, 0.2f);
            case "Elias": return new Color(0.25f, 0.5f, 0.8f);
            case "Mamma": return new Color(0.85f, 0.45f, 0.65f);
            case "Landlord": return new Color(0.55f, 0.55f, 0.6f);
        }
        UnityEngine.Random.State s = UnityEngine.Random.state;
        UnityEngine.Random.InitState(name.GetHashCode());
        Color c = Color.HSVToRGB(UnityEngine.Random.value, 0.5f, 0.75f);
        UnityEngine.Random.state = s;
        return c;
    }

    void Play(AudioClip clip, float volume) { if (clip != null) audioSource.PlayOneShot(clip, volume); }

    static AudioClip MakeBuzz()
    {
        int rate = 44100; float len = 0.55f;
        var data = new float[(int)(rate * len)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate;
            bool on = t < 0.18f || (t > 0.3f && t < 0.48f);   // bzz-bzz
            float env = on ? 1f : 0f;
            data[i] = env * 0.5f * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 165f * t)) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 30f * t));
        }
        var clip = AudioClip.Create("PhoneBuzz", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip MakeRing()
    {
        int rate = 44100; float len = 2.6f;
        var data = new float[(int)(rate * len)];
        float[] notes = { 659f, 784f, 988f, 784f };   // a little marimba-ish loop
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate;
            int n = Mathf.FloorToInt(t / 0.18f);
            if (n >= 8) continue;   // then silence
            float nt = t - n * 0.18f;
            float env = Mathf.Exp(-nt * 14f);
            data[i] = env * 0.45f * Mathf.Sin(2f * Mathf.PI * notes[n % 4] * t);
        }
        var clip = AudioClip.Create("PhoneRing", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip MakeBeep()
    {
        int rate = 44100; float len = 0.9f;
        var data = new float[(int)(rate * len)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate;
            data[i] = 0.25f * Mathf.Sin(2f * Mathf.PI * 425f * t) * Mathf.Clamp01((len - t) * 20f) * Mathf.Clamp01(t * 50f);
        }
        var clip = AudioClip.Create("PhoneBeep", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static Sprite Rounded()
    {
        if (rounded != null) return rounded;
        int s = 64, r = 18;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Max(r - x, x - (s - 1 - r), 0f), dy = Mathf.Max(r - y, y - (s - 1 - r), 0f);
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        rounded = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        return rounded;
    }

    static Sprite Circle()
    {
        if (circle != null) return circle;
        int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(31.5f - d)));
            }
        tex.Apply();
        circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return circle;
    }

    static TextMeshProUGUI MakeText(Transform parent, float size, Color color)
    {
        var t = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform r, float margin)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(margin, margin);
        r.offsetMax = new Vector2(-margin, -margin);
    }
}
