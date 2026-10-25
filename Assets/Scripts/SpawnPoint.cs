using UnityEngine;

// Where the player appears after coming through a SceneDoor. The door names the id; this marker's
// position and facing (blue arrow) are used.
public class SpawnPoint : MonoBehaviour
{
    public string id;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.4f);
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward);
    }
}
