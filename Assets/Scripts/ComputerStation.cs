using System.Collections;
using UnityEngine;

public class ComputerStation : MonoBehaviour
{
    [Header("Current State")]
    public bool isAtPC = false;

    [Header("Cameras, UI & Player")]
    public GameObject mainCamera;
    public GameObject pcCamera;
    public GameObject pcMonitorCanvas;

    // NY LUCK FÖR DITT CROSSHAIR:
    public GameObject crosshairUI;

    public MonoBehaviour[] playerScriptsToDisable;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip pcLoopSound; // Endast ett loopande ljud nu!

    void Start()
    {
        if (pcCamera != null) pcCamera.SetActive(false);
        if (pcMonitorCanvas != null) pcMonitorCanvas.SetActive(false);
    }

    void Update()
    {
        if (!isAtPC) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitPC();
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnMouseDown() { TryStartPC(); }
    public void OnInteractableClicked(string objectTag) { TryStartPC(); }

    void TryStartPC()
    {
        if (!isAtPC) TurnOnPC();
    }

    public void TurnOnPC()
    {
        isAtPC = true;

        foreach (var script in playerScriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }

        if (mainCamera != null) mainCamera.SetActive(false);
        if (pcCamera != null) pcCamera.SetActive(true);
        if (pcMonitorCanvas != null) pcMonitorCanvas.SetActive(true);

        // STÄNG AV CROSSHAIRET!
        if (crosshairUI != null) crosshairUI.SetActive(false);

        // STARTA DET ENDA LOOPANDE LJUDET!
        if (audioSource != null && pcLoopSound != null)
        {
            audioSource.clip = pcLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitPC()
    {
        isAtPC = false;

        if (pcMonitorCanvas != null) pcMonitorCanvas.SetActive(false);
        if (pcCamera != null) pcCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        // SLÅ PÅ CROSSHAIRET IGEN!
        if (crosshairUI != null) crosshairUI.SetActive(true);

        foreach (var script in playerScriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }

        // STÄNG AV LJUDET NÄR DU RESER DIG
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}