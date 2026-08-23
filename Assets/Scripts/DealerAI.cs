using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DealerAI : MonoBehaviour
{
    public enum DealerState { Idle, Approaching, Waiting, Leaving }

    [Header("Referenser")]
    public Transform stopPoint;      // Dealer_StopPos - var han stannar inne i lägenheten
    public GameObject weedJarInHand; // Jar-modellen i handen (valfri, kan lämnas tom)
    public DrugSite drugSite;

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

    [Header("Ljud")]
    public AudioSource audioSource;
    public AudioClip knockSound;

    [Header("Rotation vid Dealer_StopPos")]
    public float turnAngle = 90f;
    public float turnDuration = 0.4f;

    public DealerState CurrentState { get; private set; } = DealerState.Idle;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

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
        // Under trappklättringen (agent.enabled == false) sätts Speed manuellt i ClimbStairs istället.
        if (animator != null && agent != null && agent.enabled)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    // Anropas från DrugSite.OrderDrugs() när en beställning läggs
    public void StartDelivery()
    {
        if (CurrentState != DealerState.Idle || stopPoint == null) return;

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);

        if (weedJarInHand != null) weedJarInHand.SetActive(true);
        if (audioSource != null && knockSound != null) audioSource.PlayOneShot(knockSound);

        agent.enabled = true;
        agent.Warp(spawnPosition);
        agent.isStopped = false;
        CurrentState = DealerState.Approaching;
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

        CurrentState = DealerState.Waiting;
        agent.isStopped = true;
        StartCoroutine(TurnRoutine(turnAngle));
    }

    // Anropas från DealerClickable när spelaren klickar på honom medan han väntar
    public void Interact()
    {
        if (CurrentState != DealerState.Waiting) return;

        if (drugSite != null) drugSite.CompleteHandoff();
        if (weedJarInHand != null) weedJarInHand.SetActive(false);

        CurrentState = DealerState.Leaving;
        agent.isStopped = false;
        StartCoroutine(LeaveRoutine());
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

                if (animator != null) animator.SetFloat("Speed", moveDelta.magnitude / Time.deltaTime);

                yield return null;
            }
        }

        if (animator != null) animator.SetFloat("Speed", 0f);
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
