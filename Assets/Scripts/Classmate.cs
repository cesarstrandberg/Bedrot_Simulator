using UnityEngine;

// Elias, the guy next to your seat in the lecture hall. A while after you sit down he leans over,
// whispers, slides a joint across the desk to you and sends you his dealer's number. Happens once
// per game (GameState.metClassmate). You can also start it yourself with E.
public class Classmate : MonoBehaviour, IInteractable
{
    public string classmateName = "Elias";
    public string dealerName = "Jonte";
    public Transform playerSeat;          // the joint lands on the desk in front of this
    public float leanOverAfter = 20f;     // seconds after you sit down

    float seatedTime;
    bool talking;
    StudentLookAt looker;

    public string PromptLabel => GameState.metClassmate ? "..." : "Talk";
    public bool PromptAvailable => !GameState.metClassmate && !talking;
    public void OnLookedAt(Transform player) { }

    public void Interact()
    {
        if (!GameState.metClassmate && !talking) Begin();
    }

    void Update()
    {
        if (GameState.metClassmate || talking) return;
        seatedTime = LectureSeat.PlayerSeated ? seatedTime + Time.deltaTime : 0f;
        if (seatedTime >= leanOverAfter) Begin();
    }

    Transform PlayerHead => Camera.main != null ? Camera.main.transform : null;

    void Begin()
    {
        talking = true;
        if (looker == null) looker = GetComponentInChildren<StudentLookAt>();
        if (looker == null)
        {
            Animator a = GetComponentInChildren<Animator>();
            if (a != null) looker = a.gameObject.AddComponent<StudentLookAt>();
        }
        if (looker != null && PlayerHead != null) looker.Look(PlayerHead, 0f, 60f);

        Say("Psst. Hey. You look like shit, man.", new[] { "Thanks.", "Didn't sleep.", "Shh. I'm listening." }, pick =>
        {
            string reply = pick switch
            {
                0 => "Ha. No, I mean it as a compliment. Kind of.",
                1 => "Yeah? Me neither. Nobody in here sleeps.",
                _ => "To Lennart? Nobody listens to Lennart. Lennart doesn't listen to Lennart.",
            };
            Say(reply, null, _ =>
            Say("This guy's killing me. Every week. Same voice. Same jokes about bartenders.", null, _ =>
            {
                DropJoint();
                Say("Here. For after. Or now. I'm not your mom.", new[] { "What is this?", "I don't smoke.", "...Thanks, man." }, pick2 =>
                {
                    string r2 = pick2 switch
                    {
                        0 => "What does it look like? Relax. It's good stuff. Takes the edge off all this.",
                        1 => "Nobody smokes. Until they do.",
                        _ => "Don't thank me. Thank me by not being boring.",
                    };
                    Say(r2, null, _ =>
                    Say($"If you like it, I know a guy. {dealerName}. I'll send you his number.", null, _ =>
                    Say("Don't call him before noon. He hates that. And don't call him 'man'.", null, _ => Finish())));
                });
            }));
        });
    }

    void Say(string line, string[] options, System.Action<int> next)
    {
        DialogueChoiceUI.Open($"{classmateName} (whispering)", line, options, next);
    }

    void DropJoint()
    {
        PlayerInteraction hands = FindFirstObjectByType<PlayerInteraction>();
        if (hands == null || hands.jointPrefab == null || playerSeat == null) return;

        // Desk surface just in front of your seat
        Vector3 spot = playerSeat.position + playerSeat.forward * 0.45f + Vector3.up * 1.2f;
        if (Physics.Raycast(spot, Vector3.down, out RaycastHit hit, 1f)) spot = hit.point + Vector3.up * 0.03f;
        else spot = playerSeat.position + playerSeat.forward * 0.45f + Vector3.up * 0.85f;

        Instantiate(hands.jointPrefab, spot, Quaternion.Euler(0f, playerSeat.eulerAngles.y + 80f, 90f));
    }

    void Finish()
    {
        DialogueChoiceUI.Close();
        talking = false;
        GameState.metClassmate = true;
        GameState.hasDealerNumber = true;
        if (looker != null) looker.Look(PlayerHead, 0f, 2f);   // one last look, then back to the lecture
        Notify.Show($"New contact: <b>{dealerName}</b>", 3.5f);
    }
}
