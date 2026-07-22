using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 300f;
    public Transform playerBody;
    float xRotation = 0f;

    // Tar emot organiskt gung från PlayerStats
    [HideInInspector] public float zSway = 0f;
    [HideInInspector] public float xSway = 0f;
    [HideInInspector] public float ySway = 0f;

    // NYTT: Stänger av musen när vi däckar
    [HideInInspector] public bool isPassingOut = false;

    private Camera cam;

    [Header("Zoom Settings (Ctrl)")]
    private float defaultFOV;
    public float zoomFOV = 40f;

    [Header("Lean Settings (Q)")]
    private Vector3 defaultLocalPos;
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
        // Om vi däckar, låt koden under ignoreras helt!
        if (isPassingOut) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Vi lägger till xSway (Upp/Ner skak) och ySway (Höger/Vänster skak)
        transform.localRotation = Quaternion.Euler(xRotation + xSway, ySway, zSway);
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
            transform.localPosition = Vector3.Lerp(transform.localPosition, defaultLocalPos + leanOffset, 8f * Time.deltaTime);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, defaultLocalPos, 8f * Time.deltaTime);
        }
    }
}