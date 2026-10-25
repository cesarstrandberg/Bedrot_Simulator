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
