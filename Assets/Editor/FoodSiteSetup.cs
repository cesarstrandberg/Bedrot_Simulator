using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class FoodSiteSetup
{
    struct MenuItemDef
    {
        public string name;
        public float price;
        public MenuItemDef(string name, float price) { this.name = name; this.price = price; }
    }

    static readonly MenuItemDef[] Menu = new MenuItemDef[]
    {
        new MenuItemDef("Chicken Nuggets", 60f),
        new MenuItemDef("Noodles", 45f),
        new MenuItemDef("Beer", 30f),
        new MenuItemDef("Chips", 25f),
    };

    [MenuItem("Bedrot/Setup Food Order Page")]
    public static void SetupFoodOrderPage()
    {
        Transform browserWindowT = FindInactive("Browser_Window");
        if (browserWindowT == null)
        {
            Debug.LogError("FoodSiteSetup: Could not find 'Browser_Window' in the open scene. Open ApartmentScene first.");
            return;
        }
        GameObject browserWindow = browserWindowT.gameObject;

        Transform existing = browserWindow.transform.Find("Food_Order_Page");
        if (existing != null)
        {
            Debug.LogWarning("FoodSiteSetup: 'Food_Order_Page' already exists under Browser_Window. Aborting so nothing gets duplicated.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        Transform loginPage = browserWindow.transform.Find("Uni_LoginPage");
        if (loginPage == null)
        {
            Debug.LogError("FoodSiteSetup: Could not find 'Uni_LoginPage' to copy sizing from.");
            return;
        }
        RectTransform loginRect = loginPage.GetComponent<RectTransform>();

        // --- Root panel: same size/anchors as the other pages, black background ---
        GameObject page = new GameObject("Food_Order_Page", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform pageRect = page.GetComponent<RectTransform>();
        pageRect.SetParent(browserWindow.transform, false);
        pageRect.anchorMin = loginRect.anchorMin;
        pageRect.anchorMax = loginRect.anchorMax;
        pageRect.pivot = loginRect.pivot;
        pageRect.sizeDelta = loginRect.sizeDelta;
        pageRect.anchoredPosition = loginRect.anchoredPosition;

        Image pageImage = page.GetComponent<Image>();
        pageImage.color = Color.black;

        float halfWidth = pageRect.sizeDelta.x * 0.5f;

        const float rowHeight = 600f;
        const float rowGap = 60f;
        const float topPadding = 100f;
        const float imageSize = 480f;
        const float nameWidth = 2000f;
        const float priceWidth = 700f;
        const float stepperButtonSize = 180f;
        const float quantityWidth = 260f;
        const float elementGap = 80f;
        const float rowContentLocalY = 90f; // vertical offset of the text/stepper band within a row
        const float stepperLocalY = rowContentLocalY + 60f; // steppers are shorter than the text band, so nudge down to stay centered on it

        FoodSite.FoodItem[] foodItems = new FoodSite.FoodItem[Menu.Length];

        for (int i = 0; i < Menu.Length; i++)
        {
            MenuItemDef def = Menu[i];
            string baseName = def.name.Replace(" ", "");
            float rowTopY = -(topPadding + i * (rowHeight + rowGap));
            float leftEdge = -halfWidth + 150f;

            // --- Product image placeholder ---
            GameObject imageGO = new GameObject(baseName + "_Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform imageRect = imageGO.GetComponent<RectTransform>();
            imageRect.SetParent(pageRect, false);
            imageRect.anchorMin = new Vector2(0.5f, 1f);
            imageRect.anchorMax = new Vector2(0.5f, 1f);
            imageRect.pivot = new Vector2(0f, 1f);
            imageRect.sizeDelta = new Vector2(imageSize, imageSize);
            imageRect.anchoredPosition = new Vector2(leftEdge, rowTopY);
            Image itemImage = imageGO.GetComponent<Image>();
            itemImage.color = new Color(1f, 1f, 1f, 0.15f);
            itemImage.sprite = null;

            float nameX = leftEdge + imageSize + elementGap;

            // --- Name label ---
            GameObject nameGO = new GameObject(baseName + "_Name", typeof(RectTransform));
            RectTransform nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.SetParent(pageRect, false);
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.sizeDelta = new Vector2(nameWidth, 300f);
            nameRect.anchoredPosition = new Vector2(nameX, rowTopY - rowContentLocalY);
            TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = def.name;
            nameText.fontSize = 100;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = Color.white;

            float priceX = nameX + nameWidth + elementGap;

            // --- Price label ---
            GameObject priceGO = new GameObject(baseName + "_Price", typeof(RectTransform));
            RectTransform priceRect = priceGO.GetComponent<RectTransform>();
            priceRect.SetParent(pageRect, false);
            priceRect.anchorMin = new Vector2(0.5f, 1f);
            priceRect.anchorMax = new Vector2(0.5f, 1f);
            priceRect.pivot = new Vector2(0f, 1f);
            priceRect.sizeDelta = new Vector2(priceWidth, 300f);
            priceRect.anchoredPosition = new Vector2(priceX, rowTopY - rowContentLocalY);
            TextMeshProUGUI priceText = priceGO.AddComponent<TextMeshProUGUI>();
            priceText.text = def.price + " kr";
            priceText.fontSize = 90;
            priceText.alignment = TextAlignmentOptions.MidlineLeft;
            priceText.color = Color.white;

            float stepperX = priceX + priceWidth + elementGap;

            // --- Quantity stepper: [-] [qty] [+] ---
            Button decrementButton = CreateStepperButton(pageRect, baseName + "_Decrement", "-", stepperX, rowTopY - stepperLocalY, stepperButtonSize);

            float quantityX = stepperX + stepperButtonSize + elementGap * 0.5f;
            GameObject qtyGO = new GameObject(baseName + "_Quantity", typeof(RectTransform));
            RectTransform qtyRect = qtyGO.GetComponent<RectTransform>();
            qtyRect.SetParent(pageRect, false);
            qtyRect.anchorMin = new Vector2(0.5f, 1f);
            qtyRect.anchorMax = new Vector2(0.5f, 1f);
            qtyRect.pivot = new Vector2(0f, 1f);
            qtyRect.sizeDelta = new Vector2(quantityWidth, stepperButtonSize);
            qtyRect.anchoredPosition = new Vector2(quantityX, rowTopY - stepperLocalY);
            TextMeshProUGUI qtyText = qtyGO.AddComponent<TextMeshProUGUI>();
            qtyText.text = "0";
            qtyText.fontSize = 100;
            qtyText.alignment = TextAlignmentOptions.Center;
            qtyText.color = Color.white;

            float incrementX = quantityX + quantityWidth + elementGap * 0.5f;
            Button incrementButton = CreateStepperButton(pageRect, baseName + "_Increment", "+", incrementX, rowTopY - stepperLocalY, stepperButtonSize);

            foodItems[i] = new FoodSite.FoodItem
            {
                itemName = def.name,
                price = def.price,
                quantityText = qtyText,
                incrementButton = incrementButton,
                decrementButton = decrementButton
            };
        }

        // --- Cart total / checkout / status, anchored to the bottom of the page ---
        GameObject cartTotalGO = new GameObject("CartTotalText", typeof(RectTransform));
        RectTransform cartTotalRect = cartTotalGO.GetComponent<RectTransform>();
        cartTotalRect.SetParent(pageRect, false);
        cartTotalRect.anchorMin = new Vector2(0.5f, 0f);
        cartTotalRect.anchorMax = new Vector2(0.5f, 0f);
        cartTotalRect.pivot = new Vector2(0.5f, 0f);
        cartTotalRect.sizeDelta = new Vector2(2200, 100);
        cartTotalRect.anchoredPosition = new Vector2(0, 400);
        TextMeshProUGUI cartTotalText = cartTotalGO.AddComponent<TextMeshProUGUI>();
        cartTotalText.text = "Total: 0 kr";
        cartTotalText.fontSize = 80;
        cartTotalText.alignment = TextAlignmentOptions.Center;
        cartTotalText.color = Color.white;

        GameObject checkoutGO = new GameObject("CheckoutButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform checkoutRect = checkoutGO.GetComponent<RectTransform>();
        checkoutRect.SetParent(pageRect, false);
        checkoutRect.anchorMin = new Vector2(0.5f, 0f);
        checkoutRect.anchorMax = new Vector2(0.5f, 0f);
        checkoutRect.pivot = new Vector2(0.5f, 0f);
        checkoutRect.sizeDelta = new Vector2(1400, 220);
        checkoutRect.anchoredPosition = new Vector2(0, 150);
        Image checkoutImage = checkoutGO.GetComponent<Image>();
        checkoutImage.color = new Color(0.55f, 0.05f, 0.05f, 1f);
        Button checkoutButton = checkoutGO.GetComponent<Button>();

        GameObject checkoutTextGO = new GameObject("Text", typeof(RectTransform));
        RectTransform checkoutTextRect = checkoutTextGO.GetComponent<RectTransform>();
        checkoutTextRect.SetParent(checkoutRect, false);
        checkoutTextRect.anchorMin = Vector2.zero;
        checkoutTextRect.anchorMax = Vector2.one;
        checkoutTextRect.sizeDelta = Vector2.zero;
        checkoutTextRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI checkoutText = checkoutTextGO.AddComponent<TextMeshProUGUI>();
        checkoutText.text = "CHECKOUT";
        checkoutText.fontSize = 110;
        checkoutText.alignment = TextAlignmentOptions.Center;
        checkoutText.color = Color.white;

        GameObject statusGO = new GameObject("StatusText", typeof(RectTransform));
        RectTransform statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.SetParent(pageRect, false);
        statusRect.anchorMin = new Vector2(0.5f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.sizeDelta = new Vector2(2200, 100);
        statusRect.anchoredPosition = new Vector2(0, 20);
        TextMeshProUGUI statusText = statusGO.AddComponent<TextMeshProUGUI>();
        statusText.text = "";
        statusText.fontSize = 55;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;

        // --- Wire up FoodSite component ---
        FoodSite foodSite = page.AddComponent<FoodSite>();
        foodSite.playerStats = Object.FindFirstObjectByType<PlayerStats>();
        foodSite.items = foodItems;
        foodSite.cartTotalText = cartTotalText;
        foodSite.checkoutButton = checkoutButton;
        foodSite.statusText = statusText;

        WebBrowser browser = browserWindow.GetComponent<WebBrowser>();
        if (browser != null)
        {
            browser.foodSite = foodSite;
        }

        page.SetActive(false);

        EditorUtility.SetDirty(browserWindow);
        EditorUtility.SetDirty(page);
        EditorSceneManager.MarkSceneDirty(browserWindow.scene);

        Selection.activeGameObject = page;
        Debug.Log("FoodSiteSetup: Food_Order_Page created under Browser_Window with a " + Menu.Length + "-item cart. Save the scene (Ctrl+S) to keep it. Type 'snabbmat.se' into the in-game browser to open it.");
    }

    static Button CreateStepperButton(RectTransform parent, string name, string label, float x, float y, float size)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.sizeDelta = new Vector2(size, size);
        buttonRect.anchoredPosition = new Vector2(x, y);
        Image buttonImage = buttonGO.GetComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        Button button = buttonGO.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(buttonRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 130;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return button;
    }

    static Transform FindInactive(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform result = FindRecursive(root.transform, name);
            if (result != null) return result;
        }
        return null;
    }

    static Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
