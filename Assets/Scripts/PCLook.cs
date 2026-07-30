using UnityEngine;

public class PCLook : MonoBehaviour
{
    [Header("Look Settings")]
    public float lookSpeed = 3f;
    public float maxLookAngle = 35f;

    [Header("Zoom Settings (Scroll Wheel)")]
    public float minFOV = 20f;       // Hur inzoomat det kan bli max
    public float maxFOV = 60f;       // Hur utzoomat det kan bli max (sätt detta till din kameras vanliga FOV)
    public float zoomSpeed = 20f;    // Hur snabbt hjulet zoomar

    private float targetFOV;
    private Vector3 startRotation;
    private float currentX = 0f;
    private float currentY = 0f;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        targetFOV = cam.fieldOfView;
        startRotation = transform.localEulerAngles;
    }

    void Update()
    {
        // 1. ZOOM MED SCROLLHJULET
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            // Minus för att scroll uppåt ska zooma IN
            targetFOV -= scroll * zoomSpeed;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        }

        // Mjuk övergång till den nya inzoomningen
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, 10f * Time.deltaTime);

        // 2. KOLLA RUNT (Höger musknapp)
        if (Input.GetMouseButton(1))
        {
            currentX += Input.GetAxis("Mouse X") * lookSpeed;
            currentY -= Input.GetAxis("Mouse Y") * lookSpeed;

            currentX = Mathf.Clamp(currentX, -maxLookAngle, maxLookAngle);
            currentY = Mathf.Clamp(currentY, -maxLookAngle, maxLookAngle);
        }

        transform.localRotation = Quaternion.Euler(startRotation.x + currentY, startRotation.y + currentX, startRotation.z);
    }
}