using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class UniversityPortal : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerStats playerStats; // För CSN-utbetalning!

    [Header("Syntax Highlighting")]
    public TMP_Text syntaxText;

    [Header("UI Panels")]
    public GameObject loginPage;
    public GameObject dashboardPage;
    public GameObject instructionsPage;
    public GameObject examPage;
    public GameObject resultPage;
    public TextMeshProUGUI urlBarText; // URL-listen högst upp i browsern

    [Header("Login UI")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI loginErrorText;

    [Header("Exam UI")]
    public TextMeshProUGUI examTimerText;
    public TextMeshProUGUI targetCodeText;
    public TMP_InputField examInputField;
    public TextMeshProUGUI gradeDisplayText;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip keyPressSound;
    public AudioClip clickSound;
    public AudioClip successSound;
    public AudioClip failSound;

    private int selectedCourseIndex = 0; // 0 = Java 1, 1 = Java 2
    private string currentTargetCode = "";
    private float timeLimit = 240f; // 4 minuters stenhård press!
    private float currentTime;
    private bool isExamActive = false;
    private string lastInputText = "";

    // ==========================================
    // DE 3 TENTA-VERSIONERNA FÖR JAVA 1 (Grundläggande & Objekt)
    // ==========================================
    private string[] java1Exams = new string[]
     {
        "public class Student {\n\tprivate String name;\n\tprivate int credits;\n\n\tpublic Student(String name) {\n\t\tthis.name = name;\n\t\tthis.credits = 0;\n\t}\n\n\tpublic void addCredits(int hp) {\n\t\tthis.credits += hp;\n\t}\n}",
        "public class LoopTest {\n\tpublic static void main(String[] args) {\n\t\tint[] grades = {85, 72, 91, 45, 60};\n\t\tint sum = 0;\n\t\tfor (int i = 0; i < grades.length; i++) {\n\t\t\tsum += grades[i];\n\t\t}\n\t\tSystem.out.println(\"Average: \" + (sum / grades.length));\n\t}\n}",
        "import java.util.Scanner;\n\npublic class Authenticator {\n\tpublic static boolean checkLogin(String user, String pass) {\n\t\tif (user.equals(\"sven-ericsson123\") && pass.equals(\"abcdefg\")) {\n\t\t\treturn true;\n\t\t}\n\t\treturn false;\n\t}\n}"
     };

    // ==========================================
    // DE 3 TENTA-VERSIONERNA FÖR JAVA 2 (Svårare algoritmer & Arrayer)
    // ==========================================
    private string[] java2Exams = new string[]
    {
        "public class BubbleSort {\n\tpublic static void sort(int[] arr) {\n\t\tint n = arr.length;\n\t\tfor (int i = 0; i < n - 1; i++) {\n\t\t\tfor (int j = 0; j < n - i - 1; j++) {\n\t\t\t\tif (arr[j] > arr[j + 1]) {\n\t\t\t\t\tint temp = arr[j];\n\t\t\t\t\tarr[j] = arr[j + 1];\n\t\t\t\t\tarr[j + 1] = temp;\n\t\t\t\t}\n\t\t\t}\n\t\t}\n\t}\n}",
        "public class MatrixMath {\n\tpublic static int[][] multiply(int[][] a, int[][] b) {\n\t\tint rows = a.length;\n\t\tint cols = b[0].length;\n\t\tint[][] result = new int[rows][cols];\n\t\tfor (int i = 0; i < rows; i++) {\n\t\t\tfor (int j = 0; j < cols; j++) {\n\t\t\t\tresult[i][j] = a[i][j] * b[i][j];\n\t\t\t}\n\t\t}\n\t\treturn result;\n\t}\n}",
        "public class Node {\n\tint data;\n\tNode next;\n\n\tpublic Node(int d) {\n\t\tdata = d;\n\t\tnext = null;\n\t}\n\n\tpublic void appendToTail(int d) {\n\t\tNode end = new Node(d);\n\t\tNode n = this;\n\t\twhile (n.next != null) {\n\t\t\tn = n.next;\n\t\t}\n\t\tn.next = end;\n\t}\n}"
    };

    void Start()
    {
        if (examInputField != null)
        {
            examInputField.onValueChanged.AddListener(OnTyping);
        }

        if(usernameInput != null)
        {
            usernameInput.onValueChanged.AddListener(OnTyping);
        }
        if(passwordInput != null)
        {
            passwordInput.onValueChanged.AddListener(OnTyping);
        }
    }

    void Update()
    {
        if (!isExamActive) return;

        currentTime -= Time.deltaTime;
        if (examTimerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60F);
            int seconds = Mathf.FloorToInt(currentTime - minutes * 60);
            examTimerText.text = string.Format("Tid kvar: {0:00}:{1:00}", minutes, seconds);
        }

        if (currentTime <= 0)
        {
            SubmitExam(); // Tiden ute -> Tvinga inlämning!
        }
    }

    public void PlayClick()
    {
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);
    }

    // ==========================================
    // 1. INLOGGNING (Koppla till Logga In-knappen)
    // ==========================================
    public void TryLogin()
    {
        PlayClick();
        string u = usernameInput.text.Trim();
        string p = passwordInput.text.Trim();

        if (u == "sven-ericsson123" && p == "abcdefg")
        {
            Debug.Log("Inloggning lyckades!");
            loginPage.SetActive(false);
            dashboardPage.SetActive(true);
            if (urlBarText != null) urlBarText.text = "http://stockholmshogskola.se/portal/dashboard";
            if (loginErrorText != null) loginErrorText.text = "";
        }
        else
        {
            if (loginErrorText != null) loginErrorText.text = "Fel användarnamn eller lösenord! Kolla post-it lappen.";
            if (audioSource != null && failSound != null) audioSource.PlayOneShot(failSound);
        }
    }

    // ==========================================
    // 2. VÄLJ TENTA (Koppla till Exam1 och Exam2 knapparna)
    // ==========================================
    public void SelectJava1() { SelectCourse(0); }
    public void SelectJava2() { SelectCourse(1); }

    void SelectCourse(int index)
    {
        PlayClick();
        selectedCourseIndex = index;
        dashboardPage.SetActive(false);
        instructionsPage.SetActive(true);
        if (urlBarText != null) urlBarText.text = "http://stockholmshogskola.se/portal/exam-instructions";
    }

    // ==========================================
    // 3. STARTA TENTAN (Koppla till Starta Tenta-knappen)
    // ==========================================
    public void StartExam()
    {
        PlayClick();
        instructionsPage.SetActive(false);
        examPage.SetActive(true);
        if (urlBarText != null) urlBarText.text = "http://stockholmshogskola.se/portal/active-exam";

        // Slumpa 1 av 3 tunga koder baserat på kursen
        if (selectedCourseIndex == 0)
            currentTargetCode = java1Exams[Random.Range(0, java1Exams.Length)];
        else
            currentTargetCode = java2Exams[Random.Range(0, java2Exams.Length)];

        if (targetCodeText != null) targetCodeText.text = ApplySyntaxHighlighting(currentTargetCode);

        currentTime = timeLimit;
        isExamActive = true;

        if (examInputField != null)
        {
            examInputField.text = "";
            examInputField.interactable = true;
            examInputField.Select();
            examInputField.ActivateInputField();
        }
    }

    void OnTyping(string currentText)
    {
        //if (!isExamActive) return;
        if (currentText.Length != lastInputText.Length)
        {
            if (audioSource != null && keyPressSound != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(keyPressSound);
            }
        }
        lastInputText = currentText;

        if(syntaxText != null)
        {
            syntaxText.text = ApplySyntaxHighlighting(currentText);
        }
    }

    
    // ==========================================
    // 4. LÄMNA IN & BETYGSÄTTNING (A-U & CSN)
    // ==========================================
    public void SubmitExam()
    {
        PlayClick();
        isExamActive = false;
        examPage.SetActive(false);
        resultPage.SetActive(true);
        if (urlBarText != null) urlBarText.text = "http://stockholmshogskola.se/portal/result";

        // Vi rensar bort osynliga tecken (som \r och Unitys noll-bredd-mellanslag) för att vara rättvisa
        string typed = examInputField.text.Trim().Replace("\r", "").Replace("\u200B", "");
        string target = currentTargetCode.Trim().Replace("\r", "");

        // Använd vår nya, supersmarta algoritm för att få fram den sanna procenten!
        int accInt = CalculateAccuracy(typed, target);

        // Betygsskala A-U och pengar
        string grade = "U";
        int csnMoney = 0;
        string colorHex = "#FF0000"; // Röd för underkänd

        if (accInt >= 85) { grade = "A"; csnMoney = 4000; colorHex = "#00FF66"; }
        else if (accInt >= 75) { grade = "B"; csnMoney = 3500; colorHex = "#00FF66"; }
        else if (accInt >= 65) { grade = "C"; csnMoney = 3000; colorHex = "#FFD700"; }
        else if (accInt >= 55) { grade = "D"; csnMoney = 2500; colorHex = "#FFD700"; }
        else if (accInt >= 50) { grade = "E"; csnMoney = 2000; colorHex = "#FF9900"; }
        else { grade = "U"; csnMoney = 0; colorHex = "#FF0000"; }

        // Ge pengar till spelaren om godkänd!
        if (csnMoney > 0 && playerStats != null)
        {
            playerStats.money += csnMoney;
            if (audioSource != null && successSound != null) audioSource.PlayOneShot(successSound);
        }
        else
        {
            if (audioSource != null && failSound != null) audioSource.PlayOneShot(failSound);
        }

        // Visa stenhårt resultat på skärmen!
        if (gradeDisplayText != null)
        {
            gradeDisplayText.text = "<size=32><b>RESULTAT AV TENTAMEN</b></size>\n\n" +
                                    "Kurs: Java Programmering " + (selectedCourseIndex + 1) + "\n" +
                                    "Precision: " + accInt + "%\n\n" +
                                    "BETYG: <color=" + colorHex + "><size=48><b>" + grade + "</b></size></color>\n\n" +
                                    "CSN Utbetalt: <color=#00FF66><b>+" + csnMoney + " KR</b></color>\n" +
                                    "<size=16>Pengarna har satts in på ditt studentkonto.</size>";
        }
    }

    // ==========================================
    // LEVENSHTEIN-ALGORITM (Fixar förskjutnings-problemet!)
    // ==========================================
    int CalculateAccuracy(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return 0;
        if (string.IsNullOrEmpty(target)) return 0;

        int n = source.Length;
        int m = target.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(
                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        int distance = d[n, m];
        int maxLength = Mathf.Max(n, m);

        // Räkna om skillnaden (distance) till en snygg procent!
        float percentage = ((float)(maxLength - distance) / maxLength) * 100f;
        return Mathf.Clamp(Mathf.RoundToInt(percentage), 0, 100);
    }

    // ==========================================
    // 5. TILLBAKA TILL PORTALEN
    // ==========================================

    // Tillbaka: Instruktioner -> Dashboard (ett steg bakåt)
    public void BackFromInstructions()
    {
        PlayClick();
        instructionsPage.SetActive(false);
        dashboardPage.SetActive(true);
        if (urlBarText != null) urlBarText.text = "http://stockholmshogskola.se/portal/dashboard";
    }

    // Tillbaka: Aktiv tenta -> Instruktioner (ett steg bakåt, avbryter tentan utan betygsättning)
    public void BackFromExam()
    {
        PlayClick();
        isExamActive = false;
        examPage.SetActive(false);
        instructionsPage.SetActive(true);
        if (urlBarText != null) urlBarText.text = "http://stockholmshogskola.se/portal/exam-instructions";
    }

    public void ReturnToDashboard()
    {
        PlayClick();
        resultPage.SetActive(false);
        dashboardPage.SetActive(true);
        if (urlBarText != null) urlBarText.text = "http://stockholmshogskola.se/portal/dashboard";
    }

    // Anropas från WebBrowser för att öppna portalen
    public void OpenPortal()
    {
        loginPage.SetActive(true);
        dashboardPage.SetActive(false);
        instructionsPage.SetActive(false);
        examPage.SetActive(false);
        resultPage.SetActive(false);
        if (urlBarText != null) urlBarText.text = "http://stockholmshogskola.se/login";
    }

    public void ExitPortalToGoogle()
    {
        PlayClick();

        // Stäng av alla högskolesidor
        loginPage.SetActive(false);
        dashboardPage.SetActive(false);
        instructionsPage.SetActive(false);
        examPage.SetActive(false);
        resultPage.SetActive(false);

        // Prata med webbläsaren och säg åt den att visa Google igen!
        WebBrowser browser = GetComponent<WebBrowser>();
        if (browser != null)
        {
            browser.GoToSearchPage();
        }
    }

    // ==========================================
    // 7. LOGGA UT (Från Dashboard tillbaka till Login)
    // ==========================================
    public void LogOutToLogin()
    {
        PlayClick();
        dashboardPage.SetActive(false);
        loginPage.SetActive(true);
        if (urlBarText != null) urlBarText.text = "http://stockholmshogskola.se/login";
    }

    string ApplySyntaxHighlighting(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return "";

        string coloredText = rawText;

        // Färgkoder för LJUS bakgrund (VS Code Light / Eclipse)
        string keywordColor = "#0000FF"; // Mörkblå
        string classColor = "#2B91AF";   // Mörk turkos
        string stringColor = "#A31515";  // Mörkröd
        string numberColor = "#098658";  // Mörkgrön (skarp och tydlig på vit bakgrund)

        // 1. Färglägg allt som står inom citationstecken (Strings)
        coloredText = Regex.Replace(coloredText, "(\".*?\")", $"<color={stringColor}>$1</color>");

        // 2. Färglägg siffror
        coloredText = Regex.Replace(coloredText, @"\b(\d+)\b", $"<color={numberColor}>$1</color>");

        // 3. Färglägg Java-nyckelord (Blå)
        string[] keywords = { "public", "private", "class", "static", "void", "int", "boolean", "if", "else", "return", "import", "new", "true", "false", "for", "while" };
        foreach (string kw in keywords)
        {
            coloredText = Regex.Replace(coloredText, $@"\b({kw})\b", $"<color={keywordColor}>$1</color>");
        }

        // 4. Färglägg Java-klasser och system-ord (Turkos)
        string[] classes = { "String", "System", "Scanner", "Node", "Math", "out" };
        foreach (string cls in classes)
        {
            coloredText = Regex.Replace(coloredText, $@"\b({cls})\b", $"<color={classColor}>$1</color>");
        }

        return coloredText;
    }

}