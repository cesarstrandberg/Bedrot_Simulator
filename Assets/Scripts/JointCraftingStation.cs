using System.Collections;
using UnityEngine;

public class JointCraftingStation : MonoBehaviour
{
    // Static variable keeps the bud count persistent across scenes and gamemodes!
    public static int globalBudCount = 7;

    public enum CraftingStep
    {
        RemoveJarLid,
        TiltJar,
        PutBudInGrinder,
        CloseGrinder,
        GrindWeed,
        OpenGrinder,
        SpawnPaper,
        PourWeedToPaper,
        RollJoint,
        Finished
    }

    [Header("Current State")]
    public CraftingStep currentStep = CraftingStep.RemoveJarLid;
    public bool isMinigameActive = false;

    [Header("Cameras & Player")]
    public GameObject mainCamera;
    public GameObject craftingCamera;
    public MonoBehaviour[] playerScriptsToDisable;

    [Header("Jar & Buds References")]
    public Transform jarLid;
    public Transform jarLidRestPoint;
    public Transform jar;
    public Transform jarTiltPoint; // Rotated orientation for pouring
    public GameObject[] visualBudsInJar; // Array of bud models inside the jar to disable one by one, in the order they should disappear (element 0 disappears first)
    public GameObject budOnTray;

    [Header("Empty Jar")]
    public Transform emptyWeedJarSpawnPos; // Where the jar itself relocates to once it runs out of buds

    [Header("Grinder References")]
    public GameObject budInGrinder;
    public Transform grinderLid;
    public Transform grinderLidRestPoint;
    public Transform grinderLidClosedPoint;
    public Transform grinderBottom;
    public Transform grinderPourPoint; // Rotated orientation when pouring over paper
    public GameObject grindedWeedInGrinder;

    [Header("Paper & Joint References")]
    public GameObject ocbPack;
    public GameObject emptyPaperOnTray;
    public GameObject filledPaperOnTray;
    public GameObject finishedJointPrefab;
    public Transform jointSpawnPoint;

    [Header("Rolling Minigame Settings")]
    public float rollProgress = 0f;
    public float requiredRollProgress = 100f;
    public float maxAllowedMouseSpeed = 2.0f; // Dragging faster than this penalizes the player

    [Header("Audio Sources & Clips")]
    public AudioSource audioSource;
    public AudioClip lidSound;
    public AudioClip pourBudSound;
    public AudioClip grinderLidSound;
    public AudioClip grindScrollSound;
    public AudioClip pourWeedSound;
    public AudioClip paperSpawnSound;
    public AudioClip rollingSound;
    public AudioClip characterCheerSound;

    private bool emptyJarSpawned = false;
    private bool isTiltingJar = false;
    private int scrollCount = 0;
    private Vector3 originalJarPos;
    private Quaternion originalJarRot;
    private Quaternion originalGrinderBottomRot;

    //Variables to remember the lids startPosition
    private Vector3 originalJarLidPos;
    private Quaternion originalJarLidRot;
    private Transform originalJarLidParent;

    void Start()
    {
        // Save original rotations to reset them later
        if (jar != null)
        {
            originalJarPos = jar.localPosition;
            originalJarRot = jar.localRotation;
        }

        if(jarLid != null)
        {
            originalJarLidPos = jarLid.localPosition;
            originalJarLidRot = jarLid.localRotation;
            originalJarLidParent = jarLid.parent;
        }
        

        if (grinderBottom != null) originalGrinderBottomRot = grinderBottom.localRotation;

        ResetStationVisuals();
        RefreshBudVisuals();
    }

    void Update()
    {
        if (!isMinigameActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitMinigame();
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- STEP 5: GRINDING WITH MOUSE SCROLL ---
        if (currentStep == CraftingStep.GrindWeed)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                scrollCount++;
                // Rotate grinder lid visually for feedback
                grinderLid.Rotate(Vector3.up * 30f, Space.World);

                if (audioSource != null && grindScrollSound != null && !audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(grindScrollSound);
                }

                // Require 6 upward scrolls to finish grinding
                if (scrollCount >= 6)
                {
                    scrollCount = 0;
                    currentStep = CraftingStep.OpenGrinder;
                    Debug.Log("Grinding complete! Click the grinder lid to open it.");
                }
            }
        }

