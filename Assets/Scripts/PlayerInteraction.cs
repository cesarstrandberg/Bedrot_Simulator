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

    [Header("Arm Animation Settings")]
    public Transform armRoot; // Dra in ditt arm-huvudobjekt här!
    public Vector3 drinkPositionOffset = new Vector3(-0.2f, 0.15f, -0.05f); // För att dra in armen till mitten (munnen)
    public Vector3 drinkRotationOffset = new Vector3(-40f, 20f, 0f); // För att tilta handleden
    public float moveSpeed = 0.4f;

    private GameObject heldObject;
    private Rigidbody heldObjectRb;
    private Collider heldObjectCollider;
    private Camera cam;

    private bool isConsuming = false;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Göm armarna från början om vi inte håller i något
        if (armRoot != null)
        {
            armRoot.gameObject.SetActive(false);
        }
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

        // VISA ARMARNA när vi plockar upp något
        if (armRoot != null) armRoot.gameObject.SetActive(true);
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

        // GÖM ARMARNA
        if (armRoot != null) armRoot.gameObject.SetActive(false);
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

        // GÖM ARMARNA
        if (armRoot != null) armRoot.gameObject.SetActive(false);
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
        // STEG 2: FLYTTA HELA ARMEN UPP TILL MUNNEN
        // ========================================================
        Vector3 startPos = armRoot.localPosition;
        Quaternion startRot = armRoot.localRotation;

        Vector3 targetPos = startPos + drinkPositionOffset;
        Quaternion targetRot = startRot * Quaternion.Euler(drinkRotationOffset);

        float elapsed = 0f;

        while (elapsed < moveSpeed)
        {
            elapsed += Time.deltaTime;
            
            //Makes it so that the movement gets a soft start and soft stop
            float rawPercent = elapsed / moveSpeed;
            float smoothPercent = Mathf.SmoothStep(0f, 1f, rawPercent);

            //Calculate the position
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, smoothPercent);

            //Fake roation! makes the arm swing downwords in the middle of the movemenet
            //So that it feels like the rotatuion bends (Change from 0.05 for more or less swing)
            currentPos.y -= Mathf.Sin(smoothPercent * Mathf.PI) * 0.05f;

            armRoot.localPosition = currentPos;
            armRoot.localRotation = Quaternion.Lerp(startRot, targetRot, smoothPercent);

            yield return null;
        }

        yield return new WaitForSeconds(2.6f);

        // ========================================================
        // STEG 3: FÖR TILLBAKA ARMEN & RAPA
        // ========================================================
        elapsed = 0f;
        while (elapsed < moveSpeed)
        {
            elapsed += Time.deltaTime;
            float rawPercent = elapsed / moveSpeed;
            float smoothPercent = Mathf.SmoothStep(0f, 1f, rawPercent);

            Vector3 currentPos = Vector3.Lerp(targetPos, startPos, smoothPercent);
            currentPos.y -= Mathf.Sin(smoothPercent * Mathf.PI) * 0.05f; // Samma sving på väg ner

            armRoot.localPosition = currentPos;
            armRoot.localRotation = Quaternion.Lerp(targetRot, startRot, smoothPercent);

            yield return null;
        }

        // Tvinga armen att sitta helt perfekt efteråt
        armRoot.localPosition = startPos;
        armRoot.localRotation = startRot;

        if(consumable.burpSound != null && playerAudio != null)
        {
            playerAudio.PlayOneShot(consumable.burpSound);
        }

        // ========================================================
        // STEG 4: SLÄPP DEN TOMMA FLASKAN & GÖM ARMEN
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

            // Flaskan behåller sin "Interactable"-tagg så du kan ta upp den igen
            rb.AddForce(cam.transform.forward * 1f, ForceMode.Impulse);

            // Men vi spränger bort Consumable-skriptet så den inte går att dricka ur!
            Destroy(emptyBottle.GetComponent<ConsumableItem>());
        }

        // GÖM ARMARNA EFTER VI HAR DROPPAT DEN TOMMA FLASKAN
        if (armRoot != null) armRoot.gameObject.SetActive(false);

        isConsuming = false;
    }
}