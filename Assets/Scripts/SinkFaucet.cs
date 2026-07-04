using UnityEngine;

public class SinkFaucet : MonoBehaviour
{
    [Header("Sink Settings")]
    public GameObject waterStream;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip knobClickSound;
    public AudioSource waterLoopAudio;

    [Header("Model Swap (Valfritt för köket)")]
    public GameObject closedModel; // Modellen för AVSTÄNGD kran
    public GameObject openModel;   // Modellen för PÅSLAGEN kran

    private bool isWaterOn = false;

    void OnMouseDown()
    {
        isWaterOn = !isWaterOn;

        // 1. Spela klick/vrid-ljudet
        if (audioSource != null && knobClickSound != null)
        {
            audioSource.PlayOneShot(knobClickSound);
        }

        // 2. Slå på eller av vattenstrålen
        if (waterStream != null)
        {
            waterStream.SetActive(isWaterOn);
        }

        // 3. Slå på eller av det rinnande vattenljudet
        if (waterLoopAudio != null)
        {
            if (isWaterOn)
                waterLoopAudio.Play();
            else
                waterLoopAudio.Stop();
        }

        // 4. BYT MELLAN MODELLERNA (Om du har dragit in dem i Inspectorn)
        if (closedModel != null)
        {
            closedModel.SetActive(!isWaterOn); // Syns bara när vattnet är AV
        }
        if (openModel != null)
        {
            openModel.SetActive(isWaterOn);    // Syns bara när vattnet är PÅ
        }
    }
}