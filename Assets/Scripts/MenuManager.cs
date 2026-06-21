using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Панели меню")]
    // Ссылка на будущую панель титров
    public GameObject creditsMenuPanel; 

    // Этот метод мы вызовем при нажатии на кнопку CREDITS
    public void OpenCredits()
    {
        if (creditsMenuPanel != null)
        {
            creditsMenuPanel.SetActive(true); // Включаем панель
        }
    }

    // Этот метод мы вызовем при нажатии на кнопку BACK (Назад)
    public void CloseCredits()
    {
        if (creditsMenuPanel != null)
        {
            creditsMenuPanel.SetActive(false); // Выключаем панель
        }
    }
}