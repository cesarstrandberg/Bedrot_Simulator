using System.Collections;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f;
    public float throwForce = 15f;
    public float dropForce = 2f;
    public Transform holdPoint;

    [Header("Audio Settings")]
    public AudioSource playerAudio;

    private GameObject heldObject;
    private Rigidbody heldObjectRb;
    private Collider heldObjectCollider;
    private Camera cam;

    private bool isConsuming = false;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (isConsuming) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
            {
                if (Physics.Raycast(ray, out hit, interactRange))
                {
                    if (hit.collider.CompareTag("Interactable"))
                    {
                        PickUpObject(hit.collider.gameObject);
                    }
                }
            }
            else
            {
                DropObject();
            }
        }

        if (Input.GetKeyDown(KeyCode.F) && heldObject != null)
        {
            ThrowObject();
        }

        // VÄNSTERKLICK för att använda/dricka
        if (Input.GetMouseButtonDown(0) && heldObject != null)
        {
            ConsumableItem consumable = heldObject.GetComponent<ConsumableItem>();
            if (consumable != null)
            {
                StartCoroutine(ConsumeRoutine(consumable));
            }
        }
    }

    void PickUpObject(GameObject obj)
    {
        heldObject = obj;
        heldObjectRb = obj.GetComponent<Rigidbody>();
        heldObjectCollider = obj.GetComponent<Collider>();

        heldObjectRb.isKinematic = true;
        heldObjectCollider.enabled = false;

        heldObject.transform.SetParent(holdPoint);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;
    }

    void DropObject()
    {
        heldObjectRb.isKinematic = false;
        heldObjectCollider.enabled = true;

        heldObject.transform.SetParent(null);
        heldObjectRb.AddForce(cam.transform.forward * dropForce, ForceMode.Impulse);

        heldObject = null;
        heldObjectRb = null;
        heldObjectCollider = null;
    }

    void ThrowObject()
    {
        heldObjectRb.isKinematic = false;
        heldObjectCollider.enabled = true;

        heldObject.transform.SetParent(null);
        heldObjectRb.AddForce(cam.transform.forward * throwForce, ForceMode.Impulse);

        heldObject = null;
        heldObjectRb = null;
        heldObjectCollider = null;
    }

    IEnumerator ConsumeRoutine(ConsumableItem consumable)
    {
        isConsuming = true;

        // ========================================================
        // STEG 1: ÖPPNA FLASKAN DIREKT
        // ========================================================
        if (consumable.visualWithCap != null && consumable.visualWithCap.activeSelf)
        {
            if (consumable.openSound != null && playerAudio != null)
            {
                playerAudio.PlayOneShot(consumable.openSound);
            }

            consumable.visualWithCap.SetActive(false);
            if (consumable.visualWithoutCap != null)
            {
                consumable.visualWithoutCap.SetActive(true);
            }

            if (consumable.capPrefab != null)
            {
                Vector3 spawnPos = holdPoint.position - new Vector3(0f, 0.2f, 0f);
                GameObject droppedCap = Instantiate(consumable.capPrefab, spawnPos, Quaternion.identity);
                Rigidbody capRb = droppedCap.GetComponent<Rigidbody>();

                if (capRb != null)
                {
                    Vector3 popDirection = (cam.transform.forward - cam.transform.up) * 1.5f;
                    capRb.AddForce(popDirection, ForceMode.Impulse);
                }
            }

            yield return new WaitForSeconds(0.15f);
        }

        // ========================================================
        // STEG 2: FÖR UPP TILL MUNNEN, VINKLA OCH DRICK (Tyst)
        // ========================================================
        Vector3 startPos = heldObject.transform.localPosition;
        Vector3 targetPos = startPos + consumable.mouthOffset;

        // NYTT: Hantera rotationen
        Quaternion startRot = heldObject.transform.localRotation;
        Quaternion targetRot = Quaternion.Euler(consumable.mouthRotation);

        float elapsed = 0f;

        while (elapsed < consumable.consumeDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / consumable.consumeDuration;

            heldObject.transform.localPosition = Vector3.Lerp(startPos, targetPos, percent);
            heldObject.transform.localRotation = Quaternion.Lerp(startRot, targetRot, percent); // NYTT: Vinklar flaskan

            yield return null;
        }

        yield return new WaitForSeconds(0.6f);

        // ========================================================
        // STEG 3: FÖR BORT FRÅN MUNNEN, ROTERA TILLBAKA & RAPA
        // ========================================================
        elapsed = 0f;
        while (elapsed < consumable.consumeDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / consumable.consumeDuration;

            heldObject.transform.localPosition = Vector3.Lerp(targetPos, startPos, percent);
            heldObject.transform.localRotation = Quaternion.Lerp(targetRot, startRot, percent); // NYTT: Vinklar tillbaka

            yield return null;
        }

        if (consumable.burpSound != null && playerAudio != null)
        {
            playerAudio.PlayOneShot(consumable.burpSound);
            yield return new WaitForSeconds(consumable.burpSound.length);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("Drack upp: " + heldObject.name);

        // ========================================================
        // STEG 4: SLÄPP DEN TOMMA FLASKAN 
        // ========================================================
        if (consumable.destroyOnConsume)
        {
            Destroy(heldObject);
        }
        else
        {
            GameObject emptyBottle = heldObject;
            emptyBottle.transform.SetParent(null);

            Rigidbody rb = emptyBottle.GetComponent<Rigidbody>();
            Collider col = emptyBottle.GetComponent<Collider>();

            rb.isKinematic = false;
            col.enabled = true;

            rb.AddForce(cam.transform.forward * 1f, ForceMode.Impulse);
            Destroy(emptyBottle.GetComponent<ConsumableItem>());
        }

        heldObject = null;
        heldObjectRb = null;
        heldObjectCollider = null;

        isConsuming = false;
    }
}