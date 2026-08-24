using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FoodSite : MonoBehaviour
{
    [System.Serializable]
    public class FoodItem
    {
        public string itemName;
        public float price;
        public TextMeshProUGUI quantityText;
        public Button incrementButton;
        public Button decrementButton;

        [System.NonSerialized] public int quantity;
    }

    [Header("Player Reference")]
    public PlayerStats playerStats;

    [Header("Menu Items")]
    public FoodItem[] items;

    [Header("Cart")]
    public TextMeshProUGUI cartTotalText;
    public Button checkoutButton;
    public TextMeshProUGUI statusText;

    void Start()
    {
        foreach (FoodItem item in items)
        {
            FoodItem captured = item;
            if (item.incrementButton != null) item.incrementButton.onClick.AddListener(() => ChangeQuantity(captured, 1));
            if (item.decrementButton != null) item.decrementButton.onClick.AddListener(() => ChangeQuantity(captured, -1));
            UpdateItemDisplay(item);
        }

        if (checkoutButton != null) checkoutButton.onClick.AddListener(Checkout);

        UpdateCartTotal();
    }

    public void OpenSite()
    {
        gameObject.SetActive(true);
        ResetCart();
        if (statusText != null) statusText.text = "";
    }

    public void CloseSite()
    {
        gameObject.SetActive(false);
    }

    void ChangeQuantity(FoodItem item, int delta)
    {
        item.quantity = Mathf.Max(0, item.quantity + delta);
        UpdateItemDisplay(item);
        UpdateCartTotal();
        if (statusText != null) statusText.text = "";
    }

    void UpdateItemDisplay(FoodItem item)
    {
        if (item.quantityText != null) item.quantityText.text = item.quantity.ToString();
    }

    float ComputeTotal()
    {
        float total = 0f;
        foreach (FoodItem item in items)
        {
            total += item.quantity * item.price;
        }
        return total;
    }

    void UpdateCartTotal()
    {
        if (cartTotalText != null) cartTotalText.text = "Total: " + ComputeTotal() + " kr";
    }

    void ResetCart()
    {
        foreach (FoodItem item in items)
        {
            item.quantity = 0;
            UpdateItemDisplay(item);
        }
        UpdateCartTotal();
    }

    public void Checkout()
    {
        float total = ComputeTotal();

        if (total <= 0f)
        {
            if (statusText != null) statusText.text = "Cart is empty.";
            return;
        }

        if (playerStats == null) return;

        if (playerStats.money < total)
        {
            if (statusText != null) statusText.text = "Not enough money.";
            return;
        }

        playerStats.money -= total;
        if (statusText != null) statusText.text = "Order placed.";

        Debug.Log("Food order placed for " + total + " kr.");

        ResetCart();
    }
}
