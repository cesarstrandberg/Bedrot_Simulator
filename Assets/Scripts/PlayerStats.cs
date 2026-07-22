using UnityEngine;
using UnityEngine.Rendering;

public class PlayerStats : MonoBehaviour
{
    [Header("High Effects")]
    public float highLevel = 0f; // Går från 0 (nykter) till 1+ (väldigt bäng)
    public Volume highVolume;    // Dra in din High_Volume från hierarkin hit!

    private PlayerMovement movementScript;

    void Start()
    {
        // Hämtar ditt rörelseskript automatiskt
        movementScript = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        // 1. Öka de visuella effekterna mjukt (Vignette & Grönt)
        if (highVolume != null)
        {
            highVolume.weight = Mathf.Lerp(highVolume.weight, Mathf.Clamp01(highLevel), Time.deltaTime * 0.5f);
        }

        // 2. Gör spelaren lite långsammare ju högre den är
        if (movementScript != null)
        {
            // Om highLevel är 0 = 100% fart. Om highLevel är 1 = 70% fart.
            float targetSpeed = Mathf.Lerp(1f, 0.7f, highLevel);
            movementScript.weedSpeedModifier = targetSpeed;
        }
    }

    public void SmokeJoint()
    {
        highLevel += 0.35f;
        Debug.Log("Joint rökt! Nuvarande highLevel: " + highLevel + " Fart sänkt!");
    }
}