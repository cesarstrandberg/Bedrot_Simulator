using UnityEngine;

public class CraftingClickable : MonoBehaviour
{
    public string objectTag;
    public JointCraftingStation stationManager;

    public void DoClick()
    {
        if (stationManager != null)
        {
            // Om vi klickar på BORDET/BRICKAN -> Starta spelet!
            if (!stationManager.isMinigameActive && objectTag == "Table")
            {
                stationManager.StartMinigame();
                return;
            }

            // Skicka klicket till Master-skriptet
            stationManager.OnInteractableClicked(objectTag);
        }
    }

    // Denna gör att din synliga muspekare kan klicka på sakerna när kameran har bytts!
    void OnMouseDown()
    {
        DoClick();
    }
}