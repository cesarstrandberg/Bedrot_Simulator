using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

// Jonte. Normal visits: walks up (NavMesh + elevator), knocks, hands over a jar on credit, dap,
// leaves. The final visit (JonteStory, Day 7): walks straight to wherever you are with the gun out,
// holds you in place, says his piece, shoots - EndingScreen.
public class DealerAI : MonoBehaviour, IInteractable
{
    public enum DealerState { Idle, Approaching, Waiting, Leaving }

    [Header("Referenser")]
    public Transform stopPoint;      // Dealer_StopPos - var han stannar inne i lägenheten
    public GameObject weedJarInHand; // Jar-modellen i handen (valfri, kan lämnas tom)
    public DrugSite drugSite;

    [Header("Hiss (ersätter trappan - ingen NavMesh i hissen, han glider in/ut manuellt)")]
    // NavMesh täcker spawn -> hissdörren nere och hissdörren uppe -> stopPoint.
    // Lämna tom för att köra rent NavMesh.
    public Elevator elevator;
    [FormerlySerializedAs("stairMoveSpeed")] public float elevatorWalkSpeed = 1.5f;
    [FormerlySerializedAs("stairTurnSpeed")] public float elevatorTurnSpeed = 6f;

    [Header("Ljud")]
    public AudioSource audioSource;
    public AudioClip knockSound;
    public AudioClip footstepSound;
    public float footstepInterval = 0.45f;
    [Range(0f, 1f)] public float footstepVolume = 0.15f;
    [Range(0f, 1f)] public float knockVolume = 0.7f;
    // Separate source so the knock can carry across the apartment while footsteps/voice stay close-range.
    // Falls back to 'audioSource' if left empty.
    public AudioSource doorbellAudioSource;

    [Header("Dialogue")]
    // One long gibberish take - we play trimmed slices of it rather than the whole thing.
    public AudioClip voiceLine;
    public DialogueSubtitle dialogueSubtitle;
    public DialogueSubtitle.Line[] arrivalLines;
    public DialogueSubtitle.Line[] farewellLines;

    [Header("Voice clip slicing (which part of 'voiceLine' plays, and for how long)")]
    public float arrivalVoiceStartTime = 0f;
    public float arrivalVoiceDuration = 5f;
    // Covers arrivalLines[2] ("don't leave me hanging") specifically - it sits after a blank
    // pause long enough that the arrival slice above runs out before that line's text appears,
    // so it gets its own slice timed to when that line actually shows up.
    public float reminderVoiceStartTime = 5.3f;
    public float reminderVoiceDuration = 2.2f;
    public float farewellVoiceStartTime = 7.5f;
    public float farewellVoiceDuration = 1f;

    [Header("Speech trigger (player has to walk here first)")]
    // Like a trapdoor switch - he doesn't start talking until you've walked up to this point.
    public Transform toggleSpeechPos;
    public float speechTriggerRadius = 1.5f;
    public float speechTriggerDelay = 1.5f;

    [Header("Dap-up (replaces the plain click to send him off)")]
    public AudioClip dapSound;

    [Header("Rotation vid Dealer_StopPos")]
    public float turnAngle = 90f;
    public float turnDuration = 0.4f;

    [Header("Final visit (the shot)")]
    public GameObject gunInHand;          // Pistol under his right hand - only shown on the final visit
    public AudioClip gunShot;
    public float finalWalkSpeed = 1.6f;   // slower than a delivery
    public float shootDistance = 3f;      // stops this close (and with a clear view) to you
    public float fireDelay = 0.45f;       // from the Shoot trigger to the bang (gun raised by then)

    public DealerState CurrentState { get; private set; } = DealerState.Idle;
    public bool IsFinalVisit { get; private set; }

    // The dealer in the loaded scene (an object that starts disabled never registers - e.g. the old
    // DrugDealer kept switched off in ApartmentScene)
    public static DealerAI Instance { get; private set; }

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float currentSpeed;
    private float footstepTimer;
    private bool jarHandedOff;
    private bool speechTriggered;
    private bool dapped;
    private float deliverySpeed;

    void Awake()
    {
        Instance = this;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        deliverySpeed = agent.speed;

        if (weedJarInHand != null) weedJarInHand.SetActive(false);
        if (gunInHand != null) gunInHand.SetActive(false);
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // I hissen (agent.enabled == false) sätts Speed manuellt i Elevator.RideNpc istället.
        if (animator != null && agent != null && agent.enabled)
        {
            currentSpeed = agent.velocity.magnitude;
            animator.SetFloat("Speed", currentSpeed);
        }

        UpdateFootsteps();
        CheckSpeechTrigger();
    }

