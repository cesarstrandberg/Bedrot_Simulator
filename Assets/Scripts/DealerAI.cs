using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class DealerAI : MonoBehaviour
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

    public DealerState CurrentState { get; private set; } = DealerState.Idle;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float currentSpeed;
    private float footstepTimer;
    private bool jarHandedOff;
    private bool speechTriggered;
    private bool dapped;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (weedJarInHand != null) weedJarInHand.SetActive(false);
        gameObject.SetActive(false);
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
        if (CurrentState != DealerState.Waiting || speechTriggered || toggleSpeechPos == null) return;

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
        if (CurrentState != DealerState.Idle || stopPoint == null) return;

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);
        jarHandedOff = false;
        speechTriggered = false;
        dapped = false;

        if (weedJarInHand != null) weedJarInHand.SetActive(true);

        agent.enabled = true;
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
    public void Interact()
    {
        if (CurrentState != DealerState.Waiting) return;

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
