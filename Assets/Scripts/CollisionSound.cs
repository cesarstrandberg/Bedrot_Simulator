using UnityEngine;

public class CollisionSound : MonoBehaviour
{
    [Header("Impact Audio")]
    public AudioClip impactSound;
    private AudioSource audioSource;
    private Rigidbody rb;
     

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if(audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnCollissionEnter(Collision collission)
    {
        //Only play sound if the object hits something hard enough (velocity > 0.2)
        if (rb != null && rb.linearVelocity.magnitude > 0.2f && impactSound != null)
        {
            audioSource.PlayOneShot(impactSound);
        }
    }

}