        // --- STEP 9: THE ROLLING MINIGAME (HOLD LEFT CLICK & DRAG UP STEADILY) ---
        if (currentStep == CraftingStep.RollJoint && Input.GetMouseButton(0))
        {
            float mouseY = Input.GetAxis("Mouse Y");

            // Only count upward movement
            if (mouseY > 0f)
            {
                // Check if dragging too fast (panic rolling!)
                if (mouseY > maxAllowedMouseSpeed)
                {
                    Debug.Log("Rolling too fast! Be gentle with the paper!");
                    rollProgress -= Time.deltaTime * 10f; // Penalty
                }
                else
                {
                    // Steady rolling increases progress
                    rollProgress += mouseY * 40f;

                    if (audioSource != null && rollingSound != null && !audioSource.isPlaying)
                    {
                        audioSource.PlayOneShot(rollingSound);
                    }
                }

                rollProgress = Mathf.Clamp(rollProgress, 0f, requiredRollProgress);

                if (rollProgress >= requiredRollProgress)
                {
                    StartCoroutine(FinishJointRoutine());
                }
            }
        }
    }

    // Call this method from an interactable trigger on the table to start
    public void StartMinigame()
    {
        if (emptyJarSpawned)
        {
            Debug.Log("The jar is empty! Get a new one from the dealer first.");
            return;
        }

        isMinigameActive = true;
        currentStep = CraftingStep.RemoveJarLid;
        rollProgress = 0f;

        // Disable player controls and switch to crafting camera
        foreach (var script in playerScriptsToDisable) if (script != null) script.enabled = false;
        if (mainCamera != null) mainCamera.SetActive(false);
        if (craftingCamera != null) craftingCamera.SetActive(true);

        RefreshBudVisuals();
        Debug.Log("Minigame started! Step 1: Click the jar lid to remove it.");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Master click handler - attach this to 3D colliders or call via Raycast
    public void OnInteractableClicked(string objectTag)
    {
        if (!isMinigameActive) return;

        switch (currentStep)
        {
            case CraftingStep.RemoveJarLid:
                if (objectTag == "JarLid")
                {
                    PlaySound(lidSound);
                    StartCoroutine(MoveTransform(jarLid, jarLidRestPoint.position, jarLidRestPoint.rotation, 0.3f));
                    currentStep = CraftingStep.TiltJar;
                    Debug.Log("Step 2: Click the jar to tilt it and get a bud out.");
                }
                break;

            case CraftingStep.TiltJar:
                if (objectTag == "Jar" && globalBudCount > 0 && !isTiltingJar)
                {
                    StartCoroutine(TiltJarRoutine());
                }
                else if (globalBudCount <= 0)
                {
                    Debug.Log("The jar is empty! No more buds left!");
                }
                break;

            case CraftingStep.PutBudInGrinder:
                if (objectTag == "BudOnTray")
                {
                    budOnTray.SetActive(false);
                    budInGrinder.SetActive(true);
                    currentStep = CraftingStep.CloseGrinder;
                    Debug.Log("Step 4: Click the grinder lid to close it.");
                }
                break;

            case CraftingStep.CloseGrinder:
                if (objectTag == "GrinderLid")
                {
                    PlaySound(grinderLidSound);
                    StartCoroutine(MoveTransform(grinderLid, grinderLidClosedPoint.position, grinderLidClosedPoint.rotation, 0.3f));
                    currentStep = CraftingStep.GrindWeed;
                    Debug.Log("Step 5: Hover over the grinder and scroll UP with the mouse wheel to grind!");
                }
                break;

            case CraftingStep.OpenGrinder:
                if (objectTag == "GrinderLid")
                {
                    PlaySound(grinderLidSound);
                    StartCoroutine(MoveTransform(grinderLid, grinderLidRestPoint.position, grinderLidRestPoint.rotation, 0.3f));
                    // Swap internal models: bud disappears, grinded powder appears
                    budInGrinder.SetActive(false);
                    grindedWeedInGrinder.SetActive(true);
                    currentStep = CraftingStep.SpawnPaper;
                    Debug.Log("Step 7: Click the OCB pack to get a paper.");
                }
                break;

            case CraftingStep.SpawnPaper:
                if (objectTag == "OCBPack")
                {
                    PlaySound(paperSpawnSound);
                    emptyPaperOnTray.SetActive(true);
                    currentStep = CraftingStep.PourWeedToPaper;
                    Debug.Log("Step 8: Click the bottom grinder half to pour the weed onto the paper.");
                }
                break;

            case CraftingStep.PourWeedToPaper:
                if (objectTag == "GrinderBottom")
                {
                    StartCoroutine(PourGrinderToPaperRoutine());
                }
                break;

            case CraftingStep.RollJoint:
                if (objectTag == "FilledPaperOnTray")
                {
                    StartCoroutine(FinishJointRoutine());
                }
                break;
        }
    }

    IEnumerator TiltJarRoutine()
    {
        isTiltingJar = true;
        PlaySound(pourBudSound);
        // Tilt jar over tray
        yield return StartCoroutine(MoveTransform(jar, jarTiltPoint.position, jarTiltPoint.rotation, 0.4f));

        // Decrease persistent bud count and hide one bud inside the jar
        globalBudCount--;
        UpdateVisualBudsInJar();

        // Spawn bud on tray
        budOnTray.SetActive(true);

        // Return jar to normal position
        Vector3 jarHomePos = new Vector3(-0.2214f, 0.01630974f, 0.1246f);
        yield return StartCoroutine(MoveTransformLocal(jar, jarHomePos, Quaternion.identity, 0.4f));

        if (globalBudCount <= 0)
        {
            // Last bud used - the jar now travels to its empty resting spot
            CheckJarEmpty();
        }

        currentStep = CraftingStep.PutBudInGrinder;
        Debug.Log("Step 3: Click the bud on the tray to put it into the grinder.");
        isTiltingJar = false;
    }

    IEnumerator PourGrinderToPaperRoutine()
    {
        PlaySound(pourWeedSound);
        // Tilt grinder bottom over paper
        yield return StartCoroutine(MoveTransform(grinderBottom, grinderPourPoint.position, grinderPourPoint.rotation, 0.4f));

        // Swap paper models
        grindedWeedInGrinder.SetActive(false);
        emptyPaperOnTray.SetActive(false);
        filledPaperOnTray.SetActive(true);

        // Return grinder bottom to original orientation
        yield return StartCoroutine(MoveTransform(grinderBottom, grinderBottom.parent.position, originalGrinderBottomRot, 0.4f));

        currentStep = CraftingStep.RollJoint;
        Debug.Log("Step 9: HOLD LEFT CLICK and DRAG UP smoothly to roll!");
    }

    IEnumerator FinishJointRoutine()
    {
        currentStep = CraftingStep.Finished;
        filledPaperOnTray.SetActive(false);

        // Spawn finished joint
        if (finishedJointPrefab != null && jointSpawnPoint != null)
        {
            GameObject minFardigaJoint = Instantiate(finishedJointPrefab, jointSpawnPoint.position, jointSpawnPoint.rotation);
            minFardigaJoint.SetActive(true); // Här tvingar vi den att slås på och bli synlig!
        }

        // Play character cheer sound!
        PlaySound(characterCheerSound);
        Debug.Log("JOINT ROLLED!");

        yield return new WaitForSeconds(2.0f);

        if (globalBudCount > 0)
        {
            // Still got buds - go again instead of kicking the player out
            ResetCraftingProps();
            rollProgress = 0f;
            currentStep = CraftingStep.TiltJar;
            Debug.Log("Step 2: Click the jar to tilt it and get another bud out.");
        }
        else
        {
            Debug.Log("Jar's empty - exiting the crafting station.");
            ExitMinigame();
        }
    }

    public void ExitMinigame()
    {
        isMinigameActive = false;

        // Re-enable player controls and switch back to main camera
        if (craftingCamera != null) craftingCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);
        foreach (var script in playerScriptsToDisable) if (script != null) script.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ResetStationVisuals();
    }

    void RefreshBudVisuals()
    {
        UpdateVisualBudsInJar();
        CheckJarEmpty();
    }

    void UpdateVisualBudsInJar()
    {
        // Hide bud models starting from element 0, so visualBudsInJar[0] disappears first
        // and visualBudsInJar[Length - 1] disappears last.
        int total = visualBudsInJar.Length;
        for (int i = 0; i < total; i++)
        {
            if (visualBudsInJar[i] != null)
            {
                visualBudsInJar[i].SetActive(i >= total - globalBudCount);
            }
        }
    }

    void CheckJarEmpty()
    {
        if (emptyJarSpawned || globalBudCount > 0) return;

        emptyJarSpawned = true;

        if (jarLid != null && jar != null)
        {
            // Snap the lid shut onto the jar (jar is at its normal, untilted resting pose here)
            // and parent it so it travels along with the jar as one object from now on.
            jarLid.localPosition = originalJarLidPos;
            jarLid.localRotation = originalJarLidRot;
            jarLid.SetParent(jar, true);
        }

        if (jar != null && emptyWeedJarSpawnPos != null)
        {
            StartCoroutine(MoveTransform(jar, emptyWeedJarSpawnPos.position, emptyWeedJarSpawnPos.rotation, 0.5f));
        }
    }

    // Called by PlayerInteraction when the player clicks this station while holding a FilledWeedJarItem.
    // Returns false (and does nothing) if the station doesn't need a new jar right now.
    public bool TryPlaceHeldJar()
    {
        if (!emptyJarSpawned) return false;

        StartCoroutine(PlaceNewJarRoutine());
        return true;
    }

    IEnumerator PlaceNewJarRoutine()
    {
        // Move the jar (lid still attached from CheckJarEmpty) back to its home position
        yield return StartCoroutine(MoveTransformLocal(jar, originalJarPos, originalJarRot, 0.5f));

        // Detach the lid again so it can move independently during future tilt animations
        if (jarLid != null)
        {
            jarLid.SetParent(originalJarLidParent, true);
            jarLid.localPosition = originalJarLidPos;
            jarLid.localRotation = originalJarLidRot;
        }

        globalBudCount = 7;
        emptyJarSpawned = false;
        UpdateVisualBudsInJar();

        Debug.Log("New jar placed! The station is ready to use again.");
    }

    void ResetCraftingProps()
    {
        if (budOnTray != null) budOnTray.SetActive(false);
        if (budInGrinder != null) budInGrinder.SetActive(false);
        if (grindedWeedInGrinder != null) grindedWeedInGrinder.SetActive(false);
        if (emptyPaperOnTray != null) emptyPaperOnTray.SetActive(false);
        if (filledPaperOnTray != null) filledPaperOnTray.SetActive(false);
    }

    void ResetStationVisuals()
    {
        ResetCraftingProps();

        if (jarLid != null && !emptyJarSpawned)
        {
            // Once the jar is empty the lid is parented to it (see CheckJarEmpty) and
            // "local" position/rotation means something different - leave it alone then.
            jarLid.localPosition = originalJarLidPos;
            jarLid.localRotation = originalJarLidRot;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Helper coroutine to smoothly lerp positions and rotations
    IEnumerator MoveTransform(Transform target, Vector3 toPos, Quaternion toRot, float duration)
    {
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.position = Vector3.Lerp(startPos, toPos, percent);
            target.rotation = Quaternion.Lerp(startRot, toRot, percent);
            yield return null;
        }
        target.position = toPos;
        target.rotation = toRot;
    }

    //Function to move stuff in the local space
    IEnumerator MoveTransformLocal(Transform target, Vector3 toLocalPos, Quaternion toLocalRot, float duration)
    {
        Vector3 startPos = target.localPosition;
        Quaternion startRot = target.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.localPosition = Vector3.Lerp(startPos, toLocalPos, percent);
            target.localRotation = Quaternion.Lerp(startRot, toLocalRot, percent);
            yield return null;
        }
        target.localPosition = toLocalPos;
        target.localRotation = toLocalRot;
    }
}