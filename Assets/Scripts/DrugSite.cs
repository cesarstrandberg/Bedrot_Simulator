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

        // Jonte is the only dealer - you need his number first (Elias, lecture hall). No cash
        // needed: every jar is on credit.
        if (!GameState.hasDealerNumber)
        {
            if (statusText != null) statusText.text = "You don't know anyone who delivers.";
            return;
        }
        if (dealerAI == null || dealerAI.CurrentState != DealerAI.DealerState.Idle || dealerAI.IsFinalVisit)
        {
            if (statusText != null) statusText.text = "Jonte is already on his way.";
            return;
        }

        if (statusText != null) statusText.text = "Order placed. Jonte is on his way. (On credit)";
        if (orderButton != null) orderButton.interactable = false;

        JonteStory.PrepareDeliveryLines(dealerAI);
        dealerAI.StartDelivery();

        Debug.Log("Drug order placed for " + price + " kr.");
    }

    // Anropas av DealerAI.Interact() när spelaren tar emot varorna vid dörren
    public void CompleteHandoff()
    {
        if (playerStats == null) return;

        // He takes what you've got toward the old tab, then this jar goes on it too
        float paid = 0f;
        if (GameState.dealerDebt > 0f && playerStats.money > 0f)
        {
            paid = Mathf.Min(playerStats.money, GameState.dealerDebt);
            playerStats.money -= paid;
            GameState.dealerDebt -= paid;
        }
        GameState.dealerDebt += price;
        GameState.jarsFromDealer++;
        Notify.Show(paid > 0f
            ? $"Jonte took {paid:0} kr.\n<size=70%>New jar on credit - you owe him {GameState.dealerDebt:0} kr</size>"
            : $"Jar on credit.\n<size=70%>You owe Jonte {GameState.dealerDebt:0} kr</size>", 4f);

        if (weedJarPickupPrefab != null && dealerAI != null && dealerAI.weedJarInHand != null)
        {
            GameObject spawnedJar = Instantiate(weedJarPickupPrefab, dealerAI.weedJarInHand.transform.position, dealerAI.weedJarInHand.transform.rotation);
            spawnedJar.SetActive(true); // Guard against the prefab ever being saved disabled again
        }

        if (statusText != null) statusText.text = "";
        if (orderButton != null) orderButton.interactable = true;

        Debug.Log("Jonte delivered a jar on credit. Debt now " + GameState.dealerDebt + " kr.");
    }
}
