using System.Collections;
using TMPro;
using UnityEngine;

// Runs the lecture in the LectureHall scene. It's already going when you walk in (you're late), the
// teacher calls you out, then talks through a set of slides as subtitles while CodeScreen types the
// code. About 7 minutes. Swaps between the teacher's talking clips so he never freezes in one gesture.
// Lines are in Slides below - edit freely; a line's duration is worked out from its length.
public class LectureManager : MonoBehaviour
{
    public Animator teacher;
    public CodeScreen screen;
    public string teacherName = "Lennart";
    public float gapBetweenLines = 0.6f;
    public float pauseBetweenSlides = 3f;

    [Header("Walks up to you when you come in late")]
    public float walkSpeed = 1.3f;
    public float stopDistance = 1.6f;   // how close he comes before stopping in front of you

    [Header("Voice (Resources/TeacherVoice.wav - gibberish, plays while each line is up)")]
    [Range(0f, 1f)] public float voiceVolume = 0.9f;
    // Comes from where he stands: full volume within voiceNearDistance, quieter further back
    // (about a third in the back row), so it's loudest when he's in your face.
    public float voiceNearDistance = 4f;
    AudioSource voice;
    AudioClip voiceClip;
    Coroutine voiceStop;

    static readonly string[] TalkStates = { "Talking", "Talking (2)" };   // "Talking (1)" is actually a sitting clip

    TextMeshProUGUI subtitle;
    Canvas subtitleCanvas;
    Coroutine lecture, gestures, facing;
    bool over, caught;
    Vector3 homePos;
    Quaternion homeRot;
    PlayerStats playerStats;

    static readonly string[] CaughtLines =
    {
        "...",
        "Is that...",
        "Is somebody smoking? In my lecture hall?",
        "Back row. The late one. Of course it's the late one.",
        "You know what? No. I'm not doing this. Eleven years.",
        "Lecture's over. Thank your friend in the back.",
    };
    static readonly string[] CaughtAfterLines =
    {
        "Really? I'm still in the room.",
        "Eleven years.",
    };

    class Slide
    {
        public string title, code;
        public string[] lines;
        public Slide(string t, string c, params string[] l) { title = t; code = c; lines = l; }
    }

    static readonly string[] LateRemark =
    {
        "Oh. Look who it is.",
        "No, no. Take your time. We're only halfway through.",
        "There's a seat in the back. There's always a seat in the back.",
    };

