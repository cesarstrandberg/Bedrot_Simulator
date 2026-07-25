using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Money & Needs (0-100)")]
    public float money = 1500f;
    public float hunger = 0f;
    public float thirst = 0f;
    public float craving = 0f; // Abstinens / Sug efter weed

    [Header("High Effects (Weed)")]
    public float highLevel = 0f;
    public Volume highVolume;

    [Header("Drunk Effects (Alcohol)")]
    public float drunkLevel = 0f;
    public Transform playerCamera;
    public GameObject pukePrefab;
    public AudioClip pukeSound;

    [Header("Sleep & Fade UI")]
    public Image fadeImage;
    public float fadeDuration = 1.5f;
    public float sleepDuration = 4f;

    private PlayerMovement movementScript;
    private bool hasPassedOut = false;

    void Start()
    {
        movementScript = GetComponent<PlayerMovement>();
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    void Update()
    {
        if (hasPassedOut) return;

        // ==========================================
        // 1. BEHOV SOM TICKAR ÖVER TID
        // ==========================================
        // Om highLevel är över 0 får man munchies och hungern ökar 3 gånger snabbare!
        // Vanlig hunger ökar nu med 0.25 (tar över 6 min till max). Munchies (highLevel > 0) ökar med 1.0.
        float hungerRate = (highLevel > 0) ? 1.0f : 0.25f;
        hunger = Mathf.Clamp(hunger + (Time.deltaTime * hungerRate), 0f, 100f);

        // Törst ökar med 0.4 (tar drygt 4 minuter till max från noll)
        thirst = Mathf.Clamp(thirst + (Time.deltaTime * 0.4f), 0f, 100f);

        // Abstinens / Sug ökar med 0.5 (tar drygt 3 minuter innan man MÅSTE röka)
        craving = Mathf.Clamp(craving + (Time.deltaTime * 0.5f), 0f, 100f);

        // ==========================================
        // 2. WEED & ALKOHOL EFFEKTER
        // ==========================================
        if (highVolume != null)
        {
            highVolume.weight = Mathf.Lerp(highVolume.weight, Mathf.Clamp01(highLevel), Time.deltaTime * 0.5f);
        }

        if (movementScript != null)
        {
            float targetSpeed = Mathf.Lerp(1f, 0.7f, highLevel);
            movementScript.weedSpeedModifier = targetSpeed;
        }

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

        // 3. KOLLA OM VI SKA DÄCKA (Av fylla, weed, hunger eller törst!)
        if (drunkLevel >= 1.0f || highLevel >= 1.05f || thirst >= 100f)
        {
            StartCoroutine(PassOutSequence());
        }
    }

    public void SmokeJoint()
    {
        highLevel += 0.35f;
        craving = 0f; // NYTT: Abstinensen nollställs direkt när man tar en holk!
        Debug.Log("Joint rökt! Craving nollställd.");
    }

    public void DrinkBeer()
    {
        drunkLevel += 0.2f;
        thirst = Mathf.Clamp(thirst - 25f, 0f, 100f); // NYTT: En kall bärs släcker törsten med 25%!
        Debug.Log("Öl drucken! Törst minskad.");
    }

    // ========================================================
    // SEKVENS: YRSEL, FALL, SPYA, SIDLÄNGES, FADE & SOV
    // ========================================================
    IEnumerator PassOutSequence()
    {
        hasPassedOut = true;
        if (movementScript != null) movementScript.enabled = false;

        MouseLook mouseLook = playerCamera.GetComponent<MouseLook>();
        if (mouseLook != null) mouseLook.isPassingOut = true;

        Vector3 originalCamPos = playerCamera.localPosition;
        Quaternion originalCamRot = playerCamera.localRotation;

        float dizzynessTime = 0f;
        float dizzynessDuration = 5.0f;

        while (dizzynessTime < dizzynessDuration)
        {
            float progress = dizzynessTime / dizzynessDuration;
            float wildX = Mathf.Sin(dizzynessTime * 4f) * (20f * progress);
            float wildY = Mathf.Cos(dizzynessTime * 3f) * (30f * progress);
            float wildZ = Mathf.Sin(dizzynessTime * 6f) * (15f * progress);
            float downwardDroop = Mathf.Lerp(0f, 40f, progress);

            Quaternion targetDizzyRot = Quaternion.Euler(originalCamRot.eulerAngles.x + downwardDroop + wildX, originalCamRot.eulerAngles.y + wildY, wildZ);
            playerCamera.localRotation = Quaternion.Slerp(playerCamera.localRotation, targetDizzyRot, Time.deltaTime * 5f);

            dizzynessTime += Time.deltaTime;
            yield return null;
        }

        float elapsedTime = 0f;
        float fallDuration = 1.0f;

        Vector3 startPos = playerCamera.localPosition;
        Vector3 kneePos = new Vector3(startPos.x, startPos.y - 1.0f, startPos.z);
        Quaternion kneeRot = Quaternion.Euler(75f, 0f, 0f);

        while (elapsedTime < fallDuration)
        {
            playerCamera.localPosition = Vector3.Lerp(startPos, kneePos, elapsedTime / fallDuration);
            playerCamera.localRotation = Quaternion.Slerp(playerCamera.localRotation, kneeRot, elapsedTime / fallDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        playerCamera.localPosition = kneePos;
        playerCamera.localRotation = kneeRot;

        yield return new WaitForSeconds(0.3f);

        if (pukePrefab != null)
        {
            RaycastHit hit;
            Vector3 spawnPos;
            if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, 3f))
                spawnPos = hit.point + new Vector3(0f, 0.01f, 0f);
            else
                spawnPos = playerCamera.position + playerCamera.forward * 0.8f - new Vector3(0f, 0.5f, 0f);

            Quaternion flatRotation = Quaternion.Euler(90f, playerCamera.eulerAngles.y, 0f);
            Instantiate(pukePrefab, spawnPos, flatRotation);

            if (pukeSound != null) AudioSource.PlayClipAtPoint(pukeSound, spawnPos, 1.0f);
        }

        yield return new WaitForSeconds(1.5f);

        elapsedTime = 0f;
        float sleepFallDuration = 1.0f;
        Vector3 floorPos = new Vector3(kneePos.x, kneePos.y - 0.4f, kneePos.z);
        Quaternion floorRot = Quaternion.Euler(15f, 0f, 85f);

        while (elapsedTime < sleepFallDuration)
        {
            playerCamera.localPosition = Vector3.Lerp(kneePos, floorPos, elapsedTime / sleepFallDuration);
            playerCamera.localRotation = Quaternion.Slerp(kneeRot, floorRot, elapsedTime / sleepFallDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        playerCamera.localPosition = floorPos;
        playerCamera.localRotation = floorRot;

        elapsedTime = 0f;
        if (fadeImage != null)
        {
            Color imgColor = fadeImage.color;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                imgColor.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
                fadeImage.color = imgColor;
                playerCamera.localPosition = floorPos;
                playerCamera.localRotation = floorRot;
                yield return null;
            }
            imgColor.a = 1f;
            fadeImage.color = imgColor;
        }

        float sleepTimer = 0f;
        while (sleepTimer < sleepDuration)
        {
            sleepTimer += Time.deltaTime;
            playerCamera.localPosition = floorPos;
            playerCamera.localRotation = floorRot;
            yield return null;
        }

        // Nollställ allt efter man däckat!
        drunkLevel = 0f;
        highLevel = 0f;
        thirst = 0f;
        hunger = 30f; // Vakna upp lite hungrig
        craving = 0f;

        elapsedTime = 0f;
        if (fadeImage != null)
        {
            Color imgColor = fadeImage.color;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                imgColor.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                fadeImage.color = imgColor;
                playerCamera.localPosition = floorPos;
                playerCamera.localRotation = floorRot;
                yield return null;
            }
            imgColor.a = 0f;
            fadeImage.color = imgColor;
        }

        elapsedTime = 0f;
        float standUpDuration = 1.5f;
        while (elapsedTime < standUpDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = Mathf.SmoothStep(0f, 1f, elapsedTime / standUpDuration);
            playerCamera.localPosition = Vector3.Lerp(floorPos, originalCamPos, percent);
            playerCamera.localRotation = Quaternion.Slerp(floorRot, originalCamRot, percent);
            yield return null;
        }

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