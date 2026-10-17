using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class DeliveryDriverAI : MonoBehaviour
{
    public enum DriverState { Idle, Approaching, Waiting, Leaving }

    [Header("References")]
    public Transform stopPoint;      // Where he stops at your door, like Dealer_StopPos
    public GameObject bagInHand;     // The "Bag" child on the right hand bone
    public FoodSite foodSite;

    [Header("Hiss (ersätter trappan - ingen NavMesh i hissen, han glider in/ut manuellt)")]
    // NavMesh täcker spawn -> hissdörren nere och hissdörren uppe -> stopPoint.
    // Lämna tom för att köra rent NavMesh.
    public Elevator elevator;
    [FormerlySerializedAs("stairMoveSpeed")] public float elevatorWalkSpeed = 1.5f;
    [FormerlySerializedAs("stairTurnSpeed")] public float elevatorTurnSpeed = 6f;

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
        // I hissen (agent.enabled == false) sätts Speed manuellt i Elevator.RideNpc istället.
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
        yield return RideElevator(true);

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
        yield return RideElevator(false);

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
