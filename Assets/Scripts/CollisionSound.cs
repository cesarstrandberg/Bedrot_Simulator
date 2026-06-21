using UnityEngine;

public class CollisionSound : MonoBehaviour
{
    [Header("Impact Audio")]
    public AudioClip impactSound; // Här dyker din "slot" upp i Inspectorn! 

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
        if (impactSound != null && collision.relativeVelocity.magnitude > 0.2f)
        {
            // Lite extra kärlek: Variera pitchen minimalt så det låter mer naturligt varje gång den studsar
            audioSource.pitch = Random.Range(0.9f, 1.1f);

            audioSource.PlayOneShot(impactSound);
        }
    }
}