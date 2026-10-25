using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The small "[E] Sell joint" prompt (Schedule I style): a rounded key cap + label that fades in just
// right of the crosshair while you look at an IInteractable, plus floating "+150 kr" text.
// PlayerInteraction calls Show() every frame it wants the prompt; it fades out on its own otherwise.
public class InteractPrompt : MonoBehaviour
{
    static InteractPrompt instance;

    CanvasGroup group;
    TextMeshProUGUI keyText, label;
    Image keyCap;
    RectTransform root;
    float lastShown = -1f;
    static Sprite rounded;

    public static void Show(string key, string text, bool available)
    {
        if (instance == null) Create();
        instance.lastShown = Time.unscaledTime;
        instance.keyText.text = key;
        instance.label.text = text;
        float a = available ? 1f : 0.45f;
        instance.label.color = new Color(1f, 1f, 1f, a);
        instance.keyCap.color = new Color(1f, 1f, 1f, 0.92f * a);
    }

    // Floating feedback like "+150 kr", rises and fades from just under the crosshair.
    public static void Float(string text, Color color)
    {
        if (instance == null) Create();
        instance.StartCoroutine(instance.FloatRoutine(text, color));
    }

    void Update()
    {
        bool want = Time.unscaledTime - lastShown < 0.08f;
        group.alpha = Mathf.MoveTowards(group.alpha, want ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        float s = Mathf.Lerp(0.9f, 1f, group.alpha);
        root.localScale = new Vector3(s, s, 1f);
    }

    static void Create()
    {
        var go = new GameObject("InteractPrompt");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<InteractPrompt>();
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var rootGO = new GameObject("Prompt", typeof(RectTransform));
        rootGO.transform.SetParent(go.transform, false);
        instance.root = rootGO.GetComponent<RectTransform>();
        instance.root.anchorMin = instance.root.anchorMax = new Vector2(0.5f, 0.5f);
        instance.root.pivot = new Vector2(0f, 0.5f);
        instance.root.anchoredPosition = new Vector2(38f, -6f);
        instance.root.sizeDelta = new Vector2(400f, 44f);
        instance.group = rootGO.AddComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;

        // soft dark backing so it reads on bright sidewalks
        var shadow = MakeImage("Shadow", instance.root, new Color(0f, 0f, 0f, 0.35f));
        var srt = shadow.rectTransform;
        srt.anchorMin = new Vector2(0f, 0.5f); srt.anchorMax = new Vector2(0f, 0.5f); srt.pivot = new Vector2(0f, 0.5f);
        srt.anchoredPosition = new Vector2(-4f, 0f);
        srt.sizeDelta = new Vector2(250f, 46f);

        instance.keyCap = MakeImage("Key", instance.root, Color.white);
        var krt = instance.keyCap.rectTransform;
        krt.anchorMin = krt.anchorMax = new Vector2(0f, 0.5f);
        krt.pivot = new Vector2(0f, 0.5f);
        krt.anchoredPosition = Vector2.zero;
        krt.sizeDelta = new Vector2(38f, 38f);

        instance.keyText = MakeText("KeyText", instance.keyCap.rectTransform, 24, new Color(0.08f, 0.08f, 0.09f));
        instance.keyText.fontStyle = FontStyles.Bold;
        instance.keyText.alignment = TextAlignmentOptions.Center;
        var ktr = instance.keyText.rectTransform;
        ktr.anchorMin = Vector2.zero; ktr.anchorMax = Vector2.one; ktr.offsetMin = ktr.offsetMax = Vector2.zero;

        instance.label = MakeText("Label", instance.root, 24, Color.white);
        instance.label.alignment = TextAlignmentOptions.MidlineLeft;
        var lrt = instance.label.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0f, 0.5f);
        lrt.pivot = new Vector2(0f, 0.5f);
        lrt.anchoredPosition = new Vector2(50f, 0f);
        lrt.sizeDelta = new Vector2(360f, 40f);

        // shrink the backing to fit the label once TMP has measured it
        instance.StartCoroutine(instance.FitBacking(srt));
    }

    IEnumerator FitBacking(RectTransform backing)
    {
        while (true)
        {
            backing.sizeDelta = new Vector2(50f + label.preferredWidth + 16f, 46f);
            yield return null;
        }
    }

    IEnumerator FloatRoutine(string text, Color color)
    {
        var t = MakeText("Float", (RectTransform)transform, 30, color);
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.outlineWidth = 0.18f;
        t.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        t.text = text;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 50f);
        float time = 0f;
        while (time < 1.4f)
        {
            time += Time.unscaledDeltaTime;
            rt.anchoredPosition = new Vector2(0f, -40f + time * 60f);
            float a = time < 0.15f ? time / 0.15f : Mathf.Clamp01((1.4f - time) / 0.6f);
            t.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        Destroy(t.gameObject);
    }

    static Image MakeImage(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = RoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI MakeText(string name, RectTransform parent, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    // 9-sliced rounded rectangle generated in code, so there's no sprite asset to wire up.
    static Sprite RoundedSprite()
    {
        if (rounded != null) return rounded;
        const int size = 32, radius = 10;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return rounded;
    }
}
