using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A student sitting through the lecture. Holds a sitting pose for a while, sometimes does a short
// gesture (rubbing arm, disbelief, finger gun, pointing), then goes back to sitting. Everyone starts at a
// random point so the room never moves in sync. Uses StudentAnimator.controller (state names below).
public class SeatedStudent : MonoBehaviour
{
    [System.Serializable]
    public class Pose
    {
        public string state;
        public float weight;     // how often it gets picked, relative to the others
        public bool gesture;     // plays once, then back to a sitting pose
        public Pose(string s, float w, bool g) { state = s; weight = w; gesture = g; }
    }

    public Pose[] poses =
    {
        new Pose("Sitting Idle",         30f, false),
        new Pose("Sitting",              25f, false),
        new Pose("Sitting Talking",      12f, false),
        new Pose("Talking (1)",          10f, false),   // sitting + talking with hands
        new Pose("Sitting Rubbing Arm",  10f, true),
        new Pose("Sitting Disbelief",     6f, true),
        new Pose("Sitting Gun Motion",    5f, true),
        new Pose("Sitting And Pointing",  2f, true),   // the rare one
    };
    public Vector2 holdSeconds = new Vector2(8f, 20f);   // how long a sitting pose lasts

    Animator anim;
    public StudentLookAt Looker { get; private set; }

    // Every student in the room, so the lecture can make them all turn and stare
    public static readonly List<SeatedStudent> All = new List<SeatedStudent>();
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }
    readonly Dictionary<string, float> clipLength = new Dictionary<string, float>();

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null) return;
        Looker = anim.GetComponent<StudentLookAt>();
        if (Looker == null) Looker = anim.gameObject.AddComponent<StudentLookAt>();
        foreach (var clip in anim.runtimeAnimatorController.animationClips) clipLength[clip.name] = clip.length;
        anim.speed = Random.Range(0.9f, 1.1f);

        anim.Play(PickPose(false).state, 0, Random.value);
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        yield return new WaitForSeconds(Random.Range(0f, holdSeconds.y));   // spread everyone out
        while (true)
        {
            Pose p = PickPose(true);
            anim.CrossFadeInFixedTime(p.state, 0.4f);
            if (p.gesture)
            {
                float len = clipLength.TryGetValue(p.state, out float l) ? l : 4f;
                yield return new WaitForSeconds(len / anim.speed);
                anim.CrossFadeInFixedTime(PickPose(false).state, 0.4f);
            }
            yield return new WaitForSeconds(Random.Range(holdSeconds.x, holdSeconds.y));
        }
    }

    Pose PickPose(bool allowGestures)
    {
        float total = 0f;
        foreach (var p in poses) if (allowGestures || !p.gesture) total += p.weight;
        float r = Random.value * total;
        foreach (var p in poses)
        {
            if (!allowGestures && p.gesture) continue;
            r -= p.weight;
            if (r <= 0f) return p;
        }
        return poses[0];
    }
}
