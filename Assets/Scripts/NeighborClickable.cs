using UnityEngine;

public class NeighborClickable : MonoBehaviour
{
    public NeighborAI neighborAI;

    void OnMouseDown()
    {
        if (neighborAI != null)
        {
            neighborAI.Interact();
        }
    }
}
