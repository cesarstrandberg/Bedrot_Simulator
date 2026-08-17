using UnityEngine;

public class StationClickable : MonoBehaviour
{
    public string objectTag;
    public LaptopStation stationManager;

    public void DoClick()
    {
        if(stationManager != null)
        {
            //press on the station to enter the laptop station
            if(!stationManager.isAtLaptop && objectTag == "LaptopStation")
            {
                stationManager.EnterLaptopStation();
                return;
            }

            //Send the click to the master script
            stationManager.OnInteractableClicked(objectTag);
        }
    }

    public void OnMouseDown()
    {
        DoClick();
    }
}
