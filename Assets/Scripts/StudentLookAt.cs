using System.Collections;
using UnityEngine;

// Turns a Humanoid's head (and a bit of the upper body) toward a target on top of whatever clip is
// playing. Needs "IK Pass" on the animator layer. Sits next to the Animator (SeatedStudent adds it).
public class StudentLookAt : MonoBehaviour
{
    Animator anim;
    Transform target;
    float weight, targetWeight;

    void Awake() { anim = GetComponent<Animator>(); }

    // Look at t after delay seconds, for duration seconds, then ease back
    public void Look(Transform t, float delay, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(LookRoutine(t, delay, duration));
    }

    IEnumerator LookRoutine(Transform t, float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        target = t;
        targetWeight = 1f;
        yield return new WaitForSeconds(duration);
        targetWeight = 0f;
    }

    void Update()
    {
        weight = Mathf.MoveTowards(weight, targetWeight, Time.deltaTime * 2.5f);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (anim == null || target == null || weight <= 0.001f) return;
        anim.SetLookAtWeight(weight, 0.3f, 0.9f, 0f, 0.6f);
        anim.SetLookAtPosition(target.position);
    }
}
