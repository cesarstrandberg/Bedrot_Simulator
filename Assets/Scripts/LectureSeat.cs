using UnityEngine;

// An empty seat you can sit in with E (and stand up from with Space - E is busy picking things up). While seated you can't walk, but you
// can still look around and use whatever is in your hands. Put it on a marker at the seat's floor
// level, facing the front; it adds its own trigger box so the crosshair can find it.
public class LectureSeat : MonoBehaviour, IInteractable
{
    public float eyeHeight = 1.15f;   // camera height above the seat's floor while sitting

    public static bool PlayerSeated { get; private set; }

    bool seated;
    int seatedFrame;
    Vector3 standPosition;
    Quaternion standRotation;
    PlayerMovement movement;
    Rigidbody body;
    Collider bodyCollider;
    BoxCollider box;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        if (box == null) box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.8f, 1.2f, 0.8f);
        box.center = new Vector3(0f, 0.6f, 0f);
    }

    public string PromptLabel => "Sit";
    public bool PromptAvailable => true;
    public void OnLookedAt(Transform player) { }

    public void Interact()
    {
        if (!seated) Sit();
    }

    void Update()
    {
        if (DialogueChoiceUI.IsOpen) return;
        if (seated && Time.frameCount != seatedFrame && Input.GetKeyDown(KeyCode.Space)) Stand();
    }

    void Sit()
    {
        movement = FindFirstObjectByType<PlayerMovement>();
        if (movement == null) return;
        body = movement.GetComponent<Rigidbody>();
        bodyCollider = movement.GetComponent<Collider>();
        Camera cam = movement.GetComponentInChildren<Camera>();

        standPosition = movement.transform.position;
        standRotation = movement.transform.rotation;

        movement.enabled = false;
        if (body != null) { body.linearVelocity = Vector3.zero; body.isKinematic = true; }
        if (bodyCollider != null) bodyCollider.enabled = false;   // so things on the desk don't get shoved

        // Put the body so the camera ends up at sitting eye height, facing the front
        float camOffset = cam != null ? cam.transform.position.y - movement.transform.position.y : 0.6f;
        movement.transform.SetPositionAndRotation(
            transform.position + Vector3.up * (eyeHeight - camOffset),
            Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

        seated = true;
        PlayerSeated = true;
        seatedFrame = Time.frameCount;
        box.enabled = false;   // so the crosshair doesn't keep offering "Sit"
        Notify.Show("<size=80%>[Space] Stand up</size>", 2.5f);
    }

    void Stand()
    {
        seated = false;
        PlayerSeated = false;
        box.enabled = true;
        if (movement == null) return;
        movement.transform.SetPositionAndRotation(standPosition, standRotation);
        if (bodyCollider != null) bodyCollider.enabled = true;
        if (body != null) body.isKinematic = false;
        movement.enabled = true;
    }

    void OnDestroy()
    {
        if (seated) PlayerSeated = false;
    }
}
