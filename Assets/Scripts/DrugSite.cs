using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DrugSite : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerStats playerStats;

    [Header("UI")]
    public Image productImage;
    public Button orderButton;
    public TextMeshProUGUI statusText;

    [Header("Order Settings")]
    public float price = 200f;

    [Header("Dealer")]
    public DealerAI dealerAI;
    public GameObject weedJarPickupPrefab; // Spawned into the world so the player can pick it up (Assets/Prefab/weed_jar.prefab)

    void Start()
    {
        if (orderButton != null)
        {
            orderButton.onClick.AddListener(OrderDrugs);
        }
    }

    public void OpenSite()
    {
        gameObject.SetActive(true);
        if (statusText != null) statusText.text = "";
        if (orderButton != null) orderButton.interactable = true;
    }

    public void CloseSite()
    {
        gameObject.SetActive(false);
    }

    public void OrderDrugs()
    {
        if (playerStats == null) return;

        if (playerStats.money < price)
        {
            if (statusText != null) statusText.text = "Not enough money.";
            return;
        }

        if (statusText != null) statusText.text = "Order placed. The dealer is on their way.";
        if (orderButton != null) orderButton.interactable = false;

        if (dealerAI != null) dealerAI.StartDelivery();

        Debug.Log("Drug order placed for " + price + " kr.");
    }

    // Anropas av DealerAI.Interact() när spelaren tar emot varorna vid dörren
    public void CompleteHandoff()
    {
        if (playerStats == null) return;

        playerStats.money -= price;

        if (weedJarPickupPrefab != null && dealerAI != null && dealerAI.weedJarInHand != null)
        {
            GameObject spawnedJar = Instantiate(weedJarPickupPrefab, dealerAI.weedJarInHand.transform.position, dealerAI.weedJarInHand.transform.rotation);
            spawnedJar.SetActive(true); // Guard against the prefab ever being saved disabled again
        }

        if (statusText != null) statusText.text = "";
        if (orderButton != null) orderButton.interactable = true;

        Debug.Log("Dealer delivered a new jar for " + price + " kr.");
    }
}
