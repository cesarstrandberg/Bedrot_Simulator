using UnityEngine;
using UnityEngine.AI;

// City walker: wanders between random sidewalk points on the city NavMesh, pauses a bit at each,
// and drives the animator's "Speed" (works with both DealerAnimator and NeighborAnimator).
// Streets are NavMesh area 3 ("road", set on the street colliders before baking) with a high cost,
// so they stick to sidewalks and only cross when they have to.
[RequireComponent(typeof(NavMeshAgent))]
public class Pedestrian : MonoBehaviour
{
    public float wanderRadius = 30f;
    public Vector2 speedRange = new Vector2(1.1f, 1.6f);
    public Vector2 pauseRange = new Vector2(1f, 6f);

    const int RoadArea = 3;

    NavMeshAgent agent;
    Animator animator;
    float resumeAt;
    bool pausing;
    float holdUntil;   // set by Hold() - e.g. a buyer stopping while you talk to him

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        NavMesh.SetAreaCost(RoadArea, 8f);
        agent.speed = Random.Range(speedRange.x, speedRange.y);
        agent.angularSpeed = 300f;
        agent.acceleration = 6f;
        agent.stoppingDistance = 0.3f;
        if (animator != null) animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        PickDestination();
    }

    // Stop and stand still for a moment (keeps getting extended while called every frame).
    public void Hold(Vector3 lookAt, float seconds = 0.5f)
    {
        holdUntil = Time.time + seconds;
        Vector3 dir = lookAt - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    // Most people go home between 01:00 and 06:00 - roughly 1 in 3 stays out.
    bool stayOutAtNight;
    Renderer[] renderers;
    bool hiddenForNight;

    void Awake()
    {
        stayOutAtNight = Random.value < 0.33f;
        renderers = GetComponentsInChildren<Renderer>();
    }

    void UpdateNight()
    {
        if (DayClock.Instance == null || stayOutAtNight) return;
        float h = DayClock.Instance.Hour;
        bool night = h >= 1f && h < 6f;
        if (night == hiddenForNight) return;
        hiddenForNight = night;
        foreach (var r in renderers) if (r != null) r.enabled = !night;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = !night;
    }

    void Update()
    {
        UpdateNight();
        if (agent == null || !agent.isOnNavMesh) return;

        bool held = Time.time < holdUntil;
        agent.isStopped = held;
        // Capped at their walking speed: two walkers bumping into each other can spike the agent's
        // velocity, which would flick Göran-based ones into the Run animation.
        if (animator != null) animator.SetFloat("Speed", held ? 0f : Mathf.Min(agent.velocity.magnitude, agent.speed));
        if (held) return;

        if (agent.pathPending) return;
        if (agent.remainingDistance > agent.stoppingDistance + 0.2f) return;

        if (!pausing)
        {
            pausing = true;
            resumeAt = Time.time + Random.Range(pauseRange.x, pauseRange.y);
        }
        else if (Time.time >= resumeAt)
        {
            pausing = false;
            PickDestination();
        }
    }

    void PickDestination()
    {
        int walkable = 1 << NavMesh.GetAreaFromName("Walkable");
        var path = new NavMeshPath();
        for (int i = 0; i < 12; i++)
        {
            Vector2 r = Random.insideUnitCircle * wanderRadius;
            Vector3 p = transform.position + new Vector3(r.x, 0f, r.y);
            if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, walkable)) continue;
            if (Mathf.Abs(hit.position.y - transform.position.y) > 0.6f) continue; // no rooftops
            if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path)) continue;
            if (path.status != NavMeshPathStatus.PathComplete) continue;
            agent.SetPath(path);
            return;
        }
        pausing = true;
        resumeAt = Time.time + 2f; // try again shortly
    }
}
