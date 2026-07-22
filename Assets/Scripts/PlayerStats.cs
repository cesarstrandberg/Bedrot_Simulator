using UnityEngine;
using UnityEngine.Rendering;
using System.Collections; // NYTT: Behövs för sekvenser som tar tid (IEnumerator)

public class PlayerStats : MonoBehaviour
{
    [Header("High Effects (Weed)")]
    public float highLevel = 0f;
    public Volume highVolume;

    [Header("Drunk Effects (Alcohol)")]
    public float drunkLevel = 0f; // Går från 0 till 1 (5 öl)
    public Transform playerCamera;

    private PlayerMovement movementScript;
    private bool hasPassedOut = false; // Hindrar koden från att köras flera gånger

    void Start()
    {
        movementScript = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (hasPassedOut) return; // Kör inga vanliga effekter om vi redan ligger på marken

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

            // Z är klassiskt fyllegung (fram och tillbaka som en båt)
            float newZ = Mathf.Sin(Time.time * (1.5f + (drunkLevel * 2f))) * swayAmount;

            // X och Y använder Perlin Noise för helt slumpmässigt, ryckigt "snubbel-skak"
            // Vi subtraherar 0.5f så att det skakar både upp/ner och vänster/höger
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

        // 3. KOLLA OM VI SKA DÄCKA (5 öl ELLER 3 joints)
        if (drunkLevel >= 1.0f || highLevel >= 1.05f)
        {
            StartCoroutine(PassOutSequence());
        }
    }

    public void SmokeJoint()
    {
        highLevel += 0.35f;
    }

    public void DrinkBeer()
    {
        drunkLevel += 0.2f;
    }

    // ==========================================
    // SEKVENS: FALLA PÅ KNÄ OCH DÄCKA
    // ==========================================
    IEnumerator PassOutSequence()
    {
        hasPassedOut = true;
        Debug.Log("Däckar...");

        // 1. Stäng av spelarens kontroll
        if (movementScript != null) movementScript.enabled = false;

        MouseLook mouseLook = playerCamera.GetComponent<MouseLook>();
        if (mouseLook != null) mouseLook.isPassingOut = true;

        // 2. Tvinga kameran neråt (falla på knä) över 1.5 sekunder
        float elapsedTime = 0f;
        float fallDuration = 1.5f;

        Vector3 startPos = playerCamera.localPosition;
        Vector3 targetPos = new Vector3(startPos.x, startPos.y - 1.2f, startPos.z); // Kameran åker ner ca 1.2 meter

        Quaternion startRot = playerCamera.localRotation;
        Quaternion targetRot = Quaternion.Euler(75f, 0f, 0f); // Tittar 75 grader ner i marken

        while (elapsedTime < fallDuration)
        {
            // Lerp rör kameran mjukt mot målet
            playerCamera.localPosition = Vector3.Lerp(startPos, targetPos, elapsedTime / fallDuration);
            playerCamera.localRotation = Quaternion.Slerp(startRot, targetRot, elapsedTime / fallDuration);

            elapsedTime += Time.deltaTime;
            yield return null; // Vänta till nästa frame
        }

        // Se till att vi är exakt på rätt plats i slutet
        playerCamera.localPosition = targetPos;
        playerCamera.localRotation = targetRot;

        Debug.Log("Ligger på knä. Redo för spya!");

        // HÄR KOMMER VI LÄGGA KODEN FÖR ATT SKAPA SPY-PÖLEN NÄSTA GÅNG!
    }
}