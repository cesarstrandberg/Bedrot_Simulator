using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("High Effects (Weed)")]
    public float highLevel = 0f;
    public Volume highVolume;

    [Header("Drunk Effects (Alcohol)")]
    public float drunkLevel = 0f;
    public Transform playerCamera;
    public GameObject pukePrefab;

    // NYTT: Lucka för ljudet!
    public AudioClip pukeSound;

    private PlayerMovement movementScript;
    private bool hasPassedOut = false;

    void Start()
    {
        movementScript = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (hasPassedOut) return;

        // 1. WEED: Visuella effekter och slöare gång
        if (highVolume != null)
        {
            highVolume.weight = Mathf.Lerp(highVolume.weight, Mathf.Clamp01(highLevel), Time.deltaTime * 0.5f);
        }

        if (movementScript != null)
        {
            float targetSpeed = Mathf.Lerp(1f, 0.7f, highLevel);
            movementScript.weedSpeedModifier = targetSpeed;
        }

        // 2. ALKOHOL: Organiskt kameragung (Perlin Noise)
        if (drunkLevel > 0 && playerCamera != null)
        {
            float swayAmount = 4f * drunkLevel;
            float newZ = Mathf.Sin(Time.time * (1.5f + (drunkLevel * 2f))) * swayAmount;
            float newX = (Mathf.PerlinNoise(Time.time * 2f, 0f) - 0.5f) * swayAmount * 2f;
            float newY = (Mathf.PerlinNoise(0f, Time.time * 2f) - 0.5f) * swayAmount * 2f;

            MouseLook mouseLook = playerCamera.GetComponent<MouseLook>();
            if (mouseLook != null)
            {
                mouseLook.zSway = newZ;
                mouseLook.xSway = newX;
                mouseLook.ySway = newY;
            }
        }

        // 3. KOLLA OM VI SKA DÄCKA
        if (drunkLevel >= 1.0f || highLevel >= 1.05f)
        {
            StartCoroutine(PassOutSequence());
        }
    }

    public void SmokeJoint() { highLevel += 0.35f; }
    public void DrinkBeer() { drunkLevel += 0.2f; }

    // ==========================================
    // SEKVENS: YRSEL (5s), FALL PÅ KNÄ, SPYA, SOVA
    // ==========================================
    IEnumerator PassOutSequence()
    {
        hasPassedOut = true;
        Debug.Log("Däckar... Tappar balansen completely!");

        if (movementScript != null) movementScript.enabled = false;

        MouseLook mouseLook = playerCamera.GetComponent<MouseLook>();
        if (mouseLook != null) mouseLook.isPassingOut = true;

        Vector3 originalCamPos = playerCamera.localPosition;
        Quaternion originalCamRot = playerCamera.localRotation;

        // ========================================================
        // FAS 1: KÄMPA EMOT & YRSEL (5 SEKUNDER)
        // Gubben tittar ner, rycker upp, och tappar kontrollen
        // ========================================================
        float dizzynessTime = 0f;
        float dizzynessDuration = 5.0f;

        while (dizzynessTime < dizzynessDuration)
        {
            // Hur långt i sekvensen är vi? (Går från 0 till 1)
            float progress = dizzynessTime / dizzynessDuration;

            // Skapa galet, sjukt skak som blir mer intensivt mot slutet
            float wildX = Mathf.Sin(dizzynessTime * 4f) * (20f * progress); // Nickar till / k kastar huvudet
            float wildY = Mathf.Cos(dizzynessTime * 3f) * (30f * progress); // Svajar vilt i sidled
            float wildZ = Mathf.Sin(dizzynessTime * 6f) * (15f * progress); // Lutar huvudet okontrollerat

            // Mot slutet trycks huvudet allt mer neråt (gubben orkar inte hålla upp det)
            float downwardDroop = Mathf.Lerp(0f, 40f, progress);

            // Applicera rörelsen på kameran
            Quaternion targetDizzyRot = Quaternion.Euler(originalCamRot.eulerAngles.x + downwardDroop + wildX, originalCamRot.eulerAngles.y + wildY, wildZ);
            playerCamera.localRotation = Quaternion.Slerp(playerCamera.localRotation, targetDizzyRot, Time.deltaTime * 5f);

            dizzynessTime += Time.deltaTime;
            yield return null;
        }

        // ========================================================
        // FAS 2: FALL NER PÅ KNÄ
        // ========================================================
        Debug.Log("Faller ner på knä!");
        float elapsedTime = 0f;
        float fallDuration = 1.0f;

        Vector3 startPos = playerCamera.localPosition;
        Vector3 kneePos = new Vector3(startPos.x, startPos.y - 1.0f, startPos.z);
        Quaternion kneeRot = Quaternion.Euler(75f, 0f, 0f); // Tittar ner i marken

        while (elapsedTime < fallDuration)
        {
            playerCamera.localPosition = Vector3.Lerp(startPos, kneePos, elapsedTime / fallDuration);
            playerCamera.localRotation = Quaternion.Slerp(playerCamera.localRotation, kneeRot, elapsedTime / fallDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        playerCamera.localPosition = kneePos;
        playerCamera.localRotation = kneeRot;

        // ========================================================
        // FAS 3: SPYA OCH LJUD!
        // ========================================================
        yield return new WaitForSeconds(0.3f);

        if (pukePrefab != null)
        {
            RaycastHit hit;
            Vector3 spawnPos;

            if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, 3f))
            {
                spawnPos = hit.point + new Vector3(0f, 0.01f, 0f);
            }
            else
            {
                spawnPos = playerCamera.position + playerCamera.forward * 0.8f - new Vector3(0f, 0.5f, 0f);
            }

            // NYTT: Rotera modellen 90 grader så den ligger platt mot marken!
            // (Blir det upp-och-ner, byt 90f till -90f)
            Quaternion flatRotation = Quaternion.Euler(-90f, playerCamera.eulerAngles.y, 0f);
            Instantiate(pukePrefab, spawnPos, flatRotation);

            // NYTT: Spela upp spyljudet exakt där pölen landar!
            if (pukeSound != null)
            {
                AudioSource.PlayClipAtPoint(pukeSound, spawnPos, 1.0f);
            }
        }
        Debug.Log("BLEEEH! Spypöl skapad.");

        yield return new WaitForSeconds(1.5f); // Stirra på spyan och må dåligt

        // ========================================================
        // FAS 4: FALL SIDLÄNGES NER I PÖLEN & SOV
        // ========================================================
        Debug.Log("Fall sidlänges ner i sörjan...");
        elapsedTime = 0f;
        float sleepFallDuration = 1.0f;

        Vector3 floorPos = new Vector3(kneePos.x, kneePos.y - 0.4f, kneePos.z);
        Quaternion floorRot = Quaternion.Euler(15f, 0f, 85f); // Luta huvudet 85 grader sidlänges

        while (elapsedTime < sleepFallDuration)
        {
            playerCamera.localPosition = Vector3.Lerp(kneePos, floorPos, elapsedTime / sleepFallDuration);
            playerCamera.localRotation = Quaternion.Slerp(kneeRot, floorRot, elapsedTime / sleepFallDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // ========================================================
        // FAS 5: SOV I NÅGRA SEKUNDER & VAKNA UPP
        // ========================================================
        Debug.Log("Sover zzzZZZzzz... (Väntar 6 sekunder)");
        yield return new WaitForSeconds(6f);

        Debug.Log("Vaknar upp igen!");
        drunkLevel = 0f;
        highLevel = 0f;

        playerCamera.localPosition = originalCamPos;
        playerCamera.localRotation = originalCamRot;

        if (movementScript != null) movementScript.enabled = true;
        if (mouseLook != null)
        {
            mouseLook.isPassingOut = false;
            mouseLook.zSway = 0f;
            mouseLook.xSway = 0f;
            mouseLook.ySway = 0f;
        }

        hasPassedOut = false;
    }
}