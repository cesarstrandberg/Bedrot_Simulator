using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WebBrowser : MonoBehaviour
{
    [Header("Sidor inuti fönstret")]
    public GameObject googleSidan; // Din vita sök-sida
    public DrugSite drugSite; // Beställningssidan för dealern

    [Header("Sökfält & URL")]
    public TMP_InputField searchInputField; // Ditt vit-rundade skrivfält
    public TextMeshProUGUI urlBarText;      // URL-texten uppe i din gråa list (Top_Nav_Bar)

    [Header("Ljud")]
    public AudioSource audioSource;
    public AudioClip keyPressSound;   // Smattrande för varje bokstav
    public AudioClip mouseClickSound; // Klick för knappar
    public AudioClip errorSound;      // Fel-ljud om man söker fel

    private string lastInputText = "";

    void Start()
    {
        // Se till att vi alltid startar på vita Google-sidan när webbläsaren öppnas
        GoToSearchPage();

        if (searchInputField != null)
        {
            // 1. Spela ljud när vi skriver bokstäver
            searchInputField.onValueChanged.AddListener(OnTyping);

            // 2. Lyssna på när vi trycker ENTER (Return) på tangentbordet!
            searchInputField.onSubmit.AddListener(OnEnterPressed);
        }
    }

    public void PlayClickSound()
    {
        if (audioSource != null && mouseClickSound != null)
        {
            audioSource.PlayOneShot(mouseClickSound);
        }
    }

    // Spelar tangentbordsljud för exakt VARJE tecken man skriver i rutan!
    void OnTyping(string currentText)
    {
        if (currentText.Length != lastInputText.Length)
        {
            if (audioSource != null && keyPressSound != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f); // Gör att smattrandet låter äkta!
                audioSource.PlayOneShot(keyPressSound);
            }
        }
        lastInputText = currentText;
    }

    // Denna körs automatiskt så fort du trycker ENTER i sökfältet
    void OnEnterPressed(string typedText)
    {
        PerformSearch();
    }

    // Denna kopplar vi till din bakåt-pil (<) högst upp i Top_Nav_Bar.
    // Går ETT steg bakåt oavsett vilken sida som är öppen just nu.
    public void GoBack()
    {
        // Dealer-sidan är öppen -> ett steg bakåt är Giggle-sökningen
        if (drugSite != null && drugSite.gameObject.activeSelf)
        {
            GoToSearchPage();
            return;
        }

        UniversityPortal portal = GetComponent<UniversityPortal>();
        if (portal != null)
        {
            if (portal.resultPage != null && portal.resultPage.activeSelf) { portal.ReturnToDashboard(); return; }
            if (portal.examPage != null && portal.examPage.activeSelf) { portal.BackFromExam(); return; }
            if (portal.instructionsPage != null && portal.instructionsPage.activeSelf) { portal.BackFromInstructions(); return; }
            if (portal.dashboardPage != null && portal.dashboardPage.activeSelf) { portal.LogOutToLogin(); return; }
            if (portal.loginPage != null && portal.loginPage.activeSelf) { portal.ExitPortalToGoogle(); return; }
        }

        // Redan på Giggle-sökningen (roten) - inget mer att gå tillbaka till.
    }

    // Denna kopplar vi till din bakåt-pil (<) högst upp
    public void GoToSearchPage()
    {
        PlayClickSound();
        if (googleSidan != null) googleSidan.SetActive(true);
        if (drugSite != null) drugSite.CloseSite();

        // Stäng av alla universitetssidor om de råkar vara igång
        UniversityPortal portal = GetComponent<UniversityPortal>();
        if (portal != null)
        {
            if (portal.loginPage != null) portal.loginPage.SetActive(false);
            if (portal.dashboardPage != null) portal.dashboardPage.SetActive(false);
            if (portal.instructionsPage != null) portal.instructionsPage.SetActive(false);
            if (portal.examPage != null) portal.examPage.SetActive(false);
            if (portal.resultPage != null) portal.resultPage.SetActive(false);
        }

        // Återställ URL-tråden högst upp till Giggle
        if (urlBarText != null) urlBarText.text = "http://giggle.se";
    }

    // Kollar vad du skrivit när du trycker ENTER
    public void PerformSearch()
    {
        if (searchInputField == null) return;

        // Tar bort mellanslag och ignorerar stora/små bokstäver
        string typedText = searchInputField.text.Trim().ToLower();

        // KOLLA OM MAN SKREV IN STOCKHOLMS HÖGSKOLA
        if (typedText == "stockholmshogskola.se" || typedText == "http://stockholmshogskola.se")
        {
            Debug.Log("Öppnar Stockholm Högskola...");
            PlayClickSound();

            // Dölj vita Google-sidan
            if (googleSidan != null) googleSidan.SetActive(false);
            if (drugSite != null) drugSite.CloseSite();

            // Hämta portal-skriptet på samma objekt och starta inloggningen!
            UniversityPortal portal = GetComponent<UniversityPortal>();
            if (portal != null)
            {
                portal.OpenPortal();
            }
            return;
        }

        // KOLLA OM MAN SKREV IN DEALER-SIDAN
        if (typedText == "darkmarket.se" || typedText == "http://darkmarket.se")
        {
            Debug.Log("Öppnar Darkmarket...");
            PlayClickSound();

            if (googleSidan != null) googleSidan.SetActive(false);

            UniversityPortal sitePortal = GetComponent<UniversityPortal>();
            if (sitePortal != null)
            {
                if (sitePortal.loginPage != null) sitePortal.loginPage.SetActive(false);
                if (sitePortal.dashboardPage != null) sitePortal.dashboardPage.SetActive(false);
                if (sitePortal.instructionsPage != null) sitePortal.instructionsPage.SetActive(false);
                if (sitePortal.examPage != null) sitePortal.examPage.SetActive(false);
                if (sitePortal.resultPage != null) sitePortal.resultPage.SetActive(false);
            }

            if (drugSite != null)
            {
                drugSite.OpenSite();
                if (urlBarText != null) urlBarText.text = "http://darkmarket.se";
            }
            return;
        }

        // Om man skrev något annat (stavade fel)
        Debug.Log("Hittade inte sidan: " + typedText);
        if (audioSource != null && errorSound != null) audioSource.PlayOneShot(errorSound);

        // Töm fältet så man får försöka skriva om det
        searchInputField.text = "";
    }
}
