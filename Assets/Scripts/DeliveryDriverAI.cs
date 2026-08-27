using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DeliveryDriverAI : MonoBehaviour
{
    public enum DriverState { Idle, Approaching, Waiting, Leaving }

    [Header("References")]
    public Transform stopPoint;      // Where he stops at your door, like Dealer_StopPos
    public GameObject bagInHand;     // The "Bag" child on the right hand bone
    public FoodSite foodSite;

    [Header("Trappa (ingen NavMesh där - flyttas manuellt istället)")]
    // Ordningen han passerar dem i på väg UPP. På väg ner går han dem i omvänd ordning.
    // NavMesh täcker spawn -> bottomStairPos och topStairPos -> stopPoint. Lämna tomma
    // (bottomStairPos == null) för att köra rent NavMesh som förut.
    public Transform bottomStairPos;
    public Transform midStairDownPos;
    public Transform midStairPos;
    public Transform topStairPos;
    public float stairMoveSpeed = 1.5f;
    public float stairTurnSpeed = 6f;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip knockSound;
    public AudioClip footstepSound;
    public float footstepInterval = 0.45f;
    [Range(0f, 1f)] public float footstepVolume = 0.15f;
    [Range(0f, 1f)] public float knockVolume = 0.7f;

    [Header("Visibility")]
    // He's now the invisible food runner (the visible model became the Neighbour prefab) -
    // keep NavMeshAgent/Animator/Collider/Audio active, just hide the mesh.
    public bool hideModel = true;

    [Header("Rotation at stop point")]
    public float turnAngle = 90f;
    public float turnDuration = 0.4f;

    public DriverState CurrentState { get; private set; } = DriverState.Idle;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float currentSpeed;
    private float footstepTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (bagInHand != null) bagInHand.SetActive(false);
        if (hideModel) SetModelVisible(false);
        gameObject.SetActive(false);
    }

    void SetModelVisible(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = visible;
        }
    }

    void Update()
    {
        // Under trappklättringen (agent.enabled == false) sätts Speed manuellt i ClimbStairs istället.
        if (animator != null && agent != null && agent.enabled)
        {
            currentSpeed = agent.velocity.magnitude;
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

    // Called from FoodSite.Checkout() once payment is confirmed
    public void StartDelivery()
    {
        if (CurrentState != DriverState.Idle || stopPoint == null) return;

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);

        if (bagInHand != null) bagInHand.SetActive(true);

        agent.enabled = true;
        agent.Warp(spawnPosition);
        agent.isStopped = false;
        CurrentState = DriverState.Approaching;
        StartCoroutine(ApproachRoutine());
    }

    IEnumerator ApproachRoutine()
    {
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

        CurrentState = DriverState.Waiting;
        agent.isStopped = true;
        if (audioSource != null && knockSound != null) audioSource.PlayOneShot(knockSound, knockVolume);
        yield return StartCoroutine(TurnRoutine(turnAngle));

        DropOffBag();
    }

    // Drops the bag as soon as he arrives - he's invisible, there's nothing to click on anymore.
    // Interact() is kept for DeliveryDriverClickable and just does the same thing defensively.
    void DropOffBag()
    {
        if (CurrentState != DriverState.Waiting) return;

        if (foodSite != null) foodSite.CompleteHandoff();
        if (bagInHand != null) bagInHand.SetActive(false);

        CurrentState = DriverState.Leaving;
        agent.isStopped = false;
        StartCoroutine(LeaveRoutine());
    }

    public void Interact()
    {
        DropOffBag();
    }

    IEnumerator LeaveRoutine()
    {
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
        CurrentState = DriverState.Idle;
    }

    // Väntar tills NavMeshAgent har nått sin nuvarande destination
    IEnumerator WaitUntilArrived()
    {
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }
    }

    // Flyttar honom manuellt (ingen NavMesh) genom en serie punkter på trappan.
    // Animationen fortsätter som vanligt eftersom Speed sätts utifrån hur långt han faktiskt rör sig per frame.
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

                // Bara horisontell riktning för rotationen - annars lutar han nosen neråt i trappan
                // istället för att gå upprätt medan kroppen glider ner/upp.
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

    // Vrider honom ETT extra steg när han når stopPoint, oavsett vilket håll han gick in ifrån
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
