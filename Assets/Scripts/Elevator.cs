using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Two-stop elevator: sits at either upPoint or downPoint and eases smoothly between them.
// Moves as a kinematic Rigidbody in FixedUpdate so physics bodies standing on its floor (the
// player, a dropped beer can) don't jitter, and anything inside the rider trigger gets carried
// along by the same per-step delta so the player doesn't hover/bounce on the way down.
[RequireComponent(typeof(Rigidbody))]
public class Elevator : MonoBehaviour
{
    [Header("Stops")]
    public Transform upPoint;
    public Transform downPoint;
    public bool startsUp = true;

    [Header("Movement")]
    public float travelTime = 6f;          // seconds for a full up<->down trip
    public float departDelay = 1f;         // pause after pressing the button before it starts moving

    [Header("Ljud")]
    public AudioSource startSound;         // clunk when it starts moving
    public AudioSource moveLoopSound;      // motor hum, set to Loop on the AudioSource
    public AudioSource arriveSound;        // ding/clunk when it arrives

    [Header("NPC-åkning (Dealer/Delivery/Neighbour)")]
    public Transform npcInsideSpot;        // child of the elevator, where an NPC stands while riding (faces the door)
    public Transform npcOutsideUp;         // on the NavMesh right outside the door, hallway floor
    public Transform npcOutsideDown;       // on the NavMesh right outside the door, ground floor

    public bool IsUp { get; private set; }
    public bool IsMoving { get; private set; }

    // An NPC holds this while it calls/rides the elevator so the player's button can't send it off
    // mid-ride. Refreshed every frame by RideNpc and expires on its own, so an NPC whose coroutine
    // gets stopped halfway (the neighbour restarting on a new strike) can't lock the elevator forever.
    public bool IsReserved => reservedBy != null && Time.time < reservedUntil;
    private Object reservedBy;
    private float reservedUntil;

