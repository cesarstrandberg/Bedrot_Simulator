using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerStats playerStats;

    [Header("Text References")]
    public TextMeshProUGUI moneyText;

    [Header("Slider References (0-100)")]
    public Slider hungerSlider;
    public Slider thirstSlider;
    public Slider cravingSlider;
    public Slider highSlider;
    public Slider drunkSlider;

    void Update()
    {
        if (playerStats == null) return;

        // Uppdatera Pengar-texten
        if (moneyText != null)
        {
            moneyText.text = "MONEY: " + Mathf.RoundToInt(playerStats.money) + " KR";
        }

        // Uppdatera alla sliders direkt i tid
        if (hungerSlider != null) hungerSlider.value = playerStats.hunger;
        if (thirstSlider != null) thirstSlider.value = playerStats.thirst;
        if (cravingSlider != null) cravingSlider.value = playerStats.craving;

        // Eftersom High och Drunk går från 0 till 1 i koden, multiplicerar vi med 100 för mätaren!
        if (highSlider != null) highSlider.value = playerStats.highLevel * 100f;
        if (drunkSlider != null) drunkSlider.value = playerStats.drunkLevel * 100f;
    }
}
