using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Header("Jump & Crouch Settings")]
    public float jumpForce = 5f;
    private float startYScale;
    private bool isCrouching = false;

    private Rigidbody rb;
    private float x, z;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startYScale = transform.localScale.y; // Store original height
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
                // Shrink player to half size
                transform.localScale = new Vector3(transform.localScale.x, startYScale * 0.5f, transform.localScale.z);
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse); // Push down to prevent floating
            }
            else
            {
                // Restore original height
                transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            }
        }
    }

    void FixedUpdate()
    {
        // Check if standing on the ground using a short raycast downwards
        isGrounded = Physics.Raycast(transform.position, Vector3.down, (transform.localScale.y) + 0.2f);

        // Determine speed based on sprinting or crouching state
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

        // 1. Calculate the exact direction and speed we WANT to go
        Vector3 targetDirection = (transform.right * x + transform.forward * z).normalized;
        Vector3 targetVelocity = targetDirection * currentSpeed;

        // 2. Look at how fast we are CURRENTLY going (ignoring up/down Y axis)
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // 3. Calculate the difference
        Vector3 velocityChange = targetVelocity - currentVelocity;

        // 4. Force the physics engine to apply that exact difference immediately
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }
}