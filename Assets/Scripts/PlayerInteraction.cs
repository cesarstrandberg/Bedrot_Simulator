using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f;
    public float throwForce = 10f; // The power when pressing 'F'
    public float dropForce = 2f; // The short toss when pressing 'E'
    public Transform holdPoint; // Where the object sits in your hand

    [Header("Audio Settings")]
    public AudioSource playerAudio; 


    private GameObject heldObject;
    private Rigidbody heldObjectRb;
    private Collider heldObjectCollider; //This variabels keeps tabs on the collider of the held object, so we can disable it while holding
    private Camera cam;

    void Start()
    {
        // Assumes this script is placed on the main camera
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        // Create a raycast from the center of the camera
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        // Draw a red line in the editor so you can see the laser while testing
        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red);

        // 1. Pick up or Drop (Press 'E')
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null) // If our hands are empty
            {
                if (Physics.Raycast(ray, out hit, interactRange))
                {
                    // Check if the object we hit is tagged as "Interactable"
                    if (hit.collider.CompareTag("Interactable"))
                    {
                        PickUpObject(hit.collider.gameObject);
                    }
                }
            }
            else // We are already holding something, so drop it (short toss)
            {
                DropObject();
            }
        }

        // 2. Throw the object hard (Press 'F')
        if (Input.GetKeyDown(KeyCode.F) && heldObject != null)
        {
            ThrowObject();
        }

        // 3. Use / Consume the object (Left Mouse Button)
        if (Input.GetMouseButtonDown(0) && heldObject != null)
        {
            TryConsumeObject();
        }
    }

    void PickUpObject(GameObject obj)
    {
        heldObject = obj;
        heldObjectRb = obj.GetComponent<Rigidbody>();
        heldObjectCollider = obj.GetComponent<Collider>(); // Get the collider of the held object

        // Turn off physics so the object doesn't fall out of our hand
        heldObjectRb.isKinematic = true;
        heldObjectCollider.enabled = false; // Disable the collider to prevent physics interactions while holding

        // Move the object to our HoldPoint and make it a child
        heldObject.transform.SetParent(holdPoint);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity; // Resets rotation
    }

    void DropObject()
    {
        // Turn physics back on
        heldObjectRb.isKinematic = false;
        heldObjectCollider.enabled = true; // Re-enable the collider when dropping

        // Remove it from the HoldPoint
        heldObject.transform.SetParent(null);

        // Add a small forward force to simulate a short drop/toss
        heldObjectRb.AddForce(cam.transform.forward * dropForce, ForceMode.Impulse);

        // Clear our variables
        heldObject = null;
        heldObjectRb = null;
        heldObjectCollider = null;
    }

    void ThrowObject()
    {
        // Turn physics back on
        heldObjectRb.isKinematic = false;
        heldObjectCollider.enabled = true; // Re-enable the collider when throwing

        // Remove it from the HoldPoint
        heldObject.transform.SetParent(null);

        // Add a large forward force to simulate throwing hard
        heldObjectRb.AddForce(cam.transform.forward * throwForce, ForceMode.Impulse);

        // Clear our variables
        heldObject = null;
        heldObjectRb = null;
        heldObjectCollider = null;
    }

    void TryConsumeObject()
    {
        ConsumableItem consumable = heldObject.GetComponent<ConsumableItem>();

        if (consumable != null)
        {
            // Nu hämtar koden namnet direkt från objektet i Unity-hierarkin
            Debug.Log("Consumed " + heldObject.name + ". Restored " + consumable.thirstRestore + " Thirst.");

            // Spela upp ljudet om det finns ett inlagt
            if (consumable.consumeSound != null && playerAudio != null)
            {
                playerAudio.PlayOneShot(consumable.consumeSound);
            }

            Destroy(heldObject);

            heldObject = null;
            heldObjectRb = null;
            heldObjectCollider = null;
        }
        else
        {
            Debug.Log("This item cannot be consumed.");
        }
    }

}
