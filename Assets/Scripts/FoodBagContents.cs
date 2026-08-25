using System.Collections.Generic;
using UnityEngine;

// Snapshot of what a specific delivered bag holds, set by FoodSite.CompleteHandoff() at spawn time.
public class FoodBagContents : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public GameObject pickupPrefab;
        public int quantity;
    }

    public List<Entry> entries = new List<Entry>();
}