    static readonly Slide[] Slides =
    {
        new Slide("DA143A  Java I  —  Lecture 5: Strings",
"String a = \"hej\";\nString b = new String(\"hej\");\n\nSystem.out.println(a == b);      // false\nSystem.out.println(a.equals(b)); // true",
            "...and that's why you never, ever compare strings with two equals signs.",
            "Two equals signs asks: are you the same object? Not: do you look the same.",
            "Twins. Same face, different people. Java knows the difference. Half of you don't.",
            "Use dot equals. Write it on your hand. Tattoo it somewhere.",
            "I had a student last year. Smart kid. Wrote double equals in his exam twelve times.",
            "Twelve. I counted. I had time. My evenings are very open."),

        new Slide("Lecture 5: Loops",
"int[] grades = {85, 72, 91, 45, 60};\nint sum = 0;\n\nfor (int i = 0; i < grades.length; i++) {\n    sum += grades[i];\n}\n\ndouble avg = (double) sum / grades.length;\nSystem.out.println(\"Average: \" + avg);",
            "Right. Loops. The for-loop. The most honest thing in this language.",
            "It starts somewhere, it does the same thing over and over, and then it stops.",
            "Unlike most of you, it knows when to stop.",
            "Look at i. i starts at zero. Not one. Zero. Computers count like bartenders. Zero, one, two... last call.",
            "Less than grades dot length. Not less-than-or-equal. Put an equals sign there and you walk right off the end of the array.",
            "And what's waiting for you off the end of the array?",
            "ArrayIndexOutOfBoundsException. Long name. Short meaning. You went somewhere you weren't supposed to go.",
            "Then the cast. Double. Divide two ints and Java throws away the decimals. Doesn't ask. Doesn't apologize.",
            "Seventy point six becomes seventy. That point six is just gone.",
            "That's somebody's grade. That's somebody's CSN."),

        new Slide("Lecture 5: Classes and objects",
"public class Student {\n    private String name;\n    private int credits;\n\n    public Student(String name) {\n        this.name = name;\n        this.credits = 0;\n    }\n\n    public void attend() {\n        credits++;\n    }\n}",
            "Classes. A class is a blueprint. An object is the thing you build from it.",
            "Student is the class. Every one of you is an object. Some of you more than others.",
            "Private. Name and credits are private. Nobody outside the class touches them.",
            "That's called encapsulation. It means: mind your own business.",
            "Good rule in code. Good rule in life. Especially on this campus.",
            "this dot name equals name. 'This' means me. My name. Not yours. Mine.",
            "Credits starts at zero. Everybody starts at zero.",
            "And attend adds one. That's it. You show up, the number goes up.",
            "You don't show up...",
            "...well. You read the method. There's no else."),

        new Slide("Lecture 5: Null and exceptions",
"Student s = null;\n\ntry {\n    s.attend();\n} catch (NullPointerException e) {\n    System.out.println(\"Nobody's home.\");\n} finally {\n    System.out.println(\"Lecture continues.\");\n}",
            "Null. My favourite word in the language.",
            "Null means there's nothing there. You've got a name for a thing, and the thing doesn't exist.",
            "Like a phone number for someone who never picks up.",
            "Call a method on null and you get a NullPointerException. The program dies. Right there. No goodbye.",
            "Try, catch. You try something. If it goes wrong, you catch it. You deal with it.",
            "Some things you can't catch. Java calls those Errors. Capital E. Out of memory. Stack overflow.",
            "Those you just... live with.",
            "Finally. Finally always runs. Doesn't matter what happened in the try. Doesn't matter if you got caught.",
            "Finally runs. Every time. Remember that.",
            "Speaking of people who don't pick up. Administration asked me to mention something.",
            "Some of you have been, quote, 'conducting business' behind the C building.",
            "I don't care. I want to be very clear about that. I do not care.",
            "But the guy in the grey jacket by the bike racks? He's not a student. He has never been a student.",
            "I've been here eleven years. He's been at those bike racks for nine of them.",
            "Make of that what you will.",
            "Anyway."),

        new Slide("Lecture 5: Recursion",
"public static int countdown(int n) {\n    if (n == 0) {\n        return 0;   // the way out\n    }\n    System.out.println(n);\n    return countdown(n - 1);\n}",
            "Recursion. A method that calls itself.",
            "It goes deeper and deeper, and every time it promises it'll come back up.",
            "The base case. If n is zero, return. That's the way out.",
            "Every recursive method needs a way out. Write the way out first. Before anything else.",
            "Forget it, and the method keeps calling itself, deeper and deeper, until the stack runs out of room.",
            "Stack overflow. It doesn't crash because it was wrong. It crashes because it never stopped.",
            "There's a lesson in there. I'm not going to spell it out. I'm not your father."),

        new Slide("Lecture 5: Exam",
"// Exam portal: same format as always\n// 1. Read the code\n// 2. Memorize it\n// 3. Type it back. Exactly.\n\n// Spelling counts. Semicolons count.\n// Everything counts.",
            "Okay. The exam portal opens like always. Same format.",
            "You see the code, you memorize it, you type it back.",
            "Spelling counts. Semicolons count. Everything counts.",
            "Every week someone emails me. 'Lennart, I was one semicolon off.'",
            "Yes. You were.",
            "One semicolon is the difference between a program and a pile of words.",
            "That's it. Read chapter seven. Nobody ever reads chapter seven.",
            "...Go."),
    };

    void Start()
    {
        BuildSubtitle();
        BuildVoice();
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (teacher != null)
        {
            homePos = teacher.transform.position;
            homeRot = teacher.transform.rotation;
            teacher.applyRootMotion = false;   // the walk is moved by code, not by the clip
        }
        lecture = StartCoroutine(Run());
    }

    void OnEnable() { PlayerStats.JointSmoked += OnJointSmoked; }
    void OnDisable() { PlayerStats.JointSmoked -= OnJointSmoked; }

    void Update()
    {
        // Whispering with Elias: the lecture keeps going, you just miss it
        if (subtitleCanvas != null) subtitleCanvas.enabled = !DialogueChoiceUI.IsOpen;
    }