    void CheckSpeechTrigger()
    {
        if (IsFinalVisit || CurrentState != DealerState.Waiting || speechTriggered || toggleSpeechPos == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Horizontal distance only - the camera sits at eye height, toggleSpeechPos is a floor
        // marker, so comparing raw 3D distance would almost never trigger.
        Vector3 delta = cam.transform.position - toggleSpeechPos.position;
        delta.y = 0f;

        if (delta.magnitude <= speechTriggerRadius)
        {
            speechTriggered = true;
            StartCoroutine(SpeechAfterDelay());
        }
    }

    IEnumerator SpeechAfterDelay()
    {
        yield return new WaitForSeconds(speechTriggerDelay);

        PlayVoiceSlice(arrivalVoiceStartTime, arrivalVoiceDuration);
        if (dialogueSubtitle != null) dialogueSubtitle.Play(arrivalLines);

        // Line index 2 is "don't leave me hanging" - wait out however long it takes the first
        // two lines (plus line 2's own pause) to finish, then give it its own voice slice.
        if (arrivalLines != null && arrivalLines.Length >= 3)
        {
            float reminderDelay = arrivalLines[0].duration + arrivalLines[1].duration + arrivalLines[2].delayBefore;
            yield return new WaitForSeconds(reminderDelay);
            PlayVoiceSlice(reminderVoiceStartTime, reminderVoiceDuration);
        }
    }

    // Plays a trimmed slice of 'voiceLine' - starts at startTime, cuts off after 'duration'.
    // Lets one long gibberish take stand in for several different lines instead of needing
    // a separate audio file per line.
    void PlayVoiceSlice(float startTime, float duration)
    {
        if (audioSource == null || voiceLine == null) return;

        audioSource.Stop();
        audioSource.clip = voiceLine;
        audioSource.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, voiceLine.length - 0.01f));
        audioSource.Play();

