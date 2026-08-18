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

        playerStats.money -= price;

        if (statusText != null) statusText.text = "Order placed. The dealer is on their way.";
        if (orderButton != null) orderButton.interactable = false;

        Debug.Log("Drug order placed for " + price + " kr.");
    }
}
