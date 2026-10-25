using System.Collections;
using System.Text;
using UnityEngine;

// Dragan, the doorman at Betsey Trotwood. Talk to him (E) to try to get in. Every answer nudges his
// suspicion up or down; how drunk/high you are sets the starting suspicion, changes what he asks,
// slurs your own answers and unlocks some... less helpful ones. Get it under the line and you're in
// (a night out happens off-screen: time passes, money goes, you come out drunker). Blow it and he's
// done with you for the night - push it too far and he shoves you.
public class Bouncer : MonoBehaviour, IInteractable
{
    public string speakerName = "Dragan";
    public float openHour = 18f;
    public float closeHour = 3f;
    public float nightOutCost = 250f;
    public float letInBelow = 3f;      // suspicion needed to get in
    public float shoveAbove = 5.5f;

    int rejectedOnDay = -1;
    bool talking;

    // ---------------------------------------------------------------- IInteractable
    public string PromptLabel => IsOpenNow() ? "Talk to the bouncer" : "Talk  <size=75%><color=#bbbbbb>(closed)</color></size>";
    public bool PromptAvailable => !talking;
    public void OnLookedAt(Transform player)
    {
        Vector3 d = player.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 3f);
    }
    public void Interact()
    {
        if (!talking) StartCoroutine(Conversation());
    }

    // ---------------------------------------------------------------- state
    float suspicion;
    float drunk, high;
    int pick;

    bool IsOpenNow()
    {
        if (DayClock.Instance == null) return true;
        float h = DayClock.Instance.Hour;
        return openHour > closeHour ? (h >= openHour || h < closeHour) : (h >= openHour && h < closeHour);
    }

    int Today => DayClock.Instance != null ? DayClock.Instance.Day : 1;

    IEnumerator Ask(string line, params string[] options)
    {
        pick = -1;
        for (int i = 0; i < options.Length; i++) options[i] = Slur(options[i]);
        DialogueChoiceUI.Open(speakerName, line, options, p => pick = p);
        while (pick < 0) yield return null;
    }

    IEnumerator Say(string line)
    {
        pick = -1;
        DialogueChoiceUI.Open(speakerName, line + "\n<size=70%><color=#999999>[E] continue</color></size>", null, p => pick = p);
        while (pick < 0) yield return null;
    }

    IEnumerator Conversation()
    {
        talking = true;
        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        drunk = stats != null ? stats.drunkLevel : 0f;
        high = stats != null ? stats.highLevel : 0f;

        if (!IsOpenNow())
        {
            yield return Say($"We open at {openHour:00}:00. Go home, sleep, come back looking like a person.");
            End(); yield break;
        }

        if (rejectedOnDay == Today || (rejectedOnDay == Today - 1 && DayClock.Instance != null && DayClock.Instance.Hour < closeHour))
        {
            yield return Say("We've done this already. Walk away.");
            End(); yield break;
        }

        suspicion = drunk * 3f + high * 2f + Random.Range(-0.4f, 0.4f);

        // ---- opener depends on the state you show up in
        if (drunk > 0.5f)
        {
            yield return Ask("Whoa, whoa. Stop there. How much have you had tonight?",
                "Two beers.",
                "Not enough!",
                "Honestly? Too much. One drink and I'm home.");
            if (pick == 0) suspicion += 1f;
            else if (pick == 1) suspicion += 2f;
            else suspicion -= 1.2f;
        }
        else if (high > 0.5f)
        {
            yield return Ask("...Why are you smiling at me like that?",
                "I'm just happy to be here.",
                "Is that a real moustache?",
                "Smiling? I'm not smiling. You're smiling.");
            if (pick == 0) suspicion -= 0.3f;
            else if (pick == 1)
            {
                yield return Ask("Excuse me?", "Nothing. It's great. Really suits you.", "I'm a big fan of your work.");
                suspicion += pick == 0 ? 0f : 1f;
            }
            else suspicion += 1.5f;
        }
        else
        {
            yield return Ask("Evening. ID?",
                "Sure, here you go.",
                "Come on man, I'm thirty.",
                "Do you know who I am?");
            if (pick == 1) { suspicion += 1f; yield return Say("Everyone's thirty. ID."); }
            else if (pick == 2)
            {
                yield return Ask("No. And neither do you, by the looks of it.",
                    "Fair. Sorry. Long day.",
                    "I'm on the list. Should be under... me.",
                    "I'll remember your face.");
                if (pick == 0) suspicion -= 1f;
                else if (pick == 1) { suspicion += 1f; yield return Say("There's no list. It's a pub."); }
                else suspicion += 3f;
            }
        }

        // ---- the ID look
        yield return Ask("Hm. Picture looks nothing like you.",
            "Had a rough few months.",
            "That's my brother's. I mean - mine. It's mine.",
            "It's the lighting.");
        if (pick == 0) suspicion -= 1f;
        else if (pick == 1) suspicion += 2f;

        // ---- last question
        string[] last = high > 0.5f
            ? new[] { "Whatever's cheapest.", "Do you guys have... snacks?", "Water. Lots of water." }
            : new[] { "Whatever's cheapest.", "Whisky. Neat. Like my life.", "Water, honestly." };
        yield return Ask("What're you drinking tonight, then?", last);
        if (pick == 0) suspicion -= 0.6f;
        else if (pick == 1) suspicion += high > 0.5f ? 1f : -0.3f;
        else suspicion += drunk > 0.5f ? -0.8f : 0.5f;

        // ---- verdict
        if (suspicion < letInBelow)
        {
            yield return Say("...Fine. In you go. Behave, or you'll meet me again on the way out.");
            DialogueChoiceUI.Close();
            yield return NightOut(stats);
        }
        else if (suspicion < shoveAbove)
        {
            rejectedOnDay = Today;
            yield return Say("Not tonight, friend. Go home.");
        }
        else
        {
            rejectedOnDay = Today;
            yield return Say("Right. Get lost.");
            DialogueChoiceUI.Close();
            Shove();
        }
        End();
    }

    void End()
    {
        DialogueChoiceUI.Close();
        talking = false;
    }

    // You got in. No interior yet, so the night happens behind a black screen.
    IEnumerator NightOut(PlayerStats stats)
    {
        var fade = FindFadeImage(stats);
        yield return FadeTo(fade, 1f, 0.8f);

        string[] nights =
        {
            "The music's too loud. Someone buys you a shot. You buy three back.",
            "You argue with a stranger about Tarantino for two hours. You're right. Nobody cares.",
            "You end up telling the bartender your entire life story. She charges you for it.",
            "You dance. You don't remember deciding to dance.",
        };
        Notify.Show(nights[Random.Range(0, nights.Length)], 4f);
        yield return new WaitForSeconds(4.5f);

        if (stats != null)
        {
            float cost = Mathf.Min(nightOutCost, stats.money);
            stats.money -= cost;
            stats.drunkLevel = Mathf.Min(stats.drunkLevel + 0.35f, 0.95f);
            stats.thirst = Mathf.Clamp(stats.thirst + 15f, 0f, 100f);
            Notify.Show($"-{cost:0} kr", 2.5f);
        }
        if (DayClock.Instance != null) DayClock.Instance.SkipHours(Random.Range(2.5f, 4f));

        yield return FadeTo(fade, 0f, 1f);
    }

    void Shove()
    {
        var move = FindFirstObjectByType<PlayerMovement>();
        if (move == null) return;
        var rb = move.GetComponent<Rigidbody>();
        Vector3 away = move.transform.position - transform.position; away.y = 0f;
        if (rb != null) rb.AddForce(away.normalized * 7f + Vector3.up * 2f, ForceMode.VelocityChange);
        InteractPrompt.Float("*shove*", new Color(1f, 0.55f, 0.5f));
    }

    // Slurs your answers the drunker you are (and the higher, a little).
    string Slur(string s)
    {
        float amount = Mathf.Clamp01(drunk - 0.3f) + Mathf.Clamp01(high - 0.6f) * 0.3f;
        if (amount <= 0f) return s;
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            if (c == 's' && Random.value < amount) sb.Append("sh");
            else if ("aeiou".IndexOf(c) >= 0 && Random.value < amount * 0.35f) sb.Append(c).Append(c);
            else sb.Append(c);
        }
        if (Random.value < amount * 0.5f) sb.Append("... *hic*");
        return sb.ToString();
    }

    static UnityEngine.UI.Image FindFadeImage(PlayerStats stats) => stats != null ? stats.fadeImage : null;

    static IEnumerator FadeTo(UnityEngine.UI.Image img, float target, float duration)
    {
        if (img == null) yield break;
        Color c = img.color; float start = c.a, t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(start, target, t / duration);
            img.color = c;
            yield return null;
        }
        c.a = target; img.color = c;
    }
}
