using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Header("Jump & Crouch Settings")]
    public float jumpForce = 5f;
    private bool isCrouching = false;

    [Header("Ground snap (stairs/steps)")]
    // Walking up a step edge flung the capsule upward and it floated over the stairs in the lecture
    // hall. While walking (not jumping) with floor just below, leftover upward speed is removed and
    // you're pulled down onto it.
    public float snapDistance = 0.45f;   // how far below the capsule the floor can be and still snap
    public float snapAcceleration = 40f;
    private float lastJumpTime = -10f;

    [HideInInspector] 
    public float weedSpeedModifier = 1f; //modified by PlayerStats.

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
        rb.interpolation = RigidbodyInterpolation.Interpolate;   // smooth camera over steps (was off)
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
            lastJumpTime = Time.time;
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
        float currentSpeed = walkSpeed * weedSpeedModifier;

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

        SnapToGround();
    }

    void SnapToGround()
    {
        if (rb.isKinematic || Time.time - lastJumpTime < 0.4f) return;

        float halfHeight = (col != null ? col.height * 0.5f * transform.lossyScale.y : 1f);
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, Vector3.down, out hit, halfHeight + snapDistance, ~0, QueryTriggerInteraction.Ignore)) return;
        if (hit.normal.y < 0.5f) return;   // a wall, not a floor

        Vector3 v = rb.linearVelocity;
        if (v.y > 0f) rb.linearVelocity = new Vector3(v.x, 0f, v.z);

        // Floor is below the capsule but not touching it yet (just walked off a step) - pull down.
        if (hit.distance > halfHeight + 0.02f) rb.AddForce(Vector3.down * snapAcceleration, ForceMode.Acceleration);
    }
}