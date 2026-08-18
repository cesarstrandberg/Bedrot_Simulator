using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class DrugSiteSetup
{
    [MenuItem("Bedrot/Setup Drug Order Page")]
    public static void SetupDrugOrderPage()
    {
        // GameObject.Find skips inactive objects, and Browser_Window is inactive
        // until the laptop boots, so we have to walk the hierarchy manually.
        Transform browserWindowT = FindInactive("Browser_Window");
        if (browserWindowT == null)
        {
            Debug.LogError("DrugSiteSetup: Could not find 'Browser_Window' in the open scene. Open ApartmentScene first.");
            return;
        }
        GameObject browserWindow = browserWindowT.gameObject;

        Transform existing = browserWindow.transform.Find("Drug_Order_Page");
        if (existing != null)
        {
            Debug.LogWarning("DrugSiteSetup: 'Drug_Order_Page' already exists under Browser_Window. Aborting so nothing gets duplicated.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        Transform loginPage = browserWindow.transform.Find("Uni_LoginPage");
        if (loginPage == null)
        {
            Debug.LogError("DrugSiteSetup: Could not find 'Uni_LoginPage' to copy sizing from.");
            return;
        }
        RectTransform loginRect = loginPage.GetComponent<RectTransform>();

        // --- Root panel: same size/anchors as the other pages, black background ---
        GameObject page = new GameObject("Drug_Order_Page", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform pageRect = page.GetComponent<RectTransform>();
        pageRect.SetParent(browserWindow.transform, false);
        pageRect.anchorMin = loginRect.anchorMin;
        pageRect.anchorMax = loginRect.anchorMax;
        pageRect.pivot = loginRect.pivot;
        pageRect.sizeDelta = loginRect.sizeDelta;
        pageRect.anchoredPosition = loginRect.anchoredPosition;

        Image pageImage = page.GetComponent<Image>();
        pageImage.color = Color.black;

        // --- Product image slot ---
        GameObject productImageGO = new GameObject("ProductImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform productRect = productImageGO.GetComponent<RectTransform>();
        productRect.SetParent(pageRect, false);
        productRect.anchorMin = new Vector2(0.5f, 1f);
        productRect.anchorMax = new Vector2(0.5f, 1f);
        productRect.pivot = new Vector2(0.5f, 1f);
        productRect.sizeDelta = new Vector2(1800, 1400);
        productRect.anchoredPosition = new Vector2(0, -200);
        Image productImage = productImageGO.GetComponent<Image>();
        productImage.color = new Color(1f, 1f, 1f, 0.15f); // faint placeholder box until a sprite is assigned
        productImage.sprite = null;

        // --- Order button ---
        GameObject buttonGO = new GameObject("OrderButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.SetParent(pageRect, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(1400, 260);
        buttonRect.anchoredPosition = new Vector2(0, 260);
        Image buttonImage = buttonGO.GetComponent<Image>();
        buttonImage.color = new Color(0.55f, 0.05f, 0.05f, 1f);
        Button orderButton = buttonGO.GetComponent<Button>();

        GameObject buttonTextGO = new GameObject("Text", typeof(RectTransform));
        RectTransform buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.SetParent(buttonRect, false);
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;
        buttonTextRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = "ORDER";
        buttonText.fontSize = 130;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        // --- Status text (feedback after ordering) ---
        GameObject statusGO = new GameObject("StatusText", typeof(RectTransform));
        RectTransform statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.SetParent(pageRect, false);
        statusRect.anchorMin = new Vector2(0.5f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.sizeDelta = new Vector2(2200, 140);
        statusRect.anchoredPosition = new Vector2(0, 80);
        TextMeshProUGUI statusText = statusGO.AddComponent<TextMeshProUGUI>();
        statusText.text = "";
        statusText.fontSize = 70;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;

        // --- Wire up DrugSite component ---
        DrugSite drugSite = page.AddComponent<DrugSite>();
        drugSite.productImage = productImage;
        drugSite.orderButton = orderButton;
        drugSite.statusText = statusText;
        drugSite.playerStats = Object.FindFirstObjectByType<PlayerStats>();

        WebBrowser browser = browserWindow.GetComponent<WebBrowser>();
        if (browser != null)
        {
            browser.drugSite = drugSite;
        }

        page.SetActive(false);

        EditorUtility.SetDirty(browserWindow);
        EditorUtility.SetDirty(page);
        EditorSceneManager.MarkSceneDirty(browserWindow.scene);

        Selection.activeGameObject = page;
        Debug.Log("DrugSiteSetup: Drug_Order_Page created under Browser_Window. Save the scene (Ctrl+S) to keep it. Type 'darkmarket.se' into the in-game browser to open it.");
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
