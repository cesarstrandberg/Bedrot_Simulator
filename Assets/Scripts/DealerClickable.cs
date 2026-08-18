using UnityEngine;

public class DealerClickable : MonoBehaviour
{
    public DealerAI dealerAI;

    void OnMouseDown()
    {
        if (dealerAI != null)
        {
            dealerAI.Interact();
        }
    }
}
