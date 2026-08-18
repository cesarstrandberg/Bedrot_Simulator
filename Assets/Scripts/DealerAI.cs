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
        if (animator != null && agent != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        if (CurrentState == DealerState.Approaching && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            CurrentState = DealerState.Waiting;
            agent.isStopped = true;
            StartCoroutine(TurnRoutine(turnAngle));
        }
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

    // Anropas från DrugSite.OrderDrugs() när en beställning läggs
    public void StartDelivery()
    {
        if (CurrentState != DealerState.Idle || stopPoint == null) return;

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);

        if (weedJarInHand != null) weedJarInHand.SetActive(true);
        if (audioSource != null && knockSound != null) audioSource.PlayOneShot(knockSound);

        agent.Warp(spawnPosition);
        agent.isStopped = false;
        agent.SetDestination(stopPoint.position);
        CurrentState = DealerState.Approaching;
    }

    // Anropas från DealerClickable när spelaren klickar på honom medan han väntar
    public void Interact()
    {
        if (CurrentState != DealerState.Waiting) return;

        if (drugSite != null) drugSite.CompleteHandoff();
        if (weedJarInHand != null) weedJarInHand.SetActive(false);

        CurrentState = DealerState.Leaving;
        agent.isStopped = false;
        agent.SetDestination(spawnPosition);
        StartCoroutine(DespawnWhenArrived());
    }

    IEnumerator DespawnWhenArrived()
    {
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        gameObject.SetActive(false);
        CurrentState = DealerState.Idle;
    }
}
