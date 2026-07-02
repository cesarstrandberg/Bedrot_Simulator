using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    [Header("Knappens 3D-modeller")]
    public GameObject switchModelOff;
    public GameObject switchModelOn;

    [Header("Ljud")]
    public AudioSource switchSound; // <-- Här är din nya slot för ljudet!

    [Header("Lampor & Ljus")]
    public Light[] roomLights;
    public Renderer[] bulbModels;

    [Header("Material")]
    public Material materialOff;
    public Material materialOn;

    [Header("Startläge")]
    public bool isLightOnFromStart = false;

    private bool isCurrentlyOn;

    void Start()
    {
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