        StartCoroutine(StopVoiceAfter(duration));
    }

    IEnumerator StopVoiceAfter(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (audioSource != null && audioSource.clip == voiceLine) audioSource.Stop();
    }

    void UpdateFootsteps()
    {
        if (audioSource == null || footstepSound == null) return;

        if (currentSpeed > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                audioSource.PlayOneShot(footstepSound, footstepVolume);
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    // Anropas från DrugSite.OrderDrugs() när en beställning läggs
    public void StartDelivery()
    {
        if (IsFinalVisit || CurrentState != DealerState.Idle || stopPoint == null) return;

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);
        jarHandedOff = false;
        speechTriggered = false;
        dapped = false;

        if (weedJarInHand != null) weedJarInHand.SetActive(true);
        if (gunInHand != null) gunInHand.SetActive(false);

        agent.enabled = true;
        agent.speed = deliverySpeed;
        agent.Warp(spawnPosition);
        agent.isStopped = false;
        CurrentState = DealerState.Approaching;
        StartCoroutine(ApproachRoutine());
    }

    IEnumerator ApproachRoutine()
    {
        yield return RideElevator(true);

        agent.SetDestination(stopPoint.position);
        yield return WaitUntilArrived();

        CurrentState = DealerState.Waiting;
        agent.isStopped = true;

        AudioSource bellSource = doorbellAudioSource != null ? doorbellAudioSource : audioSource;
        if (bellSource != null && knockSound != null) bellSource.PlayOneShot(knockSound, knockVolume);

        yield return StartCoroutine(TurnRoutine(turnAngle));

        // He doesn't start talking until you've walked up to toggleSpeechPos - see CheckSpeechTrigger().
    }

    // Anropas från DealerClickable. Första klicket = ta emot burken från honom (precis som förut,
    // spelaren styr själv NÄR det händer - E/klick på honom, inget som sker automatiskt).
    // Andra klicket (dappen) = skicka i väg honom.
    // IInteractable - E / left-click on him with the crosshair (no OnMouseDown/DealerClickable needed)
    public string PromptLabel
    {
        get
        {
            if (IsFinalVisit || CurrentState != DealerState.Waiting || dapped) return "Jonte";
            return jarHandedOff ? "Dap him up" : "Take the jar";
        }
    }

    public bool PromptAvailable => !IsFinalVisit && CurrentState == DealerState.Waiting && !dapped;

    public void OnLookedAt(Transform player) { }

    public void Interact()
    {
        if (IsFinalVisit || CurrentState != DealerState.Waiting) return;

        if (!jarHandedOff)
        {
            jarHandedOff = true;
            if (drugSite != null) drugSite.CompleteHandoff();
            if (weedJarInHand != null) weedJarInHand.SetActive(false);
            return;
        }

        if (dapped) return;
        dapped = true;

        if (audioSource != null && dapSound != null) audioSource.PlayOneShot(dapSound);
        StartCoroutine(FarewellThenLeave());
    }

    // He stays put and says his farewell line(s) before actually walking off.
    IEnumerator FarewellThenLeave()
    {
        PlayVoiceSlice(farewellVoiceStartTime, farewellVoiceDuration);
        if (dialogueSubtitle != null) yield return StartCoroutine(dialogueSubtitle.PlayRoutine(farewellLines));

        CurrentState = DealerState.Leaving;
        agent.isStopped = false;
        StartCoroutine(LeaveRoutine());
    }

    IEnumerator LeaveRoutine()
    {
        yield return RideElevator(false);

        agent.SetDestination(spawnPosition);
        yield return WaitUntilArrived();

        gameObject.SetActive(false);
        CurrentState = DealerState.Idle;
    }

    // Väntar tills NavMeshAgent har nått sin nuvarande destination
    IEnumerator WaitUntilArrived()
    {
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }
    }

    // Går (NavMesh) fram till hissdörren, sen tar Elevator.RideNpc över: kallar hissen, glider in,
    // åker, glider ut. Därefter NavMesh igen från dörren på andra våningen.
    IEnumerator RideElevator(bool toUp)
    {
        if (elevator == null) yield break;

        agent.SetDestination(elevator.NpcOutsideSpot(!toUp).position);
        yield return WaitUntilArrived();

        agent.isStopped = true;
        agent.enabled = false;
        yield return elevator.RideNpc(transform, animator, toUp, elevatorWalkSpeed, elevatorTurnSpeed);

        currentSpeed = 0f;
        agent.enabled = true;
        agent.Warp(elevator.NpcOutsideSpot(toUp).position);
        agent.isStopped = false;
    }

    // ---------- Final visit (Day 7) ----------

    // Called by JonteStory. Only from Idle (JonteStory waits out a delivery in progress - he could
    // be inside the elevator), comes in from the street door.
    public bool StartFinalVisit()
    {
        if (IsFinalVisit) return true;
        if (CurrentState != DealerState.Idle) return false;
        IsFinalVisit = true;

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);
        if (weedJarInHand != null) weedJarInHand.SetActive(false);
        if (gunInHand != null) gunInHand.SetActive(true);

        agent.enabled = true;
        agent.Warp(spawnPosition);
        agent.isStopped = false;
        agent.speed = finalWalkSpeed;
        CurrentState = DealerState.Approaching;
        StartCoroutine(FinalVisitRoutine());
        return true;
    }

    IEnumerator FinalVisitRoutine()
    {
        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        PlayerMovement playerBody = FindFirstObjectByType<PlayerMovement>();
        Transform player = playerBody != null ? playerBody.transform : (Camera.main != null ? Camera.main.transform : null);
        if (player == null) { EndingScreen.Play(gunShot); yield break; }

        // Walk to the player until close with a clear view. Re-plans every 0.3s (you can move);
        // takes the elevator when you're on the other floor.
        var path = new NavMeshPath();
        while (true)
        {
            Vector3 flat = player.position - transform.position;
            flat.y = 0f;
            bool sameFloor = elevator == null
                || elevator.IsButtonOnUpperFloor(player.position) == elevator.IsButtonOnUpperFloor(transform.position);

            if (sameFloor && flat.magnitude <= shootDistance && CanSee(player)) break;

            if (!sameFloor)
            {
                yield return RideElevator(elevator.IsButtonOnUpperFloor(player.position));
                agent.speed = finalWalkSpeed;
                continue;
            }

            if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.CalculatePath(hit.position, path);
                agent.SetPath(path);
            }
            yield return new WaitForSeconds(0.3f);
        }

        // Got you
        CurrentState = DealerState.Waiting;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        if (stats != null) stats.HoldForHit(transform);
        yield return StartCoroutine(FacePoint(player.position, 0.5f));

        DialogueSubtitle.Line[] lines = JonteStory.FinalLines();
        float talk = 0f;
        foreach (var l in lines) talk += l.delayBefore + l.duration;
        PlayVoiceSlice(arrivalVoiceStartTime, Mathf.Min(talk, arrivalVoiceDuration + reminderVoiceDuration));
        if (dialogueSubtitle != null) yield return StartCoroutine(dialogueSubtitle.PlayRoutine(lines));
        else yield return new WaitForSeconds(talk);

        yield return new WaitForSeconds(0.8f);   // the beat before
        if (animator != null) animator.SetTrigger("Shoot");
        yield return StartCoroutine(FacePoint(player.position, fireDelay));
        EndingScreen.Play(gunShot);
    }

    // Clear line from his eyes to your camera (nothing but you in between)
    bool CanSee(Transform player)
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        Vector3 eye = transform.position + Vector3.up * 1.6f;
        Vector3 to = cam.transform.position - eye;
        RaycastHit[] hits = Physics.RaycastAll(eye, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.transform.IsChildOf(transform) || h.transform.IsChildOf(player) || h.transform == cam.transform) continue;
            return false;
        }
        return true;
    }

    IEnumerator FacePoint(Vector3 point, float duration)
    {
        Vector3 dir = point - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) yield break;
        Quaternion start = transform.rotation, target = Quaternion.LookRotation(dir);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            transform.rotation = Quaternion.Slerp(start, target, t / duration);
            yield return null;
        }
        transform.rotation = target;
    }

    // Vrider honom ETT extra steg när han når Dealer_StopPos, oavsett vilket håll han gick in ifrån
    IEnumerator TurnRoutine(float angleDegrees)
    {
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0f, angleDegrees, 0f);
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / turnDuration);
            yield return null;
        }

        transform.rotation = targetRot;
    }
}
