using UnityEngine;

public class FridgeDoor : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float minAngle = 0f;   // Open
    [SerializeField] private float maxAngle = 90f;  // Closed

    private float currentYRotation;
    private bool isDragging = false;

    void Start()
    {
        // Initialize the rotation to the door's starting local Y rotation
        currentYRotation = transform.localEulerAngles.y;

        // If it's slightly off in editor, force it to the closed state (90)
        currentYRotation = maxAngle;
        UpdateDoorRotation();
    }

    void OnMouseDown()
    {
        isDragging = true;
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    void Update()
    {
        if (isDragging)
        {
            // Get the mouse movement on the horizontal axis
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;

            // Adjust the rotation. Moving mouse left/right will open/close the door.
            // Depending on which way the fridge is facing, you might need to change -= to +=
            currentYRotation -= mouseX;

            // Clamp the rotation so the door can't swing past 0 or 90 degrees
            currentYRotation = Mathf.Clamp(currentYRotation, minAngle, maxAngle);

            UpdateDoorRotation();
        }
    }

    void UpdateDoorRotation()
    {
        // Apply the local rotation around the Y-axis
        transform.localRotation = Quaternion.Euler(0f, currentYRotation, 0f);
    }
}