using System.Collections;
using UnityEngine;

public class ComputerStation : MonoBehaviour
{
    [Header("Current State")]
    public bool isAtPC = false;

    [Header("Cameras, UI & Player")]
    public GameObject mainCamera;
    public GameObject pcCamera;
    public GameObject pcMonitorCanvas; // <-- Dra in din World Space Canvas här!
    public MonoBehaviour[] playerScriptsToDisable;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip pcBootSound;     // Pipet när du sätter dig
    public AudioClip pcHummingSound;  // Fläktljudet som surrar i bakgrunden

    void Start()
    {
        // Se till att datorkameran och skärmen är avstängda när spelet startar
        if (pcCamera != null) pcCamera.SetActive(false);
        if (pcMonitorCanvas != null) pcMonitorCanvas.SetActive(false);
    }

    void Update()
    {
        if (!isAtPC) return;

        // Tryck ESCAPE för att stänga ner datorn och ställa dig upp
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitPC();
            return;
        }

        // Håll muspekaren fri så att du kan klicka på shoppen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Klick-mottagare för 3D-världen (stödjer både crosshair-tag och direktklick)
    void OnMouseDown() { TryStartPC(); }
    public void OnInteractableClicked(string objectTag) { TryStartPC(); }

    void TryStartPC()
    {
        if (!isAtPC)
        {
            TurnOnPC();
        }
    }

    // ==========================================
    // SITT NER & STARTA DATORN
    // ==========================================
    public void TurnOnPC()
    {
        isAtPC = true;
        Debug.Log("Startar datorn... *BEEP*");

        // 1. Stäng av spelarens rörelser och slå på datorkameran
        foreach (var script in playerScriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }
        if (mainCamera != null) mainCamera.SetActive(false);
        if (pcCamera != null) pcCamera.SetActive(true);

        // 2. Tänd upp 3D-skärmen i rummet!
        if (pcMonitorCanvas != null) pcMonitorCanvas.SetActive(true);

        // 3. Spela upp ljuden
        if (audioSource != null)
        {
            if (pcBootSound != null) audioSource.PlayOneShot(pcBootSound);

            if (pcHummingSound != null)
            {
                audioSource.clip = pcHummingSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ==========================================
    // STÄNG NER & STÄLL DIG UPP
    // ==========================================
    public void ExitPC()
    {
        isAtPC = false;
        Debug.Log("Stänger ner datorn...");

        // 1. Släck skärmen, byt tillbaka till vanliga kameran och slå på rörelserna
        if (pcMonitorCanvas != null) pcMonitorCanvas.SetActive(false);
        if (pcCamera != null) pcCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        foreach (var script in playerScriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }

        // 2. Stäng av fläktljudet
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}