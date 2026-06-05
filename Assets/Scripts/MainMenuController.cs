using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Панели окон")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject exitConfirmationPanel; // Поле для окна выхода

    [Header("Элементы настроек")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    private void Start()
    {
        if (volumeSlider != null)
            volumeSlider.value = AudioListener.volume;

        if (muteToggle != null)
            muteToggle.isOn = !AudioListener.pause; 
    }

    // --- Переключение окон ---
    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsMenuPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // --- Окно подтверждения выхода ---
    public void OpenExitConfirmation()
    {
        mainMenuPanel.SetActive(false);
        exitConfirmationPanel.SetActive(true); // Включаем окно выхода
    }

    public void CloseExitConfirmation()
    {
        exitConfirmationPanel.SetActive(false); // Выключаем окно выхода
        mainMenuPanel.SetActive(true);
    }

    // Реальное закрытие игры
    public void ConfirmExit()
    {
        Debug.Log("Игра закрывается... (В билде это закроет приложение)"); 
        Application.Quit(); 
    }

    // --- Управление звуком ---
    public void ChangeVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void ToggleSound(bool soundOn)
    {
        AudioListener.pause = !soundOn;
    }
}