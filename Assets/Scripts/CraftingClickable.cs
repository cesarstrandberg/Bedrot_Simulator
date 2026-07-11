using UnityEngine;

public class CraftingClickable : MonoBehaviour
{
    //Tag to identify which object was clicked
    public string objectTag;
    public JointCraftingStation stationManager;

    void OnMouseDown()
    {
        if(stationManager != null)
        {
            stationManager.OnInteractableClicked(objectTag);
        }
    }
}
