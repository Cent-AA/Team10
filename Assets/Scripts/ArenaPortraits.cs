using UnityEngine;
using UnityEngine.UI;

public class ArenaPortraits : MonoBehaviour
{
    [Header("UI Картинки на экране (Слева и Справа)")]
    public Image player1Portrait;
    public Image player2Portrait;

    [Header("Все возможные аватарки (строго по порядку!)")]
    public Sprite[] characterSprites;

    void Start()
    {
        // Читаем из памяти, кого выбрали. Если вдруг данных нет, ставим 0 (Хэви)
        int p1Choice = PlayerPrefs.GetInt("P1_Character", 0);
        int p2Choice = PlayerPrefs.GetInt("P2_Character", 0);

        // Меняем аватарку первому игроку
        if (p1Choice >= 0 && p1Choice < characterSprites.Length)
        {
            player1Portrait.sprite = characterSprites[p1Choice];
        }

        // Меняем аватарку второму игроку
        if (p2Choice >= 0 && p2Choice < characterSprites.Length)
        {
            player2Portrait.sprite = characterSprites[p2Choice];
        }
    }
}