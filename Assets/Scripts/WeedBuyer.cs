using UnityEngine;

// A customer in the city. Look at him and press E (prompt shows) to sell a joint from your pocket
// (GameState.pocketJoints - press R while holding a joint to pocket it). Buys a limited number per
// day. If he's also a Pedestrian he stops walking while you're looking at him.
public class WeedBuyer : MonoBehaviour, IInteractable
{
    public float pricePerJoint = 150f;
    public int maxPerDay = 3;
    public AudioSource audioSource;
    public AudioClip saleSound;

    int soldToday;
    int lastDay = -1;
    Pedestrian walker;

    void Awake() { walker = GetComponent<Pedestrian>(); }

    void RefreshDay()
    {
        int today = DayClock.Instance != null ? DayClock.Instance.Day : 1;
        if (today != lastDay) { lastDay = today; soldToday = 0; }
    }

    public string PromptLabel
    {
        get
        {
            RefreshDay();
            if (soldToday >= maxPerDay) return "Talk";
            if (GameState.pocketJoints <= 0) return "Sell joint  <size=75%><color=#bbbbbb>(none in pocket)</color></size>";
            return $"Sell joint  <size=75%><color=#9be29b>{pricePerJoint:0} kr</color></size>";
        }
    }

    public bool PromptAvailable
    {
        get { RefreshDay(); return GameState.pocketJoints > 0 && soldToday < maxPerDay; }
    }

    public void OnLookedAt(Transform player)
    {
        if (walker != null) walker.Hold(player.position, 1.5f);
    }

    public void Interact() { TrySell(); }

    public void TrySell()
    {
        RefreshDay();

        if (soldToday >= maxPerDay)
        {
            Notify.Show("\"Nah I'm good, come back tomorrow.\"", 3f);
            return;
        }

        if (GameState.pocketJoints <= 0)
        {
            Notify.Show("\"You got anything for me?\"\n<size=70%>(No joints in your pocket - hold one and press R)</size>", 3.5f);
            return;
        }

        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        if (stats == null) return;

        GameState.pocketJoints--;
        soldToday++;
        stats.money += pricePerJoint;
        if (audioSource != null && saleSound != null) audioSource.PlayOneShot(saleSound);
        InteractPrompt.Float($"+{pricePerJoint:0} kr", new Color(0.55f, 0.95f, 0.55f));
        string[] lines = { "\"Nice. Pleasure doing business.\"", "\"Smells right. Cheers.\"", "\"You didn't see me.\"", "\"Same time tomorrow?\"" };
        Notify.Show(lines[Random.Range(0, lines.Length)] + $"\n<size=70%>{GameState.pocketJoints} left in your pocket</size>", 2.5f);
    }
}
