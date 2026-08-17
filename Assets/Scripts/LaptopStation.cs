using System.Collections;
using UnityEngine;

public class LaptopStation : MonoBehaviour
{
    public enum LaptopStep
    {
        Idle,
        InsertUSB,
        PressPowerButton,
        LaptopActive
    }

    [Header("Current State")]
    public LaptopStep currentStep = LaptopStep.InsertUSB;
    public bool isAtLaptop = false;

    [Header("Cameras & Player")]
    public GameObject mainCamera;
    public GameObject laptopCamera;
    public MonoBehaviour[] playerScriptsToDisable;

    [Header("USB References")]
    public Transform usbStick;          // Själva 3D-modellen för USB-stickan
    public Transform usbPortTarget;      // Tomt GameObject vid laptopens port
    private Vector3 originalUsbPos;
    private Quaternion originalUsbRot;

    [Header("Power Button Reference")]
    public Transform powerButton;        // Laptopens power-knapp (valfritt om du vill animera/trycka ner den)

    [Header("Screen & UI")]
    public GameObject laptopScreenCanvas; // Skärm-Canvasen som tänds vid boot

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip enterStationSound;
    public AudioClip usbInsertSound;
    public AudioClip powerButtonClickSound;
    public AudioClip laptopBootSound;

    void Start()
    {
        if (laptopCamera != null) laptopCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        if (usbStick != null)
        {
            originalUsbPos = usbStick.localPosition;
            originalUsbRot = usbStick.localRotation;
        }

        ResetStationVisuals();
    }

    void Update()
    {
        if (!isAtLaptop) return;

        // Res dig upp med Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitLaptopStation();
            return;
        }

        // Håll muspekaren fri och synlig
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Startas när spelaren klickar på laptopen från rummet
    public void EnterLaptopStation()
    {
        isAtLaptop = true;

        foreach (var script in playerScriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }

        if (mainCamera != null) mainCamera.SetActive(false);
        if (laptopCamera != null) laptopCamera.SetActive(true);

        PlaySound(enterStationSound);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Master click handler för alla delar
    public void OnInteractableClicked(string objectTag)
    {
        if (!isAtLaptop) return;

        switch (currentStep)
        {
            case LaptopStep.InsertUSB:
                if (objectTag == "USBStick")
                {
                    PlaySound(usbInsertSound);
                    StartCoroutine(MoveTransform(usbStick, usbPortTarget.position, usbPortTarget.rotation, 0.25f));
                    currentStep = LaptopStep.PressPowerButton;
                    Debug.Log("USB ikopplad! Tryck på Power-knappen för att starta.");
                }
                break;

            case LaptopStep.PressPowerButton:
                if (objectTag == "PowerButton")
                {
                    PlaySound(powerButtonClickSound);
                    StartCoroutine(BootLaptopRoutine());
                }
                break;
        }
    }

    IEnumerator BootLaptopRoutine()
    {
        currentStep = LaptopStep.LaptopActive;

        if (laptopBootSound != null)
        {
            PlaySound(laptopBootSound);
            yield return new WaitForSeconds(0.6f);
        }

        if (laptopScreenCanvas != null)
        {
            laptopScreenCanvas.SetActive(true);
        }

        Debug.Log("Laptop igång och skärmen är tänd!");
    }

    public void ExitLaptopStation()
    {
        isAtLaptop = false;

        if (laptopCamera != null) laptopCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        foreach (var script in playerScriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ResetStationVisuals()
    {
        if (laptopScreenCanvas != null) laptopScreenCanvas.SetActive(false);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    IEnumerator MoveTransform(Transform target, Vector3 toPos, Quaternion toRot, float duration)
    {
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.position = Vector3.Lerp(startPos, toPos, percent);
            target.rotation = Quaternion.Lerp(startRot, toRot, percent);
            yield return null;
        }

        target.position = toPos;
        target.rotation = toRot;
        if (usbPortTarget != null) target.SetParent(usbPortTarget);
    }
}