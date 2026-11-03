using UnityEngine;

public class CollisionSound : MonoBehaviour
{
    [Header("Impact Audio")]
    public AudioClip impactSound; // Här dyker din "slot" upp i Inspectorn!
    [Range(0f, 1f)] public float volume = 0.15f;   // was always full volume - way too loud
    public float cooldown = 0.25f;                 // stops walking on a bottle from spamming the sound

    private float lastPlayTime = -10f;

    private AudioSource audioSource;
    private Rigidbody rb;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // FIX 1: Vi måste faktiskt hämta din Rigidbody från objektet!
        rb = GetComponent<Rigidbody>();
    }

    // FIX 2: Rättat stavningen till "OnCollisionEnter" (ett 's') så Unity fattar interaktionen.
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // FIX 3: Vi kollar om vi har ett ljud, och använder collision.relativeVelocity 
        // för att mäta smällen istället för flaskans egen hastighet efter krocken.
        float speed = collision.relativeVelocity.magnitude;
        if (impactSound != null && speed > 0.2f && Time.time - lastPlayTime >= cooldown)
        {
            lastPlayTime = Time.time;

            // Lite extra kärlek: Variera pitchen minimalt så det låter mer naturligt varje gång den studsar
            audioSource.pitch = Random.Range(0.9f, 1.1f);

            // Soft knocks are quieter than hard drops (full volume at ~4 m/s and up).
            float loudness = volume * Mathf.Clamp(speed / 4f, 0.3f, 1f);
            audioSource.PlayOneShot(impactSound, loudness);
        }
    }
}