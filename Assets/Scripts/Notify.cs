using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Short on-screen messages ("Rent paid", "Sold a joint +150 kr"...). Call Notify.Show("text") from
// anywhere - builds its own overlay the first time and survives scene changes. Messages queue up and
// show one at a time.
public class Notify : MonoBehaviour
{
    static Notify instance;
    readonly Queue<(string text, float seconds)> queue = new Queue<(string, float)>();
    TextMeshProUGUI label;
    bool showing;

    public static void Show(string text, float seconds = 4f)
    {
        if (instance == null) Create();
        instance.queue.Enqueue((text, seconds));
        if (!instance.showing) instance.StartCoroutine(instance.Run());
    }

    static void Create()
    {
        var go = new GameObject("Notify");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Notify>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 30;
        text.alignment = TextAlignmentOptions.Top;
        text.raycastTarget = false;
        text.color = new Color(1f, 1f, 1f, 0f);
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        var rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -90f);
        rt.sizeDelta = new Vector2(1100f, 120f);
        instance.label = text;
    }

    IEnumerator Run()
    {
        showing = true;
        while (queue.Count > 0)
        {
            var (text, seconds) = queue.Dequeue();
            label.text = text;
            yield return FadeTo(1f, 0.25f);
            yield return new WaitForSecondsRealtime(seconds);
            yield return FadeTo(0f, 0.4f);
        }
        showing = false;
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float start = label.color.a, t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            label.color = new Color(1f, 1f, 1f, Mathf.Lerp(start, target, t / duration));
            yield return null;
        }
        label.color = new Color(1f, 1f, 1f, target);
    }
}