    // Someone lit up in the lecture hall. Everyone turns, Lennart stops.
    void OnJointSmoked()
    {
        if (caught) return;
        caught = true;
        Transform player = Camera.main != null ? Camera.main.transform : null;
        if (player == null) return;

        // The stare spreads outward from you, nearest first
        foreach (var s in SeatedStudent.All)
        {
            if (s.Looker == null) continue;
            float d = Vector3.Distance(s.transform.position, player.position);
            s.Looker.Look(player, 0.3f + d * 0.06f + Random.Range(0f, 0.4f), Random.Range(9f, 14f));
        }
        Notify.Show("Everyone is staring at you.", 3f);

        if (lecture != null) StopCoroutine(lecture);
        if (gestures != null) StopCoroutine(gestures);
        if (facing != null) StopCoroutine(facing);
        if (playerStats != null) playerStats.ReleaseFocus();   // in case he was right in your face
        StartCoroutine(Caught(player, over ? CaughtAfterLines : CaughtLines));
    }

    IEnumerator Caught(Transform player, string[] lines)
    {
        subtitle.text = "";
        yield return new WaitForSeconds(1.2f);

        // Lennart turns to face you
        if (teacher != null)
        {
            Transform t = teacher.transform;
            Vector3 dir = player.position - t.position; dir.y = 0f;
            Quaternion from = t.rotation, to = Quaternion.LookRotation(dir);
            for (float f = 0f; f < 1f; f += Time.deltaTime / 0.8f)
            {
                t.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, f));
                yield return null;
            }
            teacher.CrossFadeInFixedTime("Talking (2)", 0.3f);
        }

        foreach (string l in lines) yield return Say(l);
        subtitle.text = "";
        over = true;
        caught = false;   // a second joint gets the short version
        gestures = StartCoroutine(Gestures());
    }

    IEnumerator Run()
    {
        if (screen != null) screen.Show(Slides[0].title, Slides[0].code);

        // You walked in late: he stops mid-lecture, walks straight up to you and lets you have it
        // to your face (camera zoomed on him), then goes back to the front and carries on.
        yield return LateWalkUp();
        gestures = StartCoroutine(Gestures());

        for (int s = 0; s < Slides.Length; s++)
        {
            Slide slide = Slides[s];
            if (screen != null) screen.Show(slide.title, slide.code);
            if (s > 0) yield return new WaitForSeconds(pauseBetweenSlides);

            for (int i = 0; i < slide.lines.Length; i++)
                yield return Say(slide.lines[i]);
        }
        subtitle.text = "";
        over = true;
        Notify.Show("The lecture is over.", 3f);
    }

    IEnumerator LateWalkUp()
    {
        Transform player = Camera.main != null ? Camera.main.transform : null;
        if (teacher == null || player == null)
        {
            foreach (string l in LateRemark) yield return Say(l);
            yield break;
        }

        yield return new WaitForSeconds(0.4f);

        // First line on the way over
        teacher.CrossFadeInFixedTime("Walking", 0.3f);
        Coroutine first = StartCoroutine(Say(LateRemark[0]));
        yield return WalkTo(() => player.position, stopDistance);
        yield return first;

        // In your face for the rest
        teacher.CrossFadeInFixedTime("Talking (2)", 0.3f);
        if (playerStats != null) playerStats.FocusOn(teacher.transform);
        facing = StartCoroutine(KeepFacing(player));
        for (int i = 1; i < LateRemark.Length; i++) yield return Say(LateRemark[i]);
        StopCoroutine(facing);
        if (playerStats != null) playerStats.ReleaseFocus();

        // Back to the front of the room
        teacher.CrossFadeInFixedTime("Walking", 0.3f);
        yield return WalkTo(() => homePos, 0.05f);
        yield return TurnTo(homeRot, 0.6f);
        teacher.CrossFadeInFixedTime("Talking", 0.3f);
        yield return new WaitForSeconds(0.8f);
    }

    // Straight-line walk (the hall has no NavMesh; the floor between the stage and the doors is flat
    // and clear), keeping his feet on whatever floor is under him.
    IEnumerator WalkTo(System.Func<Vector3> target, float stopAt)
    {
        Transform t = teacher.transform;
        float started = Time.time;
        while (Time.time - started < 30f)
        {
            Vector3 to = target() - t.position;
            to.y = 0f;
            // Small margin: stepping exactly "what's left" left him a rounding error short forever,
            // walking on the spot next to his place.
            float left = to.magnitude - stopAt;
            if (left <= 0.02f) break;

            Quaternion look = Quaternion.LookRotation(to);
            t.rotation = Quaternion.Slerp(t.rotation, look, Time.deltaTime * 6f);
            float step = walkSpeed * Time.deltaTime;
            bool arriving = step >= left;
            Vector3 pos = t.position + to.normalized * Mathf.Min(step, left);
            pos.y = FloorHeight(pos, t.position.y);
            t.position = pos;
            if (arriving) break;
            yield return null;
        }
    }

    float FloorHeight(Vector3 at, float fallback)
    {
        RaycastHit[] hits = Physics.RaycastAll(at + Vector3.up * 1f, Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        foreach (RaycastHit h in hits)
        {
            if (h.transform.IsChildOf(teacher.transform)) continue;
            if (h.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (h.normal.y < 0.5f) continue;
            if (h.point.y > best) best = h.point.y;
        }
        return best > float.NegativeInfinity ? best : fallback;
    }

    IEnumerator KeepFacing(Transform player)
    {
        Transform t = teacher.transform;
        while (true)
        {
            Vector3 dir = player.position - t.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
            yield return null;
        }
    }

    IEnumerator TurnTo(Quaternion rot, float seconds)
    {
        Transform t = teacher.transform;
        Quaternion from = t.rotation;
        for (float f = 0f; f < 1f; f += Time.deltaTime / seconds)
        {
            t.rotation = Quaternion.Slerp(from, rot, Mathf.SmoothStep(0f, 1f, f));
            yield return null;
        }
        t.rotation = rot;
    }

    IEnumerator Say(string line)
    {
        subtitle.text = $"<color=#d8c49a>{teacherName}:</color> {line}";
        float duration = 1.6f + line.Length * 0.055f;
        Speak(line == "..." ? 0f : duration * 0.9f);
        yield return new WaitForSeconds(duration);
        subtitle.text = "";
        yield return new WaitForSeconds(gapBetweenLines);
    }

    void BuildVoice()
    {
        voiceClip = Resources.Load<AudioClip>("TeacherVoice");
        if (teacher == null || voiceClip == null) return;
        voice = teacher.gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.clip = voiceClip;
        voice.spatialBlend = 1f;
        voice.rolloffMode = AudioRolloffMode.Logarithmic;
        voice.minDistance = voiceNearDistance;
        voice.maxDistance = 40f;
        voice.dopplerLevel = 0f;
    }

    // Plays a random stretch of the long voice take for as long as the line is on screen.
    void Speak(float seconds)
    {
        if (voice == null || seconds <= 0f) return;
        if (voiceStop != null) StopCoroutine(voiceStop);
        voice.volume = voiceVolume;
        voice.time = Random.Range(0f, Mathf.Max(0f, voiceClip.length - seconds - 0.5f));
        voice.Play();
        voiceStop = StartCoroutine(StopVoiceAfter(seconds));
    }

    IEnumerator StopVoiceAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            voice.volume = Mathf.Lerp(voiceVolume, 0f, t / 0.12f);
            yield return null;
        }
        voice.Stop();
    }

    // Swap talking clips every so often so he keeps moving
    IEnumerator Gestures()
    {
        if (teacher == null) yield break;
        while (true)
        {
            teacher.CrossFadeInFixedTime(TalkStates[Random.Range(0, TalkStates.Length)], 0.5f);
            yield return new WaitForSeconds(Random.Range(6f, 12f));
        }
    }

    void BuildSubtitle()
    {
        var go = new GameObject("LectureSubtitle");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        subtitleCanvas = canvas;
        var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        subtitle = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        subtitle.transform.SetParent(go.transform, false);
        subtitle.fontSize = 34;
        subtitle.alignment = TextAlignmentOptions.Bottom;
        subtitle.color = Color.white;
        subtitle.outlineWidth = 0.2f;
        subtitle.outlineColor = Color.black;
        subtitle.raycastTarget = false;
        var rt = subtitle.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 90f);
        rt.sizeDelta = new Vector2(1400f, 200f);
        subtitle.text = "";
    }
}
