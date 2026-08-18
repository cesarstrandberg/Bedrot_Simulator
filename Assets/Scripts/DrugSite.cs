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
    public int budsPerOrder = 5;

    [Header("Dealer")]
    public DealerAI dealerAI;

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
        JointCraftingStation.globalBudCount += budsPerOrder;

        if (statusText != null) statusText.text = "";
        if (orderButton != null) orderButton.interactable = true;

        Debug.Log("Dealer delivered " + budsPerOrder + " buds for " + price + " kr.");
    }
}
