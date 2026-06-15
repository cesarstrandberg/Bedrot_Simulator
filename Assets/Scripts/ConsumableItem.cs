using UnityEngine;

public class ConsumableItem : MonoBehaviour
{
    [Header("Consumable Stats")]
    public float thirstRestore = 25f;
    public float hungerRestore = 0f;

    [Header("Effects")]
    public AudioClip consumeSound;
}
