using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Conversation box with numbered answers (press 1-4). Freezes the player while open.
// DialogueChoiceUI.Open("Dragan", "ID?", new[]{"Sure.", "No."}, pick => ...);  Close() when done.
public class DialogueChoiceUI : MonoBehaviour
{
    static DialogueChoiceUI instance;

    TextMeshProUGUI speakerText, lineText;
    readonly List<TextMeshProUGUI> optionTexts = new List<TextMeshProUGUI>();
    GameObject panel;
    Action<int> onPick;
    int optionCount;
    float openedAt;

    public static bool IsOpen => instance != null && instance.panel.activeSelf;

    public static void Open(string speaker, string line, string[] options, Action<int> pick)
    {
        if (instance == null) Create();
        instance.panel.SetActive(true);
        instance.speakerText.text = speaker.ToUpperInvariant();
        instance.lineText.text = line;
        instance.optionCount = options != null ? options.Length : 0;
        for (int i = 0; i < instance.optionTexts.Count; i++)
        {
            bool on = i < instance.optionCount;
            instance.optionTexts[i].gameObject.SetActive(on);
            if (on) instance.optionTexts[i].text = $"<color=#e8c56a>{i + 1}.</color>  {options[i]}";
        }
        instance.onPick = pick;
        instance.openedAt = Time.unscaledTime;
        SetPlayerLocked(true);
    }

    public static void Close()
    {
        if (instance == null) return;
        instance.panel.SetActive(false);
        instance.onPick = null;
        SetPlayerLocked(false);
    }

    void Update()
    {
        if (!panel.activeSelf || onPick == null) return;
        if (Time.unscaledTime - openedAt < 0.25f) return; // don't eat the key press that opened it
        for (int i = 0; i < optionCount && i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                var cb = onPick;
                onPick = null;
                cb(i);
                return;
            }
        }
        // No options = just a line; any of E/Space/1 continues.
        if (optionCount == 0 && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha1)))
        {
            var cb = onPick;
            onPick = null;
            cb(0);
        }
    }

    static void SetPlayerLocked(bool locked)
    {
        var move = FindFirstObjectByType<PlayerMovement>();
        if (move != null)
        {
            move.enabled = !locked && !LectureSeat.PlayerSeated;   // stay put if sitting in the lecture
            var rb = move.GetComponent<Rigidbody>();
            if (locked && rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
        var look = FindFirstObjectByType<MouseLook>();
        if (look != null) look.isPassingOut = locked;
        var hands = FindFirstObjectByType<PlayerInteraction>();
        if (hands != null) hands.enabled = !locked;
    }

    static void Create()
    {
        var go = new GameObject("DialogueChoiceUI");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DialogueChoiceUI>();
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        instance.panel = new GameObject("Panel", typeof(RectTransform));
        instance.panel.transform.SetParent(go.transform, false);
        var bg = instance.panel.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.03f, 0.04f, 0.82f);
        bg.raycastTarget = false;
        var prt = (RectTransform)instance.panel.transform;
        prt.anchorMin = new Vector2(0.5f, 0f); prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 40f);
        prt.sizeDelta = new Vector2(1100f, 330f);

        instance.speakerText = Text(prt, new Vector2(40f, -22f), 22, new Color(0.91f, 0.77f, 0.42f));
        instance.speakerText.fontStyle = FontStyles.Bold;
        instance.speakerText.characterSpacing = 6f;
        instance.lineText = Text(prt, new Vector2(40f, -58f), 30, Color.white);
        instance.lineText.rectTransform.sizeDelta = new Vector2(1020f, 100f);
        for (int i = 0; i < 4; i++)
        {
            var t = Text(prt, new Vector2(60f, -165f - i * 40f), 26, new Color(0.88f, 0.88f, 0.88f));
            instance.optionTexts.Add(t);
        }
        instance.panel.SetActive(false);
    }

    static TextMeshProUGUI Text(RectTransform parent, Vector2 pos, float size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        t.alignment = TextAlignmentOptions.TopLeft;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1000f, 40f);
        return t;
    }
}
