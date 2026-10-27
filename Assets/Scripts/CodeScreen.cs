using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The big projector screen in the lecture hall. Builds a world-space panel the size of this object's
// RectTransform area and types code onto it like a live demo. LectureManager calls Show(title, code).
public class CodeScreen : MonoBehaviour
{
    public Vector2 sizeMeters = new Vector2(9.4f, 3.1f);
    public float charsPerSecond = 28f;

    TextMeshProUGUI titleText, codeText;
    Coroutine typing;

    void Awake() { Build(); }

    public void Show(string title, string code)
    {
        if (codeText == null) Build();
        titleText.text = title;
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(Type(code));
    }

    IEnumerator Type(string code)
    {
        string colored = Highlight(code);
        codeText.text = colored;
        codeText.maxVisibleCharacters = 0;
        codeText.ForceMeshUpdate();
        int total = codeText.textInfo.characterCount;
        float shown = 0f;
        while (shown < total)
        {
            shown += charsPerSecond * Time.deltaTime * Random.Range(0.5f, 1.6f);   // uneven, like typing
            codeText.maxVisibleCharacters = Mathf.Min(total, (int)shown);
            yield return null;
        }
        codeText.maxVisibleCharacters = total;
        typing = null;
    }

    // Light syntax colouring: keywords, strings, comments
    static string Highlight(string code)
    {
        var sb = new System.Text.StringBuilder();
        foreach (string rawLine in code.Split('\n'))
        {
            string line = rawLine;
            string comment = "";
            int c = line.IndexOf("//");
            if (c >= 0) { comment = "<color=#6A9955>" + line.Substring(c) + "</color>"; line = line.Substring(0, c); }

            line = System.Text.RegularExpressions.Regex.Replace(line, "\"[^\"]*\"", m => "<color=#CE9178>" + m.Value + "</color>");
            line = System.Text.RegularExpressions.Regex.Replace(line,
                @"\b(public|private|static|void|int|double|boolean|class|new|return|if|else|for|while|try|catch|finally|null|this|true|false)\b",
                m => "<color=#569CD6>" + m.Value + "</color>");
            sb.Append(line).Append(comment).Append('\n');
        }
        return sb.ToString();
    }

    void Build()
    {
        if (codeText != null) return;
        var canvasGO = new GameObject("CodeScreenCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = sizeMeters * 100f;
        rt.localScale = Vector3.one * 0.01f;

        var bg = new GameObject("Background").AddComponent<Image>();
        bg.transform.SetParent(canvasGO.transform, false);
        bg.color = new Color(0.09f, 0.09f, 0.11f);
        Stretch(bg.rectTransform, 0f);

        titleText = MakeText(canvasGO.transform, 22, new Color(0.6f, 0.6f, 0.65f));
        titleText.alignment = TextAlignmentOptions.TopLeft;
        Stretch(titleText.rectTransform, 18f);

        codeText = MakeText(canvasGO.transform, 26, new Color(0.86f, 0.86f, 0.86f));
        codeText.alignment = TextAlignmentOptions.TopLeft;
        Stretch(codeText.rectTransform, 18f);
        codeText.rectTransform.offsetMax = new Vector2(-18f, -58f);   // below the title
        codeText.textWrappingMode = TextWrappingModes.NoWrap;
        codeText.enableAutoSizing = true;   // long slides shrink to fit the screen
        codeText.fontSizeMin = 12f;
        codeText.fontSizeMax = 52f;
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
