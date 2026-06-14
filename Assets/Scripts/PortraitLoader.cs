using UnityEngine;

public class PortraitLoader : MonoBehaviour
{
    [Header("Скрипты здоровья игроков")]
    public BG3PortraitHealthBar p1HealthBar;
    public BG3PortraitHealthBar p2HealthBar;

    [Header("4 иконки (строго по порядку!)")]
    public Sprite[] characterSprites;

    void Start()
    {
        // Достаем выбор из памяти
        int p1Choice = PlayerPrefs.GetInt("P1_Character", 0);
        int p2Choice = PlayerPrefs.GetInt("P2_Character", 0);

        // Передаем картинку первому игроку
        if (p1HealthBar != null && p1Choice >= 0 && p1Choice < characterSprites.Length)
        {
            p1HealthBar.SetPortrait(characterSprites[p1Choice]);
        }

        // Передаем картинку второму игроку
        if (p2HealthBar != null && p2Choice >= 0 && p2Choice < characterSprites.Length)
        {
            p2HealthBar.SetPortrait(characterSprites[p2Choice]);
        }
    }
}