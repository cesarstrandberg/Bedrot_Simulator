using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class NeighborAI : MonoBehaviour
{
    public enum NeighborState { Idle, Approaching, Waiting, Yelling, Retreating, Returning, Charging, Attacking, Leaving }

    [Header("References")]
    public Transform stopPoint;          // Where he stops right outside the door, like Dealer_StopPos
    public Transform retreatPoint;       // Strike 2 only - how far back toward his own place he stalks off before turning around
    public PlayerStats playerStats;
    public Transform attackPoint;        // His hand bone - how close this needs to get to the player to "catch" them
    public float attackRadius = 1.2f;
    public GameObject batInHand;         // Child of his right hand bone, only shown for the strike 3+ charge

    [Header("Hiss (ersätter trappan - ingen NavMesh i hissen, han glider in/ut manuellt)")]
    // NavMesh täcker spawn -> hissdörren nere och hissdörren uppe -> stopPoint.
    // Lämna tom för att köra rent NavMesh.
    public Elevator elevator;
    [FormerlySerializedAs("stairMoveSpeed")] public float elevatorWalkSpeed = 1.5f;
    [FormerlySerializedAs("stairTurnSpeed")] public float elevatorTurnSpeed = 6f;

    [Header("Movement speeds")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;

    [Header("Ljud")]
    public AudioSource audioSource;
    public AudioSource doorbellAudioSource; // Falls back to audioSource if left empty - same pattern as DealerAI.
    public AudioClip knockSound;
    public AudioClip footstepSound;
    public float footstepInterval = 0.45f;
    [Range(0f, 1f)] public float footstepVolume = 0.15f;
    [Range(0f, 1f)] public float knockVolume = 0.7f;
    // Footsteps play from their own fully-3D source (built in Awake) so they fade with distance
    // instead of echoing through the whole building at full volume like a 2D source does.
    [Range(0f, 1f)] public float footstepLoudness = 0.3f; // Multiplies footstepVolume on top
    public float footstepMaxDistance = 10f;               // Silent beyond this many meters

    [Header("Dialogue")]
    public DialogueSubtitle dialogueSubtitle;
    public DialogueSubtitle.Line[] strike1Lines;
    public DialogueSubtitle.Line[] strike2LinesFirst;
    public DialogueSubtitle.Line[] strike2LinesSecond;

    [Header("Röst (gibberish, en fil per block - lika lång som blockets repliker + pauser)")]
    public AudioClip strike1Voice;
    public AudioClip strike2FirstVoice;
    public AudioClip strike2SecondVoice;

    [Header("Knockout (strike 3+, när han hunnit ikapp)")]
    // He freezes the player, says this line, and the bat swing (PatrickHit, "SwingTrigger") is timed
    // so it lands right as the line ends.
    public AudioClip knockoutLine;
    public float swingImpactTime = 0.95f;   // Seconds into the swing clip where the bat comes down
    // He starts the line while still running at you once he's this close, so less of it is left for the
    // frozen stare after the catch. Higher = shorter stare.
    public float lineStartDistance = 6f;
    public AudioClip hitThud;
    [Range(0f, 1f)] public float hitThudVolume = 0.6f;

    [Header("Rotation vid stopPoint")]
    // Faces stopPoint's own rotation directly rather than a relative turn from whatever heading he
    // happened to arrive with - the arrival heading isn't reliably consistent, so a blind "+90 from
    // here" turn ended up facing the wrong way. Rotate stopPoint itself in the Inspector until it
    // faces the door correctly.
    public float turnDuration = 0.4f;

    public NeighborState CurrentState { get; private set; } = NeighborState.Idle;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float currentSpeed;
    private float footstepTimer;
    private int jointsSmoked;
    private bool hasCaughtPlayer;
    private bool waitingForInteract;
    private Coroutine activeRoutine;
    private AudioSource footstepSource;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 1f;
        footstepSource.rolloffMode = AudioRolloffMode.Linear;
        footstepSource.minDistance = 1f;
        footstepSource.maxDistance = footstepMaxDistance;
        footstepSource.reverbZoneMix = 0f;
        if (audioSource != null) footstepSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;

        if (batInHand != null) batInHand.SetActive(false);
        gameObject.SetActive(false);
    }

    // States where he's actively supposed to be walking/running toward a destination -
    // used below to force the Walk/Run animation through NavMeshAgent's autoBraking deceleration
    // (his real velocity dips under the animator's Speed threshold well before he actually arrives,
    // which flickered him into Idle while still visibly sliding).
    static bool IsMovingState(NeighborState state)
    {
        return state == NeighborState.Approaching
            || state == NeighborState.Retreating
            || state == NeighborState.Returning
            || state == NeighborState.Charging
            || state == NeighborState.Leaving;
    }

    void Update()
    {
        if (animator != null && agent != null && agent.enabled)
        {
            currentSpeed = IsMovingState(CurrentState) ? agent.speed : agent.velocity.magnitude;
            animator.SetFloat("Speed", currentSpeed);
        }

        UpdateFootsteps();
    }

    void UpdateFootsteps()
    {
        if (footstepSource == null || footstepSound == null) return;

        if (currentSpeed > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                footstepSource.PlayOneShot(footstepSound, footstepVolume * footstepLoudness);
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    // Called from PlayerStats.SmokeJoint(). Drives the whole 3-strike escalation - joints 1 and 2
    // trigger the yelling beats below, joint 3+ cuts straight to the charge regardless of what he
    // was doing (stopping/resetting a persistent strike counter across sessions is a problem for
    // later - see CLAUDE.md).
    public void OnJointSmoked()
    {
        // Already got you - let the knockout play out.
        if (CurrentState == NeighborState.Attacking) return;

        jointsSmoked++;

        // Neighbour starts deactivated (see Awake) - has to be turned on before StartCoroutine
        // will run on him at all, same as DealerAI.StartDelivery() does before its own approach
        // coroutine. ClimbToStopPoint() also sets this, but that's too late on the very first
        // call since the coroutine that calls it would never have started.
        gameObject.SetActive(true);

        // Always cuts short whatever he's currently doing and jumps straight to the new strike -
        // previously this silently no-op'd (dropping the strike entirely) if he hadn't made it back
        // to Idle yet, which is exactly what happens when joints are smoked faster than his walk
        // cycle. ClimbToStopPoint() resets him to spawn at the start of every routine, so restarting
        // mid-walk/mid-yell is safe.
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        if (dialogueSubtitle != null) dialogueSubtitle.Stop();
        StopVoice();
        waitingForInteract = false;

        // The bat only comes out for the charge - put away again if a routine restarts on strike 1/2.
        if (batInHand != null) batInHand.SetActive(jointsSmoked >= 3);

        if (jointsSmoked >= 3)
        {
            hasCaughtPlayer = false;
            activeRoutine = StartCoroutine(ChargeRoutine());
        }
        else if (jointsSmoked == 1)
        {
            activeRoutine = StartCoroutine(Strike1Routine());
        }
        else if (jointsSmoked == 2)
        {
            activeRoutine = StartCoroutine(Strike2Routine());
        }
    }

    // Click-to-dismiss, exactly like dapping up the dealer. While he's standing at the door waiting
    // (strikes 1 & 2, post-knock), a click is what actually triggers him to start yelling - he won't
    // start on his own. Once he IS yelling, a click cuts him off early and sends him home instead.
    // Once he's charging (strike 3+) this is a no-op either way.
    public void Interact()
    {
        if (CurrentState == NeighborState.Waiting)
        {
            waitingForInteract = false;
            return;
        }

        if (CurrentState != NeighborState.Yelling) return;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        if (dialogueSubtitle != null) dialogueSubtitle.Stop();
        StopVoice();
        activeRoutine = StartCoroutine(LeaveToSpawn());
    }

    IEnumerator Strike1Routine()
    {
        yield return ApproachAndKnock(walkSpeed);

        CurrentState = NeighborState.Yelling;
        if (animator != null) animator.SetTrigger("AngryTrigger");
        PlayVoice(strike1Voice);
        if (dialogueSubtitle != null) yield return StartCoroutine(dialogueSubtitle.PlayRoutine(strike1Lines));

        yield return LeaveToSpawn();
    }

    IEnumerator Strike2Routine()
    {
        yield return ApproachAndKnock(walkSpeed);

        CurrentState = NeighborState.Yelling;
        if (animator != null) animator.SetTrigger("AngryTrigger");
        PlayVoice(strike2FirstVoice);
        if (dialogueSubtitle != null) yield return StartCoroutine(dialogueSubtitle.PlayRoutine(strike2LinesFirst));

        yield return WaitForGestureToFinish();
        CurrentState = NeighborState.Retreating;
        agent.updateRotation = true;
        agent.isStopped = false;
        if (retreatPoint != null)
        {
            agent.SetDestination(retreatPoint.position);
            yield return WaitUntilArrived();
        }

        CurrentState = NeighborState.Returning;
        agent.SetDestination(stopPoint.position);
        yield return WaitUntilArrived();
        agent.isStopped = true;
        agent.updateRotation = false;
        yield return FaceRotationRoutine(stopPoint.rotation);

        CurrentState = NeighborState.Yelling;
        // More escalated the second time round - the pointing/finger-jabbing variant.
        if (animator != null) animator.SetTrigger("AngryPointTrigger");
        PlayVoice(strike2SecondVoice);
        if (dialogueSubtitle != null) yield return StartCoroutine(dialogueSubtitle.PlayRoutine(strike2LinesSecond));

        yield return LeaveToSpawn();
    }

    // Strike 3+: run to the door same as the walk-up above, then keep chasing the player's live
    // position through the apartment instead of stopping at stopPoint. Ends either when he gets
    // within attackRadius (KnockedOut(), placeholder reuse of the drug pass-out) or the player
    // blacks out on their own from smoking a 4th joint first - PlayerStats guards against a double
    // trigger either way.
    IEnumerator ChargeRoutine()
    {
        yield return ClimbToStopPoint(runSpeed);

        CurrentState = NeighborState.Charging;
        agent.updateRotation = true;
        agent.isStopped = false;

        while (!hasCaughtPlayer)
        {
            if (playerStats != null)
            {
                if (agent != null && agent.enabled) agent.SetDestination(playerStats.transform.position);

                if (attackPoint != null)
                {
                    float dist = Vector3.Distance(attackPoint.position, playerStats.transform.position);
                    // If the player already blacked out from the 4th joint they're lying on the floor,
                    // out of reach of his hand - just get next to them instead.
                    Vector3 flat = playerStats.transform.position - transform.position;
                    flat.y = 0f;
                    if (dist <= attackRadius || (playerStats.IsPassedOut && flat.magnitude <= 1.6f))
                        hasCaughtPlayer = true;

                    // Start the line on the way in; if you get away again, cut it so it restarts next time.
                    bool linePlaying = IsKnockoutLinePlaying();
                    if (!linePlaying && flat.magnitude <= lineStartDistance) PlayVoice(knockoutLine);
                    else if (linePlaying && !hasCaughtPlayer && flat.magnitude > lineStartDistance + 4f) StopVoice();
                }
            }

            yield return null;
        }

        yield return AttackRoutine();
    }

    // Caught: stop dead (no more running in place), hold the player still, deliver the line, and
    // swing so the bat lands on the last word. He then just stands there until the player has
    // blacked out - OnPlayerBlackedOut() sends him home.
    IEnumerator AttackRoutine()
    {
        CurrentState = NeighborState.Attacking;
        agent.isStopped = true;
        agent.ResetPath();
        agent.updateRotation = false;

        playerStats.HoldForHit(transform);

        // Usually already mid-line from the run-in (see ChargeRoutine) - time the swing to its end.
        if (!IsKnockoutLinePlaying()) PlayVoice(knockoutLine);
        float lineLeft = knockoutLine != null && audioSource != null ? knockoutLine.length - audioSource.time : 0f;
        float swingAt = Mathf.Max(0.3f, lineLeft - swingImpactTime);
        yield return FacePlayerFor(swingAt);

        if (animator != null) animator.SetTrigger("SwingTrigger");
        yield return FacePlayerFor(swingImpactTime);

        if (audioSource != null && hitThud != null) audioSource.PlayOneShot(hitThud, hitThudVolume);
        playerStats.KnockedOut(); // no-op if they're already passing out on their own
    }

    bool IsKnockoutLinePlaying()
    {
        return audioSource != null && knockoutLine != null && audioSource.isPlaying && audioSource.clip == knockoutLine;
    }

    IEnumerator FacePlayerFor(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            Vector3 dir = playerStats.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
            t += Time.deltaTime;
            yield return null;
        }
    }

    // Called by PlayerStats while the screen is fully black, whatever caused the blackout. Göran is
    // gone when the player wakes up and the strike counter starts over (a blackout is the reset for now).
    public void OnPlayerBlackedOut()
    {
        jointsSmoked = 0;
        if (!gameObject.activeInHierarchy) return;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        if (dialogueSubtitle != null) dialogueSubtitle.Stop();
        waitingForInteract = false;
        activeRoutine = StartCoroutine(GoHomeAfterBlackout());
    }

    IEnumerator GoHomeAfterBlackout()
    {
        // Let the Dorsia line finish over the black screen instead of cutting it mid-word.
        while (audioSource != null && audioSource.isPlaying && audioSource.clip == knockoutLine)
            yield return null;
        StopVoice();

        if (batInHand != null) batInHand.SetActive(false);
        if (agent.enabled) agent.Warp(spawnPosition);
        transform.rotation = spawnRotation;
        CurrentState = NeighborState.Idle;
        activeRoutine = null;
        gameObject.SetActive(false);
    }

    // Walks him from spawn, up in the elevator, to stopPoint. Shared by the strike 1/2 approach and the
    // strike 3+ charge (just called with a different speed).
    IEnumerator ClimbToStopPoint(float speed)
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);

        agent.enabled = true;
        agent.Warp(spawnPosition);
        agent.speed = speed;
        agent.isStopped = false;
        agent.updateRotation = true;
        CurrentState = NeighborState.Approaching;

        yield return RideElevator(true);

        agent.SetDestination(stopPoint.position);
        yield return WaitUntilArrived();
    }

    IEnumerator ApproachAndKnock(float speed)
    {
        yield return ClimbToStopPoint(speed);

        agent.isStopped = true;
        // Stop the agent from fighting the manual face-turn below with its own rotation-to-velocity
        // behaviour - re-enabled again once he actually moves (LeaveToSpawn/ChargeRoutine/retreat).
        agent.updateRotation = false;
        AudioSource bellSource = doorbellAudioSource != null ? doorbellAudioSource : audioSource;
        if (bellSource != null && knockSound != null) bellSource.PlayOneShot(knockSound, knockVolume);

        yield return FaceRotationRoutine(stopPoint.rotation);

        // Doesn't start yelling on his own - waits here until the player clicks him, same beat as
        // taking the jar from the dealer.
        CurrentState = NeighborState.Waiting;
        waitingForInteract = true;
        yield return new WaitUntil(() => !waitingForInteract);
    }

    // Each voice clip is built to the exact length of its subtitle block (pauses included), so it
    // just starts alongside the subtitles - no slicing like DealerAI's single long take.
    void PlayVoice(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    // Cut off mid-sentence when he's interrupted (new strike, or clicked away while yelling).
    void StopVoice()
    {
        if (audioSource == null) return;
        if (audioSource.clip == strike1Voice || audioSource.clip == strike2FirstVoice || audioSource.clip == strike2SecondVoice
            || audioSource.clip == knockoutLine)
            audioSource.Stop();
    }

    // Yelling/AngryPoint only hand back to Idle via exit time (near the end of the clip), so if he
    // starts walking before that he slides off across the floor still mid-gesture - the "floating
    // away" look. Let the gesture play out first, then walk.
    IEnumerator WaitForGestureToFinish()
    {
        if (animator == null) yield break;
        yield return null; // a trigger set this frame hasn't been picked up by the animator yet

        while (animator.IsInTransition(0)
            || animator.GetCurrentAnimatorStateInfo(0).IsName("Yelling")
            || animator.GetCurrentAnimatorStateInfo(0).IsName("AngryPoint"))
        {
            yield return null;
        }
    }

    IEnumerator LeaveToSpawn()
    {
        yield return WaitForGestureToFinish();

        CurrentState = NeighborState.Leaving;
        agent.updateRotation = true;
        agent.isStopped = false;
        agent.speed = walkSpeed;

        yield return RideElevator(false);

        agent.SetDestination(spawnPosition);
        yield return WaitUntilArrived();

        gameObject.SetActive(false);
        CurrentState = NeighborState.Idle;
    }

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

    // Smoothly rotates to an absolute target rotation (stopPoint.rotation) instead of turning a
    // fixed number of degrees relative to wherever he happened to end up facing - the arrival
    // heading off a NavMesh path isn't reliably consistent, so the old relative turn produced a
    // different (and often wrong) final facing depending on exactly how he approached.
    IEnumerator FaceRotationRoutine(Quaternion targetRot)
    {
        Quaternion startRot = transform.rotation;
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
