using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [Header("Dra in din Spelare/Kamera här!")]
    public Transform player;

    [Header("Dra in din nya DoorCompass här!")]
    public Transform doorCompass;

    [Header("Rotation Settings")]
    public float sensitivity = 3f;
    public float minAngle = 0f;
    public float maxAngle = 120f;

    private float currentYRotation;
    private float originalX;
    private float originalZ;
    private bool isDragging = false;

    void Start()
    {
        originalX = transform.localEulerAngles.x;
        originalZ = transform.localEulerAngles.z;
        currentYRotation = transform.localEulerAngles.y;

        if (currentYRotation > 180f) currentYRotation -= 360f;
        currentYRotation = Mathf.Clamp(currentYRotation, minAngle, maxAngle);
    }

    void OnMouseDown() { isDragging = true; }
    void OnMouseUp() { isDragging = false; }

    void Update()
    {
        if (isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;

            // Kollar vilken sida av dörrkarmen spelaren står på
            if (player != null && doorCompass != null)
            {
                // Drar en linje från kompassen till spelaren
                Vector3 directionToPlayer = player.position - doorCompass.position;

                // Jämför linjen med kompassens blå pil (forward)
                float side = Vector3.Dot(directionToPlayer, doorCompass.forward);

                // Om side är negativt står spelaren på baksidan (inuti rummet)
                if (side < 0)
                {
                    mouseX = -mouseX; // Spegelvänd musrörelsen!
                }
            }

            currentYRotation -= mouseX;
            currentYRotation = Mathf.Clamp(currentYRotation, minAngle, maxAngle);
            transform.localRotation = Quaternion.Euler(originalX, currentYRotation, originalZ);
        }
    }
}