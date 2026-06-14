using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 300f;
    public Transform playerBody;
    float xRotation = 0f;

    private Camera cam;

    [Header("Zoom Settings (Ctrl)")]
    private float defaultFOV;
    public float zoomFOV = 40f;

    [Header("Lean Settings (Q)")]
    private Vector3 defaultLocalPos;
    // X (Sidled), Y (Upp/Ner), Z (Framåt/Bakåt)
    // Ändrad till att sträcka sig 2.5 meter framåt!
    public Vector3 leanOffset = new Vector3(0f, -0.4f, 2.5f);

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        cam = GetComponent<Camera>();
        defaultFOV = cam.fieldOfView;
        defaultLocalPos = transform.localPosition;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);

        if (Input.GetKey(KeyCode.LeftControl))
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, zoomFOV, 10f * Time.deltaTime);
        }
        else
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, defaultFOV, 10f * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            // Ökade hastigheten från 5f till 8f för att hantera det längre avståndet snabbare
            transform.localPosition = Vector3.Lerp(transform.localPosition, defaultLocalPos + leanOffset, 8f * Time.deltaTime);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, defaultLocalPos, 8f * Time.deltaTime);
        }
    }
}