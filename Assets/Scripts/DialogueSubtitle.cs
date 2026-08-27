using System.Collections;
using UnityEngine;
using TMPro;

// Generic timed-subtitle player - shared by whichever NPC is "talking" at the moment
// (dealer for now, meant to double up for the planned LectureManager/NeighborAI later
// per the no-voice-acting subtitle pattern in CLAUDE.md).
public class DialogueSubtitle : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        [TextArea] public string text;
        public float duration = 2.5f;
        public float delayBefore = 0f; // Blank pause before this line shows, for pacing.
    }

    public TextMeshProUGUI subtitleText;

    public void Play(Line[] lines)
    {
        StopAllCoroutines();
        StartCoroutine(PlayRoutine(lines));
    }

    public void Stop()
    {
        StopAllCoroutines();
        if (subtitleText != null) subtitleText.text = "";
    }

    // Public so other scripts (e.g. DealerAI waiting out the farewell line before leaving)
    // can yield on it directly via StartCoroutine(dialogueSubtitle.PlayRoutine(lines)).
    public IEnumerator PlayRoutine(Line[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;

        foreach (Line line in lines)
        {
            if (subtitleText != null) subtitleText.text = "";

            if (line.delayBefore > 0f)
            {
                yield return new WaitForSeconds(line.delayBefore);
            }

            if (subtitleText != null) subtitleText.text = line.text;
            yield return new WaitForSeconds(line.duration);
        }

        if (subtitleText != null) subtitleText.text = "";
    }
}
