using UnityEngine;

public class DeliveryDriverClickable : MonoBehaviour
{
    public DeliveryDriverAI deliveryDriverAI;

    void OnMouseDown()
    {
        if (deliveryDriverAI != null)
        {
            deliveryDriverAI.Interact();
        }
    }
}
