using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Jonte's week. Builds itself (survives scene loads), nothing to wire. Talks to the DealerAI in the
// apartment scene (the only dealer in the game).
//   - Once you have his number (Elias), from Day 2 14:00 he calls: first jar, on credit.
//   - You can call him for more (Phone contacts). Before noon he hangs up on you.
//   - A delivery asked for while you're out waits until you're back in the apartment.
//   - Day 6 evening: a text. Day 7 from FinalHour: he comes for you (DealerAI.StartFinalVisit) and
//     shoots you, whatever you did. If you're not home he texts and waits for you to come back.
public class JonteStory : MonoBehaviour
{
    public const string Name = "Jonte";
    public const string ApartmentScene = "ApartmentScene";

    public const int CallDay = 2;     public const float CallHour = 14f;
    public const int WarnDay = 6;     public const float WarnHour = 18f;
    public const int FinalDay = 7;    public const float FinalHour = 12f;   // the shot - not final, tune here
    const float DeliveryDelay = 12f;  // seconds after the call before he starts walking over

    static JonteStory instance;
    static bool called, warned, finalTexted, deliveryPending, orderOnCallEnd;
    static float deliverAt;

    public static void ResetAll()
    {
        called = warned = finalTexted = deliveryPending = orderOnCallEnd = false;
        deliverAt = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        if (instance != null) return;
        var go = new GameObject("JonteStory");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<JonteStory>();
        SceneManager.sceneLoaded += (s, m) => HookPhone();
        HookPhone();
    }

    // Phone.ResetAll() (new game) clears these, so set them again on every scene load
    static void HookPhone()
    {
        Phone.OnCallContact = OnCallContact;
        Phone.OnCallEnded = OnCallEnded;
    }

    static DealerAI Dealer => SceneManager.GetActiveScene().name == ApartmentScene ? DealerAI.Instance : null;

    static bool Reached(int day, float hour, int d, float h) => day > d || (day == d && hour >= h);

    void Update()
    {
        if (EndingScreen.IsPlaying || DayClock.Instance == null) return;
        int day = DayClock.Instance.Day;
        float hour = DayClock.Instance.Hour;

        // Day 7: he's coming. Everything else stops mattering.
        if (Reached(day, hour, FinalDay, FinalHour))
        {
            DealerAI dealer = Dealer;
            if (dealer != null) dealer.StartFinalVisit();   // false while a delivery is still out - retried
            else if (!finalTexted && GameState.hasDealerNumber && !Phone.Busy)
            {
                finalTexted = true;
                Phone.Text(Name, "I'm at your place. Where are you?");
            }
            return;
        }

        if (!called && GameState.hasDealerNumber && Reached(day, hour, CallDay, CallHour) && !Phone.Busy)
        {
            called = true;
            Phone.IncomingCall(Name, FirstCallLines(), answered =>
            {
                if (answered) OrderDelivery();
                else Phone.Text(Name, "Call me back. I don't call twice.");
            });
        }

        if (!warned && GameState.hasDealerNumber && Reached(day, hour, WarnDay, WarnHour) && !Phone.Busy)
        {
            warned = true;
            Phone.Text(Name, GameState.dealerDebt > 0f
                ? $"{GameState.dealerDebt:0}. Tomorrow. Be home."
                : "Tomorrow. Be home.");
        }

        if (deliveryPending && Time.time >= deliverAt)
        {
            DealerAI dealer = Dealer;
            if (dealer != null && dealer.CurrentState == DealerAI.DealerState.Idle)
            {
                deliveryPending = false;
                PrepareDeliveryLines(dealer);
                dealer.StartDelivery();
            }
        }
    }

    static void OrderDelivery()
    {
        deliveryPending = true;
        deliverAt = Time.time + DeliveryDelay;
    }

    // ---------- calls ----------

    static string[] FirstCallLines() => new[]
    {
        "Jonte: Yeah. Jonte. Elias gave you my number, so don't act surprised.",
        "You: ...Hey.",
        "Jonte: He says you liked it. I'm around your area. I'll swing by your place.",
        "You: I don't really have cash right now.",
        "Jonte: Relax. First one's on credit. You pay me when you pay me.",
        "Jonte: Within the week. I'm not a bank.",
    };

