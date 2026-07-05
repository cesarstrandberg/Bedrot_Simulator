using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Bed : MonoBehaviour
{
    [Header("UI Settings")]
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    [Header("Audio Settings")]
    public AudioSource voiceAudioSource;
    public AudioClip sleepVoiceLine;

    [Header("Sleep Animation Settings")]
    public Transform sleepPoint;        // <-- NY! Dra in ditt nyskapade SleepPoint här!
    public float lieDownDuration = 2f;
    public float sleepDuration = 4f;

    [Header("Lock Player Settings")]
    public MonoBehaviour[] scriptsToDisableDuringSleep;

    private Transform cameraTransform;
    private Vector3 originalCamPos;     // Nu använder vi Världskoordinater!
    private Quaternion originalCamRot;
    private bool isSleeping = false;

    void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    void OnMouseDown()
    {
        if (!isSleeping && cameraTransform != null && sleepPoint != null)
        {
            StartCoroutine(SleepSequence());
        }
        else if (sleepPoint == null)
        {
            Debug.LogError("DU HAR GLÖMT ATT DRA IN ETT SLEEP POINT I SÄNGENS INSPECTOR!");
        }
    }

    IEnumerator SleepSequence()
    {
        isSleeping = true;

        // 0. Stäng av rörelseskript (PlayerMovement, MouseLook, HeadBob etc.)
        foreach (MonoBehaviour script in scriptsToDisableDuringSleep)
        {
            if (script != null) script.enabled = false;
        }

        // Spara kamerans exakta startposition i världen
        originalCamPos = cameraTransform.position;
        originalCamRot = cameraTransform.rotation;

        // 1. SPELA RÖST & GLID BORT TILL KUDDEN
        if (voiceAudioSource != null && sleepVoiceLine != null)
        {
            voiceAudioSource.PlayOneShot(sleepVoiceLine);
        }

        float elapsed = 0f;
        while (elapsed < lieDownDuration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.SmoothStep(0f, 1f, elapsed / lieDownDuration);

            // Glid från där spelaren står rakt till kudden!
            cameraTransform.position = Vector3.Lerp(originalCamPos, sleepPoint.position, percent);
            cameraTransform.rotation = Quaternion.Lerp(originalCamRot, sleepPoint.rotation, percent);
            yield return null;
        }

        // --- KAMERALÅS! Tvinga kameran att stanna på kudden ---
        cameraTransform.position = sleepPoint.position;
        cameraTransform.rotation = sleepPoint.rotation;


        // 2. TONA TILL SVART (Nu låser vi positionen varje frame så inget skript kan slita upp dig!)
        elapsed = 0f;
        if (fadeImage != null)
        {
            Color imgColor = fadeImage.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                imgColor.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadeImage.color = imgColor;

                // LÅS KAMERAN UNDER FADEN!
                cameraTransform.position = sleepPoint.position;
                cameraTransform.rotation = sleepPoint.rotation;

                yield return null;
            }
            imgColor.a = 1f;
            fadeImage.color = imgColor;
        }


        // 3. SOV I MÖRKER
        Debug.Log("Spelaren sover... 6 timmar passerar.");

        // Håll kameran fastlåst även medan vi väntar i mörkret
        float sleepTimer = 0f;
        while (sleepTimer < sleepDuration)
        {
            sleepTimer += Time.deltaTime;
            cameraTransform.position = sleepPoint.position;
            cameraTransform.rotation = sleepPoint.rotation;
            yield return null;
        }


        // 4. TONA TILL LJUST IGEN
        elapsed = 0f;
        if (fadeImage != null)
        {
            Color imgColor = fadeImage.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                imgColor.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                fadeImage.color = imgColor;

                // LÅS KAMERAN TILLS BILDEN ÄR BORTA!
                cameraTransform.position = sleepPoint.position;
                cameraTransform.rotation = sleepPoint.rotation;

                yield return null;
            }
            imgColor.a = 0f;
            fadeImage.color = imgColor;
        }


        // 5. STÄLL DIG UPP IGEN (Glid tillbaka från kudden till spelarens kropp)
        elapsed = 0f;
        while (elapsed < lieDownDuration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.SmoothStep(0f, 1f, elapsed / lieDownDuration);
            cameraTransform.position = Vector3.Lerp(sleepPoint.position, originalCamPos, percent);
            cameraTransform.rotation = Quaternion.Lerp(sleepPoint.rotation, originalCamRot, percent);
            yield return null;
        }
        cameraTransform.position = originalCamPos;
        cameraTransform.rotation = originalCamRot;


        // 6. SLÅ PÅ SKRIPTEN IGEN
        foreach (MonoBehaviour script in scriptsToDisableDuringSleep)
        {
            if (script != null) script.enabled = true;
        }

        isSleeping = false;
    }
}