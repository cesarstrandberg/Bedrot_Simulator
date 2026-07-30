using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WebBrowser : MonoBehaviour
{
    [Header("Sidor inuti fönstret")]
    public GameObject googleSidan; // Din vita sök-sida

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

    // Denna kopplar vi till din bakåt-pil (<) högst upp
    public void GoToSearchPage()
    {
        PlayClickSound();
        if (googleSidan != null) googleSidan.SetActive(true);

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

            // Hämta portal-skriptet på samma objekt och starta inloggningen!
            UniversityPortal portal = GetComponent<UniversityPortal>();
            if (portal != null)
            {
                portal.OpenPortal();
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
