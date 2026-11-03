using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    [Header("Knappens 3D-modeller")]
    public GameObject switchModelOff;
    public GameObject switchModelOn;

    [Header("Ljud")]
    public AudioSource switchSound; // <-- Här är din nya slot för ljudet!
    [Range(0f, 1f)] public float switchVolume = 0.15f;   // overrides the AudioSource's volume - was far too loud

    [Header("Lampor & Ljus")]
    public Light[] roomLights;
    public Renderer[] bulbModels;

    [Header("Material")]
    public Material materialOff;
    public Material materialOn;

    [Header("Startläge")]
    public bool isLightOnFromStart = false;

    private bool isCurrentlyOn;

    // The switch's AudioSource had Play On Awake on, so it clicked at full blast every time the
    // apartment loaded (game start, coming in from the city). Only a real flip should make a sound.
    void Awake() { SilenceOnLoad(); }

    void SilenceOnLoad()
    {
        // The kitchen switch has its AudioSource but the slot was never filled - use the one on itself.
        if (switchSound == null) switchSound = GetComponent<AudioSource>();
        if (switchSound == null) return;
        switchSound.playOnAwake = false;
        switchSound.Stop();
        switchSound.volume = switchVolume;
    }

    void Start()
    {
        SilenceOnLoad();
        isCurrentlyOn = isLightOnFromStart;
        UpdateSwitchAndLights();
    }

    void OnMouseDown()
    {
        // Flippa värdet
        isCurrentlyOn = !isCurrentlyOn;

        // --- SPELA LJUDET ---
        if (switchSound != null)
        {
            switchSound.volume = switchVolume;
            switchSound.Play();
        }

        UpdateSwitchAndLights();
    }

    void UpdateSwitchAndLights()
    {
        // Tänd rätt 3D-modell
        if (switchModelOn != null && switchModelOff != null)
        {
            switchModelOn.SetActive(isCurrentlyOn);
            switchModelOff.SetActive(!isCurrentlyOn);
        }

        // Tänd/Släck alla inkopplade Point Lights
        foreach (Light l in roomLights)
        {
            if (l != null)
            {
                l.enabled = isCurrentlyOn;
            }
        }

        // Byt material på glödlamporna
        foreach (Renderer r in bulbModels)
        {
            if (r != null)
            {
                r.material = isCurrentlyOn ? materialOn : materialOff;
            }
        }
    }
}
