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

    [Header("Wake up here on a new game")]
    public bool wakeUpOnNewGame = true;
    public Transform wakeUpPoint;       // where the player stands after getting out of bed
    public float wakeDarkness = 0.8f;   // black screen after the instructions close
    public float wakeStare = 1.2f;      // staring at the ceiling before getting up

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

        // Fresh game (not coming back in from the city): start lying in this bed
        if (wakeUpOnNewGame && !GameState.hasData && wakeUpPoint != null && cameraTransform != null && sleepPoint != null)
            StartCoroutine(WakeUpSequence());
    }

    IEnumerator WakeUpSequence()
    {
        isSleeping = true;

        // Put the player beside the bed, everything that moves the camera off
        PlayerMovement body = FindFirstObjectByType<PlayerMovement>();
        MouseLook look = cameraTransform.GetComponent<MouseLook>();
        PlayerInteraction hands = cameraTransform.GetComponentInParent<PlayerInteraction>();
        if (hands == null) hands = FindFirstObjectByType<PlayerInteraction>();
        if (body != null)
        {
            Rigidbody rb = body.GetComponent<Rigidbody>();
            body.transform.SetPositionAndRotation(wakeUpPoint.position, wakeUpPoint.rotation);
            if (rb != null) { rb.position = wakeUpPoint.position; rb.rotation = wakeUpPoint.rotation; rb.linearVelocity = Vector3.zero; }
            Physics.SyncTransforms();
            body.enabled = false;
        }
        if (look != null) look.enabled = false;
        if (hands != null) hands.enabled = false;

        Vector3 standLocalPos = cameraTransform.localPosition;
        cameraTransform.position = sleepPoint.position;
        cameraTransform.rotation = sleepPoint.rotation;

        // Lying here (ceiling view) while the title / instructions are up (timeScale 0). One frame
        // first so the panel (built the same frame) has paused the game.
        yield return null;
        while (Time.timeScale == 0f || InstructionsPanel.IsOpen)
        {
            cameraTransform.position = sleepPoint.position;
            cameraTransform.rotation = sleepPoint.rotation;
            yield return null;
        }

        // Eyes shut... open
        SetFade(1f);
        yield return new WaitForSeconds(wakeDarkness);
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            SetFade(1f - Mathf.SmoothStep(0f, 1f, t / fadeDuration));
            yield return null;
        }
        SetFade(0f);
        yield return new WaitForSeconds(wakeStare);

        // Get up: glide from the pillow to standing
        Vector3 fromPos = sleepPoint.position;
        Quaternion fromRot = sleepPoint.rotation;
        for (float t = 0f; t < lieDownDuration; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / lieDownDuration);
            Transform parent = cameraTransform.parent;
            Vector3 toPos = parent != null ? parent.TransformPoint(standLocalPos) : standLocalPos;
            Quaternion toRot = parent != null ? parent.rotation : Quaternion.identity;
            cameraTransform.position = Vector3.Lerp(fromPos, toPos, p);
            cameraTransform.rotation = Quaternion.Slerp(fromRot, toRot, p);
            yield return null;
        }
        cameraTransform.localPosition = standLocalPos;
        cameraTransform.localRotation = Quaternion.identity;

        if (body != null) body.enabled = true;
        if (look != null) look.enabled = true;
        if (hands != null) hands.enabled = true;
        isSleeping = false;
    }

    void SetFade(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
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
        // Never quite the same night: 5-7 hours.
        float hoursSlept = Random.Range(5f, 7f);
        Debug.Log("Spelaren sover... " + hoursSlept.ToString("0.0") + " timmar passerar.");
        if (DayClock.Instance != null) DayClock.Instance.SkipHours(hoursSlept);

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