    private Rigidbody rb;
    private readonly HashSet<Rigidbody> riders = new HashSet<Rigidbody>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        IsUp = startsUp;
        Transform start = IsUp ? upPoint : downPoint;
        if (start != null) transform.position = start.position;
    }

    // Called by ElevatorButton. Sends it to whichever stop it isn't at.
    public void Toggle()
    {
        if (IsMoving) return;
        StartCoroutine(MoveRoutine(IsUp ? downPoint : upPoint, !IsUp));
    }

    public void GoDown() { if (IsUp && !IsMoving) StartCoroutine(MoveRoutine(downPoint, false)); }
    public void GoUp() { if (!IsUp && !IsMoving) StartCoroutine(MoveRoutine(upPoint, true)); }

    public Transform NpcOutsideSpot(bool up) => up ? npcOutsideUp : npcOutsideDown;

    bool TryReserve(Object who)
    {
        if (IsReserved && reservedBy != who) return false;
        if (IsMoving && reservedBy != who) return false; // let a player-ridden trip finish first
        reservedBy = who;
        reservedUntil = Time.time + 1f;
        return true;
    }

    // Takes an NPC (whose NavMeshAgent the caller has already walked to NpcOutsideSpot(!toUp) and
    // then disabled) through the elevator: calls it to his floor if needed, slides him in, rides,
    // slides him out to NpcOutsideSpot(toUp). Caller re-enables the agent and Warps it there after.
    // Same manual position-slide + Speed-from-actual-movement idea the old ClimbStairs used.
    public IEnumerator RideNpc(Transform npc, Animator animator, bool toUp, float moveSpeed, float turnSpeed)
    {
        bool fromUp = !toUp;

        while (!TryReserve(npc)) { SetSpeed(animator, 0f); yield return null; }

        // Call it to his floor and wait for it
        while (IsMoving || IsUp != fromUp)
        {
            TryReserve(npc);
            if (!IsMoving) { if (fromUp) GoUp(); else GoDown(); }
            SetSpeed(animator, 0f);
            yield return null;
        }

        yield return SlideNpc(npc, animator, npcInsideSpot, moveSpeed, turnSpeed);

        if (toUp) GoUp(); else GoDown();
        while (IsMoving)
        {
            TryReserve(npc);
            npc.position = npcInsideSpot.position;
            npc.rotation = Quaternion.Slerp(npc.rotation, npcInsideSpot.rotation, turnSpeed * Time.deltaTime);
            SetSpeed(animator, 0f);
            yield return null;
        }

        yield return SlideNpc(npc, animator, NpcOutsideSpot(toUp), moveSpeed, turnSpeed);
        reservedBy = null;
    }

    IEnumerator SlideNpc(Transform npc, Animator animator, Transform target, float moveSpeed, float turnSpeed)
    {
        while (Vector3.Distance(npc.position, target.position) > 0.05f)
        {
            TryReserve(npc);
            Vector3 fromPos = npc.position;
            npc.position = Vector3.MoveTowards(npc.position, target.position, moveSpeed * Time.deltaTime);

            Vector3 moveDelta = npc.position - fromPos;
            Vector3 flatDelta = new Vector3(moveDelta.x, 0f, moveDelta.z);
            if (flatDelta.sqrMagnitude > 0.0001f)
            {
                npc.rotation = Quaternion.Slerp(npc.rotation, Quaternion.LookRotation(flatDelta.normalized), turnSpeed * Time.deltaTime);
            }

            SetSpeed(animator, moveDelta.magnitude / Time.deltaTime);
            yield return null;
        }
        SetSpeed(animator, 0f);
    }

    static void SetSpeed(Animator animator, float speed)
    {
        if (animator != null) animator.SetFloat("Speed", speed);
    }

    IEnumerator MoveRoutine(Transform target, bool endsUp)
    {
        if (target == null)
        {
            Debug.LogWarning("Elevator: upPoint/downPoint not assigned.", this);
            yield break;
        }

        IsMoving = true;
        yield return new WaitForSeconds(departDelay);

        if (startSound != null) startSound.Play();
        if (moveLoopSound != null) moveLoopSound.Play();

        Vector3 from = rb.position;
        Vector3 to = target.position;
        float t = 0f;

        while (t < 1f)
        {
            yield return new WaitForFixedUpdate();
            t = Mathf.Min(1f, t + Time.fixedDeltaTime / Mathf.Max(0.01f, travelTime));
            MoveTo(Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
        }

        if (moveLoopSound != null) moveLoopSound.Stop();
        if (arriveSound != null) arriveSound.Play();

        IsUp = endsUp;
        IsMoving = false;
    }

    void MoveTo(Vector3 newPos)
    {
        Vector3 delta = newPos - rb.position;
        rb.MovePosition(newPos);

        foreach (Rigidbody rider in riders)
        {
            if (rider != null && !rider.isKinematic) rider.position += delta;
        }
    }

    // Trigger messages from child colliders bubble up to the Rigidbody's GameObject (this one),
    // so the RiderZone trigger can just be a child BoxCollider with no script of its own.
    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody != rb) riders.Add(other.attachedRigidbody);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null) riders.Remove(other.attachedRigidbody);
    }

    [Header("Spot setup (editor only)")]
    public float floorHeight = 3f;         // used by the context menu below for the down spot

#if UNITY_EDITOR
    // Right-click the component header -> "Create Up/Down Spots". Drops two empty markers next to
    // the elevator (not as children, they'd ride along with it): Up at the current position, Down
    // floorHeight below it. Drag the Down one to the exact spot afterwards.
    [ContextMenu("Create Up/Down Spots")]
    void CreateSpots()
    {
        var up = new GameObject("ElevatorPos_Up").transform;
        var down = new GameObject("ElevatorPos_Down").transform;
        UnityEditor.Undo.RegisterCreatedObjectUndo(up.gameObject, "Create Elevator Spots");
        UnityEditor.Undo.RegisterCreatedObjectUndo(down.gameObject, "Create Elevator Spots");

        up.SetParent(transform.parent, false);
        down.SetParent(transform.parent, false);
        up.SetPositionAndRotation(transform.position, transform.rotation);
        down.SetPositionAndRotation(transform.position + Vector3.down * floorHeight, transform.rotation);

        UnityEditor.Undo.RecordObject(this, "Assign Elevator Spots");
        upPoint = up;
        downPoint = down;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        if (upPoint != null) Gizmos.DrawWireCube(upPoint.position, Vector3.one * 0.5f);
        if (downPoint != null) Gizmos.DrawWireCube(downPoint.position, Vector3.one * 0.5f);
        if (upPoint != null && downPoint != null) Gizmos.DrawLine(upPoint.position, downPoint.position);
    }
#endif
}