    // You calling him. null = he doesn't pick up.
    static string[] OnCallContact(string name)
    {
        orderOnCallEnd = false;
        if (name != Name) return null;

        int day = DayClock.Instance != null ? DayClock.Instance.Day : 1;
        float hour = DayClock.Instance != null ? DayClock.Instance.Hour : 12f;

        if (Reached(day, hour, FinalDay, FinalHour)) return null;   // he's already on his way

        if (hour < 12f)
            return new[] { "Jonte: ...It's not even noon.", "Jonte: Call me later." };

        DealerAI dealer = Dealer;
        if (deliveryPending || (dealer != null && dealer.CurrentState != DealerAI.DealerState.Idle))
            return new[] { "You: Hey, it's me.", "Jonte: I know. I'm on my way. Relax." };

        orderOnCallEnd = true;
        string tab = GameState.dealerDebt > 0f
            ? $"Jonte: You owe me {GameState.dealerDebt:0} already. Have something ready."
            : "Jonte: Fine. Same as before. On the tab.";
        return new[] { "Jonte: Yeah.", "You: Can you come by? Same as last time.", tab, "Jonte: I'll come to your place." };
    }

    static void OnCallEnded(string name)
    {
        if (name == Name && orderOnCallEnd) OrderDelivery();
        orderOnCallEnd = false;
    }

    // ---------- what he says ----------

    static DialogueSubtitle.Line L(string text, float delayBefore = 0f) =>
        new DialogueSubtitle.Line { text = text, duration = 1.4f + text.Length * 0.05f, delayBefore = delayBefore };

    // At the door, before you take the jar. DealerAI's speech code plays a second voice slice at
    // line index 2, so keep it 3 lines.
    public static void PrepareDeliveryLines(DealerAI dealer)
    {
        if (GameState.jarsFromDealer == 0)
            dealer.arrivalLines = new[]
            {
                L("So you're Elias' guy."),
                L("Two hundred. Not today - when you've got it. I remember everything."),
                L("Take it. I don't stand in hallways.", 3f),
            };
        else if (GameState.dealerDebt > 0f)
            dealer.arrivalLines = new[]
            {
                L($"You owe me {GameState.dealerDebt:0}. I'm taking what you've got."),
                L("The rest goes on the tab. The tab's getting long."),
                L("Take it. Come on.", 3f),
            };
        else
            dealer.arrivalLines = new[]
            {
                L("Same as always."),
                L("On the tab. Don't make it weird."),
                L("Take it.", 3f),
            };

        dealer.farewellLines = new[] { L("Be smart.", 0.4f) };
    }

    // Day 7, the last thing you hear. Same ending every time - only the words change.
    public static DialogueSubtitle.Line[] FinalLines()
    {
        if (!GameState.hasDealerNumber)
            return new[]
            {
                L("You don't know me."),
                L("Somebody gave me your name. That's all it takes."),
                L("Nothing personal.", 1f),
            };

        if (GameState.jarsFromDealer == 0)
            return new[]
            {
                L("Hey. Don't get up."),
                L("You never called. That's fine."),
                L("But you've got my number. I don't like people having my number."),
                L("Nothing personal.", 1f),
            };

        if (GameState.dealerDebt > 0f)
            return new[]
            {
                L("Hey. Don't get up."),
                L($"{GameState.dealerDebt:0} kroner. Seven days."),
                L("You know how many times I've heard 'tomorrow' this week?"),
                L("Me neither. I stopped counting."),
                L("Nothing personal.", 1f),
            };

        return new[]
        {
            L("Relax. You're square. You paid every krona."),
            L("That's the thing, though. You talk."),
            L("Your teacher knows my jacket. Your little friend knows my number."),
            L("Nothing personal.", 1f),
        };
    }

#if UNITY_EDITOR
    // Test: F9 sends Jonte for the final visit right now (apartment scene).
    // F10 = he calls and delivers a jar (gives you his number if you don't have it).
    void LateUpdate()
    {
        if (EndingScreen.IsPlaying) return;
        if (Input.GetKeyDown(KeyCode.F9))
        {
            DealerAI dealer = Dealer;
            if (dealer == null || !dealer.StartFinalVisit()) Notify.Show("[test] Jonte: not in the apartment / still delivering");
        }
        if (Input.GetKeyDown(KeyCode.F10))
        {
            GameState.hasDealerNumber = true;
            Phone.AddContact(Name);
            OrderDelivery();
            deliverAt = Time.time;
        }
    }
#endif
}
