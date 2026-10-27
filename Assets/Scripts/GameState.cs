// Holds the player's state while switching scenes (apartment <-> city). Each scene has its own
// Player with its own PlayerStats/DayClock, so they read from here on load and SceneDoor writes
// here right before leaving. Static = survives the scene load; resets when the game restarts.
public static class GameState
{
    public static bool hasData;

    public static float money, hunger, thirst, craving, highLevel, drunkLevel;
    public static int day;
    public static float hour;

    public static string nextSpawnPoint;   // SpawnPoint.id to place the player at in the next scene

    // Joints you're carrying (pocketed with R in PlayerInteraction). Kept across scenes, unlike
    // whatever is in your hand.
    public static int pocketJoints;

    // Week stats for the end card (EndingScreen)
    public static int examsTaken, jointsSold;
    public static float moneyEarned;
    public static bool rentPaid;

    // Story
    public static bool metClassmate;      // Elias gave you the joint (lecture hall)
    public static bool hasDealerNumber;   // ...and Jonte's number

    // Fresh game (EndingScreen restart). hasData = false makes PlayerStats/DayClock use their own
    // starting values again.
    public static void ResetAll()
    {
        hasData = false;
        nextSpawnPoint = null;
        pocketJoints = 0;
        examsTaken = jointsSold = 0;
        moneyEarned = 0f;
        rentPaid = false;
        metClassmate = hasDealerNumber = false;
    }

    public static void Save()
    {
        PlayerStats stats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        if (stats != null)
        {
            money = stats.money;
            hunger = stats.hunger;
            thirst = stats.thirst;
            craving = stats.craving;
            highLevel = stats.highLevel;
            drunkLevel = stats.drunkLevel;
        }

        if (DayClock.Instance != null)
        {
            day = DayClock.Instance.Day;
            hour = DayClock.Instance.Hour;
        }

        hasData = true;
    }
}
