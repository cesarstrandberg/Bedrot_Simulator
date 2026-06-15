using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Header("Jump & Crouch Settings")]
    public float jumpForce = 5f;
    private bool isCrouching = false;

    private Rigidbody rb;
    private CapsuleCollider col;
    private Camera cam;

    private float startHeight;
    private float camStartY;
    private float x, z;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        cam = GetComponentInChildren<Camera>(); // Hittar kameran automatiskt

        // Sparar originalhöjden på collidern och kamerans position istället för scale
        if (col != null) startHeight = col.height;
        if (cam != null) camStartY = cam.transform.localPosition.y;
    }

    void Update()
    {
        // GetAxisRaw is instant (-1, 0, or 1), perfect for snappy CS movement
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");

        // Jump (Space)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Crouch (C - Toggle)
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
            if (isCrouching)
            {
                // Ändrar höjden på krocklådan
                if (col != null) col.height = startHeight * 0.5f;

                // Flyttar ner kameran
                if (cam != null) cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, camStartY - (startHeight * 0.25f), cam.transform.localPosition.z);

                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse); // Push down
            }
            else
            {
                // Återställer höjden på krocklådan
                if (col != null) col.height = startHeight;

                // Flyttar upp kameran igen
                if (cam != null) cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, camStartY, cam.transform.localPosition.z);
            }
        }
    }

    void FixedUpdate()
    {
        // Check if standing on ground (Uppdaterad till att mäta via krocklådans höjd istället för scale)
        float rayLength = (col != null ? col.height / 2f : 1f) + 0.2f;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayLength);

        // Determine speed
        float currentSpeed = walkSpeed;

        if (Input.GetKey(KeyCode.LeftShift)) // Sprint
        {
            currentSpeed *= sprintMultiplier;
        }
        else if (isCrouching) // Move slower while crouching
        {
            currentSpeed *= 0.5f;
        }

        // --- CS-STYLE SNAPPY MOVEMENT ---
        Vector3 targetDirection = (transform.right * x + transform.forward * z).normalized;
        Vector3 targetVelocity = targetDirection * currentSpeed;

        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 velocityChange = targetVelocity - currentVelocity;

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }
}