using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NeighborAI : MonoBehaviour
{
    public enum NeighborState { Idle, Approaching, Waiting, Yelling, Retreating, Returning, Charging, Leaving }

    [Header("References")]
    public Transform stopPoint;          // Where he stops right outside the door, like Dealer_StopPos
    public Transform retreatPoint;       // Strike 2 only - how far back toward his own place he stalks off before turning around
    public PlayerStats playerStats;
    public Transform attackPoint;        // His hand bone - how close this needs to get to the player to "catch" them
    public float attackRadius = 1.2f;

    [Header("Trappa (ingen NavMesh där - flyttas manuellt istället)")]
    public Transform bottomStairPos;
    public Transform midStairDownPos;
    public Transform midStairPos;
    public Transform topStairPos;
    public float stairMoveSpeed = 1.5f;
    public float stairTurnSpeed = 6f;

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

    [Header("Dialogue")]
    public DialogueSubtitle dialogueSubtitle;
    public DialogueSubtitle.Line[] strike1Lines;
    public DialogueSubtitle.Line[] strike2LinesFirst;
    public DialogueSubtitle.Line[] strike2LinesSecond;

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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

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

    // Called from PlayerStats.SmokeJoint(). Drives the whole 3-strike escalation - joints 1 and 2
    // trigger the yelling beats below, joint 3+ cuts straight to the charge regardless of what he
    // was doing (stopping/resetting a persistent strike counter across sessions is a problem for
    // later - see CLAUDE.md).
    public void OnJointSmoked()
    {
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
        waitingForInteract = false;

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
        activeRoutine = StartCoroutine(LeaveToSpawn());
    }

    IEnumerator Strike1Routine()
    {
        yield return ApproachAndKnock(walkSpeed);

        CurrentState = NeighborState.Yelling;
        if (animator != null) animator.SetTrigger("AngryTrigger");
        if (dialogueSubtitle != null) yield return StartCoroutine(dialogueSubtitle.PlayRoutine(strike1Lines));

        yield return LeaveToSpawn();
    }

    IEnumerator Strike2Routine()
    {
        yield return ApproachAndKnock(walkSpeed);

        CurrentState = NeighborState.Yelling;
        if (animator != null) animator.SetTrigger("AngryTrigger");
        if (dialogueSubtitle != null) yield return StartCoroutine(dialogueSubtitle.PlayRoutine(strike2LinesFirst));

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
                    if (dist <= attackRadius)
                    {
                        hasCaughtPlayer = true;
                        playerStats.KnockedOut();
                    }
                }
            }

            yield return null;
        }
    }

    // Walks him from spawn up the stairs to stopPoint. Shared by the strike 1/2 approach and the
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

        if (bottomStairPos != null)
        {
            agent.SetDestination(bottomStairPos.position);
            yield return WaitUntilArrived();

            yield return ClimbStairs(bottomStairPos, midStairDownPos, midStairPos, topStairPos);

            agent.enabled = true;
            agent.Warp(topStairPos.position);
            agent.isStopped = false;
        }

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

    IEnumerator LeaveToSpawn()
    {
        CurrentState = NeighborState.Leaving;
        agent.updateRotation = true;
        agent.isStopped = false;
        agent.speed = walkSpeed;

        if (topStairPos != null)
        {
            agent.SetDestination(topStairPos.position);
            yield return WaitUntilArrived();

            yield return ClimbStairs(topStairPos, midStairPos, midStairDownPos, bottomStairPos);

            agent.enabled = true;
            agent.Warp(bottomStairPos.position);
            agent.isStopped = false;
        }

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

    IEnumerator ClimbStairs(params Transform[] waypoints)
    {
        agent.isStopped = true;
        agent.enabled = false;

        foreach (var point in waypoints)
        {
            if (point == null) continue;

            while (Vector3.Distance(transform.position, point.position) > 0.05f)
            {
                Vector3 fromPos = transform.position;
                transform.position = Vector3.MoveTowards(transform.position, point.position, stairMoveSpeed * Time.deltaTime);

                Vector3 moveDelta = transform.position - fromPos;

                Vector3 flatDelta = new Vector3(moveDelta.x, 0f, moveDelta.z);
                if (flatDelta.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatDelta.normalized), stairTurnSpeed * Time.deltaTime);
                }

                currentSpeed = moveDelta.magnitude / Time.deltaTime;
                if (animator != null) animator.SetFloat("Speed", currentSpeed);

                yield return null;
            }
        }

        currentSpeed = 0f;
        if (animator != null) animator.SetFloat("Speed", 0f);
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
