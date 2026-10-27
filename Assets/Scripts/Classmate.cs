using System.Collections;
using TMPro;
using UnityEngine;

// Elias, the guy next to your seat in the lecture hall. A while after you sit down he turns to you
// and just starts whispering (timed subtitle lines, no key presses, you can still look around),
// slides a joint onto the desk in front of you and gives you his dealer's number.
// Happens once per game (GameState.metClassmate).
public class Classmate : MonoBehaviour
{
    public string classmateName = "Elias";
    public string dealerName = "Jonte";
    public Transform playerSeat;          // the joint lands on the desk in front of this
    public float leanOverAfter = 15f;     // seconds after you sit down

    // Lines before the joint, then after it. {0} = dealer name.
    public string[] linesBefore =
    {
        "Psst. Hey.",
        "You look like shit, man. No offence.",
        "This guy's killing me. Every week. Same voice. Same joke about bartenders.",
        "You're the one who's always late, right? I respect that. Honestly.",
    };
    public string[] linesAfter =
    {
        "Here. For after. Or now. I'm not your mom.",
        "It's good stuff. Takes the edge off all... this.",
        "If you like it, I know a guy. {0}. I'm sending you his number.",
        "Don't call him before noon. He hates that.",
        "And don't call him 'man'. He hates that more.",
    };

    float seatedTime;
    bool started, interrupted;
    Coroutine talk;
    StudentLookAt looker;
    TextMeshProUGUI whisper;

    void Update()
    {
        if (started || GameState.metClassmate) return;
        seatedTime = LectureSeat.PlayerSeated ? seatedTime + Time.deltaTime : 0f;
        if (seatedTime >= leanOverAfter) { started = true; talk = StartCoroutine(Talk()); }
    }

    void OnEnable() { PlayerStats.JointSmoked += OnJointSmoked; }
    void OnDisable() { PlayerStats.JointSmoked -= OnJointSmoked; }

    // You lit it right here. He shuts up - one stunned line, then pretends he doesn't know you.
    void OnJointSmoked()
    {
        if (!started || interrupted || GameState.metClassmate) return;
        interrupted = true;
        if (talk != null) StopCoroutine(talk);
        StartCoroutine(Interrupted());
    }

    IEnumerator Interrupted()
    {
        whisper.text = "";
        yield return new WaitForSeconds(0.6f);
        yield return Say("...Bro. Not in HERE.");
        GameState.metClassmate = true;
        GameState.hasDealerNumber = true;
        yield return new WaitForSeconds(2f);
        Notify.Show($"{classmateName} sent you a contact: <b>{dealerName}</b>", 3.5f);
    }

    IEnumerator Talk()
    {
        BuildWhisper();
        Transform head = Camera.main != null ? Camera.main.transform : null;
        looker = GetComponentInChildren<StudentLookAt>();
        if (looker != null && head != null) looker.Look(head, 0f, 60f);
        yield return new WaitForSeconds(0.8f);

        foreach (string l in linesBefore) yield return Say(l);
        DropJoint();
        foreach (string l in linesAfter) yield return Say(string.Format(l, dealerName));

        GameState.metClassmate = true;
        GameState.hasDealerNumber = true;
        Notify.Show($"New contact: <b>{dealerName}</b>", 3.5f);
        if (looker != null && head != null) looker.Look(head, 0f, 1.5f);   // a last look, then back to the lecture
    }

    IEnumerator Say(string line)
    {
        whisper.text = $"<color=#9fb7c9><i>{classmateName} (whispering):</i></color> <i>{line}</i>";
        yield return new WaitForSeconds(1.4f + line.Length * 0.055f);
        whisper.text = "";
        yield return new WaitForSeconds(0.5f);
    }

    void DropJoint()
    {
        PlayerInteraction hands = FindFirstObjectByType<PlayerInteraction>();
        if (hands == null || hands.jointPrefab == null || playerSeat == null) return;

        // On the desk just in front of your seat, a little toward Elias' side
        Vector3 spot = playerSeat.position + playerSeat.forward * 0.45f - playerSeat.right * 0.15f + Vector3.up * 1.2f;
        if (Physics.Raycast(spot, Vector3.down, out RaycastHit hit, 1f)) spot = hit.point + Vector3.up * 0.03f;
        else spot = playerSeat.position + playerSeat.forward * 0.45f + Vector3.up * 0.85f;

        // Lying flat across the desk
        Instantiate(hands.jointPrefab, spot, Quaternion.Euler(0f, playerSeat.eulerAngles.y + 90f, 0f));
    }

    void BuildWhisper()
    {
        var go = new GameObject("WhisperSubtitle");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 41;
        var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        whisper = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        whisper.transform.SetParent(go.transform, false);
        whisper.fontSize = 30;
        whisper.alignment = TextAlignmentOptions.Bottom;
        whisper.outlineWidth = 0.2f;
        whisper.outlineColor = Color.black;
        whisper.raycastTarget = false;
        var rt = whisper.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 180f);   // just above the lecture subtitles
        rt.sizeDelta = new Vector2(1400f, 120f);
        whisper.text = "";
    }
}
