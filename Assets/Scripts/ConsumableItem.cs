using UnityEngine;

public class ConsumableItem : MonoBehaviour
{
    [Header("Consumable Stats")]
    public float thirstRestore = 25f;
    public float hungerRestore = 0f;

    [Header("Post-Consume Settings")]
    public bool destroyOnConsume = false;

    [Header("Audio Clips")]
    public AudioClip openSound;
    public AudioClip burpSound;

    [Header("Visuals & Swapping")]
    public GameObject visualWithCap;
    public GameObject visualWithoutCap;
    public GameObject capPrefab;

    [Header("Animation Settings")]
    public float consumeDuration = 0.5f;
    public Vector3 mouthOffset = new Vector3(-0.3f, 0.1f, -0.4f);
    // NYTT: Hur mycket flaskan ska vinklas när du dricker (X, Y, Z i grader)
    public Vector3 mouthRotation = new Vector3(-80f, 0f, 0f);
}